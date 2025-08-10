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
}
