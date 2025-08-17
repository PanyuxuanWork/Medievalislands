/***************************************************************************
// File       : GameBootstrap.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using Core;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] CityContext cityPrefab;

    void Start()
    {
        if (FindObjectOfType<TickSystem>() == null)
            new GameObject("TickSystem").AddComponent<TickSystem>();

        if (FindObjectOfType<CityContext>() == null)
        {
            var city = Instantiate(cityPrefab);
            city.name = "City_Main";
        }
    }
}
