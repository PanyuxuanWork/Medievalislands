/***************************************************************************
// File       : PickupTask.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Description: [TODO] 从目标点取货到居民背包；支持 IStorage / IProducer；
//              资源不足时策略：Exact / TakeAllAvailable / WaitUntilAvailable；统一 TLog。
// ***************************************************************************/

using UnityEngine;

public enum PickupPolicy
{
    Exact = 0,              // 必须足量，不足即 Fail
    TakeAllAvailable = 1,   // 不足则把当前可用的全部拿走（>=1）
    WaitUntilAvailable = 2  // 不足则等待，间隔重试，超时仍不足则 Fail
}

public class PickupTask : TaskBase
{
    private object _from;                 // IStorage 或 IProducer
    private ResourceType _type;
    private int _requestAmount;

    // 策略相关
    private PickupPolicy _policy = PickupPolicy.TakeAllAvailable;
    private float _retryInterval = 1.0f;      // 等待模式下的重试间隔（秒）
    private float _timeoutSeconds = 15.0f;    // 等待模式下的最长等待（秒）
    private float _lastTryTime = -999f;
    private float _startTime = -1f;

    public static PickupTask Create(
        object from, ResourceType type, int amount,
        PickupPolicy policy = PickupPolicy.TakeAllAvailable,
        float retryIntervalSec = 1.0f,
        float timeoutSec = 15.0f,
        int priority = 0)
    {
        PickupTask t = new PickupTask();
        t._from = from;
        t._type = type;
        t._requestAmount = amount;
        t._policy = policy;
        t._retryInterval = retryIntervalSec;
        t._timeoutSeconds = timeoutSec;
        t.Priority = priority;
        return t;
    }

    protected override void OnStart()
    {
        _startTime = Time.time;
        _lastTryTime = -999f;
        if (_from == null || _requestAmount <= 0)
        {
            TLog.Warning("[PickupTask] 参数非法：来源为空或请求量<=0");
            Fail();
        }
    }

    protected override void OnTick()
    {
        if (Status != TaskStatus.Running) return;
        if (Ctx == null || Ctx.Owner == null || Ctx.Owner.Inventory == null || _from == null)
        {
            TLog.Warning("[PickupTask] 上下文非法。");
            Fail();
            return;
        }

        // Wait 策略的节流与超时
        if (_policy == PickupPolicy.WaitUntilAvailable)
        {
            if (Time.time - _startTime > _timeoutSeconds)
            {
                TLog.Warning("[PickupTask] 等待超时，仍不足以取货：" + _type + " x" + _requestAmount);
                Fail();
                return;
            }
            if (Time.time - _lastTryTime < _retryInterval)
            {
                return; // 等到下次重试再试
            }
        }

        _lastTryTime = Time.time;

        // 1) IStorage 路线（有 Get，可直接查可用量）
        IStorage storage = _from as IStorage;
        if (storage != null)
        {
            if (storage.TryPickup(_type, _requestAmount))
            {
                Ctx.Owner.Inventory.Add(_type, _requestAmount);
                TLog.Log("[PickupTask] IStorage 拿到：" + _type + " x" + _requestAmount, LogColor.Green);
                Succeed();
                return;
            }

            // 不足 → 策略
            if (_policy == PickupPolicy.Exact)
            {
                TLog.Warning("[PickupTask] IStorage 不足（Exact），失败：" + _type + " x" + _requestAmount);
                Fail();
                return;
            }
            else if (_policy == PickupPolicy.TakeAllAvailable)
            {
                int have = storage.Get(_type);
                if (have > 0 && storage.TryPickup(_type, have))
                {
                    Ctx.Owner.Inventory.Add(_type, have);
                    TLog.Log("[PickupTask] IStorage 不足，改为全部拿走：" + _type + " x" + have, LogColor.Yellow);
                    Succeed();
                    return;
                }
                else
                {
                    TLog.Warning("[PickupTask] IStorage 可用量为 0，拿不到。");
                    Fail();
                    return;
                }
            }
            else // Wait
            {
                TLog.Log("[PickupTask] IStorage 不足，等待中… 目标：" + _type + " x" + _requestAmount, LogColor.Grey, showInConsole: false);
                return;
            }
        }

        // 2) IProducer 路线（无 Get，用渐降尝试）
        IProducer producer = _from as IProducer;
        if (producer != null)
        {
            if (producer.TryCollect(_type, _requestAmount))
            {
                Ctx.Owner.Inventory.Add(_type, _requestAmount);
                TLog.Log("[PickupTask] IProducer 拿到：" + _type + " x" + _requestAmount, LogColor.Green);
                Succeed();
                return;
            }

            if (_policy == PickupPolicy.Exact)
            {
                TLog.Warning("[PickupTask] IProducer 不足（Exact），失败：" + _type + " x" + _requestAmount);
                Fail();
                return;
            }
            else if (_policy == PickupPolicy.TakeAllAvailable)
            {
                for (int take = _requestAmount - 1; take >= 1; take--)
                {
                    if (producer.TryCollect(_type, take))
                    {
                        Ctx.Owner.Inventory.Add(_type, take);
                        TLog.Log("[PickupTask] IProducer 不足，改为全部拿走（可得）：" + _type + " x" + take, LogColor.Yellow);
                        Succeed();
                        return;
                    }
                }
                TLog.Warning("[PickupTask] IProducer 当前可得 0。");
                Fail();
                return;
            }
            else // Wait
            {
                TLog.Log("[PickupTask] IProducer 不足，等待中… 目标：" + _type + " x" + _requestAmount, LogColor.Grey, showInConsole: false);
                return;
            }
        }

        TLog.Warning("[PickupTask] 目标不实现 IStorage/IProducer。");
        Fail();
    }
}
