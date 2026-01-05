using UnityEngine;
using GameDB;

public class EntityDataInitDataBase : IInstancePoolInitData
{
    public EntityBase Owner;
    public ulong DataUniqueID;
    public EntityDataCategory Category;
    public uint EntityTableId;

    public void SetBaseInitData(EntityBase owner, ulong dataUniqueID, EntityDataCategory category, uint entityTableId)
    {
        Owner = owner;
        DataUniqueID = dataUniqueID;
        Category = category;
        EntityTableId = entityTableId;
    }
}

public abstract class EntityDataBase : IInstancePoolElement
{
    protected EntityBase _owner;

    public ulong DataUniqueID { get; private set; }
    public EntityDataCategory Category { get; private set; }

    public uint EntityTableID { get; protected set; }
    public EntityTable TableData => DBEntity.Get(EntityTableID);

    public virtual void OnPoolInitialize() { }

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var idata = initData as EntityDataInitDataBase;
        _owner = idata.Owner;
        DataUniqueID = idata.DataUniqueID;
        Category = idata.Category;
        EntityTableID = idata.EntityTableId;
    }

    public virtual void OnPoolReturned()
    {
        _owner = null;
        DataUniqueID = 0;
        Category = EntityDataCategory.None;
        EntityTableID = 0;
    }

    public abstract void ReturnToPool();
}
