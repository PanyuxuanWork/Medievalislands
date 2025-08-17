/***************************************************************************
// File       : BuildingPlacer.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

// [TODO] 建筑放置器（90°旋转 + 占位校验 + 预览轮廓与底部填充 + 占坑 + 施工点参数传递）
// 功能：Q/E 旋转预览；按旋转后的 footprint 在 GridAsset 上校验；基于格子画绿/红轮廓与底部半透明填充；
//      左键落地→占坑→生成施工点（传 OccupiedCells + RotationIndex）；右键/Esc 取消。

public class BuildingPlacer : MonoBehaviour
{
    [Header("References")]
    public CityContext City;
    public BuildingAsset SelectedBuilding;

    [Header("Grid")]
    public float gridSize = 1f;

    [Header("Build Mode")]
    public BuildMode defaultBuildMode = BuildMode.Instant;
    [Tooltip("仅在 LogisticsAndWorkers 模式下生效；未指定则由 ConstructionSite 自己决定")]
    public ConstructionRecipe defaultRecipe;
    [Tooltip("仅在 Instant 模式下；为 0 则由 ConstructionSite/Asset 决定")]
    public float defaultInstantSeconds = 3f;

    [Header("Logistics Settings")]
    public int logisticsMaxWorkers = 2;
    public float logisticsChunkSeconds = 4f;

    [Header("Preview")]
    public Material previewMatValid;      // 半透明绿色
    public Material previewMatInvalid;    // 半透明红色
    public bool keepPlacingAfterClick = false; // 连续放置

    // 你原有的成员（预览、占位、旋转等）
    private readonly List<Vector2Int> _bufferCells = new List<Vector2Int>();
    private int _rotIndex = 0;


    // === Compatibility shim for legacy BuildController.StartPlacing() ===
    [SerializeField] private LayerMask groundMask = ~0; // 可在 Inspector 指定地面层；默认命中所有
    [SerializeField] private float rayMaxDistance = 2000f;

    // 内部状态
    private GameObject _previewGO;
    private Renderer[] _previewRenderers; // 0..3
    //private bool _isPlacing = false;

    public GameObject PlaceAt(Vector3 worldPos)
    {
        if (SelectedBuilding == null)
        {
            TLog.Warning(this, "[BuildingPlacer] 未选择建筑。");
            return null;
        }

        // 网格对齐（与你原来的实现保持一致）
        worldPos.x = Mathf.Round(worldPos.x / gridSize) * gridSize;
        worldPos.z = Mathf.Round(worldPos.z / gridSize) * gridSize;

        // 生成一个工地对象（用你的 ConstructionSite.cs：双模式版）
        var go = new GameObject($"ConstructionSite_{SelectedBuilding.name}");
        go.transform.position = worldPos;

        var cs = go.AddComponent<ConstructionSite>();

        // ---- 保留你之前的字段赋值（兼容你旧代码）----
        cs.Asset = SelectedBuilding;                         // 兼容字段（见下方 ConstructionSite 补丁）
        cs.City = City;                                      // 兼容字段
        cs.BuildTimeSeconds = (defaultInstantSeconds > 0f)   // 兼容字段：映射到 instantBuildSeconds
            ? defaultInstantSeconds
            : cs.BuildTimeSeconds;                           // 让 ConstructionSite 自己决定（可由 Asset 覆盖）
        cs.OccupiedCells = new List<Vector2Int>(_bufferCells);
        cs.RotationIndex = _rotIndex;

        // ---- 新增：建造模式与参数 ----
        cs.Mode = defaultBuildMode;

        // 目标 Prefab（如果 ConstructionSite 里没被其他逻辑赋值，就传递 SelectedBuilding 的 Prefab）
        // 注意：如果 BuildAsset 的 Prefab 字段名不同，请把下行替换为正确的属性名
        if (cs.targetPrefab == null && SelectedBuilding.BuildPrefab != null)
            cs.targetPrefab = SelectedBuilding.BuildPrefab;

        if (defaultBuildMode == BuildMode.Instant)
        {
            // 即时模式：把默认时长也写到新字段（与兼容字段同步）
            if (defaultInstantSeconds > 0f) cs.instantBuildSeconds = defaultInstantSeconds;
            // 如果你希望从 Asset 里读“即时配方/时长”，也可以在此处补：
            // cs.instantRecipe = SelectedBuilding.instantRecipe;
            // if (SelectedBuilding.buildSeconds > 0) cs.instantBuildSeconds = SelectedBuilding.buildSeconds;
        }
        else // LogisticsAndWorkers
        {
            // 物流+工人模式：可以把默认 Recipe 与并发参数传进去
            if (defaultRecipe != null) cs.recipe = defaultRecipe;
            cs.maxConcurrentWorkers = Mathf.Max(1, logisticsMaxWorkers);
            cs.workChunkSeconds = Mathf.Max(0.5f, logisticsChunkSeconds);
        }

        // 你原来 Place 里可能还有注册到网格/地图/遮罩的逻辑，按需要在这里继续做……
        // e.g. GridManager.MarkOccupied(cs.OccupiedCells, true);

        TLog.Log(this, $"[BuildingPlacer] 放置工地：{SelectedBuilding.name} @ {worldPos} | Mode={cs.Mode}", LogColor.Cyan);
        return go;
    }


    public void StartPlacing()
    {
        if (SelectedBuilding == null)
        {
            TLog.Warning(this, "[BuildingPlacer] StartPlacing 失败：未选择建筑。");
            return;
        }
        if (City == null) City = FindObjectOfType<CityContext>();

        Vector3 pos;
        if (!TryGetMouseWorld(out pos))
        {
            // 兜底：取主摄前方某个位置（y=0）
            var cam = Camera.main;
            pos = cam != null ? cam.transform.position + cam.transform.forward * 10f : Vector3.zero;
            pos.y = 0f;
        }

        // 直接一次性落地（简化版；后续你要预览的话，把这里改成进入“放置模式”）
        PlaceAt(pos);
        TLog.Log(this, "[BuildingPlacer] StartPlacing 兼容调用：已在鼠标位置直接落地一次。", LogColor.Cyan);
    }

    private bool TryGetMouseWorld(out Vector3 world)
    {
        world = default;
        var cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, rayMaxDistance, groundMask))
        {
            world = hit.point;
            return true;
        }

        // 没有可撞到的地面：用 y=0 平面作为兜底
        if (ray.direction.y != 0f)
        {
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0f)
            {
                world = ray.origin + ray.direction * t;
                return true;
            }
        }
        return false;
    }

}
