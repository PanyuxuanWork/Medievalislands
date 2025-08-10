/***************************************************************************
// File       : Resident.cs
// Author     : Panyuxuan
// Created    : 202507
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using System.Linq;
using UnityEngine;

public class Resident : MonoBehaviour
{
    public enum Role { Lumberjack, Porter, Baker }
    public Role role = Role.Lumberjack;

    public CityContext city;
    public float moveSpeed = 3f;

    Vector3 _target;
    bool _hasTarget;
    ResourceType _carryType;
    int _carryAmount;

    void Start()
    {
        if (city == null) city = FindObjectOfType<CityContext>();
        NewRoutine();
    }

    void Update()
    {
        if (_hasTarget)
        {
            var dir = (_target - transform.position);
            var dist = dir.magnitude;
            if (dist < 0.05f) { _hasTarget = false; Arrive(); }
            else transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void NewRoutine()
    {
        switch (role)
        {
            case Role.Lumberjack: LumberjackRoutine(); break;
            case Role.Baker: BakerRoutine(); break;
            default: PorterRoutine(); break;
        }
    }

    void LumberjackRoutine()
    {
        // 1) 去最近“伐木场”（ProductionBuilding，假定不需要输入，产出 Wood）
        var lumber = city.productions.FirstOrDefault(p => p.recipe && p.recipe.outputs.Any(o => o.type == ResourceType.Wood));
        if (lumber == null) return;
        MoveTo(lumber.transform.position + new Vector3(1, 0, 0));
        _carryType = ResourceType.Wood;
        _carryAmount = 5; // 每趟 5 木材（demo）
    }

    void BakerRoutine()
    {
        // 1) 去仓库取小麦（Wheat），然后送到“面包房”的 inputInv
        var bakery = city.productions.FirstOrDefault(p => p.recipe && p.recipe.outputs.Any(o => o.type == ResourceType.Bread));
        var wh = FindNearestWarehouse(transform.position);
        if (bakery == null || wh == null) return;

        // 先去仓库
        MoveTo(wh.transform.position);
        _carryType = ResourceType.Wheat;
        _carryAmount = 5;
    }

    void PorterRoutine()
    {
        // 示例：把任何生产的输出搬到仓库
        var prod = city.productions.FirstOrDefault();
        var wh = FindNearestWarehouse(transform.position);
        if (prod == null || wh == null) return;

        MoveTo(prod.transform.position + new Vector3(1, 0, 0));
        _carryType = ResourceType.Wood; // demo：默认搬木头
        _carryAmount = 5;
    }

    void Arrive()
    {
        switch (role)
        {
            case Role.Lumberjack:
                {
                    // 把虚拟木头送到最近仓库
                    var wh = FindNearestWarehouse(transform.position);
                    if (wh != null)
                    {
                        wh.Deliver(_carryType, _carryAmount);
                    }
                    // 下一趟
                    LumberjackRoutine();
                    break;
                }
            case Role.Baker:
                {
                    var wh = FindNearestWarehouse(transform.position);
                    var bakery = city.productions.FirstOrDefault(p => p.recipe && p.recipe.outputs.Any(o => o.type == ResourceType.Bread));
                    if (wh != null && bakery != null)
                    {
                        // 第一次到仓库 → 取 Wheat
                        if (wh.TryPickup(_carryType, _carryAmount))
                        {
                            // 第二目标：面包房
                            MoveTo(bakery.transform.position - new Vector3(1, 0, 0));
                            // 投料在到达时做：
                            _onNextArrive = () => {
                                bakery.inputInv.Add(ResourceType.Wheat, _carryAmount);
                                _carryAmount = 0;
                                // 再回仓库取
                                BakerRoutine();
                            };
                            return;
                        }
                    }
                    // 没拿到就稍后重试
                    Invoke(nameof(NewRoutine), 1f);
                    break;
                }
            default:
                {
                    // 简化 porter：送仓库
                    var wh = FindNearestWarehouse(transform.position);
                    if (wh != null) wh.Deliver(_carryType, _carryAmount);
                    PorterRoutine();
                    break;
                }
        }
    }

    System.Action _onNextArrive;

    void MoveTo(Vector3 pos)
    {
        _target = pos; _hasTarget = true;
        _onNextArrive = null;
    }

    void OnTriggerEnter(Collider other)
    {
        // 可扩展拾取/触发
    }

    void LateUpdate()
    {
        if (!_hasTarget && _onNextArrive != null)
        {
            var act = _onNextArrive; _onNextArrive = null; act();
        }
    }

    WarehouseBuilding FindNearestWarehouse(Vector3 pos)
    {
        WarehouseBuilding best = null;
        float bestD = float.MaxValue;
        foreach (var w in city.warehouses)
        {
            float d = (w.transform.position - pos).sqrMagnitude;
            if (d < bestD) { bestD = d; best = w; }
        }
        return best;
    }
}