/***************************************************************************
// File       : BuildSelector.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using Sirenix.OdinInspector;

// [TODO] 建筑选择器：摄像机射线选中 BuildingBase，左键选中，右键清除
public class BuildingSelector : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode PickKey = KeyCode.Mouse0;
    public KeyCode ClearKey = KeyCode.Mouse1;

    [Header("Cache")]
    public Camera Cam;

    private void Awake()
    {
        if (Cam == null) Cam = Camera.main;
    }

    private void Update()
    {
        if (Cam == null) return;

        if (Input.GetKeyDown(PickKey))
        {
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1024f))
            {
                BuildingBase bb = hit.collider.GetComponentInParent<BuildingBase>();
                if (bb != null)
                {
                    BuildingSelectionService.Instance.Select(bb);
                }
            }
        }

        if (Input.GetKeyDown(ClearKey))
        {
            BuildingSelectionService.Instance.Clear();
        }
    }

    [Button("清空选择")]
    private void BtnClear()
    {
        BuildingSelectionService.Instance.Clear();
    }
}
