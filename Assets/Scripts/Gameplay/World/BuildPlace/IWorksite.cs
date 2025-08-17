/***************************************************************************
// File       : IWorksite.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Desc       : 任何可被“建造/施工”的目标实现此接口
// ***************************************************************************/
public interface IWorksite
{
    bool NeedsWork { get; }                // 是否仍需要施工
    float WorkRemaining { get; }           // 剩余工作量（工时）
    bool CanStartWork();                   // 是否可以开始施工（通常=材料已齐）
    void AddWork(float workAmount);        // 增加施工进度（由工人调用）
}
