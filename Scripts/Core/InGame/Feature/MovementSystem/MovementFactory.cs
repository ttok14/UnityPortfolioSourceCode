using GameDB;
using System.Collections.Generic;
using UnityEngine;

public static class MovementFactory
{
    static Dictionary<E_ProjectileMovementType, IInstancePoolInitData> _initDataCache = new Dictionary<E_ProjectileMovementType, IInstancePoolInitData>();

    public static void Initialize()
    {
        _initDataCache.Clear();

        _initDataCache.Add(E_ProjectileMovementType.Linear, new LinearMovementInitData());
        _initDataCache.Add(E_ProjectileMovementType.Curve, new CurveMovementInitData());
    }

    public static MovementStrategyBase Create(E_ProjectileMovementType type, IMovementDataSource dataSource)
    {
        if (type == E_ProjectileMovementType.None)
            return null;

        var initData = _initDataCache[type];

        switch (type)
        {
            case E_ProjectileMovementType.Linear:
                (initData as LinearMovementInitData).Set(
                    dataSource.Mover,
                    dataSource.Target,
                    dataSource.StartPosition,
                    dataSource.Destination,
                    dataSource.DestOffsetPosition,
                    dataSource.MoveSpeed,
                    dataSource.RotationSpeed,
                    dataSource.AimType);

                return CreateInternal<LinearMovement>(initData);
            case E_ProjectileMovementType.Curve:
                var data = initData as CurveMovementInitData;

                data.Set(
                    dataSource.Mover,
                    dataSource.Target,
                    dataSource.StartPosition,
                    dataSource.Destination,
                    dataSource.DestOffsetPosition,
                    dataSource.MoveSpeed,
                    dataSource.RotationSpeed,
                    dataSource.AimType);

                data.SetCurveData(
                    EntityManager.Instance.CurveData.StandardProjectileMovementCurveData.curve,
                    // Height 우짜할까
                    3f,
                    DG.Tweening.Ease.Linear);

                return CreateInternal<CurveMovement>(initData);
            default:
                TEMP_Logger.Err($"Not implemented type : {type}");
                return null;
        }
    }

    static MovementStrategyBase CreateInternal<T>(IInstancePoolInitData initData) where T : MovementStrategyBase
    {
        return InGameManager.Instance.CacheContainer.MovementStrategyInstancePool.GetOrCreate<T>(initData);
    }

    public static void Return<T>(T instance) where T : MovementStrategyBase
    {
        InGameManager.Instance.CacheContainer.MovementStrategyInstancePool.Return(instance);
    }
}
