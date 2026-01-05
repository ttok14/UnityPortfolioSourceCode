using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovementInitDataBase : IInstancePoolInitData
{
    public Transform Mover;
    public EntityBase Target;
    public Vector3 StartPosition;
    public Vector3 FixedDest;
    public Vector3 DestOffset;
    public float MoveSpeed;
    public float RotationSpeed;
    public E_AimType AimType;

    public void Set(
        Transform mover,
        EntityBase target,
        Vector3 startPosition,
        Vector3 fixedDest,
        Vector3 destOffset,
        float moveSpeed,
        float rotationSpeed,
        E_AimType aimType)
    {
        Mover = mover;
        Target = target;
        StartPosition = startPosition;
        FixedDest = fixedDest;
        DestOffset = destOffset;
        MoveSpeed = moveSpeed;
        RotationSpeed = rotationSpeed;
        AimType = aimType;
    }
}

public class MovementInstancePool
{
    /*
 * TODO (?) : 팩토리를 주입해서 중간에 좀 디테일한 제어를 할 일이 있을까? 
 * */

    public Dictionary<Type, IInstancePool> Pools = new Dictionary<Type, IInstancePool>()
    {
        [typeof(LinearMovement)] = new InstancePool<LinearMovement>(() => new LinearMovement()),
        [typeof(CurveMovement)] = new InstancePool<CurveMovement>(() => new CurveMovement()),
    };

    public InstanceType GetOrCreate<InstanceType>(IInstancePoolInitData initData) where InstanceType : MovementStrategyBase
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

    public InstancePool<T> GetPool<T>() where T : MovementStrategyBase
    {
        if (Pools.TryGetValue(typeof(T), out var pool))
            return pool as InstancePool<T>;

        return null;
    }

    public void Return<T>(T element) where T : MovementStrategyBase
    {
        var pool = GetPool<T>();
        pool.Return(element);
    }
}
