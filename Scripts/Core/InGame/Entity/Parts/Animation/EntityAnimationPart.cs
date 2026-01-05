using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnimationPartInitData : EntityPartInitDataBase
{
    public Animator Anim;
    public EntityAnimationStateBeh[] StateBehaviours;

    public EntityAnimationPartInitData(EntityBase owner) : base(owner) { }

    public void Set(Animator animator, EntityAnimationStateBeh[] stateBehs)
    {
        Anim = animator;
        StateBehaviours = stateBehs;
    }
}

public class EntityAnimationPart : EntityPartBase
{
    protected Animator _animator;
    protected AnimatorOverrideController _controller;
    protected EntityAnimationStateBeh[] _stateBehaviours;

    public IReadOnlyDictionary<int, AnimatorControllerParameter> AnimatorParamTable { get; private set; }
    public bool HasAnimationParameter(int hash) => AnimatorParamTable.ContainsKey(hash);

    public bool IsPlayingSkillAnimation { get; private set; }

    EntityStatData _statData;

    TweenValue _moveStopSpeedTween = new TweenValue();

    float _lastAttackSpeedRate;

    bool _isStoppingOrStopped = true;

    int _skillAnimationLayerIndex;

    bool _hasAtttackRateParameter;

    // Null 일 수있음 , 트리거 데이터 따로 테이블로 안쓰면
    public IReadOnlyDictionary<EntityAnimationStateID, float> TriggerTimeData { get; private set; }

    #region ====:: 람다 캐싱 ::====
    Action<float> _doMoveSpeedRateAction;
    #endregion

    public override void OnPoolInitialize()
    {
        base.OnPoolInitialize();

        _doMoveSpeedRateAction = (value) => MoveSpeedRate(value);
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var idata = initData as EntityAnimationPartInitData;
        if (idata.Anim == null)
        {
            TEMP_Logger.Err($"Failed to get Entity Aniamtor | Name : {Owner.name} Tid : {Owner.EntityTID}");
            return;
        }

        _animator = idata.Anim;
        _stateBehaviours = idata.StateBehaviours;

        SetupAnimator();

        Owner.DataModifiedListener += OnDataModified;
        Owner.MovementBeginListener += OnMovementBegin;
        Owner.MovementProcessingListener += OnMoving;
        Owner.MovementEndListener += OnMovementEnd;

        Owner.ModelPart.OnSkillAnimationEventReceivedListener += OnSkillAnimationEventReceived;

        _statData = Owner.GetData(EntityDataCategory.Stat) as EntityStatData;
    }

    public override void OnPoolReturned()
    {
        AnimatorParamTable = null;
        _controller = null;
        _statData = null;
        IsPlayingSkillAnimation = false;
        _skillAnimationLayerIndex = 0;
        _hasAtttackRateParameter = false;

        if (_stateBehaviours != null)
        {
            foreach (var stateBeh in _stateBehaviours)
            {
                stateBeh.Release();
            }
        }

        _stateBehaviours = null;
        _isStoppingOrStopped = false;
        _moveStopSpeedTween.Release();
    }

    public void OnDie(Vector3 attackerPosition)
    {
        ResetParameters();

        int layerCnt = _animator.layerCount;
        for (int i = 0; i < layerCnt; i++)
        {
            float weight;
            if (i == 0)
                weight = 1f;
            else weight = 0f;

            _animator.SetLayerWeight(i, weight);
        }
    }

    public void PlaySkillAnimation(EntityAnimationParameterType parameter /*, Vector3? ikTarget = null*/)
    {
        if (parameter == EntityAnimationParameterType.Skill01 ||
            parameter == EntityAnimationParameterType.Skill02 ||
            parameter == EntityAnimationParameterType.Skill03)
        {
            EntityAnimationParameter.SetParameter(this, _animator, parameter);

            _animator.SetLayerWeight(_skillAnimationLayerIndex, 1f);

            //if (ikTarget.HasValue)
            //{
            //    ApplyIK(ikTarget.Value);
            //}
        }
        else TEMP_Logger.Err($"Given Type is not Skill Type : {parameter}");
    }

