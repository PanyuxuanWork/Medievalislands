/***************************************************************************
// File       : LogisticsSmokeTest.cs
// Author     : Panyuxuan
// Updated    : 2025/08/13
// Description: [TODO] 居民物流冒烟测试（支持逐单位多仓取/存）
//              - 生产→仓库：先在生产点取货（尽量多拿），再逐单位存入多个仓库直到清空。
//              - 仓库→生产：逐单位从多个仓库取货直到背包达标，然后一次性投给生产建筑。
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

    [Tooltip("任意一个仓库 Transform（仅用于起步时靠近，实际会自动切换多仓）")]
    public Transform warehouseTransform;

    [Tooltip("任意一个仓库组件（需实现 IStorage，例如 WarehouseBuilding）")]
    public MonoBehaviour warehouseComponent;

    [Header("资源与数量")]
    public ResourceType haulType = ResourceType.Wood;

    [Tooltip("每次测试的目标数量（生产→仓库：作为单趟在生产点的期望获取量；仓库→生产：背包达标阈值）")]
    public int amount = 5;

    [Header("逐单位操作参数")]
    [Tooltip("逐单位操作间隔（秒）")]
    public float perUnitInterval = 0.15f;

    [Tooltip("单条连续任务的时间预算（秒）")]
    public float timeBudgetSec = 20f;

    [Header("测试开关")]
    [Tooltip("测试：生产 -> 仓库（拉成品送入多个仓库，逐单位存放直到背包清空）")]
    public bool testProductionToWarehouse = true;

    [Tooltip("测试：仓库 -> 生产（从多个仓库逐单位取，直到背包达标；然后一次性投给生产）")]
    public bool testWarehouseToProduction = true;

    private TaskManager _tm;

    private void Start()
    {
        // TaskManager
        _tm = FindObjectOfType<TaskManager>();
        if (_tm == null)
        {
            GameObject go = new GameObject("TaskManager");
            _tm = go.AddComponent<TaskManager>();
            TLog.Log("[LogisticsSmokeTest] 场景缺少 TaskManager，已自动创建。", LogColor.Grey);
        }

        // 基础校验
        if (testResident == null || producerTransform == null || warehouseTransform == null)
        {
            TLog.Error("[LogisticsSmokeTest] 引用未设置：Resident/ProducerTransform/WarehouseTransform");
            return;
        }
        if (productionComponent == null || warehouseComponent == null)
        {
            TLog.Error("[LogisticsSmokeTest] 组件未设置：productionComponent / warehouseComponent");
            return;
        }

        // 接口识别
        IProducer producerIf = productionComponent as IProducer;
        IConsumer consumerIf = productionComponent as IConsumer; // 同一组件可同时实现 IProducer/IConsumer
        IStorage warehouseIf = warehouseComponent as IStorage;

        if (warehouseIf == null)
        {
            TLog.Error("[LogisticsSmokeTest] 仓库组件未实现 IStorage 接口。");
            return;
        }

        // ========== 测试链：生产 -> 仓库 ==========
        if (testProductionToWarehouse)
        {
            if (producerIf == null)
            {
                TLog.Warning("[LogisticsSmokeTest] 生产->仓库测试已跳过：生产组件未实现 IProducer。");
            }
            else
            {
                // 步骤：
                // 1) 去生产点
                // 2) 在生产点尽量拿（不足则拿可得量）
                // 3) 去任意仓库起步
                // 4) 逐单位向多个仓库存放，直到背包清空或超时
                _tm.Enqueue(TaskSequence.Create(
                    MoveToTask.Create(producerTransform.position),
                    PickupTask.Create(productionComponent, haulType, amount, PickupPolicy.TakeAllAvailable),
                    MoveToTask.Create(warehouseTransform.position),
                    ContinuousDeliverWarehousesTask.Create(haulType, perUnitInterval, timeBudgetSec)
                ));
                TLog.Log("[LogisticsSmokeTest] 已派发【生产->仓库】链：TakeAll + 连续逐单位存放。", LogColor.Cyan);
            }
        }

        // ========== 测试链：仓库 -> 生产 ==========
        if (testWarehouseToProduction)
        {
            if (consumerIf == null)
            {
                TLog.Warning("[LogisticsSmokeTest] 仓库->生产测试已跳过：生产组件未实现 IConsumer。");
            }
            else
            {
                // 步骤：
                // 1) 去任意仓库起步
                // 2) 逐单位从多个仓库取货，直到背包达到 amount 或超时
                // 3) 去生产点
                // 4) 一次性按 Exact 投给生产（若担心不足可改成 DeliverCarried，但对投料通常希望 Exact）
                _tm.Enqueue(TaskSequence.Create(
                    MoveToTask.Create(warehouseTransform.position),
                    ContinuousPickupWarehousesTask.Create(haulType, carryGoal: Mathf.Max(1, amount), perUnitInterval: perUnitInterval, timeBudgetSec: timeBudgetSec),
                    MoveToTask.Create(producerTransform.position),
                    DeliverTask.Create(productionComponent, haulType, amount, 0, DeliverPolicy.Exact)
                ));
                TLog.Log("[LogisticsSmokeTest] 已派发【仓库->生产】链：连续逐单位取 + Exact 投料。", LogColor.Cyan);
            }
        }

        if (!testProductionToWarehouse && !testWarehouseToProduction)
        {
            TLog.Warning("[LogisticsSmokeTest] 两个测试开关均为 false，本脚本不派发任何任务。");
        }
    }
}
