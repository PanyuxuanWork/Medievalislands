/***************************************************************************
// File       : ProductionBuilding.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using System;
using UnityEngine;

public class ProductionBuilding : BuildingBase, IProducer, IConsumer
{
    [Header("Recipe")]
    public RecipeDef recipe;

    [Header("Inventories")]
    public Inventory inputInv = new Inventory();   // 投料仓
    public Inventory outputInv = new Inventory();  // 成品仓

    [Header("Timing")]
    [Tooltip("一个生产周期的实际秒数；若未设置则从 recipe.cycleSeconds 读取")]
    public float cycleSecondsOverride = 0f;

    private float _accSeconds = 0f;  // 实时累计（秒）
    private float _cycleSeconds = 5f;

    // 可选：如果你以后要按“需求工人数量”影响效率，可以在这里读 recipe.workersRequired
    // 并结合绑定工人数来决定是否生产或放慢生产。

    protected override void Start()
    {
        base.Start();

        // 初始化生产周期
        if (recipe != null && recipe.cycleSeconds > 0f)
        {
            _cycleSeconds = recipe.cycleSeconds;
        }
        if (cycleSecondsOverride > 0f)
        {
            _cycleSeconds = cycleSecondsOverride;
        }
    }

    private void OnEnable()
    {
        TickSystem.OnTick += OnTick;
    }

    private void OnDisable()
    {
        TickSystem.OnTick -= OnTick;
    }

    private void Update()
    {
        // 非激活/无配方则不计时
        if (state != BuildingState.Active) return;
        if (recipe == null) return;

        // 实时累计到达一个周期的判断量
        _accSeconds += Time.deltaTime;
    }

    // === 生产周期结算（由 TickSystem 驱动） ===
    private void OnTick()
    {
        if (state != BuildingState.Active) return;
        if (recipe == null) return;

        // 未达到一个周期，不生产
        if (_accSeconds < _cycleSeconds) return;

        // 检查输入是否充足（逐项判断，避免使用新语法）
        bool hasAllInputs = true;
        for (int i = 0; i < recipe.inputs.Length; i++)
        {
            ResourceType t = recipe.inputs[i].type;
            int amount = recipe.inputs[i].amount;
            if (inputInv.Get(t) < amount)
            {
                hasAllInputs = false;
                break;
            }
        }
        if (!hasAllInputs) return;

        // 扣除输入
        for (int i = 0; i < recipe.inputs.Length; i++)
        {
            ResourceType t = recipe.inputs[i].type;
            int amount = recipe.inputs[i].amount;
            inputInv.TryConsume(t, amount);
        }

        // 增加产出
        for (int i = 0; i < recipe.outputs.Length; i++)
        {
            ResourceType t = recipe.outputs[i].type;
            int amount = recipe.outputs[i].amount;
            outputInv.Add(t, amount);
        }

        // 重置周期累积
        _accSeconds = 0f;
    }

    // === IConsumer：外部投料 ===
    public bool CanAccept(ResourceType t, int amount)
    {
        // 你可以在此加入容量/白名单等规则；当前只要激活就接受
        if (state != BuildingState.Active) return false;
        return true;
    }

    public bool TryAccept(ResourceType t, int amount)
    {
        if (!CanAccept(t, amount)) return false;
        inputInv.Add(t, amount);
        return true;
    }

    // === IProducer：外部拉取成品 ===
    public bool TryCollect(ResourceType t, int amount)
    {
        if (state != BuildingState.Active) return false;
        return outputInv.TryConsume(t, amount);
    }
}
