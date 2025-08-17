/***************************************************************************
// File       : Resident.cs
// Author     : Panyuxuan
// Created    : 2025/07/28
// Latest     : 2025/08/16 V2.0
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

    [Header("职业与分类")]
    public ProfessionType Profession = ProfessionType.None; // 具体职业
    [ReadOnly] public ProfessionCategory ProfessionCate = ProfessionCategory.None; // 职业类别（由职业映射）

    [Header("人口学")]
    [Range(0, 120)] public int AgeYears = 20; // 年龄（年）
    public GenderType Gender = GenderType.Unknown;

    [Header("住所与单位")]
    public BuildingBase Home;                    // 住所（仅容器）
    public BuildingBase Workplace;               // 工作单位（可为空）

    [Header("工作时间")]
    public WorkShift WorkTime = new WorkShift();

    [Header("当前任务（只读显示）")]
    [ShowInInspector, ReadOnly] public string CurrentTaskName = ""; // 供 UI 面板显示

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
        Inventory ??= new Inventory();
    }

    // 由 ResidentAI 设置，用于 UI 展示
    public void SetCurrentTask(ITask task)
    {
        if (task == null) { CurrentTaskName = string.Empty; return; }
        CurrentTaskName = task.GetType().Name;
    }

    // 当职业变动时手动调用，刷新职业类别
    public void RefreshProfessionCategory()
    {
        ProfessionCate = ProfessionCategoryUtil.Map(Profession);
    }

}
