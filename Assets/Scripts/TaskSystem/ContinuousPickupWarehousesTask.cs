/***************************************************************************
// File       : ContinuousPickupWarehousesTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Description: [TODO] 在给定时间上限内，从城市里的多个仓库逐单位获取资源：
//              - 背包未达目标 carryGoal，则每次取 1 单位；
//              - 当前仓库无货则自动切换到下一个有货仓库；
//              - 若所有仓库均无该资源，则任务失败；
//              - 带移动、节流间隔 perUnitInterval、总超时 timeBudgetSec。
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public class ContinuousPickupWarehousesTask : TaskBase
{
    private ResourceType _type;
    private int _carryGoal;                 // 目标携带量（相当于“背包上限”），达到即成功
    private float _perUnitInterval;         // 每次取 1 单位的最小间隔（秒）
    private float _timeBudgetSec;           // 整体超时（秒）

    private CityContext _city;
    private List<IStorage> _warehouses = new List<IStorage>();
    private int _currentIndex = -1;

    private float _startTime;
    private float _lastOpTime = -999f;

    private enum Phase { SelectWarehouse, MoveTo, PickupLoop }
    private Phase _phase = Phase.SelectWarehouse;

    public static ContinuousPickupWarehousesTask Create(ResourceType type, int carryGoal, float perUnitInterval = 0.15f, float timeBudgetSec = 20f, int priority = 0)
    {
        ContinuousPickupWarehousesTask t = new ContinuousPickupWarehousesTask();
        t._type = type;
        t._carryGoal = Mathf.Max(1, carryGoal);
        t._perUnitInterval = Mathf.Max(0.01f, perUnitInterval);
        t._timeBudgetSec = Mathf.Max(1f, timeBudgetSec);
        t.Priority = priority;
        return t;
    }

    protected override void OnStart()
    {
        if (Ctx == null || Ctx.Owner == null || Ctx.Mover == null)
        {
            TLog.Warning("[PickupLoop] 上下文无效。");
            Fail(); return;
        }
        _city = Ctx.City;
        if (_city == null || _city.warehouses == null || _city.warehouses.Count == 0)
        {
            TLog.Warning("[PickupLoop] 城市内没有仓库。");
            Fail(); return;
        }

        // 收集所有实现了 IStorage 的仓库，并按与 Resident 的距离排序（就近优先）
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
            float d2 = (w.transform.position - pos).sqrMagnitude;
            temp.Add((s, d2));
        }
        temp.Sort((a, b) => a.d2.CompareTo(b.d2));
        for (int i = 0; i < temp.Count; i++) _warehouses.Add(temp[i].stor);

        _startTime = Time.time;
        _lastOpTime = -999f;
        _phase = Phase.SelectWarehouse;

        TLog.Log("[PickupLoop] 启动；资源=" + _type + "，目标携带=" + _carryGoal + "，仓库数=" + _warehouses.Count, LogColor.Cyan);
    }

    protected override void OnTick()
    {
        if (Status != TaskStatus.Running) return;

        // 总超时
        if (Time.time - _startTime > _timeBudgetSec)
        {
            TLog.Warning("[PickupLoop] 超时，失败。当前携带 " + Ctx.Owner.Inventory.Get(_type));
            Fail(); return;
        }

        // 已达成携带目标
        if (Ctx.Owner.Inventory.Get(_type) >= _carryGoal)
        {
            TLog.Log("[PickupLoop] 达成携带目标，成功。", LogColor.Green);
            Succeed(); return;
        }

        switch (_phase)
        {
            case Phase.SelectWarehouse:
                if (!SelectNextWarehouseWithStock()) { TLog.Warning("[PickupLoop] 所有仓库无该资源，失败。"); Fail(); return; }
                _phase = Phase.MoveTo;
                MoveToCurrentWarehouse();
                break;

            case Phase.MoveTo:
                if (!Ctx.Mover.IsMoving()) _phase = Phase.PickupLoop;
                break;

            case Phase.PickupLoop:
                // 间隔节流
                if (Time.time - _lastOpTime < _perUnitInterval) return;
                _lastOpTime = Time.time;

                // 尝试从当前仓库取 1 单位
                IStorage s = GetCurrentStorage();
                if (s == null) { _phase = Phase.SelectWarehouse; return; }

                // 仓库还有货吗？
                if (s.Get(_type) <= 0)
                {
                    // 换下一个有货仓库
                    _phase = Phase.SelectWarehouse;
                    return;
                }

                // 尝试取 1 单位
                if (s.TryPickup(_type, 1))
                {
                    Ctx.Owner.Inventory.Add(_type, 1);
                    // 达标即成功；否则继续
                    // TLog.Log("[PickupLoop] 取 1 成功，现携带=" + Ctx.Owner.Inventory.Get(_type), LogColor.Grey, showInConsole:false);
                }
                else
                {
                    // 当前仓库可能刚好被清空或受限 → 尝试下一个
                    _phase = Phase.SelectWarehouse;
                }
                break;
        }
    }

    private bool SelectNextWarehouseWithStock()
    {
        // 遍历所有仓库，找到第一个有库存的
        for (int i = 0; i < _warehouses.Count; i++)
        {
            int idx = ((_currentIndex + 1) + i) % _warehouses.Count;
            IStorage s = _warehouses[idx];
            if (s != null && s.Get(_type) > 0)
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
            TLog.Warning("[PickupLoop] 当前仓库没有 Transform。");
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
