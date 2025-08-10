/***************************************************************************
// File       : GridPreviewer.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;

// [TODO] 网格预览器（场景可视化）：
// 作用：在 Scene 视图中渲染一个 GridAsset 的格子轮廓与填充色。
// 特性：可选显示索引；根据 BuildStatus/TerrainType 着色；大网格自动抽样防卡顿。
[ExecuteAlways]
public class GridPreviewer : MonoBehaviour
{
    [Header("Asset")]
    public GridAsset Asset;

    [Header("Draw Options")]
    public bool ShowCells = true;              // 是否绘制格子
    public bool FillCells = true;              // 是否填充半透明色
    public bool ShowIndices = false;           // 是否显示格子索引（列,行）
    [Range(0.01f, 1f)] public float LineAlpha = 0.6f;
    [Range(0.01f, 0.6f)] public float FillAlpha = 0.15f;
    public float Elevation = 0.02f;            // 提升到地面上方一点，避免与地面 ZFighting

    [Header("Performance")]
    [Tooltip("当格子数超过该阈值时，采用抽样步进绘制（每 step 个格子绘制一个），以防编辑器卡顿")]
    public int MaxDrawCells = 8000;
    public int StepWhenLarge = 2;

    private static readonly Color kLineColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color kBuildable = new Color(0.2f, 1f, 0.2f, 1f);
    private static readonly Color kOccupied = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color kRestricted = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color kOcean = new Color(0.2f, 0.6f, 1f, 1f);
    private static readonly Color kRiver = new Color(0.2f, 0.9f, 1f, 1f);
    private static readonly Color kLand = new Color(0.5f, 0.35f, 0.05f, 1f);
    private static readonly Color kBeach = new Color(0.95f, 0.9f, 0.6f, 1f);
    private static readonly Color kMountain = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void OnDrawGizmos()
    {
        if (!ShowCells) return;
        if (Asset == null || Asset.Cells == null || Asset.Cells.Count == 0) return;

        int total = Asset.Cells.Count;
        int step = (total > MaxDrawCells) ? Mathf.Max(1, StepWhenLarge) : 1;

        for (int i = 0; i < total; i += step)
        {
            GridCell cell = Asset.Cells[i];
            if (cell == null) continue;

            // 取角点并抬高到地面上方
            Vector3 tl = cell.GetTopLeft(); tl.y += Elevation;
            Vector3 tr = cell.GetTopRight(); tr.y += Elevation;
            Vector3 bl = cell.GetBottomLeft(); bl.y += Elevation;
            Vector3 br = cell.GetBottomRight(); br.y += Elevation;

            // 线框
            Gizmos.color = new Color(kLineColor.r, kLineColor.g, kLineColor.b, LineAlpha);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);

            // 填充色：优先按 BuildStatus，再按 Terrain 着色
            if (FillCells)
            {
                Color baseCol;
                if (cell.BuildStatus == BuildStatus.Occupied) baseCol = kOccupied;
                else if (cell.BuildStatus == BuildStatus.Restricted) baseCol = kRestricted;
                else
                {
                    // Buildable：按地形色系
                    switch (cell.TerrainType)
                    {
                        case TerrainType.Ocean: baseCol = kOcean; break;
                        case TerrainType.River: baseCol = kRiver; break;
                        case TerrainType.Beach: baseCol = kBeach; break;
                        case TerrainType.Mountain: baseCol = kMountain; break;
                        case TerrainType.Land:
                        default: baseCol = kLand; break;
                    }
                }

                Color fill = new Color(baseCol.r, baseCol.g, baseCol.b, FillAlpha);
                Gizmos.color = fill;

                // 用一个很薄的立方体近似填充
                Vector3 center = cell.WorldCenter;
                center.y += Elevation;
                Vector3 size = new Vector3(cell.Width, 0.001f, cell.Height);
                Gizmos.DrawCube(center, size);
            }
        }
    }
}
