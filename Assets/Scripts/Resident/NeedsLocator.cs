/***************************************************************************
// File       : NeedsLocator.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// ===============================
// Resident v2 - Tasks & AI Decisions
// 目标：让 Resident 具备可运行的“吃/喝/睡/上班/游荡”最小循环
// 说明：
// - 所有仿真推进均通过 ISimTickable（ResidentAI/ResidentStats 已实现）。
// - 这里新增：通用找点器（NeedsLocator）、供给点接口（INeedsProvider），
//   以及 4 个任务（EatDrinkTask / SleepAtHomeTask / WorkAtWorkplaceTask / WanderTask）。
// - ResidentAI 改为基于 WorkShift + 体征 + GlobalTime 进行意图决策（未取 TaskManager 时）。
// - 你仍可保留原 TaskManager：若无内部意图，可作为兜底请求一个任务。
// [TODO] INeedsProvider：提供“食物/饮水”的建筑或点位实现该接口
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace ResidentNamespace
{
    public interface INeedsProvider
    {
        Transform GetEntrance();          // 供通勤/靠近（可为空）
        bool CanServeFood();
        bool CanServeWater();
        // amount 返回实际提供量，便于后续做库存/价格
        int ServeFood(int requestedAmount);
        int ServeWater(int requestedAmount);
    }

    public class NeedsLocator : MonoSingleton<NeedsLocator>
    {
        private static readonly List<INeedsProvider> _providers = new List<INeedsProvider>();

        public static void Register(INeedsProvider p)
        {
            if (p == null) return;
            if (!_providers.Contains(p)) _providers.Add(p);
        }

        public static void Unregister(INeedsProvider p)
        {
            if (p == null) return;
            _providers.Remove(p);
        }

        public INeedsProvider FindNearestFood(Vector3 from)
        {
            INeedsProvider best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _providers.Count; i++)
            {
                INeedsProvider p = _providers[i];
                if (p == null || !p.CanServeFood()) continue;
                Transform e = p.GetEntrance();
                Vector3 pos = e != null ? e.position : (p as Component).transform.position;
                float d = (pos - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public INeedsProvider FindNearestWater(Vector3 from)
        {
            INeedsProvider best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _providers.Count; i++)
            {
                INeedsProvider p = _providers[i];
                if (p == null || !p.CanServeWater()) continue;
                Transform e = p.GetEntrance();
                Vector3 pos = e != null ? e.position : (p as Component).transform.position;
                float d = (pos - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
