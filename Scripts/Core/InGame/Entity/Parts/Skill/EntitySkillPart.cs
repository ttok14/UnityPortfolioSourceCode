using System;
using UnityEngine;
using GameDB;
using System.Collections.Generic;

public enum GetSkillResult
{
    None = 0,

    Success,

    Failed_NotAvailableYet,
    Failed_OutOfRange,
    Failed_ETC,
}

public class EntitySkillPartInitData : EntityPartInitDataBase
{
    public List<EntitySkillBase> SkillSet;

    public EntitySkillPartInitData(EntityBase owner) : base(owner) { }
}

public class EntitySkillPart : EntityPartBase
{
    protected List<EntitySkillBase> _skillSet;

    EntitySkillBase _currentCastingSkill;
    EntitySkillTriggerContext _currentCastingSkillContext;

    public bool UseAnimationIK { get; set; }

    public int SkillCount => _skillSet.Count;

    #region ====:: 람다 캐싱용 ::====
    EntityEventDelegates.SkillTriggered _onSkillTriggered;
    Action _doTriggerSkill;
    #endregion

    public bool IsCasting => _currentCastingSkill != null;

    public override void OnPoolInitialize()
    {
        base.OnPoolInitialize();

        _doTriggerSkill = () => OnSkillTriggered(Owner);
        _onSkillTriggered = (executor) => OnSkillTriggered(executor);
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var idata = initData as EntitySkillPartInitData;

        _skillSet = idata.SkillSet;
    }

    public override void OnPoolReturned()
    {
        Reset();

        base.OnPoolReturned();
    }

    public void Reset()
    {
        _currentCastingSkill = null;
        _currentCastingSkillContext = default;
        Owner.SkillAniTriggerListener -= _onSkillTriggered;
    }

    public EntitySkillBase GetSkill(int idx)
    {
        if (_skillSet == null || idx >= _skillSet.Count)
        {
            TEMP_Logger.Err($"Skill Idx OutofRange : {idx} | Entity ID : {Owner.EntityTID}");
            return null;
        }

        return _skillSet[idx];
    }

    public (GetSkillResult res, EntitySkillBase skill) GetBestAvailableSkill(EntityBase target)
    {
        var res = GetBestAvailableSkillIdx(target);
        if (res.res != GetSkillResult.Success)
            return (res.res, null);
        return (res.res, _skillSet[res.idx]);
    }

    public (GetSkillResult res, int idx) GetBestAvailableSkillIdx(EntityBase target)
    {
        // 기본 스킬 인덱스 
        int bestSkillIdx = -1;
        bool hasSkillInRange = false;
        bool hasAvailableSkill = false;

        float sqrDistFromTarget = Vector3.SqrMagnitude(target.ApproxPosition.FlatHeight() - Owner.ApproxPosition.FlatHeight());

        // TODO : '더 나은 스킬' 을 고르는 기준이 필요할지도''.
        // 일단 현 기준으로는 Idx 가 높을수록 더 강한 스킬임을 고려해서 조건에 맞으면
        // 걍 결과 idx 에 i 을 넣어주는 식으로 구현.
        for (int i = 0; i < _skillSet.Count; i++)
        {
            var s = _skillSet[i];

            float executableRange = s.TableData.Range + target.ModelPart.VolumeRadius + Owner.ModelPart.VolumeRadius;
            hasSkillInRange = sqrDistFromTarget <= executableRange * executableRange;

            if (s.IsAvailable && hasSkillInRange)
            {
                if (s.TableData.SkillType == E_SkillType.Attack && target.Team != Owner.Team)
                {
                    bestSkillIdx = i;
                }
            }

            if (s.IsAvailable)
                hasAvailableSkill = true;
        }

        GetSkillResult res;

        if (bestSkillIdx != -1)
            res = GetSkillResult.Success;
        else if (hasSkillInRange == false)
            res = GetSkillResult.Failed_OutOfRange;
        else if (hasAvailableSkill == false)
            res = GetSkillResult.Failed_NotAvailableYet;
        else
            res = GetSkillResult.Failed_ETC;

        return (res, bestSkillIdx);
    }

    public float GetCurrentRequiredRange(EntityBase target)
    {
        var res = GetBestAvailableSkill(target);
        if (res.res == GetSkillResult.Success)
        {
            return res.skill.TableData.Range;
        }

        return GetSkill(0).TableData.Range;
    }

    public float GetRange(int idx)
    {
        return _skillSet[idx].TableData.Range;
    }

    public float GetCooltimeProgress(int slotIdx)
    {
        var skill = GetSkill(slotIdx);
        if (skill == null)
            return 0;

        return skill.CooltimeProgress;
    }

    public float GetDistanceRemainedToTarget(EntityBase target)
    {
        if (target == null)
            return 0f;

        var targetPos = target.ApproxPosition.FlatHeight();
        var myPosition = Owner.ApproxPosition.FlatHeight();

        float dist = Vector3.Distance(targetPos, myPosition);
        var bestSkillRange = GetCurrentRequiredRange(target);
        var executableRange = bestSkillRange + target.ModelPart.VolumeRadius + Owner.ModelPart.VolumeRadius;
        return dist - executableRange;
    }

