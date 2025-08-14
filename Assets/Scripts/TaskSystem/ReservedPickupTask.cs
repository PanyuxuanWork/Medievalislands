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

public class ReservedPickupTask : TaskBase
{
    private ReservationManager _rm;
    private IStorage _from;
    private ResourceType _type;
    private int _amount;

    public static ReservedPickupTask Create(ReservationManager rm, IStorage from, ResourceType type, int amount, int priority = 0)
    {
        ReservedPickupTask t = new ReservedPickupTask();
        t._rm = rm; t._from = from; t._type = type; t._amount = amount; t.Priority = priority;
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
            Fail(); return;
        }

        Ctx.Actor.Inventory.Add(_type, _amount);
        TLog.Log("[ReservedPickupTask] 取到预留 " + _type + " x" + _amount, LogColor.Green);
        Succeed();
    }
}
