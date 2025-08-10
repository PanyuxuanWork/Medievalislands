/***************************************************************************
// File       : ConstructionSite.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 施工现场（ConstructionSite）：
// 负责建筑从“占位→扣料→计时施工→完工替换成成品”的全过程管理。
// 要点：
// 1) 放置器会把占位格列表（OccupiedCells）与旋转索引（RotationIndex）传进来；
// 2) Start 时先从 City 仓储扣除建造成本（CityEconomy.TryConsume）；不足则释放占位并取消；
// 3) Update 按 BuildTimeSeconds 计时；完工后生成成品建筑（Asset.BuildPrefab）；
// 4) 占位格不释放，而是“转交”给成品建筑（BuildingBase.OccupiedCells）；
// 5) 若中途取消/失败/被销毁，释放占位格（把每个格的 BuildStatus 设回 Buildable）。
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
public class ConstructionSite : MonoBehaviour
{
    [Header("Refs")]
    public BuildingAsset Asset;
    public CityContext City;

    [Header("Build")]
    [Min(0f)] public float BuildTimeSeconds = 0f;

    [Tooltip("放置器传入的占位格")]
    public List<Vector2Int> OccupiedCells = new List<Vector2Int>();

    [Tooltip("放置器传入的旋转索引 0/1/2/3（对应 0/90/180/270°）")]
    public int RotationIndex = 0;

    private float _accSeconds = 0f;
    private bool _started = false;
    private bool _transferred = false; // 占位是否已转交给成品

    private void Start()
    {
        if (City == null) City = GameObject.FindObjectOfType<CityContext>();
        if (Asset == null || City == null)
        {
            TLog.Warning("ConstructionSite: Asset/City 为空，取消建造。");
            ReleaseOccupiedCells();
            Destroy(gameObject);
            return;
        }

        // ★ 优先使用模板时长，其次用本地字段，最后兜底 3f
        float dur = 3f;
        if (Asset.BuildTimeSeconds > 0f) dur = Asset.BuildTimeSeconds;
        else if (BuildTimeSeconds > 0f) dur = BuildTimeSeconds;
        BuildTimeSeconds = dur;

        bool ok = CityEconomy.TryConsume(City, Asset.BuildCosts);
        if (!ok)
        {
            TLog.Warning("ConstructionSite: 资源不足，无法开工。");
            ReleaseOccupiedCells();
            Destroy(gameObject);
            return;
        }

        _started = true;
        transform.rotation = Quaternion.Euler(0f, 90f * (RotationIndex & 3), 0f);
        TLog.Log(this, "施工开始，耗时(秒) = " + BuildTimeSeconds); ;
    }

    private void Update()
    {
        if (!_started) return;

        _accSeconds += Time.deltaTime;
        if (_accSeconds >= BuildTimeSeconds)
        {
            CompleteConstruction();
        }
    }

    private void CompleteConstruction()
    {
        if (Asset.BuildPrefab == null)
        {
            TLog.Error("ConstructionSite: BuildPrefab 未设置，无法完工。");
            CancelConstruction();
            return;
        }

        GameObject go = GameObject.Instantiate(Asset.BuildPrefab, transform.position, transform.rotation);
        BuildingBase bb = go.GetComponent<BuildingBase>();
        if (bb != null)
        {
            bb.Init(Asset, City);
            bb.OccupiedCells = new List<Vector2Int>(OccupiedCells); // 占位转交
            bb.Activate(); // 统一在基类里完成登记
            _transferred = true;
        }

        TLog.Log(this, "施工完成 → 成品生成。");
        Destroy(gameObject);
    }

    [Button("取消建造")]
    public void CancelConstruction()
    {
        if (!_transferred) ReleaseOccupiedCells();
        TLog.Warning(this, "施工取消。");
        Destroy(gameObject);
    }

    [Button("立即完工")]
    public void FinishInstant()
    {
        if (!_started)
        {
            Start();
            if (!_started) return;
        }
        _accSeconds = BuildTimeSeconds;
        CompleteConstruction();
    }

    private void OnDestroy()
    {
        if (!_transferred)
        {
            ReleaseOccupiedCells();
        }
    }

    private void ReleaseOccupiedCells()
    {
        GridAsset grid = GridManager.GetGrid();
        if (grid == null || OccupiedCells == null || OccupiedCells.Count == 0) return;

        for (int i = 0; i < OccupiedCells.Count; i++)
        {
            Vector2Int c = OccupiedCells[i];
            if (!grid.InBounds(c)) continue;
            GridCell cell = grid.GetCell(c);
            if (cell != null) cell.BuildStatus = BuildStatus.Buildable;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(grid);
#endif
    }
}




