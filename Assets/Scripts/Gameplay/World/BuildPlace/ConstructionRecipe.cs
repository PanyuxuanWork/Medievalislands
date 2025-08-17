/***************************************************************************
// File       : ConstructionRecipe.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 定义建材与施工时长
// ***************************************************************************/

using UnityEngine;

[CreateAssetMenu(fileName = "ConstructionRecipe", menuName = "Game/ConstructionRecipe")]
public class ConstructionRecipe : ScriptableObject
{
    [System.Serializable]
    public struct Cost { public ResourceType type; public int amount; }

    [Header("施工材料需求")]
    public Cost[] costs;

    [Header("基础施工时长（秒，满材料后计算）")]
    [Min(0f)] public float buildSeconds = 8f;
}
