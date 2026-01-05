
using System;

public class AggroTargetingPolicyInitData : IInstancePoolInitData
{
    public AggroSystemBase AggroSystem;

    public void Set(AggroSystemBase aggroSystem)
    {
        AggroSystem = aggroSystem;
    }
}

public class AggroTargetingPolicy : ITargetSelectionPolicy
{
    AggroSystemBase _aggroSystem;

    public EntityBase FindTarget(EntityBase asker)
    {
        if (asker.SkillPart == null)
        {
            TEMP_Logger.Err($"Entity with no skill, should not have aggroTargetingPolicy | EntityTID : {asker.EntityTID} | {asker.gameObject.name}");
            return null;
        }

        return _aggroSystem.FindTarget(asker);
    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as AggroTargetingPolicyInitData;
        _aggroSystem = data.AggroSystem;
    }

    public void OnPoolInitialize()
    {
    }

    public void OnPoolReturned()
    {
        if (_aggroSystem != null)
        {
            _aggroSystem.Release();
            _aggroSystem = null;
        }
    }

    public void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }
}
