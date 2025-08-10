/***************************************************************************
// File       : CityHUD.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

public class CityHUD : MonoBehaviour
{
    public CityContext city;

    void OnGUI()
    {
        if (city == null) return;
        GUILayout.BeginArea(new Rect(10, 10, 320, 300), GUI.skin.box);
        GUILayout.Label("City Inventory (Warehouses Sum):");
        int wood = 0, wheat = 0, bread = 0, stone = 0, iron = 0, tools = 0;

        foreach (var w in city.warehouses)
        {
            wood += w.inventory.Get(ResourceType.Wood);
            wheat += w.inventory.Get(ResourceType.Wheat);
            bread += w.inventory.Get(ResourceType.Bread);
            stone += w.inventory.Get(ResourceType.Stone);
            iron += w.inventory.Get(ResourceType.IronOre);
            tools += w.inventory.Get(ResourceType.Tools);
        }
        GUILayout.Label($"Wood: {wood}");
        GUILayout.Label($"Wheat: {wheat}");
        GUILayout.Label($"Bread: {bread}");
        GUILayout.EndArea();
    }
}
