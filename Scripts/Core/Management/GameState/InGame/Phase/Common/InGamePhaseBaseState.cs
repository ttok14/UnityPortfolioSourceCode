using System;
using UnityEngine;

public abstract class InGamePhaseBaseState : BaseState<InGamePhase, InGameManager, InGameFSMEnterArgBase>
{

    public override void OnInitialize(InGameManager _parent, InGamePhase state)
    {
        base.OnInitialize(_parent, state);
    }

    public override void OnEnter(Action callback, params InGameFSMEnterArgBase[] args)
    {
        base.OnEnter(callback, args);
    }
}
