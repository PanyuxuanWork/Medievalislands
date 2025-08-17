/***************************************************************************
// File       : TimeHUD.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 示例：时间 HUD（放 HUD 层，Singleton）
using UI.UGUI;
using UnityEngine;
using UnityEngine.UI;

public class TimeHUD : UIElement
{
    public Text Label;

    private void Reset()
    {
        Layer = UILayer.HUD; IsSingleton = true; IsModal = false;
    }

    private void OnEnable()
    {
        GlobalTime.OnMinute += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GlobalTime.OnMinute -= Refresh;
    }

    public override void OnOpen(object args)
    {
        base.OnOpen(args);
        Refresh();
    }

    private void Refresh()
    {
        if (Label == null) return;
        string s = GlobalTime.Instance != null ? GlobalTime.Instance.GetTimeString() : "--";
        Label.text = s;
    }
}