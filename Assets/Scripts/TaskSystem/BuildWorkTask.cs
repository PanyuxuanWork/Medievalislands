/***************************************************************************
// File       : BuildWorkTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// Desc       : 施工任务（在工地上消耗一段时间，把工时转换为进度）
// ***************************************************************************/

using UnityEngine;

public class BuildWorkTask : TaskBase
{
    private IWorksite _site;
    private float _workSeconds;
    private float _elapsed;

    public static BuildWorkTask Create(IWorksite site, float workSeconds, int priority = 0)
    {
        var t = new BuildWorkTask
        {
            _site = site,
            _workSeconds = UnityEngine.Mathf.Max(0.5f, workSeconds),
            Priority = priority
        };
        return t;
    }

    protected override void OnTick()
    {
        var mb = _site as UnityEngine.MonoBehaviour;
        if (_site == null || mb == null || !mb.isActiveAndEnabled) { TLog.Warning("[BuildWorkTask] 工地无效"); Fail(); return; }

        if (!_site.CanStartWork() || !_site.NeedsWork) { Succeed(); return; }

        _elapsed += UnityEngine.Time.deltaTime;
        _site.AddWork(UnityEngine.Time.deltaTime); // 1秒=1工时（后续可乘“工人效率”）

        if (_elapsed >= _workSeconds || !_site.NeedsWork)
        {
            TLog.Log("[BuildWorkTask] 完成工时块：" + _elapsed.ToString("F1") + "s");
            Succeed();
        }
    }
}
