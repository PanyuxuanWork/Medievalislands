/***************************************************************************
// File       : Inventory.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 仓库
// ***************************************************************************/

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Inventory
{
    [ShowInInspector]
    public readonly Dictionary<ResourceType, int> _map = new();

    public int Get(ResourceType t) => _map.TryGetValue(t, out var v) ? v : 0;

    public bool TryConsume(ResourceType t, int amt)
    {
        int cur = Get(t);
        if (cur < amt) return false;
        _map[t] = cur - amt;
        return true;
    }

    public void Add(ResourceType t, int amt)
    {
        _map[t] = Get(t) + amt;
    }

    public bool HasAll((ResourceType t, int amt)[] reqs)
    {
        foreach (var r in reqs) if (Get(r.t) < r.amt) return false;
        return true;
    }

    // 放进 Inventory 类内部
    // [TODO] 从背包中尝试扣减指定资源数量。成功返回 true，失败不改动。
    public bool TryTake(ResourceType type, int amount)
    {
        if (amount <= 0) return true;

        // 若有 GetAmount / HasAll 等方法，可先检查存量
        int have = Get(type); // 若你没有该方法，请改为你现有的查询方式
        if (have < amount) return false;

        // —— 优先路径：如果你有 Decrease/Remove/Consume 等方法，请改用它 —— 
        // bool ok = Decrease(type, amount); // 示例：若你已有这样的 API
        // if (ok) return true;

        // —— 退化路径：用 Add(负数) 扣减（你的 Add 已存在）——
        Add(type, -amount);
        return true;
    }

    // 可选：若你还没有这个查询接口，一并加上
    public int GetAmount(ResourceType type)
    {
        // 若你已有实现，请删掉这个占位改用你自己的
        // 下面是一种常见实现：字典存储
        // Dictionary<ResourceType, int> _items;
        // if (_items == null) return 0;
        // int val;
        // return _items.TryGetValue(type, out val) ? val : 0;

        // 占位返回，为了编译通过；请换成你的真实实现
        return 0;
    }

}
