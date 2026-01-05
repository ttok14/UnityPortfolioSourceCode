using System;
using System.Collections.Generic;
using UnityEngine;

public class SharedDeliveryHistory<T> : IInstancePoolElement
{
    public HashSet<T> VisitedIDs { get; private set; }

    // ImpactStrategy 가 적용된 ID들
    public HashSet<T> ImpactedIDs { get; private set; }

    public uint ReferenceCount { get; private set; }

    Action<SharedDeliveryHistory<T>> _returnHandler;

    public SharedDeliveryHistory(Action<SharedDeliveryHistory<T>> returnHandler)
    {
        _returnHandler = returnHandler;
    }

    public void IncreaseReferenceCount(uint addCount = 1)
    {
        ReferenceCount += addCount;
    }

    public void DecreaseReferenceCount(uint decreaseCount = 1)
    {
        if (ReferenceCount == 0)
        {
            TEMP_Logger.Err($"This SharedDeliveryHistory Ref Count is already zero. This is a bug.");
            return;
        }

        if (decreaseCount >= ReferenceCount)
            ReferenceCount = 0;
        else
            ReferenceCount -= decreaseCount;

        if (ReferenceCount == 0)
            ReturnToPool();
    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        IncreaseReferenceCount();
    }

    public void OnPoolInitialize()
    {
        VisitedIDs = new HashSet<T>();
        ImpactedIDs = new HashSet<T>();

        ReferenceCount = 0;
    }

    public void OnPoolReturned()
    {
        VisitedIDs.Clear();
        ImpactedIDs.Clear();

        ReferenceCount = 0;
    }

    public void ReturnToPool()
    {
        _returnHandler(this);
    }
}
