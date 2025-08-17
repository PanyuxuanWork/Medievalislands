/***************************************************************************
// File       : TaskContext.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 任务核心组件
// ***************************************************************************/

using UnityEngine;

// [TODO] TaskStatus.cs
public enum TaskStatus { Pending, Running, Success, Failed, Canceled }

// [TODO] ITask.cs
public interface ITask
{
    int Priority { get; }
    TaskStatus Status { get; }
    void Init(TaskContext ctx);
    void Tick();
    void Cancel();
}

public class TaskContext
{
    public CityContext City;
    public Resident Owner;
    public ResidentMover Mover;

    public TaskContext(CityContext city, Resident owner, ResidentMover mover)
    {
        City = city; Owner = owner; Mover = mover;
    }
}

// [TODO] TaskBase.cs
public abstract class TaskBase : ITask
{
    public int Priority { get; protected set; }
    public TaskStatus Status { get; protected set; } = TaskStatus.Pending;
    protected TaskContext Ctx;

    public void Init(TaskContext ctx) { Ctx = ctx; Status = TaskStatus.Running; OnStart(); }
    public void Tick() { if (Status == TaskStatus.Running) OnTick(); }
    public void Cancel() { if (Status == TaskStatus.Running) { Status = TaskStatus.Canceled; OnCancel(); } }

    protected void Succeed() { Status = TaskStatus.Success; }
    protected void Fail() { Status = TaskStatus.Failed; }

    protected virtual void OnStart() { }
    protected virtual void OnTick() { }
    protected virtual void OnCancel() { }
}

