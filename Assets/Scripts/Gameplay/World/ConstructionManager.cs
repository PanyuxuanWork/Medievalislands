
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

    /// <summary>
    /// 瞬间建造
    /// </summary>
    /// <param name="type"></param>
    /// <param name="worldPos"></param>
    /// <returns></returns>
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

    // ConstructionManager.cs 内新增方法（保持你原有 using/字段风格）
    /// <summary>
    /// 持续时间内建造
    /// </summary>
    /// <param name="type"></param>
    /// <param name="worldPos"></param>
    /// <param name="recipe"></param>
    /// <param name="maxWorkers"></param>
    /// <param name="chunkSeconds"></param>
    /// <returns></returns>
    public GameObject PlaceWithConstruction(BuildType type, Vector3 worldPos, ConstructionRecipe recipe, int maxWorkers = 2, float chunkSeconds = 4f)
    {
        if (_map == null || !_map.ContainsKey(type))
        {
            TLog.Warning("[ConstructionManager] 未找到该类型的目标 Prefab：" + type);
            return null;
        }

        // 网格对齐（照你原先 Place 的做法）
        worldPos.x = Mathf.Round(worldPos.x / gridSize) * gridSize;
        worldPos.z = Mathf.Round(worldPos.z / gridSize) * gridSize;

        // 创建一个“工地”空物体
        var siteGO = new GameObject("ConstructionSite_" + type);
        siteGO.transform.position = worldPos;

        // 给工地挂 BuildingBase（便于被选中/显示状态）
        var bb = siteGO.AddComponent<BuildingBase>();
        bb.state = BuildingState.Constructing;

        // 挂 ConstructionSite，配置目标与配方
        var site = siteGO.AddComponent<ConstructionSite>();
        site.targetPrefab = _map[type];
        site.recipe = recipe;
        site.maxConcurrentWorkers = Mathf.Max(1, maxWorkers);
        site.workChunkSeconds = Mathf.Max(0.5f, chunkSeconds);

        // 这里不直接把目标建筑加入 City 列表，等完工后由 ConstructionSite 完成注册
        TLog.Log(this, $"[ConstructionManager] 已放置工地：{type} @ {worldPos}", LogColor.Cyan);
        return siteGO;
    }

}
