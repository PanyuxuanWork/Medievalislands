/***************************************************************************
// File       : AutoLogisticsSystem.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 自动物流系统：
// ***************************************************************************/


// 负责在城市内自动将仓库与生产建筑之间的资源进行调配。
// 每个Tick会执行：
// 1) 从仓库拉取生产建筑需要的原料
// 2) 将生产建筑的成品推回仓库
// 用于在没有AI工人的情况下验证经济链闭环。
using UnityEngine;

public class AutoLogisticsSystem : MonoBehaviour
{
    [Header("Refs")]
    public CityContext City;

    [Header("Logic")]
    public int PullPerTick = 5;   // 每个 Tick 拉多少原料
    public int PushPerTick = 5;   // 每个 Tick 回收多少产出
    public float TickSeconds = 0.1f; // 与 TickSystem 10Hz 对齐

    private float _acc;

    private void Awake()
    {
        if (City == null) { City = GetComponent<CityContext>(); }
    }

    private void Update()
    {
        // 简单按实时秒计时，约每 0.1s 执行一次
        _acc += Time.deltaTime;
        if (_acc < TickSeconds) return;
        _acc = 0f;

        if (City == null) return;

        // 1) 对每个生产建筑：从最近仓库拉输入
        for (int i = 0; i < City.productions.Count; i++)
        {
            ProductionBuilding prod = City.productions[i];
            if (prod == null || prod.recipe == null || prod.state != BuildingState.Active) continue;

            // 拉取输入（每种原料最多拉 PullPerTick）
            for (int k = 0; k < prod.recipe.inputs.Length; k++)
            {
                ResourceType needType = prod.recipe.inputs[k].type;
                int needAmount = PullPerTick;

                WarehouseBuilding fromWh = FindNearestWarehouseWith(City, prod.transform.position, needType, needAmount);
                if (fromWh != null && fromWh.TryPickup(needType, needAmount))
                {
                    // 投料到工坊输入仓
                    prod.inputInv.Add(needType, needAmount);
                }
            }
        }

        // 2) 回收产出到最近仓库
        for (int i = 0; i < City.productions.Count; i++)
        {
            ProductionBuilding prod = City.productions[i];
            if (prod == null || prod.recipe == null || prod.state != BuildingState.Active) continue;

            for (int k = 0; k < prod.recipe.outputs.Length; k++)
            {
                ResourceType outType = prod.recipe.outputs[k].type;
                int fetchAmount = PushPerTick;

                // 从工坊成品仓扣，推回最近仓库
                if (prod.outputInv.TryConsume(outType, fetchAmount))
                {
                    WarehouseBuilding toWh = FindNearestWarehouse(City, prod.transform.position);
                    if (toWh != null)
                    {
                        toWh.Deliver(outType, fetchAmount);
                    }
                }
            }
        }
    }

    // 寻找最近且持有指定资源的仓库
    private WarehouseBuilding FindNearestWarehouseWith(CityContext city, Vector3 pos, ResourceType t, int minAmount)
    {
        WarehouseBuilding best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < city.warehouses.Count; i++)
        {
            WarehouseBuilding wh = city.warehouses[i];
            if (wh == null) continue;

            int have = wh.Get(t);
            if (have < minAmount) continue;

            float d = (wh.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = wh;
            }
        }
        return best;
    }

    // 最近仓库（不关心库存）
    private WarehouseBuilding FindNearestWarehouse(CityContext city, Vector3 pos)
    {
        WarehouseBuilding best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < city.warehouses.Count; i++)
        {
            WarehouseBuilding wh = city.warehouses[i];
            if (wh == null) continue;

            float d = (wh.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = wh;
            }
        }
        return best;
    }
}

