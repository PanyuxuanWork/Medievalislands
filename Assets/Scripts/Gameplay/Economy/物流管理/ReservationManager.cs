/***************************************************************************
// File       : ReservationManager.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 供给侧软预留，避免多个居民抢同一份库存
// Description: 供给侧“库存预留”与接收侧“空间预留”的统一管理
// ***************************************************************************/

using System.Collections.Generic;

public class ReservationManager
{
    // 库存已预留：storage -> (type -> qty)
    private readonly Dictionary<IStorage, Dictionary<ResourceType, int>> _stockReserved
        = new Dictionary<IStorage, Dictionary<ResourceType, int>>();

    // 空间已预留：storage -> (type -> qty)
    private readonly Dictionary<IStorage, Dictionary<ResourceType, int>> _spaceReserved
        = new Dictionary<IStorage, Dictionary<ResourceType, int>>();

    // --------- 库存预留 ----------
    public int GetReservedStock(IStorage s, ResourceType t)
    {
        Dictionary<ResourceType, int> map;
        if (!_stockReserved.TryGetValue(s, out map)) return 0;
        int v; return map.TryGetValue(t, out v) ? v : 0;
    }

    public int GetAvailableStockForReserve(IStorage s, ResourceType t)
    {
        int have = s.Get(t);
        int r = GetReservedStock(s, t);
        int avail = have - r;
        return avail < 0 ? 0 : avail;
    }

    public int TryReserveStock(IStorage s, ResourceType t, int want)
    {
        int can = GetAvailableStockForReserve(s, t);
        int take = want <= can ? want : can;
        if (take <= 0) return 0;

        Dictionary<ResourceType, int> map;
        if (!_stockReserved.TryGetValue(s, out map)) { map = new Dictionary<ResourceType, int>(); _stockReserved[s] = map; }
        int cur; map.TryGetValue(t, out cur);
        map[t] = cur + take;
        return take;
    }

    public void UnreserveStock(IStorage s, ResourceType t, int amount)
    {
        Dictionary<ResourceType, int> map;
        if (!_stockReserved.TryGetValue(s, out map)) return;
        int cur; map.TryGetValue(t, out cur);
        cur -= amount; if (cur < 0) cur = 0;
        map[t] = cur;
    }

    // 兑现库存预留（居民真正取走）
    public bool ConsumeReservedStock(IStorage s, ResourceType t, int amount)
    {
        Dictionary<ResourceType, int> map;
        if (!_stockReserved.TryGetValue(s, out map)) return false;
        int cur; map.TryGetValue(t, out cur);
        if (cur < amount) return false;

        map[t] = cur - amount;
        return s.TryPickup(t, amount);
    }

    // --------- 空间预留 ----------
    public int GetReservedSpace(IStorage s, ResourceType t)
    {
        Dictionary<ResourceType, int> map;
        if (!_spaceReserved.TryGetValue(s, out map)) return 0;
        int v; return map.TryGetValue(t, out v) ? v : 0;
    }

    // 计算可用空间 = Capacity - (当前量 + 已预留空间)
    public int GetAvailableSpaceForReserve(IStorage s, ResourceType t)
    {
        int cur = 0;
        int cap = int.MaxValue;
        try { cur = s.Get(t); } catch { cur = 0; }
        try { cap = (s as WarehouseBuilding) != null ? (s as WarehouseBuilding).Capacity : int.MaxValue; } catch { cap = int.MaxValue; }

        int reserved = GetReservedSpace(s, t);
        int avail = cap == int.MaxValue ? int.MaxValue : (cap - (cur + reserved));
        if (avail < 0) avail = 0;
        return avail;
    }

    public int TryReserveSpace(IStorage s, ResourceType t, int want)
    {
        int can = GetAvailableSpaceForReserve(s, t);
        int take = want <= can ? want : can;
        if (take <= 0) return 0;

        Dictionary<ResourceType, int> map;
        if (!_spaceReserved.TryGetValue(s, out map)) { map = new Dictionary<ResourceType, int>(); _spaceReserved[s] = map; }
        int cur; map.TryGetValue(t, out cur);
        map[t] = cur + take;
        return take;
    }

    public void UnreserveSpace(IStorage s, ResourceType t, int amount)
    {
        Dictionary<ResourceType, int> map;
        if (!_spaceReserved.TryGetValue(s, out map)) return;
        int cur; map.TryGetValue(t, out cur);
        cur -= amount; if (cur < 0) cur = 0;
        map[t] = cur;
    }

    // 兑现空间预留（居民实际投递时调用：先扣预留，再 Deliver）
    public bool ConsumeReservedSpace(IStorage s, ResourceType t, int amount)
    {
        Dictionary<ResourceType, int> map;
        if (!_spaceReserved.TryGetValue(s, out map)) return false;
        int cur; map.TryGetValue(t, out cur);
        if (cur < amount) return false;

        map[t] = cur - amount;
        // 注意：IStorage.Deliver 不一定校验容量，这里只负责“兑现预留”以避免并发超额
        s.Deliver(t, amount);
        return true;
    }
}

