/***************************************************************************
// File       : BuildAsset.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 资源建造基类
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

// [TODO] 建筑模板（ScriptableObject）：
// 用于配置建筑的静态属性，分为“大类 + 小类”两级分类。
// 大类（BuildingType）：功能驱动系统逻辑，例如生产、仓储、军事等。
// 小类（BuildingSubType）：细分建筑类型，例如小麦农田、渔场、铁匠铺等。
// 动态运行状态（库存、进度等）由 BuildingBase 持有。
[CreateAssetMenu(fileName = "BuildingAsset", menuName = "Game/Building Asset")]
public class BuildingAsset : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("唯一ID，便于数据表/存档引用")]
    [Min(0)]
    public int ID;

    [Tooltip("显示名（本地化可在外部做映射）")]
    public string Name;

    [Tooltip("建筑功能大类，用于驱动核心逻辑")]
    public BuildingType BuildingType;

    [Tooltip("建筑功能小类，用于更细分的AI、UI、策划管理")]
    public BuildingSubType BuildingSubType;

    [Tooltip("该建筑可绑定的职业类型（可多选，例如仓库管理员+搬运工）")]
    public List<ProfessionType> ProfessionTypes = new List<ProfessionType>();

    [Header("占位与雇员")]
    [Tooltip("网格占位尺寸（格数），用于放置/寻路遮挡等")]
    [MinValue(1)]
    public Vector2Int Footprint = new Vector2Int(1, 1);

    [Tooltip("最大雇员数")]
    [Min(0)]
    public int MaxEmployees = 5;

    [Tooltip("是否允许系统自动分配雇员")]
    public bool AllowAutoAssign = true;

    [Header("建造成本")]
    [Tooltip("放置/完工所需资源清单")]
    public List<ResourceCost> BuildCosts = new List<ResourceCost>();

    [Header("预制体与表现")]
    [Tooltip("建成后的正式预制体")]
    public GameObject BuildPrefab;

    [Tooltip("放置预览用的预制体（可选，含半透明/占位网格）")]
    public GameObject PreviewPrefab;

    [Tooltip("小图标（UI显示）")]
    public Sprite Icon;

    [Tooltip("说明文本/风味描述")]
    [TextArea(2, 4)]
    public string Description;

    // [TODO] 施工参数（每种建筑可独立配置）
    [Header("施工参数")]
    [Tooltip("从开工到完工的时间（秒）。<=0 表示使用系统默认值。")]
    public float BuildTimeSeconds = 3f;

    // —— 便捷校验（Odin）——
    [Button("校验配置")]
    private void ValidateAsset()
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning($"{name}: Name 为空。");
        }

        if (BuildPrefab == null)
        {
            Debug.LogWarning($"{name}: BuildPrefab 未设置。");
        }

        for (int i = 0; i < BuildCosts.Count; i++)
        {
            if (BuildCosts[i].Amount <= 0)
            {
                Debug.LogWarning($"{name}: BuildCosts[{i}] 数量应 > 0。");
            }
        }
    }
}

// [TODO] 建造消耗项：一种资源与数量
[System.Serializable]
public struct ResourceCost
{
    public ResourceType Type;
    [Min(1)]
    public int Amount;
}

// [TODO] 建筑小类枚举：更细分的建筑类型
public enum BuildingSubType
{
    None = 0,
    // 农业类
    WheatFarm = 1,
    BarleyFarm = 2,
    VegetableFarm = 3,
    Vineyard = 4,
    FishingDock = 5,
    HuntingLodge = 6,
    // 采集/矿业类
    StoneQuarry = 10,
    IronMine = 11,
    GoldMine = 12,
    // 工业/生产类
    Blacksmith = 20,
    Weaver = 21,
    Brewery = 22,
    // 仓储类
    Granary = 30,
    Warehouse = 31,
    // 军事类
    Barracks = 40,
    GuardTower = 41,
    // 市政类
    TownHall = 50,
    Market = 51,
    // 装饰类
    Statue = 60,
    Fountain = 61
}
