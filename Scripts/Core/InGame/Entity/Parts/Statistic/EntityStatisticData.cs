using GameDB;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityStatisticInitData : EntityDataInitDataBase
{

}

public class EntityStatisticData : EntityDataBase
{
    public uint KillCount { get; private set; }

    public void SetkillCount(uint killCount)
    {
        KillCount = killCount;
        _owner.DataModifiedListener?.Invoke(EntityDataCategory.Statistic, this);
    }

    public void ResetKillCount()
    {
        KillCount = 0;
        _owner.DataModifiedListener?.Invoke(EntityDataCategory.Statistic, this);
    }

    public void IncreaseKillCount(uint count = 1)
    {
        KillCount += count;
        _owner.DataModifiedListener?.Invoke(EntityDataCategory.Statistic, this);
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        KillCount = 0;
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityDataPool.Return(this);
    }
}
