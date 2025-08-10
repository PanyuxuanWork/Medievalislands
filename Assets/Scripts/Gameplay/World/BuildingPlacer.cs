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
    [Header("Refs")]
    public CityContext City;
    public BuildingAsset SelectedBuilding;

    [Header("Preview Materials")]
    public Material OkMaterial;       // 绿色透明材质（URP Unlit/Transparent）
    public Material BlockMaterial;    // 红色透明材质（URP Unlit/Transparent）

    [Header("Controls")]
    public KeyCode RotateCWKey = KeyCode.E;        // 顺时针  +90°
    public KeyCode RotateCCWKey = KeyCode.Q;        // 逆时针  -90°
    public KeyCode CancelKey = KeyCode.Escape;   // 取消

    private GameObject _preview;
    private Renderer[] _previewRenderers;
    private bool _placing = false;

    private int _rotIndex = 0; // 0/1/2/3 → 0/90/180/270°
    private readonly List<Vector2Int> _bufferCells = new List<Vector2Int>();

    // 预览辅助组件（懒加载）
    private PlacementOutline _outline;
    private PlacementFill _fill;

    [Button(ButtonSizes.Large)]
    public void StartPlacing()
    {
        if (SelectedBuilding == null || SelectedBuilding.PreviewPrefab == null)
        {
            TLog.Warning("BuildingPlacer: 未选择建筑或 PreviewPrefab 为空。");
            return;
        }

        if (City == null)
        {
            City = GameObject.FindObjectOfType<CityContext>();
        }

        GridAsset grid = GridManager.GetGrid();
        if (grid == null)
        {
            TLog.Error("BuildingPlacer: 缺少有效的 GridAsset（请在场景中放置 GridManager 并设置 ActiveGrid）。");
            return;
        }

        _placing = true;
        _rotIndex = 0;

        _preview = GameObject.Instantiate(SelectedBuilding.PreviewPrefab);
        _previewRenderers = _preview.GetComponentsInChildren<Renderer>(true);

        // 旋转到初始朝向
        ApplyPreviewRotation();

        // —— 附加/获取预览辅助组件 —— 
        _outline = _preview.GetComponent<PlacementOutline>();
        if (_outline == null)
        {
            _outline = _preview.AddComponent<PlacementOutline>();
        }

        _fill = _preview.GetComponent<PlacementFill>();
        if (_fill == null)
        {
            _fill = _preview.AddComponent<PlacementFill>();
        }
        // 让填充用与放置器一致的材质
        _fill.OkMaterial = OkMaterial;
        _fill.BlockMaterial = BlockMaterial;
    }

    private void Update()
    {
        if (!_placing || _preview == null) return;

        GridAsset grid = GridManager.GetGrid();
        if (grid == null) return;

        // —— 鼠标吸附到网格中心 ——
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 512f))
        {
            Vector2Int cell = grid.WorldToCell(hit.point);
            Vector3 center = grid.CellToWorldCenter(cell);
            _preview.transform.position = center;
        }

        // —— 旋转控制（Q/E） ——
        if (Input.GetKeyDown(RotateCWKey))
        {
            _rotIndex = (_rotIndex + 1) & 3;
            ApplyPreviewRotation();
        }
        else if (Input.GetKeyDown(RotateCCWKey))
        {
            _rotIndex = (_rotIndex + 3) & 3;
            ApplyPreviewRotation();
        }

        // —— 校验：旋转后的 footprint + 区域可建 + 资源足够 ——
        Vector2Int baseCell = grid.WorldToCell(_preview.transform.position);
        Vector2Int rotatedFootprint = GridAsset.RotateFootprint(SelectedBuilding.Footprint, _rotIndex);

        bool areaOK = grid.IsAreaBuildable(baseCell, rotatedFootprint);
        bool moneyOK = CityEconomy.CanAfford(City, SelectedBuilding.BuildCosts);
        bool placeable = areaOK && moneyOK;

        ApplyPreviewMaterial(placeable ? OkMaterial : BlockMaterial);

        // —— 生成格子四角，驱动轮廓与底部填充 —— 
        // 收集占位格
        _bufferCells.Clear();
        grid.GetAreaCells(baseCell, rotatedFootprint, _bufferCells);

        // 计算四角
        List<Vector3[]> cellRects = new List<Vector3[]>(_bufferCells.Count);
        for (int i = 0; i < _bufferCells.Count; i++)
        {
            GridCell c = grid.GetCell(_bufferCells[i]);
            if (c == null) continue;
            Vector3 tl = c.GetTopLeft();
            Vector3 tr = c.GetTopRight();
            Vector3 br = c.GetBottomRight();
            Vector3 bl = c.GetBottomLeft();
            cellRects.Add(new Vector3[4] { tl, tr, br, bl });
        }

        // 轮廓
        if (_outline != null)
        {
            _outline.Placeable = placeable;
            _outline.CellRects = cellRects; // 直接引用（编辑器绘制只读）
        }
        // 底部填充
        if (_fill != null)
        {
            _fill.Rebuild(cellRects, placeable);
        }

        // —— 左键落地：占坑 + 生成施工点（传 OccupiedCells 与 RotationIndex） ——
        if (Input.GetMouseButtonDown(0))
        {
            if (!placeable)
            {
                TLog.Warning("区域不可建或资源不足。");
            }
            else
            {
                // 占坑
                grid.MarkArea(baseCell, rotatedFootprint, BuildStatus.Occupied);

                // 生成施工点
                GameObject site = new GameObject(SelectedBuilding.Name + "_ConstructionSite");
                site.transform.position = _preview.transform.position;
                site.transform.rotation = Quaternion.Euler(0f, 90f * (_rotIndex & 3), 0f);

                ConstructionSite cs = site.AddComponent<ConstructionSite>();
                cs.Asset = SelectedBuilding;
                cs.City = City;
                cs.BuildTimeSeconds = 3f; // 具体时长在 ConstructionSite.Start() 会优先用 Asset.BuildTimeSeconds
                cs.OccupiedCells = new List<Vector2Int>(_bufferCells);
                cs.RotationIndex = _rotIndex;

                CancelPlacing();
            }
        }

        // —— 右键或 Esc 取消 —— 
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(CancelKey))
        {
            CancelPlacing();
        }
    }

    private void ApplyPreviewRotation()
    {
        if (_preview == null) return;
        float y = 90f * (_rotIndex & 3);
        _preview.transform.rotation = Quaternion.Euler(0f, y, 0f);
    }

    private void ApplyPreviewMaterial(Material mat)
    {
        if (_previewRenderers == null || mat == null) return;
        for (int i = 0; i < _previewRenderers.Length; i++)
        {
            Renderer r = _previewRenderers[i];
            if (r != null) r.sharedMaterial = mat;
        }
    }

    private void CancelPlacing()
    {
        _placing = false;
        if (_preview != null) GameObject.Destroy(_preview);
        _preview = null;
        _previewRenderers = null;
        _outline = null;
        _fill = null;
    }
}
