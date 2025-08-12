/***************************************************************************
// File       : DeliverTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Description: [TODO] 将居民背包货物送达目标；支持 IStorage / IConsumer；
//              DeliverPolicy 支持 Exact（足量）/ DeliverCarried（按携带量投递）；统一 TLog。
// ***************************************************************************/

public enum DeliverPolicy
{
    Exact = 0,            // 需要足量（背包至少 amount）
    DeliverCarried = 1    // 以背包当前携带量为准（最多投到 amount）
}

public class DeliverTask : TaskBase
{
    private object _to;                   // 目标对象：可为 IStorage 或 IConsumer
    private ResourceType _type;
    private int _amount;
    private DeliverPolicy _policy = DeliverPolicy.Exact;

    public static DeliverTask Create(object to, ResourceType type, int amount, int priority = 0, DeliverPolicy policy = DeliverPolicy.Exact)
    {
        DeliverTask t = new DeliverTask();
        t._to = to;
        t._type = type;
        t._amount = amount;
        t.Priority = priority;
        t._policy = policy;
        return t;
    }

    protected override void OnTick()
    {
        if (Ctx == null || Ctx.Actor == null || Ctx.Actor.Inventory == null || _to == null)
        {
            TLog.Warning("[DeliverTask] 上下文/目标为空。");
            Fail();
            return;
        }

        // 计算本次实际投递量
        int deliverAmount = _amount;
        if (_policy == DeliverPolicy.DeliverCarried)
        {
            int carried = Ctx.Actor.Inventory.Get(_type); // 你的 Inventory/Warehouse 已有 Get(type)
            if (carried <= 0)
            {
                TLog.Warning("[DeliverTask] 背包没有可投递资源：" + _type);
                Fail();
                return;
            }
            if (carried < deliverAmount) deliverAmount = carried;
        }

        // 先从背包扣（避免复制）
        bool took = Ctx.Actor.Inventory.TryTake(_type, deliverAmount);
        if (!took)
        {
            TLog.Warning("[DeliverTask] 背包扣除失败，缺少 " + _type + " x" + deliverAmount);
            Fail();
            return;
        }

        // 1) 优先 IConsumer（容量/白名单语义）
        IConsumer consumer = _to as IConsumer;
        if (consumer != null)
        {
            if (!consumer.CanAccept(_type, deliverAmount) || !consumer.TryAccept(_type, deliverAmount))
            {
                TLog.Warning("[DeliverTask] IConsumer 拒收： " + _type + " x" + deliverAmount + "，回滚背包");
                Ctx.Actor.Inventory.Add(_type, deliverAmount);
                Fail();
                return;
            }

            TLog.Log("[DeliverTask] 投递到 IConsumer 成功： " + _type + " x" + deliverAmount, LogColor.Green);
            Succeed();
            return;
        }

        // 2) 回落到 IStorage（通用仓储）
        IStorage storage = _to as IStorage;
        if (storage != null)
        {
            storage.Deliver(_type, deliverAmount);
            TLog.Log("[DeliverTask] 投递到 IStorage 成功： " + _type + " x" + deliverAmount, LogColor.Green);
            Succeed();
            return;
        }

        // 3) 既不是 IConsumer 也不是 IStorage → 回滚并失败
        TLog.Warning("[DeliverTask] 目标不实现 IConsumer/IStorage，回滚背包。");
        Ctx.Actor.Inventory.Add(_type, deliverAmount);
        Fail();
    }
}
