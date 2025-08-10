/***************************************************************************
// File       : BuildHotkeys.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 建造按键
// ***************************************************************************/

using UnityEngine;

public class BuildHotkeys : MonoBehaviour
{
    public ConstructionManager cm;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 999))
            {
                if (Input.GetKey(KeyCode.Alpha1))
                    cm.Place(BuildType.Warehouse, hit.point);
                else if (Input.GetKey(KeyCode.Alpha2))
                    cm.Place(BuildType.LumberCamp, hit.point);
                else if (Input.GetKey(KeyCode.Alpha3))
                    cm.Place(BuildType.Bakery, hit.point);
            }
        }
    }
}