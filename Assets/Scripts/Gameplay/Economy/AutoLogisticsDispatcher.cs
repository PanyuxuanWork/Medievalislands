/***************************************************************************
// File       : AutoLogisticsDispatcher.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 自动物流派单器（任务制）：同时支持 IStorage / IProducer / IConsumer
// Tip        : 用 TaskManager + MoveTo/Pickup/DeliverTask 实现从生产到仓库、或仓库到生产。
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public class AutoLogisticsDispatcher : MonoBehaviour
{
    [Header("开关")]
    public bool enableDispatch = true;              // 总开关
    public bool pushFromProduction = true;          // 生产 -> 仓库
    public bool pullToProduction = true;            // 仓库 -> 生产（投原料）

    [Header("资源与批量")]
    public ResourceType haulType = ResourceType.Wood;
    [Tooltip("每次派送的搬运数量")]
    public int batchAmount = 5;

    [Header("节流")]
    [Tooltip("每个 OnTick 最多派出多少条任务链")]
    public int chainsPerTick = 1;
    [Tooltip("同一个生产建筑取货失败后的冷却（秒），避免持续空跑")]
    public float producerFailCooldown = 3f;

    private CityContext _city;
    private TaskManager _tm;

    // 记录生产点最近一次“取货失败”的时间（避免反复派单空跑）
    private readonly Dictionary<ProductionBuilding, float> _producerFailUntil = new Dictionary<ProductionBuilding, float>();

    private void Start()
    {
        _city = FindObjectOfType<CityContext>();
        _tm = FindObjectOfType<TaskManager>();
        TickSystem.OnTick += OnTick;
    }

    private void OnDestroy()
    {
        TickSystem.OnTick -= OnTick;
    }

    private void OnTick()
    {
        if (!enableDispatch || _city == null || _tm == null) return;

        int budget = chainsPerTick;

        // 1) 生产 -> 仓库
        if (pushFromProduction && budget > 0)
        {
            for (int i = 0; i < _city.productions.Count && budget > 0; i++)
            {
                ProductionBuilding pb = _city.productions[i];
                if (pb == null || pb.state != BuildingState.Active) continue;

                // 冷却判断：若上次失败，在冷却期内则跳过
                if (_producerFailUntil.ContainsKey(pb))
                {
                    float until = _producerFailUntil[pb];
                    if (Time.time < until) continue;
                }

                WarehouseBuilding wh = FindNearestWarehouse(pb.transform.position);
                if (wh == null) continue;

                // 不预判是否有货；PickupTask 会在现场用 IProducer.TryCollect 判定
                EnqueueChain_ProdToWh(pb, wh, haulType, batchAmount);
                budget--;
            }
        }

        // 2) 仓库 -> 生产（给原料）
        if (pullToProduction && budget > 0)
        {
            // 简单策略：按距离找到最近生产，找最近仓库；不做预估需求，只由 IConsumer.CanAccept 判定
            for (int i = 0; i < _city.productions.Count && budget > 0; i++)
            {
                ProductionBuilding pb = _city.productions[i];
                if (pb == null || pb.state != BuildingState.Active) continue;

                WarehouseBuilding wh = FindNearestWarehouse(pb.transform.position);
                if (wh == null) continue;

                // 这里可选做一个“仓库库存检查”，但 IStorage.Get 在不同实现可能不可用
                // 若你希望避免空跑，可在这里： if (wh.Get(haulType) < batchAmount) continue;

                EnqueueChain_WhToProd(wh, pb, haulType, batchAmount);
                budget--;
            }
        }
    }

    // === 任务链模板：生产 -> 仓库 ===
    private void EnqueueChain_ProdToWh(ProductionBuilding pb, WarehouseBuilding wh, ResourceType type, int amount)
    {
        // 1) 去生产点
        _tm.Enqueue(MoveToTask.Create(pb.transform.position));
        // 2) 从 IProducer 取货（支持 IProducer）
        _tm.Enqueue(PickupTask.Create(pb, type, amount));
        // 3) 去仓库
        _tm.Enqueue(MoveToTask.Create(wh.transform.position));
        // 4) 投递到 IStorage
        _tm.Enqueue(DeliverTask.Create(wh, type, amount));
    }

    // === 任务链模板：仓库 -> 生产 ===
    private void EnqueueChain_WhToProd(WarehouseBuilding wh, ProductionBuilding pb, ResourceType type, int amount)
    {
        // 1) 去仓库
        _tm.Enqueue(MoveToTask.Create(wh.transform.position));
        // 2) 从 IStorage 取货
        _tm.Enqueue(PickupTask.Create(wh, type, amount));
        // 3) 去生产点
        _tm.Enqueue(MoveToTask.Create(pb.transform.position));
        // 4) 投递到 IConsumer
        _tm.Enqueue(DeliverTask.Create(pb, type, amount));
    }

    private WarehouseBuilding FindNearestWarehouse(Vector3 pos)
    {
        WarehouseBuilding best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < _city.warehouses.Count; i++)
        {
            WarehouseBuilding w = _city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            float d = (w.transform.position - pos).sqrMagnitude;
            if (d < bestD) { bestD = d; best = w; }
        }
        return best;
    }

    // ——— 可选：在 PickupTask 失败处回调这里做冷却（若你愿意把回调打通的话） ———
    // 你也可以在 PickupTask 失败后调用：
    // AutoLogisticsDispatcher.NotifyProducerPickupFailed(pb, Time.time + producerFailCooldown);
    public void NotifyProducerPickupFailed(ProductionBuilding pb, float untilTime)
    {
        if (pb == null) return;
        _producerFailUntil[pb] = untilTime;
    }
}