    public void SetParameter(EntityAnimationParameterType parameter, bool value)
    {
        EntityAnimationParameter.SetParameter(this, _animator, parameter, value);
    }

    public void SetParameter(EntityAnimationParameterType parameter, int value)
    {
        EntityAnimationParameter.SetParameter(this, _animator, parameter, value);
    }

    public void SetParameter(EntityAnimationParameterType parameter, float value)
    {
        if (parameter == EntityAnimationParameterType.AttackSpeedRate)
        {
            if (_lastAttackSpeedRate == value)
                return;
            _lastAttackSpeedRate = value;
        }

        EntityAnimationParameter.SetParameter(this, _animator, parameter, value);
    }

    public void SetParameter(EntityAnimationParameterType parameter)
    {
        EntityAnimationParameter.SetParameter(this, _animator, parameter);
    }

    // stateName 과 clipName 이 일치해야함 .. 즉 clip을 state 에 맞춰야함 . .
    // 유니티에서 State 로 clip 을 가져오는 런타임 api 지원안함 ......
    public float GetLength(EntityAnimationStateID stateName)
    {
        return _controller?[stateName.ToString()]?.length ?? 1f;
    }

    public float GetLength(string clipName)
    {
        return _controller?[clipName]?.length ?? 1f;
    }
    public void OnSkillAnimationEventReceived()
    {
        Owner.SkillAniTriggerListener?.Invoke(Owner);
    }

    //--------------------------------------------------------------------------------//

    void OnDataModified(EntityDataCategory category, EntityDataBase data)
    {
        if (category == EntityDataCategory.Stat)
        {
            if (_hasAtttackRateParameter)
                SetParameter(EntityAnimationParameterType.AttackSpeedRate, (data as EntityStatData).CurrentAttackSpeed);
        }
    }

    void OnMovementBegin(EntityBase executor, bool pathsOrDirectional, Vector3? directionalMoveDir = null, List<Vector3> paths = null)
    {
        _moveStopSpeedTween.StopTween();

        if (_isStoppingOrStopped)
        {
            _isStoppingOrStopped = false;
        }

        MoveSpeedRate(_statData.CurrentMoveSpeed / _statData.TableMoveSpeed);
    }

    void OnMoving(EntityBase executor, Vector3 position) // , Vector3? nextDest, bool isFinalDest)
    {
        MoveSpeedRate(_statData.CurrentMoveSpeed / _statData.TableMoveSpeed);
    }

    float _lastUpdatedMoveSpeedRate;
    void OnMovementEnd(EntityBase executor, bool reachedDest)
    {
        _isStoppingOrStopped = true;

        // 감속 트윈 시작.
        // 감속 시간은 기존 속도에 따라 달라지게 처리해야 자연스러움.
        _moveStopSpeedTween.StartTween(_lastUpdatedMoveSpeedRate, 0f, 0.7f * _lastUpdatedMoveSpeedRate, DG.Tweening.Ease.OutQuad, _doMoveSpeedRateAction, null);
    }

