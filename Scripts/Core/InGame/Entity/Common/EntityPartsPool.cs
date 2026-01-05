using System;
using System.Collections.Generic;
using UnityEngine;
public class EntityPartsInstancePool
{
    /*
     * TODO (?) : 팩토리를 주입해서 중간에 좀 디테일한 제어를 할 일이 있을까? 
     * */

    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {
        [typeof(EntityStandardMovePart)] = new InstancePool<EntityStandardMovePart>(() => new EntityStandardMovePart()),
        [typeof(EntityPatrolMovePart)] = new InstancePool<EntityPatrolMovePart>(() => new EntityPatrolMovePart()),
        [typeof(EntityAIPart)] = new InstancePool<EntityAIPart>(() => new EntityAIPart()),
        [typeof(EntityAnimationPart)] = new InstancePool<EntityAnimationPart>(() => new EntityAnimationPart()),
        [typeof(EntitySkillPart)] = new InstancePool<EntitySkillPart>(() => new EntitySkillPart()),
        [typeof(EntitySpellPart)] = new InstancePool<EntitySpellPart>(() => new EntitySpellPart()),
        [typeof(EntityStatPart)] = new InstancePool<EntityStatPart>(() => new EntityStatPart()),
        [typeof(EntityResourceGeneratePart)] = new InstancePool<EntityResourceGeneratePart>(() => new EntityResourceGeneratePart()),
        [typeof(EntitySpawnerPart)] = new InstancePool<EntitySpawnerPart>(() => new EntitySpawnerPart())
    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : EntityPartBase
    {
        var pool = GetPool<InstanceType>();
        if (pool == null)
        {
            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
            return null;
        }

        //Debug.Log("GetAllCreate : " + initData + " , " + typeof(InstanceType));
        return pool.GetOrCreate(initData);
    }

    public InstancePool<T> GetPool<T>() where T : EntityPartBase
    {
        if (Pools.TryGetValue(typeof(T), out var pool))
            return pool as InstancePool<T>;

        return null;
    }

    public void Return<T>(T element) where T : EntityPartBase
    {
        var pool = GetPool<T>();
        pool.Return(element);
    }
}
