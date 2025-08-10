/***************************************************************************
// File       : CityContext.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 城市上下文
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public class CityContext : MonoBehaviour
{
    public List<WarehouseBuilding> warehouses = new();
    public List<ProductionBuilding> productions = new();
    public Transform spawnArea; // 居民出生/活动区域

    void Awake()
    {
        if (spawnArea == null)
        {
            var go = new GameObject("SpawnArea");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            spawnArea = go.transform;
        }
    }
}
