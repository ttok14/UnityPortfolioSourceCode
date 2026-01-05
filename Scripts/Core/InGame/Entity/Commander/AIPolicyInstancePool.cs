using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAIInstancePool
{
    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {
        [typeof(EntityAIBehaviour)] = new InstancePool<EntityAIBehaviour>(() => new EntityAIBehaviour()),
        [typeof(AggroTargetingPolicy)] = new InstancePool<AggroTargetingPolicy>(() => new AggroTargetingPolicy()),
        [typeof(ToTargetMovePolicy)] = new InstancePool<ToTargetMovePolicy>(() => new ToTargetMovePolicy()),
        [typeof(StaticPatrolPolicy)] = new InstancePool<StaticPatrolPolicy>(() => new StaticPatrolPolicy()),
        [typeof(MoveGuardAndCounterPolicy)] = new InstancePool<MoveGuardAndCounterPolicy>(() => new MoveGuardAndCounterPolicy()),
        [typeof(FixedTargetingWithAggroPolicy)] = new InstancePool<FixedTargetingWithAggroPolicy>(() => new FixedTargetingWithAggroPolicy())
    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : IInstancePoolElement
    {
        var pool = GetPool<InstanceType>();
        if (pool == null)
        {
            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
            return default;
        }

        //Debug.Log("GetAllCreate : " + initData + " , " + typeof(InstanceType));
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
