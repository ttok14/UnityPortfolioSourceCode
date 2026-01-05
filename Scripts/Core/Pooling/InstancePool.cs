using System;
using System.Collections.Generic;
using UnityEngine;

public interface IInstancePoolInitData { }
public interface IInstancePool { }

public class InstancePool<InstanceType> : IInstancePool where InstanceType : IInstancePoolElement
{
    //public enum Result
    //{
    //    None = 0,

    //    CreateNew,
    //    Reuse
    //}

    public int Key;

    private Queue<InstanceType> Instances = new Queue<InstanceType>(32);
    public int Count => Instances.Count;

    private Func<InstanceType> Creator;

    public InstancePool(Func<InstanceType> creator)
    {
        Creator = creator;
    }

    public int _createCount;

    public InstanceType GetOrCreate(IInstancePoolInitData initData)
    {
        InstanceType instance;

        if (Instances.Count > 0)
        {
            instance = Instances.Dequeue();
            //Debug.LogError("Reuse ! : " + initData);
        }
        else
        {
            instance = Creator.Invoke();
            instance.OnPoolInitialize();

#if UNITY_EDITOR
            _createCount++;
#endif
        }

        instance.OnPoolActivated(initData);
        return instance;
    }

    public void Return(InstanceType element)
    {
        element.OnPoolReturned();
        Instances.Enqueue(element);
    }

    public void Clear()
    {
        Instances.Clear();
    }
}

public interface IInstancePoolElement
{
    void OnPoolInitialize();
    void OnPoolActivated(IInstancePoolInitData initData);
    void OnPoolReturned();
    void ReturnToPool();
}
