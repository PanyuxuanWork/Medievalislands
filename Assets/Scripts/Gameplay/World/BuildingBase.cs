/***************************************************************************
// File       : BuildingBase.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// [TODO] 建筑基类：
// 统一提供“身份/占位/建造进度/耐久/激活状态”等通用能力。
// 子类（如仓库、工坊）继承它获得一致的生命周期管理。
// 不包含经济/仓储/生产等易变逻辑（这些放到功能接口与组件里）。
public enum BuildingState { Placed, Constructing, Active, Disabled, Destroyed }

public abstract class BuildingBase : MonoBehaviour
{
    [Header("Template")]
    [Tooltip("建筑模板（ScriptableObject），提供静态配置")]
    public BuildingAsset Asset;

    [Header("Runtime")]
    [Tooltip("当前建造进度 0..1；=1 视为可激活/已建成")]
    [Range(0f, 1f)]
    public float BuildProgress = 1f;
    [Tooltip("建筑当前状态")]
    public BuildingState state = BuildingState.Placed;

    [Header("Context")]
    [Tooltip("可选：所在城市上下文")]
    public CityContext City;

    /// <summary> 建成事件（从 Constructing/Placed 进入 Active 时触发） </summary>
    public System.Action<BuildingBase> OnBuilt;

    /// <summary> 状态变化事件（任何状态切换时触发） </summary>
    public System.Action<BuildingBase, BuildingState, BuildingState> OnStateChanged;

    public List<Vector2Int> OccupiedCells = new();

    // ------------------------ 生命周期 ------------------------

    protected virtual void Awake()
    {
        // 自动补齐 City 引用（可在外部显式指定）
        if (City == null)
        {
            City = GameObject.FindObjectOfType<CityContext>();
        }

        // 初次命名：用模板名
        if (Asset != null && !string.IsNullOrEmpty(Asset.Name))
        {
            gameObject.name = Asset.Name;
        }
    }

    protected virtual void Start()
    {
        // 若初始进度已满，则直接激活（常用于预置成品建筑）
        if (state == BuildingState.Placed && BuildProgress >= 1f)
        {
            SetState(BuildingState.Active);
            Activate();
            OnBuilt?.Invoke(this);
        }
    }

    // 子类可按需覆盖（轻量逻辑）
    public virtual void Tick(float deltaTime) { }

    // ------------------------ 公共方法 ------------------------

    /// <summary>
    /// 外部初始化入口：放置/建成时调用以绑定模板与上下文。
    /// </summary>
    public virtual void Init(BuildingAsset asset, CityContext city = null)
    {
        Asset = asset;
        if (Asset != null && !string.IsNullOrEmpty(Asset.Name))
        {
            gameObject.name = Asset.Name;
        }

        if (city != null)
        {
            City = city;
        }
    }

    /// <summary>
    /// 推进施工进度（0..1）。到 1 时自动尝试激活。
    /// </summary>
    public virtual void AddBuildProgress(float delta)
    {
        if (state == BuildingState.Active || state == BuildingState.Destroyed) return;

        if (state == BuildingState.Placed)
        {
            SetState(BuildingState.Constructing);
        }

        BuildProgress = Mathf.Clamp01(BuildProgress + delta);

        if (BuildProgress >= 1f)
        {
            // 完工 → 激活
            SetState(BuildingState.Active);
            Activate();
            OnBuilt?.Invoke(this);
        }
    }

    /// <summary>
    /// 设置状态并触发状态变更事件。
    /// </summary>
    public void SetState(BuildingState to)
    {
        BuildingState from = state;
        if (from == to) return;

        state = to;
        if (OnStateChanged != null)
        {
            OnStateChanged(this, from, to);
        }
    }

    /// <summary>
    /// 建筑建成可用时调用：自动登记到 CityContext 分类列表
    /// </summary>
    [Button("Activate (Debug)")]
    public void Activate()
    {
        if (City == null)
        {
            City = GameObject.FindObjectOfType<CityContext>();
        }
        if (City == null)
        {
            TLog.Warning("BuildingBase.Activate 未找到 CityContext，跳过登记。");
            return;
        }

        state = BuildingState.Active;
        BuildProgress = 1f;

        // 分类登记：按存在的组件加入相应列表
        WarehouseBuilding wh = GetComponent<WarehouseBuilding>();
        if (wh != null && !City.warehouses.Contains(wh))
        {
            City.warehouses.Add(wh);
        }

        ProductionBuilding prod = GetComponent<ProductionBuilding>();
        if (prod != null && !City.productions.Contains(prod))
        {
            City.productions.Add(prod);
        }

        TLog.Log(this, "已建成并登记到 CityContext。");
    }

    /// <summary>
    /// 释放占位 + 从 CityContext 反登记
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 释放占位
        GridAsset grid = GridManager.GetGrid();
        if (grid != null && OccupiedCells != null && OccupiedCells.Count > 0)
        {
            for (int i = 0; i < OccupiedCells.Count; i++)
            {
                Vector2Int c = OccupiedCells[i];
                if (!grid.InBounds(c)) continue;
                GridCell cell = grid.GetCell(c);
                if (cell != null) cell.BuildStatus = BuildStatus.Buildable;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(grid);
#endif
        }

        // 反登记
        if (City != null)
        {
            WarehouseBuilding wh = GetComponent<WarehouseBuilding>();
            if (wh != null) City.warehouses.Remove(wh);

            ProductionBuilding prod = GetComponent<ProductionBuilding>();
            if (prod != null) City.productions.Remove(prod);
        }

        TLog.Log(this, "已销毁并从 CityContext 反登记。");
    }
    // ------------------------ 只读访问器（来自模板） ------------------------

    /// <summary> 建筑功能大类（来自模板，空模板时返回 None） </summary>
    public BuildingType GetBuildingType()
    {
        return Asset != null ? Asset.BuildingType : BuildingType.None;
    }

    /// <summary> 建筑功能小类（来自模板，空模板时返回 None） </summary>
    public BuildingSubType GetBuildingSubType()
    {
        return Asset != null ? Asset.BuildingSubType : BuildingSubType.None;
    }

    /// <summary> 网格占位（格数） </summary>
    public Vector2Int GetFootprint()
    {
        return Asset != null ? Asset.Footprint : new Vector2Int(1, 1);
    }

    /// <summary> 最大雇员数（模板） </summary>
    public int GetMaxEmployees()
    {
        return Asset != null ? Asset.MaxEmployees : 0;
    }

    /// <summary> 是否允许自动分配（模板） </summary>
    public bool IsAutoAssignAllowed()
    {
        return Asset != null && Asset.AllowAutoAssign;
    }

    /// <summary> 岗位职业列表（模板） </summary>
    public List<ProfessionType> GetProfessionTypes()
    {
        return Asset != null ? Asset.ProfessionTypes : null;
    }

    // ------------------------ Odin 调试按钮 ------------------------

    [Button("设为放置态")]
    private void Btn_SetPlaced()
    {
        SetState(BuildingState.Placed);
        BuildProgress = 0f;
    }

    [Button("推进 10%")]
    private void Btn_Add10Percent()
    {
        AddBuildProgress(0.1f);
    }

    [Button("直接建成")]
    private void Btn_FinishAndActivate()
    {
        BuildProgress = 1f;
        SetState(BuildingState.Active);
        Activate();
        if (OnBuilt != null) OnBuilt(this);
    }
}
