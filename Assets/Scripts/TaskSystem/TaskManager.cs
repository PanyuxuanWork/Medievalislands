/***************************************************************************
// File       : TaskManager.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 任务管理中心
// ***************************************************************************/

// [TODO] TaskManager.cs
using System.Collections.Generic;
using Core;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private readonly List<ITask> _queue = new List<ITask>();

    void OnEnable() { TickSystem.OnTick += OnTick; }
    void OnDisable() { TickSystem.OnTick -= OnTick; }

    public void Enqueue(ITask t)
    {
        if (t == null) return;
        _queue.Add(t);
        _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    void OnTick()
    {
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            ITask t = _queue[i];
            if (t.Status == TaskStatus.Pending) continue;
            if (t.Status == TaskStatus.Running) t.Tick();
            if (t.Status == TaskStatus.Success || t.Status == TaskStatus.Failed || t.Status == TaskStatus.Canceled)
            {
                _queue.RemoveAt(i);
            }
        }
    }

    public ITask RequestOne()
    {
        if (_queue.Count == 0) return null;
        ITask t = _queue[0]; _queue.RemoveAt(0); return t;
    }
}
