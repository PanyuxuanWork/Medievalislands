/***************************************************************************
// File       : GridManager.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 网格管理器：
// 作用：提供“当前使用”的 GridAsset（分区网格），供放置/校验统一取用。
// 后续可扩展：根据玩家所在区域自动切换当前 GridAsset。
public class GridManager : MonoSingleton<GridManager>
{
    [Header("Active Grid")]
    public GridAsset ActiveGrid;  // 当前生效的网格区域

    public static GridAsset GetGrid()
    {
        return Instance != null ? Instance.ActiveGrid : null;
    }
}
