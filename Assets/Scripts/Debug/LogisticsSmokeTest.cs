/***************************************************************************
// File       : LogisticsSmokeTest.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Description: [TODO] 居民搬运资源双向冒烟测试（生产->仓库 以及 仓库->生产）
// ***************************************************************************/

using UnityEngine;

public class LogisticsSmokeTest : MonoBehaviour
{
    [Header("场景引用")]
    public Resident testResident;

    [Tooltip("生产建筑 Transform（例如 ProductionBuilding 挂载的物体）")]
    public Transform producerTransform;

    [Tooltip("生产建筑组件（需实现 IProducer / IConsumer 的脚本，例如 ProductionBuilding）")]
    public MonoBehaviour productionComponent;

    [Tooltip("仓库 Transform（例如 WarehouseBuilding 挂载的物体）")]
    public Transform warehouseTransform;

    [Tooltip("仓库组件（需实现 IStorage 的脚本，例如 WarehouseBuilding）")]
    public MonoBehaviour warehouseComponent;

    [Header("资源与数量")]
    public ResourceType haulType = ResourceType.Wood;
    public int amount = 5;

    [Header("测试开关")]
    [Tooltip("测试：生产 -> 仓库（拉成品送仓库）")]
    public bool testProductionToWarehouse = true;

    [Tooltip("测试：仓库 -> 生产（从仓库送原料给生产）")]
    public bool testWarehouseToProduction = true;

    private TaskManager _tm;

    private void Start()
    {
        // 准备 TaskManager（若场景没有则自动创建一个）
        _tm = FindObjectOfType<TaskManager>();
        if (_tm == null)
        {
            GameObject go = new GameObject("TaskManager");
            _tm = go.AddComponent<TaskManager>();
        }

        // 基础校验
        if (testResident == null || producerTransform == null || warehouseTransform == null)
        {
            Debug.LogError("[LogisticsSmokeTest] 引用未设置：Resident/ProducerTransform/WarehouseTransform");
            return;
        }
        if (productionComponent == null || warehouseComponent == null)
        {
            Debug.LogError("[LogisticsSmokeTest] 组件未设置：productionComponent / warehouseComponent");
            return;
        }

        // 接口识别
        IProducer producerIf = productionComponent as IProducer;
        IConsumer consumerIf = productionComponent as IConsumer; // 同一组件可同时实现 IProducer/IConsumer
        IStorage warehouseIf = warehouseComponent as IStorage;

        if (warehouseIf == null)
        {
            Debug.LogError("[LogisticsSmokeTest] 仓库组件未实现 IStorage 接口。");
            return;
        }
        // 派任务链：生产 -> 仓库（需要生产组件实现 IProducer）
        if (testProductionToWarehouse)
        {
            if (producerIf == null)
            {
                TLog.Warning("[LogisticsSmokeTest] 生产->仓库测试已跳过：生产组件未实现 IProducer。");
            }
            else
            {
                _tm.Enqueue(TaskSequence.Create(
                    MoveToTask.Create(producerTransform.position),
                    // 不足则尽量多拿（>=1 即拿）
                    PickupTask.Create(productionComponent, haulType, amount, PickupPolicy.TakeAllAvailable),
                    MoveToTask.Create(warehouseTransform.position),
                    // 说明：若上一步没拿满，默认 DeliverTask 会按固定 amount 扣背包，可能失败
                    // 见下方“可选改进：DeliverPolicy”
                    DeliverTask.Create(warehouseComponent, haulType, amount)
                ));
            }
        }

        // 派任务链：仓库 -> 生产（需要生产组件实现 IConsumer）
        if (testWarehouseToProduction)
        {
            if (consumerIf == null)
            {
                TLog.Warning("[LogisticsSmokeTest] 仓库->生产测试已跳过：生产组件未实现 IConsumer。");
            }
            else
            {
                _tm.Enqueue(TaskSequence.Create(
                    MoveToTask.Create(warehouseTransform.position),
                    // 投原料建议 Exact（必须凑够再拿）或 WaitUntilAvailable（等待凑够）
                    // 举例：等待每 1s 重试，最多等 20s
                    PickupTask.Create(warehouseComponent, haulType, amount, PickupPolicy.WaitUntilAvailable, 1.0f, 20.0f),
                    MoveToTask.Create(producerTransform.position),
                    DeliverTask.Create(productionComponent, haulType, amount)
                ));
            }
        }


        if (!testProductionToWarehouse && !testWarehouseToProduction)
        {
            Debug.LogWarning("[LogisticsSmokeTest] 两个测试开关均为 false，本脚本不派发任何任务。");
        }
    }
}