    public bool CheckIfTargetIsInRange(EntityBase target)
    {
        return GetDistanceRemainedToTarget(target) <= 0f;
    }

    public bool RequestUseSkill(EntitySkillTriggerContext context)
    {
        if (context.Target == null)
        {
            TEMP_Logger.Err($"SkillContext Invalid");
            return false;
        }

        if (_skillSet == null || context.SlotIdx >= _skillSet.Count)
        {
            TEMP_Logger.Err($"Invalid slot Index | isSkillNull ? : {_skillSet == null} , Skill Count : {_skillSet?.Count ?? 0}");
            return false;
        }

        if (IsCasting)
        {
            return false;
            // OnSkillTriggered(_owner);
        }

        // 지금 Triger 되지 않은 대기 상태의 스킬이 존재함.
        // 이 경우 일단 막지는 안되, 스킬 발동에 관련해 버그가 있는 것이므로
        // 무조건 체크할것. 일단 강제 발동시킴 남아있는거는 
        //if (_currentCastingSkill != null)
        //{
        //    //OnSkillTriggered(_owner);
        //    TEMP_Logger.Err($"Skill Should have been triggered before the current Use, MUST CHECK. (Forced Triggered)");
        //    return false;
        //}

        var skill = _skillSet[context.SlotIdx];

        if (skill.IsAvailable == false)
            return false;

        skill.StartCasting();

        if (Owner.MovePart != null && skill.TableData.LookAtTarget)
        {
            if (context.Target != Owner)
            {
                var dirToTarget = context.Target.ApproxPosition.FlatHeight() - Owner.ApproxPosition.FlatHeight();
                Owner.MovePart.RotateToDirection(dirToTarget);
            }
        }

        EntityAnimationParameterType paramType = EntityAnimationParameterType.None;

        switch (skill.TableData.TriggerType)
        {
            case E_SkillTriggerType.Animation:
                {
                    if (context.SlotIdx == 0)
                        paramType = EntityAnimationParameterType.Skill01;
                    else if (context.SlotIdx == 1)
                        paramType = EntityAnimationParameterType.Skill02;
                    else if (context.SlotIdx == 2)
                        paramType = EntityAnimationParameterType.Skill03;
                    else
                    {
                        TEMP_Logger.Err($"Not implented : {context.SlotIdx}");
                        return false;
                    }

                    Owner.SkillAniTriggerListener += _onSkillTriggered;

                    //Vector3? ikTarget = null;
                    //if (UseAnimationIK && context.Target != _owner)
                    //{
                    //    ikTarget = context.Target.transform.position;
                    //}

                    Owner.AnimationPart.PlaySkillAnimation(paramType /*, ikTarget */);
                }
                break;
            case E_SkillTriggerType.CastingTime:
                {
                    MainThreadDispatcher.Instance.InvokeDelay(_doTriggerSkill, skill.TableData.CastingTime);
                }
                break;
            default:
                TEMP_Logger.Err($"Wrong Type : {skill.TableData.TriggerType} | ID : {skill.TableID}");
                return false;
        }

        _currentCastingSkillContext = context;
        _currentCastingSkill = _skillSet[context.SlotIdx];

        return true;
    }

    private void OnSkillTriggered(EntityBase executor)
    {
        if (Owner == null || Owner.IsAlive == false)
        {
            _currentCastingSkill = null;

            if (Owner)
                Owner.SkillAniTriggerListener -= _onSkillTriggered;

            return;
        }

        if (_currentCastingSkill == null)
        {
            TEMP_Logger.Wrn($"No Current Casting Skill !");
            return;
        }

        _currentCastingSkill.Trigger(_currentCastingSkillContext);

        PlayAudio(_currentCastingSkill.TableData);

        _currentCastingSkill = null;

        Owner.SkillAniTriggerListener -= _onSkillTriggered;
    }

    void PlayAudio(SkillTable tableData)
    {
        if (tableData == null)
            return;

        var audioKeys = tableData.TriggerAudioKey;
        if (audioKeys == null || audioKeys.Length == 0)
            return;

        if (tableData.AudioRandomPick)
        {
            AudioManager.Instance.Play(
                tableData.TriggerAudioKey[UnityEngine.Random.Range(0, audioKeys.Length)],
                Owner.ApproxPosition,
                AudioTrigger.Default);
        }
        else
        {
            foreach (var key in tableData.TriggerAudioKey)
            {
                AudioManager.Instance.Play(
                    key,
                    Owner.ApproxPosition,
                    AudioTrigger.Default);
            }
        }
    }

    public override void ReturnToPool()
    {
        if (_skillSet != null)
        {
            for (int i = 0; i < _skillSet.Count; i++)
            {
                _skillSet[i].ReturnToPool();
            }

            _skillSet = null;
        }

        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
