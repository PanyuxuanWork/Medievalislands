/***************************************************************************
// File       : GlobalTime.cs
// Author     : Panyuxuan
// Created    : 2025/08/16
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: // [TODO] GlobalTime：基于固定步长 Tick 的游戏时钟（年月日时分秒 + 四季 + 倍速/暂停 + 事件）
// 依赖：TickSystem（固定步长仿真），继承：MonoSingleton<T>
// 风格：尽量不使用 var；关键位置注释；不写 namespace；可搭配 Odin Inspector
****************************************************************************/
using System;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

public enum Season
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}

public class GlobalTime : MonoSingleton<GlobalTime>
{
    // —— 当前时间 —— //
    [BoxGroup("Current Time"), ReadOnly] public int Year = 1;
    [BoxGroup("Current Time"), ReadOnly] public int Month = 1;   // 1..12
    [BoxGroup("Current Time"), ReadOnly] public int Day = 1;     // 1..MonthDays[Month-1]
    [BoxGroup("Current Time"), ReadOnly] public int Hour = 8;    // 0..23
    [BoxGroup("Current Time"), ReadOnly] public int Minute = 0;  // 0..59
    [BoxGroup("Current Time"), ReadOnly] public int Second = 0;  // 0..59
    [BoxGroup("Current Time"), ReadOnly] public int DayOfYear = 1; // 1..TotalDaysInYear
    [BoxGroup("Current Time"), ReadOnly] public Season CurrentSeason = Season.Spring;

    // —— 速度/暂停 —— //
    [BoxGroup("Run"), ShowInInspector, ReadOnly] public bool Paused = false;

    [BoxGroup("Run"), InfoBox("一次 Tick 推进多少“游戏秒”（在 1x 倍速下）。\n"
        + "示例：若 TickSystem 是 10Hz，而你希望 1秒真实时间≈1分钟游戏时间：\n"
        + "10 次 Tick ≈ 60 游戏秒 ⇒ SecondsPerTickAt1x 设为 6。")][ MinValue(0.0f)]
    public float SecondsPerTickAt1x = 6.0f; // 结合 10Hz Tick => 1x 时 1秒真实≈1分钟游戏

    [BoxGroup("Run"), InfoBox("倍速倍率。可用按钮快速切换 x0.5 / x1 / x2 / x4 。")]
    [ MinValue(0.0f)]
    public float TimeScale = 1.0f;

    // —— 历法/季节配置 —— //
    [BoxGroup("Calendar"), InfoBox("每月天数。默认 12 个月，每月 30 天。可按需修改。")]
    public int[] MonthDays = new int[12] { 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 };

    [BoxGroup("Calendar"), InfoBox("按“月份→季节”的映射。长度必须为 12。")]
    public Season[] SeasonByMonth = new Season[12]
    {
        Season.Spring, Season.Spring, Season.Spring,
        Season.Summer, Season.Summer, Season.Summer,
        Season.Autumn, Season.Autumn, Season.Autumn,
        Season.Winter, Season.Winter, Season.Winter
    };

    // —— 事件（按需订阅） —— //
    public static event Action OnSecond;
    public static event Action OnMinute;
    public static event Action OnHour;
    public static event Action OnDay;
    public static event Action OnMonth;
    public static event Action OnYear;
    public static event Action<Season> OnSeasonChanged;

    // —— 内部状态 —— //
    private float _secAccumulator = 0.0f;
    //private int _lastDayCached = -1;
    private Season _lastSeason;

    // ========== 生命周期 ==========
    private void OnEnable()
    {
        _lastSeason = CurrentSeason;
        TickSystem.OnTick += HandleTick; // 基于固定步长推进
        RecalcDerived();
    }

    private void OnDisable()
    {
        TickSystem.OnTick -= HandleTick;
    }

    // ========== Tick 驱动 ==========
    private void HandleTick()
    {
        if (Paused) return;

        // 每次 Tick 推进的“游戏秒”（随倍速变化）
        float secondsPerTick = SecondsPerTickAt1x * TimeScale;

        _secAccumulator += secondsPerTick;

        // 以“整秒”为步进，避免浮点误差扩散
        while (_secAccumulator >= 1.0f)
        {
            _secAccumulator -= 1.0f;
            StepOneSecond();
        }
    }

