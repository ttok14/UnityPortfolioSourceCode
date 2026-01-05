using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PostHitInitDataBase : IInstancePoolInitData
{

    public void Set()
    {

    }
}

public class PostHitInstancePool
{

    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {

    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : PostHitBase
    {
        var pool = GetPool<InstanceType>();
        if (pool == null)
        {
            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
            return null;
        }

        return pool.GetOrCreate(initData);
    }

    public InstancePool<T> GetPool<T>() where T : PostHitBase
    {
        if (Pools.TryGetValue(typeof(T), out var pool))
            return pool as InstancePool<T>;

        return null;
    }

    public void Return<T>(T element) where T : PostHitBase
    {
        var pool = GetPool<T>();
        pool.Return(element);
    }
}
