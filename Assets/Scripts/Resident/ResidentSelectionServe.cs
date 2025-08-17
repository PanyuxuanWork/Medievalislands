/***************************************************************************
// File       : ResidentSelectionServe.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using UnityEngine.EventSystems;

public class ResidentSelectionServe : MonoBehaviour, IPointerClickHandler
{
    public Resident Target;

    private void Awake()
    {
        if (Target == null) Target = GetComponent<Resident>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Target == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // TODO：弹出你的面板并显示 Target.CurrentTaskName、Target.Stats 等
            Debug.Log("Resident Selected: " + Target.name + ", Task=" + Target.CurrentTaskName);
        }
    }
}
