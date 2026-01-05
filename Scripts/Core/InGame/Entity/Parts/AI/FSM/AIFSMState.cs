using System;
using UnityEngine;


public abstract class AIFSMState : BaseState<EntityAIState, EntityAIBehaviour, EntityAIFSMArgBase>
{
    protected EntityBase _owner;

    public override void OnInitialize(EntityAIBehaviour _parent, EntityAIState state)
    {
        base.OnInitialize(_parent, state);
        _owner = _parent.OwnerEntity;
    }

    public void SendEvent(EntityAIStateEvent evt, params EntityAIFSMArgBase[] args)
    {
        Parent.OnEventOccured(evt, args);
    }
}
