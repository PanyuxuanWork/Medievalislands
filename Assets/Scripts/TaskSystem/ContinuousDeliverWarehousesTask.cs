/***************************************************************************
// File       : ContinuousDeliverWarehousesTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Description: [TODO] 在给定时间上限内，把背包资源逐单位投到城市里的多个仓库：
//              - 背包还有货则每次投 1 单位；
//              - 当前仓库满了则自动切换到下一个可接收的仓库；
//              - 若所有仓库都满 / 拒收，则任务失败；
//              - 带移动、节流间隔 perUnitInterval、总超时 timeBudgetSec。
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public class ContinuousDeliverWarehousesTask : TaskBase
{
    private ResourceType _type;
    private float _perUnitInterval;         // 每次投 1 单位的最小间隔（秒）
    private float _timeBudgetSec;           // 整体超时（秒）

    private CityContext _city;
    private List<IStorage> _warehouses = new List<IStorage>();
    private int _currentIndex = -1;

    private float _startTime;
    private float _lastOpTime = -999f;

    private enum Phase { SelectWarehouse, MoveTo, DeliverLoop }
    private Phase _phase = Phase.SelectWarehouse;

    public static ContinuousDeliverWarehousesTask Create(ResourceType type, float perUnitInterval = 0.15f, float timeBudgetSec = 20f, int priority = 0)
    {
        ContinuousDeliverWarehousesTask t = new ContinuousDeliverWarehousesTask();
        t._type = type;
        t._perUnitInterval = Mathf.Max(0.01f, perUnitInterval);
        t._timeBudgetSec = Mathf.Max(1f, timeBudgetSec);
        t.Priority = priority;
        return t;
    }

    protected override void OnStart()
    {
        if (Ctx == null || Ctx.Owner == null || Ctx.Mover == null || Ctx.Owner.Inventory == null)
        {
            TLog.Warning("[DeliverLoop] 上下文无效。");
            Fail(); return;
        }
        if (Ctx.Owner.Inventory.Get(_type) <= 0)
        {
            TLog.Warning("[DeliverLoop] 背包为空，无需投递。");
            Fail(); return;
        }

        _city = Ctx.City;
        if (_city == null || _city.warehouses == null || _city.warehouses.Count == 0)
        {
            TLog.Warning("[DeliverLoop] 城市内没有仓库。");
            Fail(); return;
        }

        // 收集可接收的仓库（Get < Capacity），按距离排序
        _warehouses.Clear();
        Vector3 pos = Ctx.Owner.transform.position;
        List<WarehouseBuilding> list = _city.warehouses;
        List<(IStorage stor, float d2)> temp = new List<(IStorage, float)>();

        for (int i = 0; i < list.Count; i++)
        {
            WarehouseBuilding w = list[i];
            if (w == null) continue;
            IStorage s = w as IStorage;
            if (s == null) continue;

            // 是否可接收该资源？（默认按容量判断：Get < Capacity）
            bool canRecv = true;
            // 如果 IStorage 暴露 Capacity 和 Get：
            try
            {
                canRecv = s.Get(_type) < s.Capacity;
            }
            catch { /* 若未实现 Capacity，这里默认 true */ }

            if (!canRecv) continue;

            float d2 = (w.transform.position - pos).sqrMagnitude;
            temp.Add((s, d2));
        }
        temp.Sort((a, b) => a.d2.CompareTo(b.d2));
        for (int i = 0; i < temp.Count; i++) _warehouses.Add(temp[i].stor);

        if (_warehouses.Count == 0)
        {
            TLog.Warning("[DeliverLoop] 没有可接收该资源的仓库。");
            Fail(); return;
        }

        _startTime = Time.time;
        _lastOpTime = -999f;
        _phase = Phase.SelectWarehouse;

        TLog.Log("[DeliverLoop] 启动；资源=" + _type + "，初始携带=" + Ctx.Owner.Inventory.Get(_type) + "，候选仓库数=" + _warehouses.Count, LogColor.Cyan);
    }

    protected override void OnTick()
    {
        if (Status != TaskStatus.Running) return;

        // 背包清空 → 成功
        if (Ctx.Owner.Inventory.Get(_type) <= 0)
        {
            TLog.Log("[DeliverLoop] 背包清空，成功。", LogColor.Green);
            Succeed(); return;
        }

        // 超时
        if (Time.time - _startTime > _timeBudgetSec)
        {
            TLog.Warning("[DeliverLoop] 超时，失败。剩余携带 " + Ctx.Owner.Inventory.Get(_type));
            Fail(); return;
        }

        switch (_phase)
        {
            case Phase.SelectWarehouse:
                if (!SelectNextWarehouseCapable()) { TLog.Warning("[DeliverLoop] 所有仓库均无法接收该资源，失败。"); Fail(); return; }
                _phase = Phase.MoveTo;
                MoveToCurrentWarehouse();
                break;

            case Phase.MoveTo:
                if (!Ctx.Mover.IsMoving()) _phase = Phase.DeliverLoop;
                break;

            case Phase.DeliverLoop:
                if (Time.time - _lastOpTime < _perUnitInterval) return;
                _lastOpTime = Time.time;

                IStorage s = GetCurrentStorage();
                if (s == null) { _phase = Phase.SelectWarehouse; return; }

                // 可接收吗？（按容量判断）
                bool canRecv = true;
                try { canRecv = s.Get(_type) < s.Capacity; } catch { canRecv = true; }
                if (!canRecv)
                {
                    _phase = Phase.SelectWarehouse;
                    return;
                }

                // 逐单位投：先从背包扣 1，再 Deliver(1)
                if (!Ctx.Owner.Inventory.TryTake(_type, 1))
                {
                    // 背包可能刚好清空了
                    TLog.Log("[DeliverLoop] 尝试扣 1 失败，可能已清空。", LogColor.Grey, showInConsole: false);
                    return;
                }

                s.Deliver(_type, 1);
                // TLog.Log("[DeliverLoop] 投 1 成功，背包剩=" + Ctx.Owner.Inventory.Get(_type), LogColor.Grey, showInConsole:false);
                break;
        }
    }

    private bool SelectNextWarehouseCapable()
    {
        // 轮询找到第一个“可接收”的仓库
        for (int i = 0; i < _warehouses.Count; i++)
        {
            int idx = ((_currentIndex + 1) + i) % _warehouses.Count;
            IStorage s = _warehouses[idx];
            if (s == null) continue;

            bool canRecv = true;
            try { canRecv = s.Get(_type) < s.Capacity; } catch { canRecv = true; }

            if (canRecv)
            {
                _currentIndex = idx;
                return true;
            }
        }
        return false;
    }

    private void MoveToCurrentWarehouse()
    {
        Transform tr = TryGetTransform(GetCurrentStorage());
        if (tr == null)
        {
            TLog.Warning("[DeliverLoop] 当前仓库没有 Transform。");
            Fail(); return;
        }
        Ctx.Mover.MoveTo(tr.position);
    }

    private IStorage GetCurrentStorage()
    {
        if (_currentIndex < 0 || _currentIndex >= _warehouses.Count) return null;
        return _warehouses[_currentIndex];
    }

    private Transform TryGetTransform(IStorage s)
    {
        MonoBehaviour mb = s as MonoBehaviour;
        return mb != null ? mb.transform : null;
    }
}
