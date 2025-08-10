/***************************************************************************
// File       : GridCell.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 网格单元：
// 表示一个以世界坐标为“中心点”的网格格子（平面为 XZ）。
// 提供：中心、四角坐标、AABB 边界、地形/建造状态、可建判断。
public enum TerrainType { Land, Ocean, Beach, Mountain, River }
public enum BuildStatus { Buildable, Occupied, Restricted }

[System.Serializable]
public class GridCell
{
    [Header("定位")]
    public Vector2Int GridPosition;   // 网格坐标（列x，行y）
    public Vector3 WorldCenter;       // 世界坐标中的中心点（y 通常为地表高度）

    [Header("尺寸")]
    public float Width;               // X 方向长度（米）
    public float Height;              // Z 方向长度（米）

    [Header("属性")]
    public TerrainType TerrainType;
    public BuildStatus BuildStatus;
    public Color CellColor;

    public GridCell() { }

    public GridCell(Vector2Int gridPos, Vector3 worldCenter, float width, float height)
    {
        GridPosition = gridPos;
        WorldCenter = worldCenter;
        Width = width;
        Height = height;
        TerrainType = TerrainType.Land;
        BuildStatus = BuildStatus.Buildable;
        CellColor = GetDefaultColor(TerrainType);
    }

    public GridCell(Vector2Int gridPos, Vector3 worldCenter, float width, float height,
                    TerrainType terrainType, BuildStatus buildStatus, Color color)
    {
        GridPosition = gridPos;
        WorldCenter = worldCenter;
        Width = width;
        Height = height;
        TerrainType = terrainType;
        BuildStatus = buildStatus;
        CellColor = color;
    }

    // —— 工具：四角与 AABB（基于 XZ 平面）——

    /// <summary> 左上角（相对世界+X 向右，+Z 向前；“左上”指 -X、+Z 角） </summary>
    public Vector3 GetTopLeft()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x - hx, WorldCenter.y, WorldCenter.z + hz);
    }

    /// <summary> 右上角（+X、+Z） </summary>
    public Vector3 GetTopRight()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x + hx, WorldCenter.y, WorldCenter.z + hz);
    }

    /// <summary> 左下角（-X、-Z） </summary>
    public Vector3 GetBottomLeft()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x - hx, WorldCenter.y, WorldCenter.z - hz);
    }

    /// <summary> 右下角（+X、-Z） </summary>
    public Vector3 GetBottomRight()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x + hx, WorldCenter.y, WorldCenter.z - hz);
    }

    /// <summary> AABB 最小角（X-, Z-） </summary>
    public Vector3 GetMin()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x - hx, WorldCenter.y, WorldCenter.z - hz);
    }

    /// <summary> AABB 最大角（X+, Z+） </summary>
    public Vector3 GetMax()
    {
        float hx = Width * 0.5f;
        float hz = Height * 0.5f;
        return new Vector3(WorldCenter.x + hx, WorldCenter.y, WorldCenter.z + hz);
    }

    // —— 可建判断 —— 

    public bool IsBuildable()
    {
        // 海洋/河流禁建，状态需是可建
        if (BuildStatus != BuildStatus.Buildable) return false;
        if (TerrainType == TerrainType.Ocean) return false;
        if (TerrainType == TerrainType.River) return false;
        return true;
    }

    public void SetState(BuildStatus state)
    {
        BuildStatus = state;
    }

    // —— 颜色映射（用于调试/可视化）——

    private static Color GetDefaultColor(TerrainType type)
    {
        // 用传统 switch，兼容性更好
        switch (type)
        {
            case TerrainType.Ocean: return new Color(0.00f, 0.50f, 1.00f, 0.30f);
            case TerrainType.Land: return new Color(0.50f, 0.35f, 0.05f, 0.30f);
            case TerrainType.River: return new Color(0.00f, 0.80f, 1.00f, 0.30f);
            case TerrainType.Beach: return new Color(0.95f, 0.90f, 0.60f, 0.30f);
            case TerrainType.Mountain: return new Color(0.40f, 0.40f, 0.40f, 0.30f);
            default: return Color.white;
        }
    }
}
