using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;

public static class DeliveryUpdateFactory
{
    static DeliveryUpdateInstancePool _instancePool = new DeliveryUpdateInstancePool();

    static readonly Dictionary<E_UpdateLogicType, IInstancePoolInitData> _initData = new Dictionary<E_UpdateLogicType, IInstancePoolInitData>()
    {
        [E_UpdateLogicType.Timer] = new TimeLimitInitData(),
        [E_UpdateLogicType.Interval] = new IntervalUpdateInitData()
    };

    public static IDeliveryUpdateStrategy GetStrategy(E_UpdateLogicType type, float param)
    {
        if (type == E_UpdateLogicType.None)
            return null;

        var initData = GetInitData(type, param);

        if (initData == null)
        {
            TEMP_Logger.Err($"Failed to create UpdateStrategy | type : {type} , param : {param}");
            return null;
        }

        switch (type)
        {
            case E_UpdateLogicType.Timer:
                return _instancePool.GetOrCreate<TimeLimitStrategy>(initData);
            case E_UpdateLogicType.Interval:
                return _instancePool.GetOrCreate<IntervalUpdateStrategy>(initData);
            default:
                return null;
        }
    }

    static IInstancePoolInitData GetInitData(E_UpdateLogicType type, float param)
    {
        if (_initData.TryGetValue(type, out var data) == false)
        {
            TEMP_Logger.Err($"Type ({type}) InitData does not exist");
            return null;
        }

        switch (type)
        {
            case E_UpdateLogicType.Timer:
                (data as TimeLimitInitData).LimitTime = param;
                break;
            case E_UpdateLogicType.Interval:
                (data as IntervalUpdateInitData).Interval = param;
                break;
            default:
                return null;
        }

        return data;
    }

    public static void Return<T>(T instance) where T : IDeliveryUpdateStrategy
    {
        _instancePool.Return(instance);
    }

    public class DeliveryUpdateInstancePool
    {
        public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
        {
            [typeof(TimeLimitStrategy)] = new InstancePool<TimeLimitStrategy>(() => new TimeLimitStrategy()),
            [typeof(IntervalUpdateStrategy)] = new InstancePool<IntervalUpdateStrategy>(() => new IntervalUpdateStrategy()),
        };

        public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : IDeliveryUpdateStrategy
        {
            var pool = GetPool<InstanceType>();
            if (pool == null)
            {
                TEMP_Logger.Err($"Failed to get Pool TypeOf : {typeof(InstanceType)}");
                return default;
            }

            return pool.GetOrCreate(initData);
        }

        public InstancePool<T> GetPool<T>() where T : IDeliveryUpdateStrategy
        {
            if (Pools.TryGetValue(typeof(T), out var pool))
                return pool as InstancePool<T>;

            return null;
        }

        public void Return<T>(T element) where T : IDeliveryUpdateStrategy
        {
            var pool = GetPool<T>();
            pool.Return(element);
        }
    }

}
