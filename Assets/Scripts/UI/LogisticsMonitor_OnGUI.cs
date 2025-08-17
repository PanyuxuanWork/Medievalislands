/***************************************************************************
// File       : LogisticsMonitor_OnGUI.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Desc : 轻量调度监控面板（F3 开关）：Pending / Assigned / InFlight
//***************************************************************************/

using UnityEngine;

public class LogisticsMonitor_OnGUI : MonoBehaviour
{
    public bool Visible = true;
    public KeyCode ToggleKey = KeyCode.F3;
    public Rect WindowRect = new Rect(760, 12, 360, 220);

    private LogisticsRequestDispatcher _dsp;

    void Start() { _dsp = FindObjectOfType<LogisticsRequestDispatcher>(); }

    void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Visible = !Visible;
        if (_dsp == null) _dsp = FindObjectOfType<LogisticsRequestDispatcher>();
    }

    void OnGUI()
    {
        if (!Visible) return;
        string title = _dsp != null ? "Logistics Monitor" : "Logistics Monitor (No Dispatcher)";
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, title);
    }

    void DrawWindow(int id)
    {
        if (_dsp == null)
        {
            GUILayout.Label("找不到 LogisticsRequestDispatcher。");
            GUI.DragWindow(); return;
        }

        GUILayout.Label("Pending:  " + _dsp.PendingCount);
        GUILayout.Label("Assigned: " + _dsp.AssignedCount);

        GUILayout.Space(6);
        GUILayout.Label("In-Flight by Resource:");
        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
        {
            int v = _dsp.InFlight(t);
            if (v > 0) GUILayout.Label("  " + t + ": " + v);
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("F3 显示/隐藏");
        GUI.DragWindow();
    }
}
