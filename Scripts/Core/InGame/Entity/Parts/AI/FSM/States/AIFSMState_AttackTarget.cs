using System;
using UnityEngine;

public class AIFSMState_AttackTarget : AIFSMState
{
    ICombatBlackboard _bb;

    public EntityBase CurrentTarget;

    public override void OnEnter(Action callback, params EntityAIFSMArgBase[] args)
    {
        base.OnEnter(callback, args);

        _bb = Parent as ICombatBlackboard;
    }

    public override void OnExit(Action callback)
    {
        CurrentTarget = null;
        _bb = null;
        base.OnExit(callback);
    }

    public override void ManualLateUpdate()
    {
        if (EntityHelper.IsValid(_bb.CurrentTarget) == false)
        {
            base.SendEvent(EntityAIStateEvent.TargetLost);
            return;
        }

        // 기존에 패던애가 사망한 상태, 이러면 FSM 에서 이 다음을 처리해주어야함 
        if (CurrentTarget != null && CurrentTarget != _bb.CurrentTarget)
        {
            SendEvent(EntityAIStateEvent.TargetChangedDuringAttack);
            return;
        }

        CurrentTarget = _bb.CurrentTarget;

        if (_owner.MovePart != null)
        {
            // 회전부터
            Vector3 dirToTarget = (CurrentTarget.ApproxPosition - _owner.ApproxPosition).FlatHeight();
            dirToTarget.Normalize();
            float dot = Vector3.Dot(_owner.MovePart.Mover.forward, dirToTarget);
            if (dot < 0.99f)
            {
                _owner.MovePart.RotateToDirection(dirToTarget);
            }

            if (dot < 0.9f)
            {
                return;
            }
        }

        if (_owner.SkillPart.IsCasting)
            return;

        var res = _owner.SkillPart.GetBestAvailableSkillIdx(CurrentTarget);

        if (res.res == GetSkillResult.Success)
        {
            _owner.SkillPart.RequestUseSkill(new EntitySkillTriggerContext()
            {
                SlotIdx = res.idx,
                Executor = _owner,
                Target = CurrentTarget,
                SkillEquipment = EntityEquipmentType.Weapon
            });
        }
        else if (res.res == GetSkillResult.Failed_OutOfRange)
        {
            SendEvent(EntityAIStateEvent.TargetLost);
        }
        else if (res.res == GetSkillResult.Failed_NotAvailableYet)
        {
            // 당장에 사용가능한 스킬이 없는거라면 일단 대기해서 스테이트 유지
        }
    }
}
