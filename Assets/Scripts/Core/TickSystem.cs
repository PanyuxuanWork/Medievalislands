/***************************************************************************
// File       : TickSystem.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] DI步进系统
// ***************************************************************************/

using System;
using UnityEngine;

namespace Core
{
    public class TickSystem : MonoSingleton<TickSystem>
    {
        [Range(2, 20)] public int ticksPerSecond = 10; // 10Hz
        public float TickInterval => 1f / ticksPerSecond;

        public static event Action OnTick;

        float _acc;

        void Update()
        {
            _acc += Time.deltaTime;
            while (_acc >= TickInterval)
            {
                _acc -= TickInterval;
                OnTick?.Invoke();
                TickRunner.Instance.TickAll();
            }
        }
    }
}
