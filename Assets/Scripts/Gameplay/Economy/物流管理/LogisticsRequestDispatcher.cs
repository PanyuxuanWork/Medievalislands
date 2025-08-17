/***************************************************************************
// File       : LogisticsRequestDispatcher.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 需求队列 -> 选择供给 -> 软预留 -> 派发一条搬运链
// Description: 调度器 v2 —— 输入/输出统一队列 + 库存/空间双预留 + 冷却与 TTL
// ***************************************************************************/

/***************************************************************************
// File       : LogisticsRequestDispatcher.cs (v2.1)
// Desc       : 输入/输出统一队列 + 库存/空间双预留 + 冷却 & TTL + 预留过期回滚
//***************************************************************************/

using System.Collections.Generic;
using Core;
using UnityEngine;

public class LogisticsRequestDispatcher : MonoBehaviour
{
    [Header("调度参数")]
    public float requestCooldown = 5f;            // 失败后冷却
    public float reserveTTL = 20f;                // 预留过期
    public int chainsPerTick = 3;                 // 每 tick 可派发任务链数
    public int defaultMinBatch = 3;
    public int minKeepPerWarehouse = 0;           // 供给仓保底库存
    public int maxConcurrentPerResource = 2;      // 粗粒度并发上限

    private readonly List<LogisticsRequest> _queue = new List<LogisticsRequest>();
    private readonly List<LogisticsRequest> _assigned = new List<LogisticsRequest>(); // 新：已派发跟踪
    private readonly Dictionary<object, float> _cooldownUntil = new Dictionary<object, float>();
    private readonly Dictionary<ResourceType, int> _inFlight = new Dictionary<ResourceType, int>();

    private ReservationManager _reservations = new ReservationManager();
    private TaskManager _tm;
    private CityContext _city;

    void Start()
    {
        _tm = FindObjectOfType<TaskManager>();
        _city = FindObjectOfType<CityContext>();
        TickSystem.OnTick += OnTick;
        TLog.Log("[LRD v2.1] 启动。", LogColor.Cyan);
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
        _queue.Sort((a, b) => b.priority.CompareTo(a.priority));

        string who = req.kind == RequestKind.PullInput ? (req.consumer as MonoBehaviour)?.name : (req.producer as MonoBehaviour)?.name;
        TLog.Log("[LRD v2.1] 入队: " + req.kind + " " + req.type + " x" + req.quantity + " from " + who, LogColor.Cyan);
    }

    void OnTick()
    {
        if (_tm == null || _city == null) return;

        // 1) 清理过期请求（还在队列里尚未处理的）
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            var r = _queue[i];
            if (Time.time - r.createTime > r.ttlSeconds)
            {
                r.state = RequestState.Canceled;
                _queue.RemoveAt(i);
                TLog.Warning("[LRD v2.1] 请求过期: " + r.kind + " " + r.type);
            }
        }

        // 2) 检查已派发请求的预留是否过期（例如搬运途中卡住）
        for (int i = _assigned.Count - 1; i >= 0; i--)
        {
            var r = _assigned[i];
            if (Time.time > r.reserveExpireTime)
            {
                // 回滚预留
                if (r.reservedFrom != null && r.reservedAmount > 0)
                {
                    _reservations.UnreserveStock(r.reservedFrom, r.type, r.reservedAmount);
                }
                if (r.reservedTo != null && r.reservedSpace > 0)
                {
                    _reservations.UnreserveSpace(r.reservedTo, r.type, r.reservedSpace);
                }

                // 并发计数回退
                DecrementInFlight(r.type);

                // 重新入队或作废（这里选择重新入队一次）
                r.state = RequestState.Pending;
                r.createTime = Time.time;  // 重新计时
                r.reservedAmount = 0; r.reservedFrom = null;
                r.reservedSpace = 0; r.reservedTo = null;
                _queue.Add(r);
                _queue.Sort((a, b) => b.priority.CompareTo(a.priority));
                _assigned.RemoveAt(i);

                TLog.Warning("[LRD v2.1] 预留过期回滚 & 重新入队: " + r.kind + " " + r.type);
            }
        }

