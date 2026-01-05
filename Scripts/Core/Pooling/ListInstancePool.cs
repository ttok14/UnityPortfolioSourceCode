using System;
using System.Collections.Generic;

public abstract class PoolableList<T> : IInstancePoolElement
{
    // public bool IsAlive;
    public List<T> Instance;

    public int Count => Instance.Count;

    public virtual void OnPoolInitialize()
    {
        Instance = new List<T>(8);
    }

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        //  if (IsAlive)
        {
            //   TEMP_Logger.Err($"This is Already alive . MUST CHECK !");
        }

        // IsAlive = true; 
    }

    public virtual void OnPoolReturned()
    {
        Instance.Clear();
        // IsAlive = false;
    }

    public abstract void ReturnToPool();
}

public class ListInstancePool<T, T2> where T : PoolableList<T2>, new()
{
    InstancePool<T> _pool = new InstancePool<T>(() => new T());

    public T GetOrCreate(IInstancePoolInitData initData)
    {
        return _pool.GetOrCreate(initData);
    }

    public void Return(T element)
    {
        _pool.Return(element);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}
