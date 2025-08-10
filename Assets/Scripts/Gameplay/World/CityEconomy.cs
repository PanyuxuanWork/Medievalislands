/***************************************************************************
// File       : CityEconomy.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;

// [TODO] 城市经济工具：
// 汇总城市内所有 WarehouseBuilding 的库存；提供 CanAfford / TryConsume 全城扣料。
// 先行实现，后续可替换为更完整的城市库存系统。
public static class CityEconomy
{
    public static bool CanAfford(CityContext city, List<ResourceCost> costs)
    {
        if (city == null || costs == null) return false;
        for (int i = 0; i < costs.Count; i++)
        {
            ResourceType type = costs[i].Type;
            int amount = costs[i].Amount;
            int total = GetTotal(city, type);
            if (total < amount) return false;
        }
        return true;
    }

    public static bool TryConsume(CityContext city, List<ResourceCost> costs)
    {
        if (!CanAfford(city, costs)) return false;

        // 逐项从多个仓库扣除（顺序扣，简单实现）
        for (int i = 0; i < costs.Count; i++)
        {
            ResourceType type = costs[i].Type;
            int remain = costs[i].Amount;

            for (int w = 0; w < city.warehouses.Count && remain > 0; w++)
            {
                WarehouseBuilding wh = city.warehouses[w];
                if (wh == null) continue;

                int have = wh.Get(type);
                if (have <= 0) continue;

                int take = remain <= have ? remain : have;
                if (wh.TryPickup(type, take))
                {
                    remain -= take;
                }
            }

            if (remain > 0)
            {
                // 理论上不会走到（前置 CanAfford 通过），保底返回 false
                return false;
            }
        }
        return true;
    }

    public static int GetTotal(CityContext city, ResourceType type)
    {
        if (city == null) return 0;
        int sum = 0;
        for (int i = 0; i < city.warehouses.Count; i++)
        {
            WarehouseBuilding wh = city.warehouses[i];
            if (wh == null) continue;
            sum += wh.Get(type);
        }
        return sum;
    }
}
