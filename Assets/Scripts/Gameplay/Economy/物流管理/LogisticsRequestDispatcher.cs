/***************************************************************************
// File       : LogisticsRequestDispatcher.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 需求队列 -> 选择供给 -> 软预留 -> 派发一条搬运链
// Description: 调度器 v2 —— 输入/输出统一队列 + 库存/空间双预留 + 冷却与 TTL
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public class LogisticsRequestDispatcher : MonoBehaviour
{
    [Header("调度参数")]
    public float requestCooldown = 5f;        // 同一 requester 失败后的冷却
    public float reserveTTL = 20f;            // 预留过期时间
    public int chainsPerTick = 3;             // 每 Tick 派发的链条数
    public int defaultMinBatch = 3;           // 默认最小批量
    public int minKeepPerWarehouse = 0;       // 供给仓保底库存（避免被抽干）
    public int maxConcurrentPerResource = 2;  // 同一资源同时在途上限（粗粒度）

    private readonly List<LogisticsRequest> _queue = new List<LogisticsRequest>();
    private readonly Dictionary<object, float> _cooldownUntil = new Dictionary<object, float>(); // key: consumer/producer
    private readonly Dictionary<ResourceType, int> _inFlight = new Dictionary<ResourceType, int>();

    private ReservationManager _reservations = new ReservationManager();
    private TaskManager _tm;
    private CityContext _city;

    void Start()
    {
        _tm = FindObjectOfType<TaskManager>();
        _city = FindObjectOfType<CityContext>();
        TickSystem.OnTick += OnTick;
        TLog.Log("[LRD v2] 启动。", LogColor.Cyan);
    }
    void OnDestroy() { TickSystem.OnTick -= OnTick; }

    public void Enqueue(LogisticsRequest req)
    {
        if (req == null) return;

        req.createTime = Time.time;
        if (req.minBatch <= 0) req.minBatch = defaultMinBatch;
        if (req.ttlSeconds <= 0f) req.ttlSeconds = 30f;
        req.state = RequestState.Pending;

        _queue.Add(req);
        // 优先级排序（同优先级按 FIFO）
        _queue.Sort((a, b) => b.priority.CompareTo(a.priority));

        string who = req.kind == RequestKind.PullInput ? (req.consumer as MonoBehaviour)?.name : (req.producer as MonoBehaviour)?.name;
        TLog.Log("[LRD v2] 入队: " + req.kind + " " + req.type + " x" + req.quantity + " from " + who, LogColor.Cyan);
    }

    void OnTick()
    {
        if (_queue.Count == 0 || _tm == null || _city == null) return;

        // 清理过期请求
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            LogisticsRequest r = _queue[i];
            if (Time.time - r.createTime > r.ttlSeconds)
            {
                r.state = RequestState.Canceled;
                _queue.RemoveAt(i);
                TLog.Warning("[LRD v2] 请求过期: " + r.kind + " " + r.type);
            }
        }
        if (_queue.Count == 0) return;

        int budget = chainsPerTick;

        for (int i = _queue.Count - 1; i >= 0 && budget > 0; i--)
        {
            LogisticsRequest req = _queue[i];

            // 同一 requester 冷却
            object key = (object)(req.kind == RequestKind.PullInput ? (req.consumer ?? (object)req) : (req.producer ?? (object)req));
            float until;
            if (_cooldownUntil.TryGetValue(key, out until) && Time.time < until) continue;

            // 并发上限（按资源粗控）
            int cur;
            _inFlight.TryGetValue(req.type, out cur);
            if (cur >= maxConcurrentPerResource) continue;

            bool assigned = false;
            if (req.kind == RequestKind.PullInput)
            {
                assigned = TryAssignPull(req);
            }
            else // PushOutput
            {
                assigned = TryAssignPush(req);
            }

            if (assigned)
            {
                _queue.RemoveAt(i);
                _inFlight[req.type] = cur + 1;
                budget--;
            }
        }
    }

    // =============== 输入：从仓库拉到 IConsumer ===============
    private bool TryAssignPull(LogisticsRequest req)
    {
        if (req.consumer == null) return false;

        IStorage supplier = SelectSupplierStock(req.type, req.minBatch);
        if (supplier == null) return false;

        int reserved = _reservations.TryReserveStock(supplier, req.type, Mathf.Max(req.minBatch, req.quantity));
        if (reserved <= 0) return false;

        req.reservedFrom = supplier;
        req.reservedAmount = reserved;
        req.reserveExpireTime = Time.time + reserveTTL;
        req.state = RequestState.Reserved;

        MonoBehaviour fromMB = supplier as MonoBehaviour;
        MonoBehaviour toMB = req.consumer as MonoBehaviour;
        if (fromMB == null || toMB == null)
        {
            _reservations.UnreserveStock(supplier, req.type, reserved);
            return false;
        }

        _tm.Enqueue(TaskSequence.Create(
            MoveToTask.Create(fromMB.transform.position),
            ReservedPickupTask.Create(_reservations, supplier, req.type, reserved),
            MoveToTask.Create(toMB.transform.position),
            // Consumer 端不需要空间预留，直接交付；不足则按 DeliverCarried 交付所携带量
            DeliverTask.Create(req.consumer, req.type, reserved, 0, DeliverPolicy.DeliverCarried)
        ));

        req.state = RequestState.Assigned;
        return true;
    }

    // =============== 输出：从 IProducer 推到仓库 ===============
    private bool TryAssignPush(LogisticsRequest req)
    {
        if (req.producer == null) return false;

        IStorage receiver = SelectReceiverSpace(req.type, req.minBatch);
        if (receiver == null) return false;

        int reservedSpace = _reservations.TryReserveSpace(receiver, req.type, Mathf.Max(req.minBatch, req.quantity));
        if (reservedSpace <= 0) return false;

        req.reservedTo = receiver;
        req.reservedSpace = reservedSpace;
        req.reserveExpireTime = Time.time + reserveTTL;
        req.state = RequestState.Reserved;

        MonoBehaviour toMB = receiver as MonoBehaviour;
        MonoBehaviour fromMB = req.producer as MonoBehaviour;
        if (fromMB == null || toMB == null)
        {
            _reservations.UnreserveSpace(receiver, req.type, reservedSpace);
            return false;
        }

        _tm.Enqueue(TaskSequence.Create(
            MoveToTask.Create(fromMB.transform.position),
            // 从生产点拿货：尽量拿到 reservedSpace（不足则拿可得量）
            PickupTask.Create(req.producer, req.type, reservedSpace, PickupPolicy.TakeAllAvailable),
            MoveToTask.Create(toMB.transform.position),
            // 兑现空间预留（按背包∩预留投递）
            ReservedDeliverTask.Create(_reservations, receiver, req.type, reservedSpace)
        ));

        req.state = RequestState.Assigned;
        return true;
    }

    // 选择供给仓（满足最小批量 + 预留后仍 ≥ minKeep）
    private IStorage SelectSupplierStock(ResourceType type, int atLeast)
    {
        IStorage best = null;
        float bestD = float.MaxValue;

        if (_city == null || _city.warehouses == null) return null;

        for (int i = 0; i < _city.warehouses.Count; i++)
        {
            WarehouseBuilding w = _city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            IStorage s = w as IStorage; if (s == null) continue;

            int avail = _reservations.GetAvailableStockForReserve(s, type);
            if (avail - minKeepPerWarehouse < atLeast) continue;

            // 简易距离：调度器 → 仓库（后续可替换为“搬运工 → 仓库 + 仓库 → 目标”）
            float d = (w.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    // 选择接收仓（有空间）
    private IStorage SelectReceiverSpace(ResourceType type, int atLeast)
    {
        IStorage best = null;
        float bestD = float.MaxValue;

        if (_city == null || _city.warehouses == null) return null;

        for (int i = 0; i < _city.warehouses.Count; i++)
        {
            WarehouseBuilding w = _city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            IStorage s = w as IStorage; if (s == null) continue;

            int space = _reservations.GetAvailableSpaceForReserve(s, type);
            if (space < atLeast) continue;

            float d = (w.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    // （可选）失败回调入口，任务脚本失败时可调用
    public void NotifyRequestFailed(object requesterOrProducer)
    {
        if (requesterOrProducer == null) return;
        _cooldownUntil[requesterOrProducer] = Time.time + requestCooldown;
    }

    // 任务完成时调用（你可以在 DeliverTask/外部事件里触发）
    public void NotifyRequestFulfilled(ResourceType type)
    {
        int cur;
        if (_inFlight.TryGetValue(type, out cur))
        {
            cur -= 1; if (cur < 0) cur = 0;
            _inFlight[type] = cur;
        }
    }
}
