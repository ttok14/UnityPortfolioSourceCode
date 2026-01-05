using UnityEngine;

public class AIFSM : FSM<EntityAIStateEvent, EntityAIState, EntityAIFSMArgBase, EntityAIBehaviour>
{
    public AIFSM(EntityAIBehaviour parent) : base(parent)
    {

    }

    public override void Release()
    {
        base.ReleaseInternal();
    }
}
