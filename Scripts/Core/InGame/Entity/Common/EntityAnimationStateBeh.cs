using System;
using UnityEngine;

public class EntityAnimationStateBeh : StateMachineBehaviour
{
    [SerializeField]
    private EntityAnimationStateID _state;
    public EntityAnimationStateID State => _state;

    ulong _enterCountStack;

    public EntityAnimationPart Part { get; set; }

    public event Action<Animator, EntityAnimationStateID, AnimatorStateInfo, int> OnEnter;
    // public event Action<Animator, EntityAnimationStateID, AnimatorStateInfo, int> OnUpdate;
    public event Action<Animator, EntityAnimationStateID, AnimatorStateInfo, bool, int> OnExit;

    [HideInInspector]
    public float ManualTriggerAt;

    #region ====:: 애니메이션 이벤트가 아니라 현 스테이트 스크립트로 발동 알릴때 사용 ::====
    public bool _useStateSkillTrigger;
    public float _triggerAtNormalizedTime;
    // int _triggeredCount;
    #endregion

    private void OnDisable()
    {
        _enterCountStack = 0;
        Release();
    }

    bool _hasTriggered;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasTriggered = false;

        //   _triggeredCount = 0;
        _enterCountStack++;
        base.OnStateEnter(animator, stateInfo, layerIndex);
        OnEnter?.Invoke(animator, _state, stateInfo, layerIndex);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_hasTriggered || Part == null)
            return;

        if (ManualTriggerAt > 0f)
        {
            if (stateInfo.normalizedTime >= ManualTriggerAt)
            {
                _hasTriggered = true;
                Part.OnSkillAnimationEventReceived();
            }
        }
        else if (_useStateSkillTrigger)
        {
            if (stateInfo.normalizedTime >= _triggerAtNormalizedTime)
            {
                _hasTriggered = true;
                Part.OnSkillAnimationEventReceived();
            }
        }
        else
        {
#if DEVELOPMENT
            TEMP_Logger.Err($"No Trigger Data, No need to be attached | name {Part.DebugText}");
#endif
        }

        //if (_useStateSkillTrigger)
        //{
        //    if (stateInfo.normalizedTime - _triggeredCount >= _triggerAtNormalizedTime)
        //    {
        //        _triggeredCount++;

        //        if (Part == null)
        //        {
        //            var entity = animator.GetComponentInParent<EntityBase>();
        //            Part = entity.AnimationPart;
        //        }
        //        Part.OnSkillAnimationEventReceived();
        //    }
        //}
    }

    //public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    base.OnStateUpdate(animator, stateInfo, layerIndex);
    //    OnUpdate?.Invoke(animator, _state, stateInfo, layerIndex);
    //}

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);

        if (_enterCountStack > 0)
            _enterCountStack--;

        OnExit?.Invoke(animator, _state, stateInfo, _enterCountStack == 0, layerIndex);
    }

    public void Release()
    {
        Part = null;
        OnEnter = null;
        OnExit = null;
        ManualTriggerAt = 0;
    }
}
