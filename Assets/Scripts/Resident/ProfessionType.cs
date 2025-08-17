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
using UnityEngine;

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


/// <summary>
/// 性别
/// </summary>
public enum GenderType
{
    Unknown = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

/// <summary>
/// 工作状态
/// </summary>
public enum EmploymentState
{
    Unemployed = 0,
    Employed = 1,
    OnLeave = 2
}


/// <summary>
/// 职业打雷
/// </summary>
public enum ProfessionCategory
{
    None = 0,
    Agriculture = 1,//农渔伐（Farmer、Fisherman、Lumberjack）
    Industry = 2,//采矿/制造/建造（Miner、Craftsman、Builder）
    Services = 3,//贸易/运输（Trader、Carrier）
    Defense = 4,//Defense：守卫/士兵（Guard、Soldier）
    Civic = 5//Civic：政务/公共服务（文官、维护等）
}

/// <summary>
/// 通过职业获取职业大类
/// </summary>
public static class ProfessionCategoryUtil
{
    public static ProfessionCategory Map(ProfessionType type)
    {
        switch (type)
        {
            case ProfessionType.Farmer:
            case ProfessionType.Fisherman:
            case ProfessionType.Lumberjack:
                return ProfessionCategory.Agriculture;
            case ProfessionType.Miner:
            case ProfessionType.Craftsman:
            case ProfessionType.Builder:
                return ProfessionCategory.Industry;
            case ProfessionType.Trader:
            case ProfessionType.Carrier:
                return ProfessionCategory.Services;
            case ProfessionType.Guard:
            case ProfessionType.Soldier:
                return ProfessionCategory.Defense;
            default:
                return ProfessionCategory.None;
        }
    }
}

// ===============================
// ===== File: Domain/Residents/WorkShift.cs =====
// [TODO] WorkShift：工作时间段（小时粒度，跨夜支持）
[System.Serializable]
public class WorkShift
{
    [Range(0, 23)] public int StartHour = 8;   // 含
    [Range(0, 23)] public int EndHour = 17;    // 不含；若 Start > End 表示跨夜

    public bool IsWithin(int hour)
    {
        if (StartHour < EndHour) return hour >= StartHour && hour < EndHour;
        return hour >= StartHour || hour < EndHour; // 跨夜
    }
}
