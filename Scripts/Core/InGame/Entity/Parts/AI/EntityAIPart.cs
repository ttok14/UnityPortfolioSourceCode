using System;
using UnityEngine;

public class EntityAIPartInitData : EntityPartInitDataBase
{
    public EntityAIPartInitData(EntityBase owner) : base(owner)
    {
    }
}

public class EntityAIPart : EntityPartBase
{
    public bool _isActivated;

    EntityAIBehaviour _currentBehaviour;

    public Func<EntityBase> TargetGetter { get; private set; }

    public override void OnPoolInitialize()
    {
        base.OnPoolInitialize();

        TargetGetter = () => _currentBehaviour != null ? _currentBehaviour.CurrentTarget : null;
    }

    //public override void Initialize(EntityBase owner, EntityPartInitData initData)
    //{
    //    base.Initialize(owner, initData);
    //}

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        if (_currentBehaviour != null)
        {
            _currentBehaviour.ReturnToPool();
            _currentBehaviour = null;
        }

        _isActivated = false;
    }

    public virtual void DoLateUpdate()
    {
        if (_isActivated == false)
            return;

        _currentBehaviour?.DoLateUpdate();
    }

    public void SetActivated(bool isActivated)
    {
        _currentBehaviour?.SetActivation(isActivated);
        _isActivated = isActivated;
    }

    public void SwitchBehaviour(EntityAIBehaviour behaviour)
    {
        if (_currentBehaviour != null)
        {
            _currentBehaviour.ReturnToPool();
        }

        _currentBehaviour = behaviour;

        if (behaviour != null)
            behaviour.SetActivation(_isActivated);
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }

    //public virtual void OnEnterPhase(InGamePhase phase)
    //{
    //    var commander = EntityManager.Instance.GetAICommander(_owner.Team);
    //    SwitchBehaviour(commander.CreateBehaviour(_owner));
    //}
}
