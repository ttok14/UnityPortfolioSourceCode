using System;
using System.Collections.Generic;
using UnityEngine;
public class EntityDataInstancePool
{
    /*
     * TODO (?) : 팩토리를 주입해서 중간에 좀 디테일한 제어를 할 일이 있을까? 
     * */

    Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {
        [typeof(EntityStatData)] = new InstancePool<EntityStatData>(() => new EntityStatData()),
        [typeof(EntityOccupationData)] = new InstancePool<EntityOccupationData>(() => new EntityOccupationData()),
        [typeof(EntityStatisticData)] = new InstancePool<EntityStatisticData>(() => new EntityStatisticData()),
    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : EntityDataBase
    {
        var pool = GetPool<InstanceType>();
        if (pool == null)
        {
            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)} | Please Add.");
            return null;
        }

        return pool.GetOrCreate(initData);
    }

    public InstancePool<T> GetPool<T>() where T : IInstancePoolElement
    {
        if (Pools.TryGetValue(typeof(T), out var pool))
            return pool as InstancePool<T>;

        return null;
    }

    public void Return<T>(T element) where T : IInstancePoolElement
    {
        var pool = GetPool<T>();
        pool.Return(element);
    }
}
