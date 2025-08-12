/***************************************************************************
// File       : TaskSequence.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Description: [TODO] 顺序执行一组任务；任一失败则整组失败，全部成功才成功。
//               用于将 Move→Pickup→Move→Deliver 合并为“原子链”，避免空跑。
// ***************************************************************************/

using System.Collections.Generic;

public class TaskSequence : TaskBase
{
    // 任务链
    private readonly List<ITask> _steps = new List<ITask>();
    private int _index = -1;

    /// <summary>
    /// 创建一条顺序任务链（steps 按顺序执行）
    /// </summary>
    public static TaskSequence Create(params ITask[] steps)
    {
        TaskSequence s = new TaskSequence();
        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null) s._steps.Add(steps[i]);
            }
        }
        return s;
    }

    protected override void OnStart()
    {
        if (_steps.Count == 0)
        {
            TLog.Log("[TaskSequence] 空任务链，直接成功。", LogColor.Grey);
            Succeed();
            return;
        }
        _index = 0;
        _steps[_index].Init(Ctx);
        TLog.Log("[TaskSequence] 开始第 1 步 / 共 " + _steps.Count + " 步。", LogColor.Cyan);
    }

    protected override void OnTick()
    {
        if (_index < 0 || _index >= _steps.Count) return;

        ITask cur = _steps[_index];
        if (cur.Status == TaskStatus.Running)
        {
            cur.Tick();
            return;
        }

        if (cur.Status == TaskStatus.Success)
        {
            _index++;
            if (_index >= _steps.Count)
            {
                TLog.Log("[TaskSequence] 全部完成。", LogColor.Green);
                Succeed();
                return;
            }

            TLog.Log("[TaskSequence] 进入第 " + (_index + 1) + " 步。", LogColor.Cyan);
            _steps[_index].Init(Ctx);
            return;
        }

        // 失败 / 取消：整链结束
        TLog.Warning("[TaskSequence] 第 " + (_index + 1) + " 步失败或被取消，整链终止。");
        for (int i = _index; i < _steps.Count; i++)
        {
            _steps[i].Cancel();
        }
        Fail();
    }

    protected override void OnCancel()
    {
        // 主链被取消时，取消剩余子任务
        for (int i = 0; i < _steps.Count; i++)
        {
            _steps[i].Cancel();
        }
        TLog.Warning("[TaskSequence] 被外部取消。");
    }
}
