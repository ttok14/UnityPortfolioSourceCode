using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;

public enum DeliveryActionType
{
    Standard,
}

public static class DeliveryActionFactory
{
    // public static DeliveryActionInstancePool ActionPool = new DeliveryActionInstancePool();
    public static DeliveryContextDataInstancePool DataPool = new DeliveryContextDataInstancePool();

    // static DeliverActionInitData _actionInitData = new DeliverActionInitData();
    static DeliveryContextInitData _contextInitData = new DeliveryContextInitData();

    // 근데 애네가 여깄는지 맞나? .. Context 의 산하 멤버긴 하니까 일단. 
    static SharedDeliveryHistoryPool<ulong> _visitIDsHashSetPool = new SharedDeliveryHistoryPool<ulong>();

    //public static DeliveryActionBase CreateStrategy(E_ActionType type, ActionData actionData)
    //{
    //    _actionInitData.data = actionData;

    //    switch (type)
    //    {
    //        case E_ActionType.Penetrate:
    //            return ActionPool.GetOrCreate<PenetrateAction>(_actionInitData);
    //        case E_ActionType.Spawn:
    //            return ActionPool.GetOrCreate<>(_actionInitData);
    //        default:
    //            return null;
    //    }
    //}

    //public static DeliveryContext GetDeliveryContext(DeliveryContext src)
    //{
    //    _contextInitData.Set(src);
    //    return DataPool.GetOrCreate<DeliveryContext>(_contextInitData);
    //}

    public static DeliveryContext GetDeliveryContext(
        DeliveryContext src,
        ulong executorID,
        EntityTeamType executorTeam,
        EntityTeamType targetTeam,
        int targetLayerMask,
        E_CollisionRangeType collisionType,
        float collisionRange,
        bool allowMultiHit,
        uint preferMaxTargetCount,
        uint damage,
        uint heal,
        bool fxPerTargetOrDeliverySelf,
        string[] sfxKeys,
        string[] fxKeys,
        float physicalForce,
        E_DeliveryContextInheritType inheritType)
    {
        _contextInitData.Set(
            src,
            executorID,
            executorTeam,
            targetTeam,
            targetLayerMask,
            collisionType,
            collisionRange,
            allowMultiHit,
            preferMaxTargetCount,
            damage,
            heal,
            fxPerTargetOrDeliverySelf,
            sfxKeys,
            fxKeys,
            physicalForce,
            inheritType);

        return DataPool.GetOrCreate<DeliveryContext>(_contextInitData);
    }

    public static DeliveryActionBase ToAction(ActionData data)
    {
        if (data == null)
        {
            TEMP_Logger.Err($"Given ActionData is null");
            return null;
        }

        DeliveryActionBase result = null;

        switch (data.Type)
        {
            case E_ActionType.Penetrate:
                result = new PenetrateAction();
                break;
            case E_ActionType.FixedSpawn:
                result = new FixedSpawnAction();
                break;
            case E_ActionType.AreaDamage:
                result = new AreaDamageAction();
                break;
            case E_ActionType.ChainBounceClosestSpawn:
                result = new ChainBounceClosestAction();
                break;
            case E_ActionType.ChainBounceRandomSpawn:
                result = new ChainBounceRandomAction();
                break;
            case E_ActionType.MultiTargetHit:
                result = new MultiTargetHitAction();
                break;
            case E_ActionType.SkyFallSpawn:
                result = new SkyFallSpawnAction();
                break;
            default:
                TEMP_Logger.Err($"Not Implemented Type : {data.Type}");
                break;
        }

        if (result != null)
            result.Initialize(data);

        return result;
    }

    public static SharedDeliveryHistory<ulong> GetOrCreateVisitIDHashSet(E_DeliveryContextInheritType inheritType, SharedDeliveryHistory<ulong> src)
    {
        // _visitIDsHashSetPool.InitDataCache.InheritType = inheritType;
        var result = _visitIDsHashSetPool.GetOrCreate(null);

        // 원본으로 부터 Copy 진행 
        if (inheritType == E_DeliveryContextInheritType.Copy && src != null)
        {
            result.VisitedIDs.UnionWith(src.VisitedIDs);
            result.ImpactedIDs.UnionWith(src.ImpactedIDs);
        }
        // Reset 타입은 어차피 Pool 에서 꺼내어질때 알아서 Reset 된 상태일 것임

        return result;
    }

