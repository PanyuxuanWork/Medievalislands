/***************************************************************************
// File       : WarehouseBuilding.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using Sirenix.OdinInspector;
using UnityEngine;

// [TODO] 仓库建筑：
// 继承 BuildingBase，提供 IStorage 能力，用于城市内集中存取资源。
// 注意：依赖 Inventory 与 ResourceType；若你已有同名脚本，请对比保留一个即可。
public class WarehouseBuilding : BuildingBase, IStorage
{
    [Header("Storage")]
    public int capacity = 99999;

    // 使用你已有的 Inventory 类（确保项目里已有 Inventory.cs）
    [ShowInInspector]
    public Inventory inventory = new Inventory();

    public int Capacity
    {
        get { return capacity; }
    }

    public int Get(ResourceType type)
    {
        return inventory.Get(type);
    }

    public bool TryPickup(ResourceType type, int amount)
    {
        if (state != BuildingState.Active) return false;
        return inventory.TryConsume(type, amount);
    }

    public void Deliver(ResourceType type, int amount)
    {
        if (state != BuildingState.Active) return;
        inventory.Add(type, amount);
    }
}