    void SetupAnimator()
    {
        _controller = _animator.runtimeAnimatorController as AnimatorOverrideController;

        TriggerTimeData = Owner.ModelPart.AnimationTriggerData;

        AnimatorParamTable = InGameManager.Instance.CacheContainer.GetAnimatorParameterDic(_animator);

        if (_stateBehaviours != null)
        {
            foreach (var stateBeh in _stateBehaviours)
            {
                stateBeh.Part = this;

                stateBeh.OnEnter += OnStateBehaviourEnterCalled;
                stateBeh.OnExit += OnStateBehaviourExitCalled;

                //if (stateBeh.State == EntityAnimationStateID.Skill01 ||
                //    stateBeh.State == EntityAnimationStateID.Skill02 ||
                //    stateBeh.State == EntityAnimationStateID.Skill03)
                //{
                if (TriggerTimeData != null && TriggerTimeData.TryGetValue(stateBeh.State, out var triggerAt))
                    stateBeh.ManualTriggerAt = triggerAt;
                //else
                //{
                //    // 의도치않은 애니메이션 데이터 누락 체크를 위한 Validation 체크 
                //    if (stateBeh._useStateSkillTrigger == false)
                //    {
                //        TEMP_Logger.Err($"Possible Animation Trigger Data(Table) Missing | name : {_owner.name} , ControllerName : {_animator.runtimeAnimatorController.name} | State : {stateBeh.State}");
                //    }
                //}
                //}
            }
        }

        _hasAtttackRateParameter = AnimatorParamTable.ContainsKey(EntityAnimationParameter.GetParameterID(EntityAnimationParameterType.AttackSpeedRate));

        // 레이어가 2개이상이면 현재 두번째는 스킬을 위한 'ActionsLayer' 로 간주중
        // 하지만 레이어가 하나인 경우에도 스킬 사용을 할 수 있으니 조건부로 인덱스 가져옴
        if (_animator.layerCount > 1)
            _skillAnimationLayerIndex = _animator.GetLayerIndex("ActionsLayer");
        else
            _skillAnimationLayerIndex = 0;

        EntityAnimationParameter.SetDefaultParameter(this, _animator);
    }

    public void ResetParameters()
    {
        EntityAnimationParameter.SetDefaultParameter(this, _animator);
    }

    void MoveSpeedRate(float rate)
    {
        if (_lastUpdatedMoveSpeedRate == rate)
            return;

        _lastUpdatedMoveSpeedRate = rate;
        EntityAnimationParameter.SetParameter(this, _animator, EntityAnimationParameterType.MoveSpeedRate, rate);
    }

    //void ApplyIK(Vector3 target)
    //{
    //    Vector2 thisXZPos = new Vector2(_owner.transform.position.x, _owner.transform.position.z);
    //    Vector2 targetXZPos = new Vector2(target.x, target.z);

    //    Vector2 forward = new Vector2(_owner.transform.forward.x, _owner.transform.forward.z).normalized;
    //    Vector2 toTarget = (targetXZPos - thisXZPos).normalized;

    //    bool leftOrRight = Vector2.SignedAngle(forward, toTarget) > 0;

    //    if (leftOrRight)
    //    {
    //        _owner.ModelPart.IK.TargetPosition = _owner.ModelPart.GetSocket(EntityModelSocket.LeftHand).position;
    //    }
    //    else
    //    {
    //        _owner.ModelPart.IK.TargetPosition = _owner.ModelPart.GetSocket(EntityModelSocket.RightHand).position;
    //    }

    //    _owner.AnimationPart.SetParameter(EntityAnimationParameterType.SkillTarget_Direction, leftOrRight ? 0f : 1f);
    //    _owner.AnimationSkillIKSetListener?.Invoke(_owner, leftOrRight);
    //}

    void OnStateBehaviourEnterCalled(Animator anim, EntityAnimationStateID id, AnimatorStateInfo stateInfo, int layerIdx)
    {
        Owner.StateAnimationBeginListener?.Invoke(Owner, id);
        if (id == EntityAnimationStateID.Skill01 || id == EntityAnimationStateID.Skill02 || id == EntityAnimationStateID.Skill03)
        {
            IsPlayingSkillAnimation = true;

            if (layerIdx != 0)
            {
                _animator.SetLayerWeight(layerIdx, 1f);
            }
        }
    }

    private void OnStateBehaviourExitCalled(Animator anim, EntityAnimationStateID id, AnimatorStateInfo stateInfo, bool isLast, int layerIdx)
    {
        Owner.StateAnimationEndListener?.Invoke(Owner, id);

        if (isLast && (id == EntityAnimationStateID.Skill01 || id == EntityAnimationStateID.Skill02 || id == EntityAnimationStateID.Skill03))
        {
            IsPlayingSkillAnimation = false;

            if (layerIdx != 0)
                _animator.SetLayerWeight(layerIdx, 0f);
        }
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
