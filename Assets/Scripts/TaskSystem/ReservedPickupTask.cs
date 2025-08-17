/***************************************************************************
// File       : ReservedPickupTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 依据 ReservationManager 兑现预留，从仓库按预留量取走
// ***************************************************************************/
/***************************************************************************
// File       : ReservedPickupTask.cs
// Description: 兑现“库存预留”，从仓库按预留量取走
// ***************************************************************************/
/***************************************************************************
// File : ReservedPickupTask.cs  (升级版)
// Desc: 兑现“库存预留”，失败时通知调度器进入冷却
//***************************************************************************/

public class ReservedPickupTask : TaskBase
{
    private ReservationManager _rm;
    private IStorage _from;
    private ResourceType _type;
    private int _amount;

    // 新增（可选）：用于失败回调的调度器与键
    private LogisticsRequestDispatcher _dispatcher;
    private object _failKey; // 通常传 consumer（Pull）或 producer（Push）
    private ResourceType _notifyType;

    public static ReservedPickupTask Create(
        ReservationManager rm, IStorage from, ResourceType type, int amount,
        int priority = 0,
        LogisticsRequestDispatcher dispatcher = null, object failKey = null, ResourceType notifyType = ResourceType.None)
    {
        var t = new ReservedPickupTask();
        t._rm = rm; t._from = from; t._type = type; t._amount = amount; t.Priority = priority;
        t._dispatcher = dispatcher; t._failKey = failKey; t._notifyType = notifyType;
        return t;
    }

    protected override void OnTick()
    {
        if (Ctx == null || Ctx.Actor == null || Ctx.Actor.Inventory == null || _rm == null || _from == null)
        {
            TLog.Warning("[ReservedPickupTask] 上下文无效。"); Fail(); return;
        }

        if (!_rm.ConsumeReservedStock(_from, _type, _amount))
        {
            TLog.Warning("[ReservedPickupTask] 兑现库存预留失败。");
            // 失败时通知调度器冷却
            _dispatcher?.NotifyRequestFailed(_failKey);
            Fail(); return;
        }

        Ctx.Actor.Inventory.Add(_type, _amount);
        TLog.Log("[ReservedPickupTask] 取到预留 " + _type + " x" + _amount, LogColor.Green);
        Succeed();
    }
}
