/***************************************************************************
// File       : ProductionCloseLoop.cs
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Created    : 2025/08/15
// Description: 最小可用“生产线闭环”组件
//              - 自动为输入缺口下单（拉式补给，走 LogisticsRequestDispatcher）
//              - 自动把产出从生产点推到有空间的仓库（Move→Pickup(IProducer)→Move→Deliver(IStorage)）
//              - 多仓兜底：按距离找最近有空间的仓库；找不到则等待下次巡检
//              - 带节流与冷却，避免刷任务
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ProductionClosedLoop : MonoBehaviour
{
    [Header("基础")]
    public ProductionBuilding production;     // 若留空将从本物体上自动获取
    public CityContext city;                  // 若留空自动 FindObjectOfType
    public TaskManager taskManager;           // 若留空自动 FindObjectOfType
    public LogisticsRequestDispatcher dispatcher; // 若留空自动 FindObjectOfType

    [Header("输入(原料) - 拉式补给")]
    [Tooltip("输入库存低于该值时触发补货下单（针对每种配方输入）。")]
    public int inputTarget = 6;
    [Tooltip("一次下单的最小批量（不足则等下一轮或被拆单）。")]
    public int inputMinBatch = 3;
    [Tooltip("输入巡检间隔（秒）。")]
    public float inputCheckInterval = 1.5f;
    [Tooltip("同一资源下单冷却（秒），避免频繁入队。")]
    public float inputCooldown = 4f;

    [Header("输出(成品) - 推式清空")]
    [Tooltip("当产出库存达到该阈值时，触发一次推送到仓库。")]
    public int outputBatchThreshold = 5;
    [Tooltip("每次尝试推送的目标批量（会按携带量实际投递）。")]
    public int outputDispatchAmount = 5;
    [Tooltip("输出巡检间隔（秒）。")]
    public float outputCheckInterval = 1.0f;
    [Tooltip("同一资源推送冷却（秒），避免在仓库满时反复派单。")]
    public float outputCooldown = 4f;

    [Header("移动/健壮性")]
    [Tooltip("若无法找到可接收仓库，将等待下一次巡检。")]
    public bool waitIfNoWarehouse = true;

    // 运行态
    private float _nextInputCheck;
    private float _nextOutputCheck;
    private readonly Dictionary<ResourceType, float> _inputNextAllowed = new Dictionary<ResourceType, float>();
    private readonly Dictionary<ResourceType, float> _outputNextAllowed = new Dictionary<ResourceType, float>();

    void Awake()
    {
        if (production == null) production = GetComponent<ProductionBuilding>();
        if (city == null) city = FindObjectOfType<CityContext>();
        if (taskManager == null) taskManager = FindObjectOfType<TaskManager>();
        if (dispatcher == null) dispatcher = FindObjectOfType<LogisticsRequestDispatcher>();
    }

    void Start()
    {
        if (production == null)
        {
            TLog.Error(this, "ProductionClosedLoop 需要挂在含 ProductionBuilding 的物体上。");
            enabled = false; return;
        }
        if (city == null || taskManager == null)
        {
            TLog.Error(this, "缺少 CityContext 或 TaskManager。");
            enabled = false; return;
        }

        _nextInputCheck = Time.time + Random.Range(0f, 0.5f);
        _nextOutputCheck = Time.time + Random.Range(0f, 0.5f);
        TLog.Log(this, "ProductionClosedLoop 已启动。", LogColor.Cyan);
    }

    void Update()
    {
        if (Time.time >= _nextInputCheck)
        {
            _nextInputCheck = Time.time + inputCheckInterval;
            TryAutoReorderInputs();
        }

        if (Time.time >= _nextOutputCheck)
        {
            _nextOutputCheck = Time.time + outputCheckInterval;
            TryAutoDispatchOutputs();
        }
    }

    // ---------- 输入：自动补货（拉式） ----------
    private void TryAutoReorderInputs()
    {
        if (dispatcher == null || production.recipe == null || production.inputInv == null) return;

        for (int i = 0; i < production.recipe.inputs.Length; i++)
        {
            ResourceType rt = production.recipe.inputs[i].type;
            int have = production.inputInv.Get(rt);

            if (have >= inputTarget) continue;

            float allowedAt;
            if (_inputNextAllowed.TryGetValue(rt, out allowedAt) && Time.time < allowedAt) continue;

            int need = inputTarget - have;
            int minBatch = Mathf.Max(1, inputMinBatch);

            // 入队请求（我们之前的 Request/Dispatcher）
            LogisticsRequest req = new LogisticsRequest
            {
                requester = production as IConsumer,
                type = rt,
                quantity = need,
                minBatch = minBatch,
                priority = 1
            };

            dispatcher.Enqueue(req);
            _inputNextAllowed[rt] = Time.time + inputCooldown;

            TLog.Log(this, $"[闭环·补货] 下单 {rt} 需 {need} (minBatch={minBatch})", LogColor.Cyan);
        }
    }

    // ---------- 输出：自动推送（推式） ----------
    private void TryAutoDispatchOutputs()
    {
        if (production.recipe == null || production.outputInv == null) return;

        for (int i = 0; i < production.recipe.outputs.Length; i++)
        {
            ResourceType rt = production.recipe.outputs[i].type;
            int have = production.outputInv.Get(rt);

            if (have < outputBatchThreshold) continue;

            float allowedAt;
            if (_outputNextAllowed.TryGetValue(rt, out allowedAt) && Time.time < allowedAt) continue;

            // 找可接收该资源的最近仓库
            IStorage target = FindNearestWarehouseWithSpace(rt);
            if (target == null)
            {
                if (!waitIfNoWarehouse)
                {
                    TLog.Warning(this, $"[闭环·成品] 未找到可接收 {rt} 的仓库。");
                }
                _outputNextAllowed[rt] = Time.time + outputCooldown;
                continue;
            }

            // 组织一条搬运链：去生产 → 从 IProducer 取 → 去仓库 → 投递（按携带量）
            MonoBehaviour targetMb = target as MonoBehaviour;
            if (targetMb == null)
            {
                _outputNextAllowed[rt] = Time.time + outputCooldown;
                continue;
            }

            int amount = Mathf.Clamp(outputDispatchAmount, 1, have);

            // 替换 TryAutoDispatchOutputs 内派链的地方：
            if (dispatcher != null)
            {
                LogisticsRequest req = new LogisticsRequest
                {
                    kind = RequestKind.PushOutput,
                    type = rt,
                    producer = production as IProducer,
                    quantity = amount,                // 目标搬运量
                    minBatch = Mathf.Max(1, amount),  // 本次就按 amount 为最小批量
                    priority = 0
                };
                dispatcher.Enqueue(req);
                _outputNextAllowed[rt] = Time.time + outputCooldown;
                TLog.Log(this, $"[闭环·成品] 入队推送 {rt} x{amount}", LogColor.Yellow);
            }
            else
            {
                // 若未挂调度器，则保持旧逻辑（直接派链）
                taskManager.Enqueue(TaskSequence.Create(
                    MoveToTask.Create(production.transform.position),
                    PickupTask.Create(production, rt, amount, PickupPolicy.TakeAllAvailable),
                    MoveToTask.Create(targetMb.transform.position),
                    DeliverTask.Create(target, rt, amount, 0, DeliverPolicy.DeliverCarried)
                ));
                _outputNextAllowed[rt] = Time.time + outputCooldown;
            }

            TLog.Log(this, $"[闭环·成品] 派发 {rt} 目标≈{amount} → 仓库[{targetMb.name}]（按携带量投递）", LogColor.Yellow);
        }
    }

    // ---------- 选择最近且可接收的仓库 ----------
    private IStorage FindNearestWarehouseWithSpace(ResourceType type)
    {
        if (city == null || city.warehouses == null || city.warehouses.Count == 0) return null;

        IStorage best = null;
        float bestD2 = float.MaxValue;
        Vector3 from = production.transform.position;

        for (int i = 0; i < city.warehouses.Count; i++)
        {
            WarehouseBuilding w = city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            IStorage s = w as IStorage;
            if (s == null) continue;

            // 是否还能接收该资源（有的实现有 Capacity；若无，默认可接收）
            bool canRecv = true;
            try { canRecv = (w.Get(type) < w.Capacity); } catch { canRecv = true; }

            if (!canRecv) continue;

            float d2 = (w.transform.position - from).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = s; }
        }

        return best;
    }
}
