/***************************************************************************
// File       : SelectionInput.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

/***************************************************************************
// File       : SelectionInput.cs
// Description: 鼠标点选（建筑 / 居民），右键清除（若你已有此脚本，可保留现实现）
// ***************************************************************************/

using UnityEngine;

public class SelectionInput : MonoBehaviour
{
    public float maxDistance = 200f;
    public LayerMask hitMask = ~0;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (BuildingSelectionService.Instance != null) BuildingSelectionService.Instance.Clear();
            if (ResidentSelectionService.Instance != null) ResidentSelectionService.Instance.Clear();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, hitMask))
            {
                BuildingBase b = hit.collider.GetComponentInParent<BuildingBase>();
                if (b != null && BuildingSelectionService.Instance != null)
                {
                    BuildingSelectionService.Instance.Select(b);
                    return;
                }
                Resident r = hit.collider.GetComponentInParent<Resident>();
                if (r != null && ResidentSelectionService.Instance != null)
                {
                    ResidentSelectionService.Instance.Select(r);
                    return;
                }
            }
        }
    }
}
