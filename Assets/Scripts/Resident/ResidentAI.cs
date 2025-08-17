/***************************************************************************
// File       : ResidentAI.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] resident驱动
// ***************************************************************************/

// [TODO] ResidentAI.cs

using Core;
using UnityEngine;

[RequireComponent(typeof(Resident))]
[RequireComponent(typeof(ResidentMover))]
public class ResidentAI : MonoBehaviour,ISimTickable
{
    public Resident Owner;
    public ResidentMover Mover;

    private TaskContext _ctx;
    private ITask _current;

    public int Order { get { return 100; } }              // 体征之后执行
    public bool Enabled { get { return isActiveAndEnabled; } }



    void Start()
    {
        if (Owner == null) Owner = GetComponent<Resident>();
        if (Mover == null) Mover = GetComponent<ResidentMover>();
        _ctx = new TaskContext(FindObjectOfType<CityContext>(), Owner, Mover);
        TickSystem.OnTick += OnTick;
    }

    void OnDestroy() { TickSystem.OnTick -= OnTick; }

    void OnTick()
    {
        if (_current == null)
        {
            _current = FindObjectOfType<TaskManager>()?.RequestOne();
            if (_current != null) _current.Init(_ctx);
            return;
        }

        if (_current.Status == TaskStatus.Running) _current.Tick();

        if (_current.Status == TaskStatus.Success || _current.Status == TaskStatus.Failed || _current.Status == TaskStatus.Canceled)
        {
            _current = null;
        }
    }

    private void OnEnable()
    {
        if (TickRunner.Instance != null) TickRunner.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (TickRunner.Instance != null) TickRunner.Instance.Unregister(this);
    }

    public void SimTick()
    {
        // 若当前任务为空则索取一个；你已有 TaskManager 体系，这里复用
        if (_current == null)
        {
            TaskManager mgr = FindObjectOfType<TaskManager>();
            if (mgr != null)
            {
                _current = mgr.RequestOne();
                if (_current != null) _current.Init(_ctx);
            }
            Owner.SetCurrentTask(_current);
            return;
        }

        // 推进任务
        if (_current.Status == TaskStatus.Running) _current.Tick();

        // 任务结束→清空
        if (_current.Status == TaskStatus.Success || _current.Status == TaskStatus.Failed || _current.Status == TaskStatus.Canceled)
        {
            _current = null;
        }

        Owner.SetCurrentTask(_current);
    }

}
