/***************************************************************************
// File       : ResourceDef.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

[CreateAssetMenu(menuName = "FL/Resource")]
public class ResourceDef : ScriptableObject
{
    public ResourceType type;
    public string displayName;
    public Sprite icon;
    public float stackSize = 100f; // 逻辑用，简单化
}
