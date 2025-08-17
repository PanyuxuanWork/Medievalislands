/***************************************************************************
// File       : ResidentState.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// [TODO] ResidentStats：体征（饥饿/口渴/睡眠质量）在固定步长中推进
// ***************************************************************************/
using UnityEngine;

public class ResidentStats : MonoBehaviour, ISimTickable
{
    [Header("体征 (0-100)")]
    [Range(0, 100)] public int Hunger = 80;         // 饥饿：越低越饿
    [Range(0, 100)] public int Thirst = 80;         // 口渴：越低越渴
    [Range(0, 100)] public int SleepQuality = 60;   // 睡眠质量：越高越精神

    [Header("每 Tick 变化")]
    [Min(0)] public int HungerDecayPerTick = 1;    // 每步长饥饿下降值
    [Min(0)] public int ThirstDecayPerTick = 1;    // 每步长口渴下降值
    [Min(0)] public int SleepDecayPerTick = 0;     // 非睡眠状态下睡眠质量缓慢下降
    [Min(0)] public int SleepRecoverPerTick = 2;   // 睡眠状态恢复值（由睡觉任务设置 IsSleeping=true）

    [Header("状态标记")]
    public bool IsSleeping = false;                // 由任务/AI 控制

    public int Order { get { return 20; } }        // 早于 AI（让 AI 读取最新体征）
    public bool Enabled { get { return isActiveAndEnabled; } }

    private void OnEnable()
    {
        if (TickRunner.Instance != null) TickRunner.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (TickRunner.Instance != null) TickRunner.Instance.Unregister(this);
    }

    public void SimTick()
    {
        // 饥饿/口渴持续下降
        Hunger = Mathf.Clamp(Hunger - HungerDecayPerTick, 0, 100);
        Thirst = Mathf.Clamp(Thirst - ThirstDecayPerTick, 0, 100);

        // 睡眠质量：睡觉时恢复，否则平缓衰减
        if (IsSleeping)
        {
            SleepQuality = Mathf.Clamp(SleepQuality + SleepRecoverPerTick, 0, 100);
        }
        else if (SleepDecayPerTick > 0)
        {
            SleepQuality = Mathf.Clamp(SleepQuality - SleepDecayPerTick, 0, 100);
        }
    }

    // —— 供任务/交互使用的便捷接口 ——
    public void Eat(int amount) { Hunger = Mathf.Clamp(Hunger + amount, 0, 100); }
    public void Drink(int amount) { Thirst = Mathf.Clamp(Thirst + amount, 0, 100); }

    // 工作效率：体征的乘性修正（0.0-1.0）
    public float GetWorkEfficiency()
    {
        float hf = Mathf.Lerp(0.4f, 1.0f, Hunger / 100.0f);
        float tf = Mathf.Lerp(0.4f, 1.0f, Thirst / 100.0f);
        float sf = Mathf.Lerp(0.5f, 1.0f, SleepQuality / 100.0f);
        return hf * tf * sf;
    }
}
