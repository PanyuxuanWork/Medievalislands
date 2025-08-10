/***************************************************************************
// File       : BuildingType.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 建筑类别枚举：
// 用于区分建筑功能类别，在UI筛选、AI逻辑、统计等地方使用。
// 按功能划分，而不是具体名称，方便同类建筑共用逻辑。
public enum BuildingType
{
    None = 0,          // 默认/未分类
    Housing = 1,       // 住宅类（村民居住）
    Storage = 2,       // 仓储类（资源存放）
    Production = 3,    // 加工/生产类（工坊、作坊等）
    ResourceGathering = 4, // 资源采集类（农田、矿井、渔场）
    Military = 5,      // 军事类（兵营、防御塔）
    Trade = 6,         // 贸易类（市场、港口）
    Civic = 7,         // 市政/功能类（市政厅、法院）
    Decorative = 8,    // 装饰类（雕像、喷泉等）
}