        // 3) 正常派发
        int budget = chainsPerTick;
        for (int i = _queue.Count - 1; i >= 0 && budget > 0; i--)
        {
            var req = _queue[i];

            object key =
                req.kind == RequestKind.PullInput
                    ? (req.consumer != null ? (object)req.consumer : (object)req)
                    : (req.producer != null ? (object)req.producer : (object)req);

            float until;
            if (_cooldownUntil.TryGetValue(key, out until) && Time.time < until) continue;

            int cur;
            _inFlight.TryGetValue(req.type, out cur);
            if (cur >= maxConcurrentPerResource) continue;

            bool ok = (req.kind == RequestKind.PullInput) ? TryAssignPull(req) : TryAssignPush(req);
            if (ok)
            {
                _queue.RemoveAt(i);
                _assigned.Add(req);
                _inFlight[req.type] = cur + 1;
                budget--;
            }
        }
    }

    // 输入：仓库 → consumer
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

        var fromMB = supplier as MonoBehaviour;
        var toMB = req.consumer as MonoBehaviour;
        if (fromMB == null || toMB == null)
        {
            _reservations.UnreserveStock(supplier, req.type, reserved);
            return false;
        }

        _tm.Enqueue(TaskSequence.Create(
            MoveToTask.Create(fromMB.transform.position),
            // 失败时触发冷却
            ReservedPickupTask.Create(_reservations, supplier, req.type, reserved,
                dispatcher: this, failKey: req.consumer, notifyType: req.type),
            MoveToTask.Create(toMB.transform.position),
            // consumer 端用 DeliverCarried（非预留）
            DeliverTask.Create(req.consumer, req.type, reserved, 0, DeliverPolicy.DeliverCarried),
            // 成功尾巴：减少并发
            NotifyFulfilledTask.Create(this, req.type) // 新增的小任务，见下方
        ));

        req.state = RequestState.Assigned;
        return true;
    }

    // 输出：producer → 仓库
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

        var toMB = receiver as MonoBehaviour;
        var fromMB = req.producer as MonoBehaviour;
        if (fromMB == null || toMB == null)
        {
            _reservations.UnreserveSpace(receiver, req.type, reservedSpace);
            return false;
        }

        _tm.Enqueue(TaskSequence.Create(
            MoveToTask.Create(fromMB.transform.position),
            PickupTask.Create(req.producer, req.type, reservedSpace, PickupPolicy.TakeAllAvailable),
            MoveToTask.Create(toMB.transform.position),
            // 失败时冷却；成功时在内部可通知 Fulfilled
            ReservedDeliverTask.Create(_reservations, receiver, req.type, reservedSpace,
                dispatcher: this, failKey: req.producer, notifyFulfilled: true, notifyType: req.type),
            // 双保险：尾巴再递减一次（安全起见；内部已通知则会抵消）
            NotifyFulfilledTask.Create(this, req.type)
        ));

        req.state = RequestState.Assigned;
        return true;
    }

    private IStorage SelectSupplierStock(ResourceType type, int atLeast)
    {
        IStorage best = null; float bestD = float.MaxValue;
        if (_city == null || _city.warehouses == null) return null;

        for (int i = 0; i < _city.warehouses.Count; i++)
        {
            var w = _city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            var s = w as IStorage; if (s == null) continue;

            int avail = _reservations.GetAvailableStockForReserve(s, type);
            if (avail - minKeepPerWarehouse < atLeast) continue;

            float d = (w.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    private IStorage SelectReceiverSpace(ResourceType type, int atLeast)
    {
        IStorage best = null; float bestD = float.MaxValue;
        if (_city == null || _city.warehouses == null) return null;

        for (int i = 0; i < _city.warehouses.Count; i++)
        {
            var w = _city.warehouses[i];
            if (w == null || w.state != BuildingState.Active) continue;
            var s = w as IStorage; if (s == null) continue;

            int space = _reservations.GetAvailableSpaceForReserve(s, type);
            if (space < atLeast) continue;

            float d = (w.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    public void NotifyRequestFailed(object requesterOrProducer)
    {
        if (requesterOrProducer == null) return;
        _cooldownUntil[requesterOrProducer] = Time.time + requestCooldown;
        TLog.Warning("[LRD v2.1] 标记冷却: " + requesterOrProducer);
    }

    public void NotifyRequestFulfilled(ResourceType type)
    {
        DecrementInFlight(type);
    }

    private void DecrementInFlight(ResourceType type)
    {
        int cur;
        if (_inFlight.TryGetValue(type, out cur))
        {
            cur -= 1; if (cur < 0) cur = 0;
            _inFlight[type] = cur;
        }

        // 从 _assigned 列表里抹掉一个同资源的已派发（粗粒度匹配）
        for (int i = 0; i < _assigned.Count; i++)
        {
            if (_assigned[i].type.Equals(type))
            {
                _assigned.RemoveAt(i);
                break;
            }
        }
    }

    // 暴露给监控面板
    public int PendingCount => _queue.Count;
    public int AssignedCount => _assigned.Count;
    public int InFlight(ResourceType t) { int v; return _inFlight.TryGetValue(t, out v) ? v : 0; }
}