    public static void ReturnVisitIdsHashSet(SharedDeliveryHistory<ulong> instance)
    {
        _visitIDsHashSetPool.Return(instance);
    }
}

//public class DeliveryActionDataInstancePool
//{
//    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
//    {
//        // [typeof(DeliveryContext)] = new InstancePool<DeliveryContext>(() => new DeliveryContext()),
//    };

//    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : DeliveryActionBase
//    {
//        var pool = GetPool<InstanceType>();
//        if (pool == null)
//        {
//            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
//            return null;
//        }

//        return pool.GetOrCreate(initData);
//    }

//    public InstancePool<T> GetPool<T>() where T : DeliveryContext
//    {
//        if (Pools.TryGetValue(typeof(T), out var pool))
//            return pool as InstancePool<T>;

//        return null;
//    }

//    public void Return<T>(T element) where T : DeliveryContext
//    {
//        var pool = GetPool<T>();
//        pool.Return(element);
//    }
//}

//public class DeliveryActionInstancePool
//{
//    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
//    {
//        // [typeof()] = new InstancePool<>(() => new ()),
//    };

//    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : DeliveryActionBase
//    {
//        var pool = GetPool<InstanceType>();
//        if (pool == null)
//        {
//            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
//            return null;
//        }

//        return pool.GetOrCreate(initData);
//    }

//    public InstancePool<T> GetPool<T>() where T : DeliveryActionBase
//    {
//        if (Pools.TryGetValue(typeof(T), out var pool))
//            return pool as InstancePool<T>;

//        return null;
//    }

//    public void Return<T>(T element) where T : DeliveryActionBase
//    {
//        var pool = GetPool<T>();
//        pool.Return(element);
//    }
//}

public class DeliveryContextInitData : IInstancePoolInitData
{
    public DeliveryContext Source;

    public ulong ExecutorID;
    public EntityTeamType ExecutorTeam;
    public EntityTeamType TargetTeam;
    public int TargetLayerMask;
    public E_CollisionRangeType CollisionType;
    public float CollisionRange;
    public float PhysicalForce;
    public bool AllowMultiHit;
    public uint PreferMaxTargetCount;
    public uint Damage;
    public uint Heal;
    public bool FXPerTargetOrDeliverySelf;
    public string[] SFXKeys;
    public string[] FXKeys;
    public E_DeliveryContextInheritType InheritType;

    public void Set(
        DeliveryContext source,
        ulong executorId,
        EntityTeamType executorTeam,
        EntityTeamType targetTeam,
        int targetLayerMask,
        E_CollisionRangeType collisionType,
        float collisionRange,
        bool allowMultiHit,
        uint preferMaxTargetCount,
        uint damage,
        uint heal,
        bool fxPerTargetOrDeliverySelf,
        string[] sfxKeys,
        string[] fxKeys,
        float physicalForce,
        E_DeliveryContextInheritType inheritType)
    {
        Source = source;

        ExecutorID = executorId;
        ExecutorTeam = executorTeam;
        TargetTeam = targetTeam;
        TargetLayerMask = targetLayerMask;
        CollisionType = collisionType;
        CollisionRange = collisionRange;
        AllowMultiHit = allowMultiHit;
        PreferMaxTargetCount = preferMaxTargetCount;
        Damage = damage;
        Heal = heal;
        FXPerTargetOrDeliverySelf = fxPerTargetOrDeliverySelf;
        SFXKeys = sfxKeys;
        FXKeys = fxKeys;
        PhysicalForce = physicalForce;
        InheritType = inheritType;
    }
}

public class DeliveryContextDataInstancePool
{
    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {
        [typeof(DeliveryContext)] = new InstancePool<DeliveryContext>(() => new DeliveryContext()),
    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : DeliveryContext
    {
        var pool = GetPool<InstanceType>();
        if (pool == null)
        {
            TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
            return null;
        }
        return pool.GetOrCreate(initData);
    }

    public InstancePool<T> GetPool<T>() where T : DeliveryContext
    {
        if (Pools.TryGetValue(typeof(T), out var pool))
            return pool as InstancePool<T>;

        return null;
    }

    public void Return<T>(T element) where T : DeliveryContext
    {
        var pool = GetPool<T>();
        pool.Return(element);
    }
}

