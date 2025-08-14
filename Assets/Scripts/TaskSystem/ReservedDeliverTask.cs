/***************************************************************************
// File       : ReservedDeliverTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 兑现“空间预留”，向仓库按预留量投递（从背包扣）
// ***************************************************************************/

public class ReservedDeliverTask : TaskBase
{
    private ReservationManager _rm;
    private IStorage _to;
    private ResourceType _type;
    private int _amount; // 期望兑现量（将按“实际携带量 ∩ 预留量”投递）

    public static ReservedDeliverTask Create(ReservationManager rm, IStorage to, ResourceType type, int amount, int priority = 0)
    {
        ReservedDeliverTask t = new ReservedDeliverTask();
        t._rm = rm; t._to = to; t._type = type; t._amount = amount; t.Priority = priority;
        return t;
    }

    protected override void OnTick()
    {
        if (Ctx == null || Ctx.Actor == null || Ctx.Actor.Inventory == null || _rm == null || _to == null)
        {
            TLog.Warning("[ReservedDeliverTask] 上下文无效。"); Fail(); return;
        }

        int carried = Ctx.Actor.Inventory.Get(_type);
        if (carried <= 0) { TLog.Warning("[ReservedDeliverTask] 背包无该资源。"); Fail(); return; }

        int deliver = _amount <= carried ? _amount : carried;

        // 先从背包扣
        if (!Ctx.Actor.Inventory.TryTake(_type, deliver))
        {
            TLog.Warning("[ReservedDeliverTask] 背包扣除失败。");
            Fail(); return;
        }

        // 兑现空间预留（同时执行 Deliver）
        if (!_rm.ConsumeReservedSpace(_to, _type, deliver))
        {
            // 回滚
            Ctx.Actor.Inventory.Add(_type, deliver);
            TLog.Warning("[ReservedDeliverTask] 兑现空间预留失败。");
            Fail(); return;
        }

        TLog.Log("[ReservedDeliverTask] 投递预留 " + _type + " x" + deliver, LogColor.Yellow);
        Succeed();
    }
}
