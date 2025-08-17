/***************************************************************************
// File       : EatDrinkTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using ResidentNamespace;
using UnityEngine;

public class EatDrinkTask : TaskBase
{
    private INeedsProvider _provider;
    private int _durationTicks;
    private bool _needFood;
    private bool _needWater;

    public EatDrinkTask(INeedsProvider provider, bool needFood, bool needWater, int durationTicks = 40)
    {
        _provider = provider; _needFood = needFood; _needWater = needWater; _durationTicks = durationTicks;
    }

    protected override void OnStart()
    {
        // 可扩展为：先通勤到 provider.GetEntrance()，到达后开始计时进食/饮水
    }

    protected override void OnTick()
    {

    }
}
