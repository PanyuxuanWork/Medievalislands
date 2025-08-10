/***************************************************************************
// File       : BuildingSelectionService.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 建筑选择服务（单例）：保存当前选中的建筑，供 UI 查询与操作
public class BuildingSelectionService : MonoSingleton<BuildingSelectionService>
{
    [Header("Runtime")]
    public BuildingBase Current;

    public void Select(BuildingBase b)
    {
        Current = b;
        if (b != null) TLog.Log(this, "选中建筑：" + b.name);
    }

    public void Clear()
    {
        Current = null;
    }
}