    // ========== 推进 ==========
    private void StepOneSecond()
    {
        Second++;
        if (Second >= 60)
        {
            Second = 0;
            Minute++;
            if (OnSecond != null) OnSecond.Invoke();
            if (Minute >= 60)
            {
                Minute = 0;
                Hour++;
                if (OnMinute != null) OnMinute.Invoke();
                if (Hour >= 24)
                {
                    Hour = 0;
                    Day++;
                    if (OnHour != null) OnHour.Invoke();

                    // 天溢出 → 进位到下月
                    int monthIndex = Mathf.Clamp(Month - 1, 0, MonthDays.Length - 1);
                    int daysInThisMonth = MonthDays[monthIndex];
                    if (Day > daysInThisMonth)
                    {
                        Day = 1;
                        Month++;
                        if (OnDay != null) OnDay.Invoke();

                        // 月溢出 → 进位到下一年
                        if (Month > MonthDays.Length)
                        {
                            Month = 1;
                            Year++;
                            if (OnMonth != null) OnMonth.Invoke();
                            if (OnYear != null) OnYear.Invoke();
                        }
                        else
                        {
                            if (OnMonth != null) OnMonth.Invoke();
                        }
                    }
                    else
                    {
                        if (OnDay != null) OnDay.Invoke();
                    }

                    // 天与季节的派生更新
                    RecalcDerived();
                }
                else
                {
                    if (OnHour != null) OnHour.Invoke();
                }
            }
            else
            {
                if (OnMinute != null) OnMinute.Invoke();
            }
        }
        else
        {
            if (OnSecond != null) OnSecond.Invoke();
        }
    }

    // ========== 派生量与季节 ==========
    private void RecalcDerived()
    {
        // DayOfYear（1 基）
        int doy = 0;
        for (int i = 0; i < Month - 1; i++)
        {
            int clampIndex = Mathf.Clamp(i, 0, MonthDays.Length - 1);
            doy += MonthDays[clampIndex];
        }
        DayOfYear = doy + Day;

        // 计算当前季节并派发事件
        Season newSeason = ResolveSeasonByMonth(Month);
        CurrentSeason = newSeason;
        if (newSeason != _lastSeason)
        {
            _lastSeason = newSeason;
            if (OnSeasonChanged != null) OnSeasonChanged.Invoke(newSeason);
        }
    }

    private Season ResolveSeasonByMonth(int month1Based)
    {
        int idx = Mathf.Clamp(month1Based - 1, 0, SeasonByMonth.Length - 1);
        return SeasonByMonth[idx];
    }

    // ========== 便捷接口（供外部系统/调试 UI 使用） ==========
    [ButtonGroup("Toolbar"), Button("Pause/Resume")]
    public void TogglePause() { Paused = !Paused; }

    [ButtonGroup("Toolbar"), Button("x0.5")]
    public void SpeedHalf() { TimeScale = 0.5f; }

    [ButtonGroup("Toolbar"), Button("x1")]
    public void Speed1() { TimeScale = 1.0f; }

    [ButtonGroup("Toolbar"), Button("x2")]
    public void Speed2() { TimeScale = 2.0f; }

    [ButtonGroup("Toolbar"), Button("x4")]
    public void Speed4() { TimeScale = 4.0f; }

    [ButtonGroup("Jump"), Button("Jump 06:00")]
    public void JumpMorning()
    {
        Hour = 6; Minute = 0; Second = 0;
        OnHour?.Invoke(); OnMinute?.Invoke(); OnSecond?.Invoke();
    }

    [ButtonGroup("Jump"), Button("+1 Hour")]
    public void PlusOneHour()
    {
        for (int i = 0; i < 3600; i++) StepOneSecond();
    }

    [ButtonGroup("Jump"), Button("+1 Day")]
    public void PlusOneDay()
    {
        int secondsInDay = 24 * 3600;
        for (int i = 0; i < secondsInDay; i++) StepOneSecond();
    }

    // 一天内绝对秒（0..86399），供灯光/昼夜曲线/动画等使用
    public int GetAbsoluteSecondsOfDay()
    {
        return Hour * 3600 + Minute * 60 + Second;
    }

    public int GetTotalDaysInYear()
    {
        int sum = 0;
        for (int i = 0; i < MonthDays.Length; i++) sum += MonthDays[i];
        return sum;
    }

    // 外部直接设置具体日期（安全校验 + 派发）
    public void SetDateTimeSafe(int year, int month, int day, int hour, int minute, int second)
    {
        // 基本裁剪
        Year = Mathf.Max(1, year);
        Month = Mathf.Clamp(month, 1, MonthDays.Length);

        int mdx = Mathf.Clamp(Month - 1, 0, MonthDays.Length - 1);
        int dni = Mathf.Clamp(day, 1, MonthDays[mdx]);

        Day = dni;
        Hour = Mathf.Clamp(hour, 0, 23);
        Minute = Mathf.Clamp(minute, 0, 59);
        Second = Mathf.Clamp(second, 0, 59);

        // 重算派生并广播（这里按“从细到粗”的粒度发一轮）
        if (OnSecond != null) OnSecond.Invoke();
        if (OnMinute != null) OnMinute.Invoke();
        if (OnHour != null) OnHour.Invoke();
        if (OnDay != null) OnDay.Invoke();
        if (OnMonth != null) OnMonth.Invoke();
        if (OnYear != null) OnYear.Invoke();

        RecalcDerived();
    }

    // 字符串格式化（UI 显示）
    public string GetTimeString()
    {
        // YYYY-MM-DD HH:mm:ss 例如：0001-04-21 09:05:07
        return string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", Year, Month, Day, Hour, Minute, Second);
    }
}
