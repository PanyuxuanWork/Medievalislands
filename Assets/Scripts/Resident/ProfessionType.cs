/***************************************************************************
// File       : ProfessionType.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 职业类型枚举：
// 用于标识居民的工作岗位类别，与建筑的可用岗位匹配。
// 建筑可以允许多个 ProfessionType（如市场既有商贩又有搬运工）。
public enum ProfessionType
{
    None = 0,         // 默认/无职业
    Farmer = 1,       // 农夫（耕作、播种、收割）
    Miner = 2,        // 矿工（采集矿石）
    Fisherman = 3,    // 渔夫（捕鱼）
    Lumberjack = 4,   // 伐木工
    Builder = 5,      // 建筑工（施工）
    Craftsman = 6,    // 工匠（作坊生产）
    Trader = 7,       // 商贩（市场、港口交易）
    Guard = 8,        // 卫兵（巡逻、防御）
    Soldier = 9,      // 士兵（战斗）
    Carrier = 10,     // 搬运工（运输物资）
}
