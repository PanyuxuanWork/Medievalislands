/***************************************************************************
// File       : ReservedDeliverTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 兑现“空间预留”，向仓库按预留量投递（从背包扣）
// ***************************************************************************/

/***************************************************************************
// File : ReservedDeliverTask.cs  (升级版)
// LastUpdateTime:2025/08/15
// Desc: 兑现“空间预留”，失败时通知调度器；成功可选通知 Fulfilled
//***************************************************************************/

public class ReservedDeliverTask : TaskBase
{
    private ReservationManager _rm;
    private IStorage _to;
    private ResourceType _type;
    private int _amount;

    // 新增：通知调度器
    private LogisticsRequestDispatcher _dispatcher;
    private object _failKey;         // 用于失败冷却
    private bool _notifyFulfilled;   // 成功后是否通知完成（常用于 PushOutput）
    private ResourceType _notifyType;

    public static ReservedDeliverTask Create(
        ReservationManager rm, IStorage to, ResourceType type, int amount, int priority = 0,
        LogisticsRequestDispatcher dispatcher = null, object failKey = null,
        bool notifyFulfilled = false, ResourceType notifyType = ResourceType.None)
    {
        var t = new ReservedDeliverTask();
        t._rm = rm; t._to = to; t._type = type; t._amount = amount; t.Priority = priority;
        t._dispatcher = dispatcher; t._failKey = failKey;
        t._notifyFulfilled = notifyFulfilled; t._notifyType = notifyType;
        return t;
    }

    protected override void OnTick()
    {
        if (Ctx == null || Ctx.Owner == null || Ctx.Owner.Inventory == null || _rm == null || _to == null)
        {
            TLog.Warning("[ReservedDeliverTask] 上下文无效。"); _dispatcher?.NotifyRequestFailed(_failKey); Fail(); return;
        }

        int carried = Ctx.Owner.Inventory.Get(_type);
        if (carried <= 0) { TLog.Warning("[ReservedDeliverTask] 背包无该资源。"); _dispatcher?.NotifyRequestFailed(_failKey); Fail(); return; }

        int deliver = _amount <= carried ? _amount : carried;

        if (!Ctx.Owner.Inventory.TryTake(_type, deliver))
        {
            TLog.Warning("[ReservedDeliverTask] 背包扣除失败。"); _dispatcher?.NotifyRequestFailed(_failKey); Fail(); return;
        }

        if (!_rm.ConsumeReservedSpace(_to, _type, deliver))
        {
            // 回滚
            Ctx.Owner.Inventory.Add(_type, deliver);
            TLog.Warning("[ReservedDeliverTask] 兑现空间预留失败。"); _dispatcher?.NotifyRequestFailed(_failKey); Fail(); return;
        }

        TLog.Log("[ReservedDeliverTask] 投递预留 " + _type + " x" + deliver, LogColor.Yellow);

        if (_notifyFulfilled && _dispatcher != null)
        {
            _dispatcher.NotifyRequestFulfilled(_notifyType);
        }
        Succeed();
    }
}
