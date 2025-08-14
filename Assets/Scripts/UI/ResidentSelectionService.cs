/***************************************************************************
// File       : ResidentSelectionService.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

/***************************************************************************
// File       : ResidentSelectionService.cs
// Description: 居民选择单例（若你已有同名文件可保留现实现）
// ***************************************************************************/

using UnityEngine;

public class ResidentSelectionService : MonoSingleton<ResidentSelectionService>
{
    public Resident Current;

    public void Select(Resident r)
    {
        Current = r;
        if (r != null) TLog.Log(this, "选中居民：" + r.name);
    }

    public void Clear()
    {
        Current = null;
    }
}
