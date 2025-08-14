/***************************************************************************
// File       : ResidentViewAdapter.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 居民视图接口（UI 只依赖此接口，脱离具体实现）
// ***************************************************************************/

using System.Reflection;
using UnityEngine;

public interface IResidentView
{
    string DisplayName { get; }
    string Role { get; }
    Vector3 Position { get; }
    bool IsMoving { get; }
    IInventoryView Inventory { get; }
    string CurrentTaskSummary { get; }
}

/***************************************************************************
// File       : ResidentViewAdapter.cs
// Description: Resident -> IResidentView 适配器（兼容无 role & 无 GetDebugTaskSummary）
// ***************************************************************************/

public class ResidentViewAdapter : IResidentView
{
    private readonly Resident _res;
    private readonly ResidentMover _mover;
    private readonly ResidentAI _ai;
    private readonly IInventoryView _invView;

    public ResidentViewAdapter(Resident r)
    {
        _res = r;
        _mover = r != null ? r.GetComponent<ResidentMover>() : null;
        _ai = r != null ? r.GetComponent<ResidentAI>() : null;

        // 新版 Resident 暴露了 Inventory 字段（public）
        Inventory inv = null;
        if (r != null)
        {
            FieldInfo fi = r.GetType().GetField("Inventory", BindingFlags.Public | BindingFlags.Instance);
            if (fi != null) inv = fi.GetValue(r) as Inventory;
        }
        _invView = new InventoryViewAdapter(inv);
    }

    public string DisplayName { get { return _res != null ? _res.name : "(null)"; } }

    // 你的 Resident 没有 role 字段/属性，这里先用 "-" 占位。
    public string Role { get { return "-"; } } // 可在将来接入 Profession/Assignment 后替换

    public Vector3 Position { get { return _res != null ? _res.transform.position : Vector3.zero; } }
    public bool IsMoving { get { return _mover != null && _mover.IsMoving(); } }
    public IInventoryView Inventory { get { return _invView; } }

    public string CurrentTaskSummary
    {
        get
        {
            if (_ai == null) return "-";

            // 优先尝试调用 _ai.GetDebugTaskSummary()（若你将来添加的话）
            MethodInfo m = _ai.GetType().GetMethod("GetDebugTaskSummary", BindingFlags.Public | BindingFlags.Instance);
            if (m != null)
            {
                try { return m.Invoke(_ai, null) as string ?? "-"; } catch { /* ignore */ }
            }

            // 退而求其次：反射读取私有字段 _current（ResidentAI.cs 中的 ITask）
            // 字段定义见文件：private ITask _current;（我们通过反射拿到任务类型名与 Status）
            FieldInfo f = _ai.GetType().GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                object cur = f.GetValue(_ai);
                if (cur != null)
                {
                    // 取类型名
                    string taskName = cur.GetType().Name;

                    // 取 Status 属性（TaskStatus 枚举）
                    PropertyInfo pStatus = cur.GetType().GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
                    string statusStr = "-";
                    if (pStatus != null)
                    {
                        object s = pStatus.GetValue(cur, null);
                        statusStr = s != null ? s.ToString() : "-";
                    }

                    return taskName + " [" + statusStr + "]";
                }
            }

            return "-";
        }
    }
}

