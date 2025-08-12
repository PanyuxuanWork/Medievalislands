/***************************************************************************
// File       : MoveTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 移动任务
// ***************************************************************************/

// [TODO] MoveToTask.cs
using UnityEngine;

public class MoveToTask : TaskBase
{
    private Vector3 _target;
    private float _arrive = 0.2f;

    public static MoveToTask Create(Vector3 target, float arrive = 0.2f, int prio = 0)
    {
        MoveToTask t = new MoveToTask(); t._target = target; t._arrive = arrive; t.Priority = prio; return t;
    }

    protected override void OnStart()
    {
        if (Ctx == null || Ctx.Mover == null) { Fail(); return; }
        Ctx.Mover.MoveTo(_target);
    }

    protected override void OnTick()
    {
        if (Ctx.Mover == null) { Fail(); return; }
        if (!Ctx.Mover.IsMoving()) Succeed();
    }
}
