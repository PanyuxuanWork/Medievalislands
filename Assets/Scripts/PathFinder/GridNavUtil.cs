/***************************************************************************
// File       : GridNavUtil.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 网格寻路
// ***************************************************************************/

using UnityEngine;

public static class GridNavUtil
{
    public static bool TryWorldToCell(Vector3 world, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        GridAsset grid = GridManager.GetGrid();
        if (grid == null) return false;
        cell = grid.WorldToCell(world);
        return grid.InBounds(cell);
    }

    public static Vector3 CellCenterToWorld(Vector2Int cell)
    {
        GridAsset grid = GridManager.GetGrid();
        if (grid == null) return Vector3.zero;
        return grid.CellToWorldCenter(cell);
    }

    public static bool IsWalkable(Vector2Int cell)
    {
        GridAsset grid = GridManager.GetGrid();
        if (grid == null) return false;
        if (!grid.InBounds(cell)) return false;
        GridCell c = grid.GetCell(cell);
        if (c == null) return false;
        return c.BuildStatus == BuildStatus.Buildable; // 简单：可建即可走
    }
}
