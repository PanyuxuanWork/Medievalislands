/***************************************************************************
// File       : NotifyFulfilledTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// Desc : 成功路径的收尾：通知调度器减少 in-flight 计数
//***************************************************************************/

public class NotifyFulfilledTask : TaskBase
{
    private LogisticsRequestDispatcher _dispatcher;
    private ResourceType _type;

    public static NotifyFulfilledTask Create(LogisticsRequestDispatcher d, ResourceType type, int priority = 0)
    {
        var t = new NotifyFulfilledTask();
        t._dispatcher = d; t._type = type; t.Priority = priority;
        return t;
    }

    protected override void OnTick()
    {
        _dispatcher?.NotifyRequestFulfilled(_type);
        Succeed();
    }
}
