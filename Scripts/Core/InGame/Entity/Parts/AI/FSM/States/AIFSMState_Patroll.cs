using System;
using UnityEngine;

public class AIFSMState_Patroll : AIFSMState
{
    ICombatBlackboard _bb;

    bool _patroling;

    public override void OnEnter(Action callback, params EntityAIFSMArgBase[] args)
    {
        base.OnEnter(callback, args);

        _bb = Parent as ICombatBlackboard;
        _patroling = false;
    }

    public override void OnExit(Action callback)
    {
        _patroling = false;
        _bb = null;
        base.OnExit(callback);
    }

    public override void ManualLateUpdate()
    {
        if (EntityHelper.IsValid(_bb.CurrentTarget) && _owner.SkillPart.CheckIfTargetIsInRange(_bb.CurrentTarget))
        {
            SendEvent(EntityAIStateEvent.TargetInRange);
        }

        if (_patroling)
            return;

        _patroling = true;

        var part = _owner.MovePart as EntityPatrolMovePart;
        if (part == null)
        {
            TEMP_Logger.Err($"This Patrol State depends on PatrolMovePart! | Entity TID : {_owner.EntityTID} , ID : {_owner.ID}");
            SendEvent(EntityAIStateEvent.TargetLost);
        }

        float angle = 30f;
        part.StartPatrol(
            Quaternion.AngleAxis(angle, Vector3.up) * _owner.transform.forward,
            Quaternion.AngleAxis(angle * -1, Vector3.up) * _owner.transform.forward,
            2f);
    }
}
