/***************************************************************************
// File       : BuildingInfoPanel_OnGUI.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 建筑面板（IMGUI）—— 生产/仓库的明细 + 趋势图
// 依赖：IBuildingView（已分离数据层），MiniGraphUtil
// ***************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingInfoPanel_OnGUI : MonoBehaviour
{
    public bool Visible = true;
    public KeyCode ToggleKey = KeyCode.F1;
    public Rect WindowRect = new Rect(12, 12, 380, 460);

    // 趋势采样（生产：输出总量；仓库：库存总量）
    public int historyLength = 120;
    public float sampleInterval = 0.5f;
    private float _lastSampleTime = -999f;
    private readonly List<int> _totalHist = new List<int>(256);
    private int _lastTargetId = -1;

    private IBuildingView _view;

    void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Visible = !Visible;

        BuildingBase b = (BuildingSelectionService.Instance != null) ? BuildingSelectionService.Instance.Current : null;
        _view = UIViewFactory.CreateBuildingView(b);

        int id = b != null ? b.GetInstanceID() : -1;
        if (id != _lastTargetId)
        {
            _totalHist.Clear();
            _lastTargetId = id;
            _lastSampleTime = -999f;
        }

        if (_view != null && Time.time - _lastSampleTime >= sampleInterval)
        {
            _lastSampleTime = Time.time;
            // 生产：看 Output；仓库：看 Storage
            IInventoryView inv = _view.OutputInventory != null ? _view.OutputInventory : _view.StorageInventory;
            int total = inv != null ? inv.Total : 0;
            _totalHist.Add(total);
            int trim = Mathf.Max(0, _totalHist.Count - historyLength);
            if (trim > 0) _totalHist.RemoveRange(0, trim);
        }
    }

    void OnGUI()
    {
        if (!Visible) return;
        string title = _view != null ? ("Building: " + _view.DisplayName) : "No Building Selected";
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, title);
    }

    void DrawWindow(int id)
    {
        if (_view == null)
        {
            GUILayout.Label("请在场景中左键点击建筑以选中。");
            GUI.DragWindow(); return;
        }

        GUILayout.Label("状态: " + _view.State);

        // 生产建筑
        if (_view.InputInventory != null || _view.OutputInventory != null)
        {
            GUILayout.Space(6);
            GUILayout.Label("—— 生产 ——");
            GUILayout.Label("周期: " + _view.CycleSeconds + " 秒");

            GUILayout.Label("输入：");
            DrawInventoryView(_view.InputInventory);

            GUILayout.Label("输出：");
            DrawInventoryView(_view.OutputInventory);

            GUILayout.Space(6);
            GUILayout.Label("—— 产出总量趋势（~" + Mathf.RoundToInt(historyLength * sampleInterval) + "s）——");
            Rect r = GUILayoutUtility.GetRect(1, 60);
            MiniGraphUtil.DrawLineGraph(r, _totalHist, new Color(0.5f, 1f, 0.3f), new Color(0f, 0f, 0f, 0.35f));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            ProductionBuilding pb = TryGetPB();
            if (pb != null)
            {
                if (GUILayout.Button(pb.Paused ? "▶ 恢复" : "⏸ 暂停", GUILayout.Height(26)))
                {
                    pb.SetPaused(!pb.Paused);
                }
                if (GUILayout.Button("日志", GUILayout.Height(26)))
                {
                    TLog.Log(pb, "生产面板日志：输入/输出总="
                        + (_view.InputInventory != null ? _view.InputInventory.Total : 0) + "/"
                        + (_view.OutputInventory != null ? _view.OutputInventory.Total : 0));
                }
            }
            GUILayout.EndHorizontal();
        }

        // 仓库
        if (_view.StorageInventory != null)
        {
            GUILayout.Space(6);
            GUILayout.Label("—— 仓库 ——");
            WarehouseBuilding wh = TryGetWH();
            if (wh != null) GUILayout.Label("容量: " + wh.Capacity);
            DrawInventoryView(_view.StorageInventory);

            GUILayout.Space(6);
            GUILayout.Label("—— 库存总量趋势（~" + Mathf.RoundToInt(historyLength * sampleInterval) + "s）——");
            Rect r = GUILayoutUtility.GetRect(1, 60);
            MiniGraphUtil.DrawLineGraph(r, _totalHist, new Color(1f, 0.8f, 0.2f), new Color(0f, 0f, 0f, 0.35f));
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("F1 显示/隐藏  |  右键清除选择");
        GUI.DragWindow();
    }

    private void DrawInventoryView(IInventoryView inv)
    {
        if (inv == null) { GUILayout.Label("  (无)"); return; }
        Array values = Enum.GetValues(typeof(ResourceType));
        int shown = 0;
        for (int i = 0; i < values.Length; i++)
        {
            ResourceType t = (ResourceType)values.GetValue(i);
            int c = inv.Get(t);
            if (c > 0)
            {
                GUILayout.Label("  " + t + " x" + c);
                shown++;
            }
        }
        if (shown == 0) GUILayout.Label("  (空)");
    }

    private ProductionBuilding TryGetPB()
    {
        BuildingBase b = (BuildingSelectionService.Instance != null) ? BuildingSelectionService.Instance.Current : null;
        return b != null ? b.GetComponent<ProductionBuilding>() : null;
    }

    private WarehouseBuilding TryGetWH()
    {
        BuildingBase b = (BuildingSelectionService.Instance != null) ? BuildingSelectionService.Instance.Current : null;
        return b != null ? b.GetComponent<WarehouseBuilding>() : null;
    }
}
