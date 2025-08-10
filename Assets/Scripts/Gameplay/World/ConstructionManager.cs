
/***************************************************************************
// File       : ConstructionManager.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 建筑管理器
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public enum BuildType { Warehouse, LumberCamp, House, Bakery }

public class ConstructionManager : MonoBehaviour
{
    public CityContext city;
    public float gridSize = 2f;

    public GameObject prefabWarehouse;
    public GameObject prefabLumberCamp; // 生产木材的假建筑
    public GameObject prefabHouse;
    public GameObject prefabBakery;

    Dictionary<BuildType, GameObject> _map;

    void Awake()
    {
        _map = new()
        {
            { BuildType.Warehouse, prefabWarehouse },
            { BuildType.LumberCamp, prefabLumberCamp },
            { BuildType.House, prefabHouse },
            { BuildType.Bakery, prefabBakery },
        };
    }

    public GameObject Place(BuildType type, Vector3 worldPos)
    {
        var p = _map[type];
        if (p == null) return null;
        worldPos.x = Mathf.Round(worldPos.x / gridSize) * gridSize;
        worldPos.z = Mathf.Round(worldPos.z / gridSize) * gridSize;
        var go = Instantiate(p, worldPos, Quaternion.identity);

        // 归档进 City 列表（若脚本存在则加入）
        if (go.TryGetComponent(out WarehouseBuilding wh)) city.warehouses.Add(wh);
        if (go.TryGetComponent(out ProductionBuilding pb)) city.productions.Add(pb);

        return go;
    }
}
