/***************************************************************************
// File       : BuildCatalog.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 建造目录（资产）：配置可建造的建筑及其快捷键
[CreateAssetMenu(fileName = "BuildCatalog", menuName = "Game/Build Catalog")]
public class BuildCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string DisplayName;
        public BuildingAsset Asset;
        public KeyCode Hotkey = KeyCode.None;   // 可选：如 Alpha1/Alpha2...
    }

    [Header("建筑清单")]
    public Entry[] Items;
}