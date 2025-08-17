/***************************************************************************
// File       : TickRunner.cs
// Author     : Panyuxuan
// Created    : 2025/08/16
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] TickRunner：集中调度 ISimTickable
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

// [TODO] ISimTickable：仿真步进接口（小的 Order 先执行）
public interface ISimTickable
{
    int Order { get; }           // 用于确定执行顺序（AI<生产<消费…）
    bool Enabled { get; }        // 可直接用 isActiveAndEnabled
    void SimTick();              // 固定步长推进一次
}


public class TickRunner : MonoSingleton<TickRunner>
{
    private readonly List<ISimTickable> _items = new List<ISimTickable>();

    public void Register(ISimTickable item)
    {
        if (item == null) return;
        if (_items.Contains(item)) return;
        _items.Add(item);
        _items.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public void Unregister(ISimTickable item)
    {
        if (item == null) return;
        _items.Remove(item);
    }

    public void TickAll()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            ISimTickable t = _items[i];
            if (t is { Enabled: true }) t.SimTick();
        }
    }
}
