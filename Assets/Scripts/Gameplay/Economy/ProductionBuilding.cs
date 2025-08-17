/***************************************************************************
// File       : ProductionBuilding.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using System;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

public class ProductionBuilding : BuildingBase, IProducer, IConsumer
{
    [Header("Recipe")]
    public RecipeDef recipe;

    [Header("Inventories")]
    public Inventory inputInv = new Inventory();
    public Inventory outputInv = new Inventory();

    [Header("Timing")]
    public float cycleSecondsOverride = 0f;

    [Header("Control")]
    [Tooltip("暂停生产（UI 切换）")]
    public bool Paused = false;

    private float _accSeconds = 0f;
    private float _cycleSeconds = 5f;

    protected override void Start()
    {
        base.Start();
        if (recipe != null && recipe.cycleSeconds > 0f) _cycleSeconds = recipe.cycleSeconds;
        if (cycleSecondsOverride > 0f) _cycleSeconds = cycleSecondsOverride;
    }

    private void OnEnable() { TickSystem.OnTick += OnTick; }
    private void OnDisable() { TickSystem.OnTick -= OnTick; }

    private void Update()
    {
        if (state != BuildingState.Active) return;
        if (Paused) return;
        if (recipe == null) return;

        _accSeconds += Time.deltaTime;
    }

    private void OnTick()
    {
        if (state != BuildingState.Active) return;
        if (Paused) return;
        if (recipe == null) return;
        if (_accSeconds < _cycleSeconds) return;

        // 检查输入是否足够
        bool hasAllInputs = true;
        for (int i = 0; i < recipe.inputs.Length; i++)
        {
            ResourceType t = recipe.inputs[i].type;
            int amount = recipe.inputs[i].amount;
            if (inputInv.Get(t) < amount) { hasAllInputs = false; break; }
        }
        if (!hasAllInputs) return;

        // 消耗输入
        for (int i = 0; i < recipe.inputs.Length; i++)
        {
            ResourceType t = recipe.inputs[i].type;
            int amount = recipe.inputs[i].amount;
            inputInv.TryConsume(t, amount);
        }

        // 产出
        for (int i = 0; i < recipe.outputs.Length; i++)
        {
            ResourceType t = recipe.outputs[i].type;
            int amount = recipe.outputs[i].amount;
            outputInv.Add(t, amount);
        }

        _accSeconds = 0f;
    }

    // UI 调用
    public void SetPaused(bool paused)
    {
        Paused = paused;
        TLog.Log(this, paused ? "已暂停生产" : "已恢复生产");
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

    [Button]
    private void EnsureInputStock(ResourceType t, int want, int priority = 0)
    {
        if (inputInv == null) return;
        int have = inputInv.Get(t);
        if (have >= want) return;

        LogisticsRequestDispatcher lrd = FindObjectOfType<LogisticsRequestDispatcher>();
        if (lrd == null) { TLog.Warning("[PB] 无调度器，无法自动下单。"); return; }

        LogisticsRequest req = new LogisticsRequest
        {
            requester = this as IConsumer,
            type = t,
            quantity = want - have, // 缺口
            minBatch = Mathf.Max(1, want / 2),   // 举例：最少半批
            priority = priority
        };
        lrd.Enqueue(req);
        TLog.Log("[PB] 自动下单: " + t + " 需 " + req.quantity + " (minBatch=" + req.minBatch + ")", LogColor.Cyan);
    }

}
