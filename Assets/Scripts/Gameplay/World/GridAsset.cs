/***************************************************************************
// File       : GridAsset.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

// [TODO] 分区网格资产（ScriptableObject）：
// 描述一个“区域”的网格信息：原点、格子尺寸、行列、以及该区域下的所有 GridCell。
// 用法：一个区域一个 GridAsset；编辑器按钮一键重建格子；提供坐标换算与占位校验。
[CreateAssetMenu(fileName = "GridAsset", menuName = "Game/GridAsset")]
public class GridAsset : ScriptableObject
{
    [Header("定义")]
    [Tooltip("区域原点（世界坐标），通常为本区域左下角或任意参照点")]
    public Vector3 Origin = Vector3.zero;

    [Tooltip("每个格子的世界尺寸（X 用 Width，Z 用 Height）")]
    [Min(0.01f)]
    public float CellWidth = 2f;

    [Min(0.01f)]
    public float CellHeight = 2f;

    [Tooltip("列数（X 方向）")]
    [Min(1)]
    public int Columns = 32;

    [Tooltip("行数（Y 方向，对应 Z 方向）")]
    [Min(1)]
    public int Rows = 32;

    [Header("数据")]
    [Tooltip("本区域内的格子（行×列）。注意：这是静态模板快照，可在运行时复制到内存使用。")]
    public List<GridCell> Cells = new List<GridCell>();

    // —— 运行时辅助：避免频繁 new —— 
    [System.NonSerialized] private List<Vector2Int> _tmpCells = new List<Vector2Int>();

    // === 编辑器工具 ===

    [Button("重建格子（清空并生成）", ButtonSizes.Medium)]
    public void RebuildGrid()
    {
        Cells.Clear();
        Cells.Capacity = Columns * Rows;

        float halfW = CellWidth * 0.5f;
        float halfH = CellHeight * 0.5f;

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                // 以中心点布局（XZ 平面）
                float cx = Origin.x + (c * CellWidth) + halfW;
                float cz = Origin.z + (r * CellHeight) + halfH;
                Vector3 center = new Vector3(cx, Origin.y, cz);

                Vector2Int gridPos = new Vector2Int(c, r);
                GridCell cell = new GridCell(gridPos, center, CellWidth, CellHeight);
                Cells.Add(cell);
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [Button("清空格子", ButtonSizes.Small)]
    public void ClearGrid()
    {
        Cells.Clear();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // === 坐标换算 ===

    /// <summary> 世界坐标 → 网格坐标（落在本区域内）。超出则返回最近格范围外的坐标（需先用 InBounds 判断） </summary>
    public Vector2Int WorldToCell(Vector3 world)
    {
        float localX = world.x - Origin.x;
        float localZ = world.z - Origin.z;

        int c = Mathf.FloorToInt(localX / Mathf.Max(CellWidth, 0.0001f));
        int r = Mathf.FloorToInt(localZ / Mathf.Max(CellHeight, 0.0001f));
        return new Vector2Int(c, r);
    }

    /// <summary> 网格坐标 → 世界中心点 </summary>
    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        float cx = Origin.x + (cell.x * CellWidth) + CellWidth * 0.5f;
        float cz = Origin.z + (cell.y * CellHeight) + CellHeight * 0.5f;
        return new Vector3(cx, Origin.y, cz);
    }

    /// <summary> 该网格坐标是否在本区域边界内 </summary>
    public bool InBounds(Vector2Int cell)
    {
        if (cell.x < 0 || cell.y < 0) return false;
        if (cell.x >= Columns || cell.y >= Rows) return false;
        return true;
    }

    /// <summary> 取单元格（越界返回 null） </summary>
    public GridCell GetCell(Vector2Int cell)
    {
        if (!InBounds(cell)) return null;
        int index = cell.y * Columns + cell.x;
        if (index < 0 || index >= Cells.Count) return null;
        return Cells[index];
    }

    // === 占位相关（正交旋转 0/90/180/270） ===

    /// <summary> 根据 Footprint 和旋转（0..3）计算实际占位尺寸 </summary>
    public static Vector2Int RotateFootprint(Vector2Int size, int rotIndex)
    {
        int i = ((rotIndex % 4) + 4) % 4;
        if (i % 2 == 0) return size;              // 0 或 180：不变
        return new Vector2Int(size.y, size.x);    // 90 或 270：宽高对调
    }

    /// <summary> 获取从 baseCell 开始，占位 size 的所有格坐标（不越界校验） </summary>
    public void GetAreaCells(Vector2Int baseCell, Vector2Int size, List<Vector2Int> buffer)
    {
        buffer.Clear();
        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                buffer.Add(new Vector2Int(baseCell.x + dx, baseCell.y + dy));
            }
        }
    }

    /// <summary> 区域可建校验：边界内 + 每格 IsBuildable + 未被标记占用 </summary>
    public bool IsAreaBuildable(Vector2Int baseCell, Vector2Int size)
    {
        GetAreaCells(baseCell, size, _tmpCells);
        for (int i = 0; i < _tmpCells.Count; i++)
        {
            Vector2Int c = _tmpCells[i];
            if (!InBounds(c)) return false;

            GridCell cell = GetCell(c);
            if (cell == null) return false;
            if (!cell.IsBuildable()) return false;
        }
        return true;
    }

    /// <summary> 将区域状态标记为占用（或还原为可建） </summary>
    public void MarkArea(Vector2Int baseCell, Vector2Int size, BuildStatus state)
    {
        GetAreaCells(baseCell, size, _tmpCells);
        for (int i = 0; i < _tmpCells.Count; i++)
        {
            Vector2Int c = _tmpCells[i];
            if (!InBounds(c)) continue;
            GridCell cell = GetCell(c);
            if (cell != null) cell.BuildStatus = state;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
