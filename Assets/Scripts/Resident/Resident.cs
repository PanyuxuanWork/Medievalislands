/***************************************************************************
// File       : Resident.cs
// Author     : Panyuxuan
// Created    : 2025/07/28
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 居民数据与执行载体：仅保留引用与背包，不再内置任务与移动循环
// ***************************************************************************/

using Sirenix.OdinInspector;
using UnityEngine;

public class Resident : MonoBehaviour
{
    [Header("基础")]
    public CityContext city;                 // 城市上下文（外部可注入）
    public float moveSpeed = 3f;             // 供 ResidentMover 使用（不在本类自更新）

    [Header("移动组件（可选）")]
    public ResidentMover mover;              // 由任务下发 MoveTo 时调用

    [Header("背包/库存")]
    [ShowInInspector]
    public Inventory Inventory;              // 居民自带背包（任务在此加减资源）

    private void Awake()
    {
        if (city == null)
        {
            city = FindObjectOfType<CityContext>();
        }
        if (mover == null)
        {
            mover = GetComponent<ResidentMover>();
        }
        if (Inventory == null)
        {
            // 需要你的 Inventory 有无参构造。若无，则改为在 Inspector 里赋值或提供 Init 方法。
            Inventory = new Inventory();
        }
    }

    // 说明：
    // 1) 本类不再在 Update/Invoke 中自发执行任何“伐木/面包/搬运”等例行任务；
    // 2) 所有移动与交互由外部系统驱动：
    //    - 寻路/移动：ResidentMover.MoveTo(...)
    //    - 任务执行：TaskManager/ResidentAI 在 Tick 内调用
    // 3) 资源携带统一走 Inventory（任务里 Pickup/Deliver 改为操作 Resident.Inventory）
}
