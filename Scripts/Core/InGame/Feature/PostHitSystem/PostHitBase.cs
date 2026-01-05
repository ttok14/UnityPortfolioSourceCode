using UnityEngine;

public abstract class PostHitBase : IInstancePoolElement
{
    public abstract void ReturnToPool();
    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as PostHitInitDataBase;
    }

    public abstract void OnPoolInitialize();
    public virtual void OnPoolReturned()
    {
    }
}
