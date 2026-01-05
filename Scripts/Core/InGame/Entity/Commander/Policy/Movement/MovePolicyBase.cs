using System;
using System.Collections.Generic;
using UnityEngine;

public class MovePolicyInitDataBase : IInstancePoolInitData
{
    public EntityBase Mover;

    public void Set(EntityBase mover)
    {
        Mover = mover;
    }
}

public abstract class MovePolicyBase : IMovePolicy
{
    protected EntityBase _mover;

    public abstract MoveCommand GetCommand(EntityBase target);

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as MovePolicyInitDataBase;
        _mover = data.Mover;
    }

    public virtual void OnPoolInitialize()
    {
    }

    public virtual void OnPoolReturned()
    {
        _mover = null;
    }

    public abstract void ReturnToPool();
}
