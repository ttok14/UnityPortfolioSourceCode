using System;
using UnityEngine;

public class AIFSMState_WaitingTarget : AIFSMState
{
    ICombatBlackboard _bb;

    Vector3 _ownerLastPosition;
    Vector3 _ownerLastEuler;
    Vector3 _lastTargetPosition;

    public override void OnEnter(Action callback, params EntityAIFSMArgBase[] args)
    {
        base.OnEnter(callback, args);

        _bb = Parent as ICombatBlackboard;

    }

    public override void OnExit(Action callback)
    {
        _bb = null;
        base.OnExit(callback);
    }

    public override void ManualLateUpdate()
    {
        if (EntityHelper.IsValid(_bb.CurrentTarget) == false)
        {
            if (_owner.MovementBeginListener != null &&
                _owner.MovePart != null &&
                _owner.SubMovePart != null)
            {
                float dot = Vector3.Dot(_owner.transform.forward, _owner.RealForward);
                if (dot < 0.98f)
                    _owner.MovePart.RotateToDirection(_owner.RealForward);
            }

            SendEvent(EntityAIStateEvent.TargetLost);

            return;
        }

        if (_owner.MovePart != null)
        {
            var ownerEulerAngles = _owner.transform.eulerAngles;

            bool updateDirection =
                _ownerLastPosition != _owner.ApproxPosition ||
                _ownerLastEuler != ownerEulerAngles ||
                _lastTargetPosition != _bb.CurrentTarget.ApproxPosition;

            if (updateDirection)
            {
                _owner.MovePart.RotateToDirection((_bb.CurrentTarget.ApproxPosition - _owner.ApproxPosition).normalized);

                _ownerLastPosition = _owner.ApproxPosition;
                _ownerLastEuler = ownerEulerAngles;
                _lastTargetPosition = _bb.CurrentTarget.ApproxPosition;
            }
        }

        if (_owner.SkillPart.CheckIfTargetIsInRange(_bb.CurrentTarget))
        {
            SendEvent(EntityAIStateEvent.TargetInRange);
        }
    }
}
