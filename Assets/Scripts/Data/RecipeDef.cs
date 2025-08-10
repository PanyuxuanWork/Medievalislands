/***************************************************************************
// File       : RecipeDef.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

[System.Serializable]
public struct IO
{
    public ResourceType type;
    public int amount;
}

[CreateAssetMenu(menuName = "FL/Recipe")]
public class RecipeDef : ScriptableObject
{
    public string recipeName;
    public IO[] inputs;
    public IO[] outputs;
    public int workersRequired = 1;
    public float cycleSeconds = 5f; // 1个生产周期时长（实时秒）
}