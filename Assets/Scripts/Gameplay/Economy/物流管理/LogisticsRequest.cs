/***************************************************************************
// File       : LogisticsRequest.cs
// Author     : Panyuxuan
// Created    : 2025/08/15
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: 物流需求与预留信息
// ***************************************************************************/
public enum RequestKind { PullInput, PushOutput }
public enum RequestState { Pending, Reserved, Assigned, Fulfilled, Failed, Canceled }

public class LogisticsRequest
{
    public IConsumer requester;          // 需求方（通常是 ProductionBuilding 作为 IConsumer）
    public RequestKind kind;
    public ResourceType type;
    public int quantity;                 // 目标数量（Exact 上限，实际可按携带量完成）
    public int minBatch;                 // 最小派发批量
    public int priority;                 // 大者先
    public float createTime;
    public float ttlSeconds = 30f;       // 请求存活时间（调度未处理则作废，可视需要调整）

    // 角色：输入由 IConsumer 请求；输出由 IProducer 请求
    public IConsumer consumer;           // kind=PullInput 时使用（缺料方）
    public IProducer producer;           // kind=PushOutput 时使用（产出方）

    public RequestState state;

    // 预留信息
    public IStorage reservedFrom;        // 库存预留：从哪个仓出
    public int reservedAmount;
    public IStorage reservedTo;          // 空间预留：送到哪个仓
    public int reservedSpace;

    // 过期时间（预留级别）
    public float reserveExpireTime;      // 预留过期（秒级时间戳）
}

