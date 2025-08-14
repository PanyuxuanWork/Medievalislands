/***************************************************************************
// File       : ResidentInfoPanel.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 居民面板（IMGUI）—— 显示当前任务、任务队列、背包趋势（最近60秒）
// 依赖：IResidentView（已分离数据层），MiniGraphUtil
// ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ResidentInfoPanel : MonoBehaviour
{
    public bool Visible = true;
    public KeyCode ToggleKey = KeyCode.F2;
    public Rect WindowRect = new Rect(390, 12, 380, 360);

    // 趋势采样（以“背包总量”为指标）
    public int historyLength = 120;          // 样本点个数（配合采样间隔即可约等于时长）
    public float sampleInterval = 0.5f;      // 每 0.5s 采样一次 → 120 点 ≈ 60 秒
    private float _lastSampleTime = -999f;
    private readonly List<int> _totalHist = new List<int>(256);
    private int _lastTargetId = -1;

    private IResidentView _view;
    private ResidentAI _ai;                  // 为任务队列反射

    void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Visible = !Visible;

        Resident r = (ResidentSelectionService.Instance != null) ? ResidentSelectionService.Instance.Current : null;
        _view = UIViewFactory.CreateResidentView(r);
        _ai = (r != null) ? r.GetComponent<ResidentAI>() : null;

        // 切换目标时重置趋势
        int id = r != null ? r.GetInstanceID() : -1;
        if (id != _lastTargetId)
        {
            _totalHist.Clear();
            _lastTargetId = id;
            _lastSampleTime = -999f;
        }

        // 采样背包总量
        if (_view != null && Time.time - _lastSampleTime >= sampleInterval)
        {
            _lastSampleTime = Time.time;
            int total = _view.Inventory != null ? _view.Inventory.Total : 0;
            _totalHist.Add(total);
            // 限制长度
            int trim = Mathf.Max(0, _totalHist.Count - historyLength);
            if (trim > 0) _totalHist.RemoveRange(0, trim);
        }
    }

    void OnGUI()
    {
        if (!Visible) return;
        string title = _view != null ? ("Resident: " + _view.DisplayName) : "No Resident Selected";
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, title);
    }

    void DrawWindow(int id)
    {
        if (_view == null)
        {
            GUILayout.Label("请在场景中左键点击居民以选中。");
            GUI.DragWindow(); return;
        }

        GUILayout.Label("位置: " + _view.Position.ToString("F2"));
        GUILayout.Label("移动: " + (_view.IsMoving ? "Moving" : "Idle"));

        GUILayout.Space(6);
        GUILayout.Label("—— 当前任务 ——");
        GUILayout.Label(_view.CurrentTaskSummary);

        GUILayout.Space(6);
        GUILayout.Label("—— 任务队列（最多显示 6 条）——");
        DrawTaskQueue(_ai, 6);

        GUILayout.Space(6);
        GUILayout.Label("—— 携带 ——");
        DrawInventoryView(_view.Inventory);

        GUILayout.Space(6);
        GUILayout.Label("—— 背包总量趋势（~" + Mathf.RoundToInt(historyLength * sampleInterval) + "s）——");
        Rect r = GUILayoutUtility.GetRect(1, 60);
        MiniGraphUtil.DrawLineGraph(r, _totalHist, new Color(0.2f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.35f));

        GUILayout.FlexibleSpace();
        GUILayout.Label("F2 显示/隐藏  |  右键清除选择");
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

    /// <summary>通过反射尽力枚举 ResidentAI 的排队任务（支持 List/Queue/IEnumerable）</summary>
    private void DrawTaskQueue(ResidentAI ai, int maxShow)
    {
        if (ai == null) { GUILayout.Label("  -"); return; }

        try
        {
            // 先找 _queue / _tasks 等字段
            FieldInfo[] fields = ai.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            IList list = null;

            for (int i = 0; i < fields.Length; i++)
            {
                Type ft = fields[i].FieldType;
                if (!typeof(IEnumerable).IsAssignableFrom(ft)) continue;

                object val = fields[i].GetValue(ai);
                if (val == null) continue;

                // 尝试将 IEnumerable<ITask> 转成一个暂存列表（不消费队列）
                IList tmp = TryMaterializeTasks(val);
                if (tmp != null && tmp.Count > 0)
                {
                    list = tmp;
                    break;
                }
            }

            if (list == null || list.Count == 0)
            {
                GUILayout.Label("  (无后续任务)");
                return;
            }

            int shown = 0;
            for (int i = 0; i < list.Count && shown < maxShow; i++, shown++)
            {
                object task = list[i];
                if (task == null) continue;
                string name = task.GetType().Name;

                // 读 Status 属性（可选）
                string statusStr = "";
                PropertyInfo p = task.GetType().GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
                if (p != null)
                {
                    object s = p.GetValue(task, null);
                    statusStr = s != null ? (" [" + s.ToString() + "]") : "";
                }

                GUILayout.Label("  • " + name + statusStr);
            }

            if (list.Count > shown) GUILayout.Label("  … " + (list.Count - shown) + " more");
        }
        catch
        {
            GUILayout.Label("  (无法读取队列)");
        }
    }

    /// <summary>把各种 IEnumerable 里的 ITask 抽出来，不改变原集合</summary>
    private IList TryMaterializeTasks(object enumerable)
    {
        if (enumerable == null) return null;

        // 如果是 IList，直接浅拷贝
        IList asList = enumerable as IList;
        if (asList != null)
        {
            List<object> copy = new List<object>(asList.Count);
            for (int i = 0; i < asList.Count; i++) copy.Add(asList[i]);
            return copy;
        }

        // Queue<T>：反射 _array 或用 foreach 拷贝
        IEnumerable seq = enumerable as IEnumerable;
        if (seq != null)
        {
            List<object> copy = new List<object>();
            foreach (object x in seq) copy.Add(x);
            return copy;
        }

        return null;
    }
}

