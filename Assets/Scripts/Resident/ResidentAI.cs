/***************************************************************************
// File       : ResidentAI.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] resident驱动
// ***************************************************************************/

// [TODO] ResidentAI.cs
using UnityEngine;

public class ResidentAI : MonoBehaviour
{
    public Resident Owner;
    public ResidentMover Mover;

    private TaskContext _ctx;
    private ITask _current;

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
}
