
using System;

public class FixedTargetingWithAggroPolicyInitData : IInstancePoolInitData
{
    public IObjectiveProvider TargetProvider;
    public AggroSystemBase AggroSystem;

    public void Set(IObjectiveProvider targetProvider, AggroSystemBase aggroSystem)
    {
        TargetProvider = targetProvider;
        AggroSystem = aggroSystem;
    }
}

public class FixedTargetingWithAggroPolicy : ITargetSelectionPolicy
{
    //enum Mode
    //{
    //    None = 0,

    //    // Aggro System 과 Objective 간 핑퐁 가능 상태
    //    FlexibleTargeting,

    //    // Aggro System 무시하고 Objective 만 타게팅
    //    FixToObjective
    //}

    const int AggroTargetChangeAllowanceCountPerObjective = 10;

    IObjectiveProvider _targetProvider;
    AggroSystemBase _aggroSystem;

    // Mode _mode;
    bool _targetingFlexible;
    int _targetChangeCountInCurrentObj;
    EntityBase _lastAggroTarget;
    EntityBase _lastObjective;

    public EntityBase FindTarget(EntityBase asker)
    {
        EntityBase currentObjective = _targetProvider.GetTargetEntity(asker.Team);

        // 마지막으로 감지된 Objective 랑 현재가 다르다면,
        // 다시 Flexible 모드로 돌려서 어그로 시스템 활성화
        if (_lastObjective != currentObjective)
        {
            _lastObjective = currentObjective;
            _targetingFlexible = true;
        }

        if (_targetingFlexible)
        {
            // 1차적으로 어그로 시스템 사용 
            var target = _aggroSystem.FindTarget(asker);
            if (target)
            {
                // 어그로 타깃이 없었다가 생겼거나 
                // 기존 어그로끌던 타겟이 변경되면
                if (_lastAggroTarget != target)
                {
                    _targetChangeCountInCurrentObj++;
                    _lastAggroTarget = target;

                    // 어그로 전환 허용 횟수를 넘어가면 그냥 강제로
                    // 현재 공동 타겟으로 직빵으로 향하게끔 강제로 모드 전환 
                    if (_targetChangeCountInCurrentObj >= AggroTargetChangeAllowanceCountPerObjective)
                    {
                        _targetingFlexible = false;
                    }
                }

                if (_targetingFlexible)
                    return target;
            }
            else
            {
                _lastAggroTarget = null;
            }
        }

        // 그 다음은 게임 공동 목표인 타겟을 타게팅함
        return currentObjective;
    }

    public void OnPoolInitialize()
    {

    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as FixedTargetingWithAggroPolicyInitData;

        _targetProvider = data.TargetProvider;
        _aggroSystem = data.AggroSystem;
        _targetingFlexible = true;
        _targetChangeCountInCurrentObj = 0;
        _lastAggroTarget = null;
        _lastObjective = null;
    }

    public void OnPoolReturned()
    {
        _targetProvider = null;
        _aggroSystem = null;
        _targetingFlexible = true;
        _targetChangeCountInCurrentObj = 0;
        _lastAggroTarget = null;
        _lastObjective = null;
    }

    public void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }
}
