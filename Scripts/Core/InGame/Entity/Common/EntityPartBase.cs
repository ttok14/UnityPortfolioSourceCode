using UnityEngine;

public class EntityPartInitDataBase : IInstancePoolInitData
{
    public EntityBase Owner;

    public EntityPartInitDataBase(EntityBase owner)
    {
        Owner = owner;
    }
}

public abstract class EntityPartBase : IInstancePoolElement
{
    public bool IsInitialized { get; private set; }
    public EntityBase Owner { get; private set; }

#if DEVELOPMENT
    public virtual string DebugText
    {
        get
        {
            if (Owner)
                return $"Name : {Owner.gameObject.name} | {Owner.TableData.Name}";
            else return "Owner Null Part";
        }
    }
#endif

    public virtual void OnPoolInitialize() { }

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as EntityPartInitDataBase;
        Owner = data.Owner;
        IsInitialized = true;
    }

    public virtual void OnPoolReturned()
    {
        Owner = null;
        IsInitialized = false;
    }

    public abstract void ReturnToPool();
}
