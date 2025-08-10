/***************************************************************************
// File       : EconomyInterface.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 经济功能接口：
// 通过接口把“仓储/投料/取成品”等功能与具体建筑解耦。
// AI 与系统仅依赖这些接口，不直接依赖具体类，便于扩展与测试。

public interface IStorage
{
    int Capacity { get; }

    // 查询库存
    int Get(ResourceType type);

    // 从仓库存走指定资源
    bool TryPickup(ResourceType type, int amount);

    // 向仓库投递资源
    void Deliver(ResourceType type, int amount);
}

public interface IConsumer
{
    // 是否可接受某资源（可用于容量/白名单判断）
    bool CanAccept(ResourceType type, int amount);

    // 尝试投料到“输入仓”
    bool TryAccept(ResourceType type, int amount);
}

public interface IProducer
{
    // 从“输出仓”拉取成品
    bool TryCollect(ResourceType type, int amount);
}
