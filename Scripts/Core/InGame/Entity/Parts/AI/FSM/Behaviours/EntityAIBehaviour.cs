using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using GameDB;

public class EntityAIBehaviourInitData : IInstancePoolInitData
{
    public EntityBase Owner;
    public ITargetSelectionPolicy TargetingPolicy;
    public IMovePolicy MovePolicy;
    public IPathProvider PathProvider;

    public void Set(EntityBase owner, ITargetSelectionPolicy targetingPolicy, IMovePolicy movePolicy, IPathProvider pathProvider)
    {
        Owner = owner;
        TargetingPolicy = targetingPolicy;
        MovePolicy = movePolicy;
        PathProvider = pathProvider;
    }
}

public class EntityAIBehaviour : ICombatBlackboard, IPathProvider, IInstancePoolElement
{
    public EntityBase OwnerEntity { get; private set; }

    protected AIFSM _FSM { get; private set; }

    EntityBase _currentTarget;
    public EntityBase CurrentTarget => _currentTarget;

    Vector3 _currentDestination;
    public Vector3 CurrentDestination => _currentDestination;
    E_EntityType _currentMoveDestEntityType;
    public E_EntityType CurrentMoveDestEntityType => _currentMoveDestEntityType;
    MoveCommandResult _currentMoveType;
    public MoveCommandResult CurrentMoveType => _currentMoveType;

    ITargetSelectionPolicy _targetingPolicy;
    IMovePolicy _movePolicy;
    IPathProvider _pathProvider;

    bool _isActivated;

    MoveCommand _moveCmd;

    public void OnPoolInitialize()
    {
        _FSM = new AIFSM(null);
    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        _FSM.Initialize(this);

        var data = initData as EntityAIBehaviourInitData;

        OwnerEntity = data.Owner;
        _targetingPolicy = data.TargetingPolicy;
        _movePolicy = data.MovePolicy;
        _pathProvider = data.PathProvider;

        _FSM.AddState(EntityAIState.Idle, OwnerEntity.gameObject.AddComponent<AIFSMState_Idle>());

        bool hasMovePolicy = _movePolicy != null;
        bool hasTargetingPolicy = _targetingPolicy != null;

        // TODO: 슬슬 이런 코드가 나오는거 보니 설계가 잘못된 것 같은 느낌이들기시작함..
        // 생각을 해보자. 초기에는 policy 만으로 다형성을 충분히 대체할줄 알앗는데
        // 확장을 할수록 바로 '이거 어케 구현하지?' 에 대한 답이 빨리 안떠오름
        // 이유는 즉슨 지금 EntityAIBehaviour 하나로 policy 만 달라지는 식으로
        // 구현이 한계가 조금씩 보이는거 같음.
        // 여차하면 리팩 생각하자. 방향은 결국 EntityAIBehaviour 도 상속 구조로 가던가,
        // 아니면 Entity 레벨에서 마치 Parts 만들듯이 ? 근데 이거는 모드따라 또 달라질텐데 음..
        // Entity 에서 모드별로 만드는건 에바고 전용 Factory 에서 조립해줄수는 있겠군.  . . FUCK 
        if (hasTargetingPolicy)
        {
            if (hasMovePolicy)
            {
                if (OwnerEntity.MovePart != null)
                {
                    // TODO : 이게좀 에바임. 추상화 안될까 고민해보자 . Patrol 에 대한 개념을
                    bool patrol = OwnerEntity.MovePart is EntityPatrolMovePart;

                    if (patrol)
                        _FSM.AddState(EntityAIState.PatrolTarget, OwnerEntity.gameObject.AddComponent<AIFSMState_Patroll>());
                    else _FSM.AddState(EntityAIState.ChaseTarget, OwnerEntity.gameObject.AddComponent<AIFSMState_ChaseTarget>());
                }
            }
            else
            {
                _FSM.AddState(EntityAIState.WaitingTarget, OwnerEntity.gameObject.AddComponent<AIFSMState_WaitingTarget>());
            }

            _FSM.AddState(EntityAIState.AttackTarget, OwnerEntity.gameObject.AddComponent<AIFSMState_AttackTarget>());
        }

        _FSM.Enable(EntityAIState.Idle);
    }

    public void OnPoolReturned()
    {
        if (_FSM != null)
            _FSM.Release();

        if (_targetingPolicy != null)
        {
            _targetingPolicy.ReturnToPool();
            _targetingPolicy = null;
        }

        if (_movePolicy != null)
        {
            _movePolicy.ReturnToPool();
            _movePolicy = null;
        }

        OwnerEntity = null;
        _pathProvider = null;
        _isActivated = false;
        _moveCmd = default;
        _currentTarget = null;
        _currentDestination = default;
        _currentMoveDestEntityType = E_EntityType.None;
        _currentMoveType = MoveCommandResult.Stop;
    }

    public void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }

    public void SetActivation(bool isActivated)
    {
        _isActivated = isActivated;

        if (_FSM.Current_State != EntityAIState.Idle)
            _FSM.ChangeState(EntityAIState.Idle);
    }

    public void DoLateUpdate()
    {
        if (_isActivated == false)
        {
            if (_FSM.Current_State != EntityAIState.Idle)
                _FSM.ChangeState(EntityAIState.Idle);
            return;
        }

        UpdateBlackboard();

        if (CanChangeState())
            DecideNextState();

        _FSM.Current.ManualLateUpdate();
    }

    bool CanChangeState()
    {
        if (_FSM.Current_State == EntityAIState.AttackTarget || OwnerEntity.SkillPart.IsCasting)
            return false;

        return true;
    }

    void DecideNextState()
    {
        if (_FSM.Current_State == EntityAIState.Idle)
        {
            if (_movePolicy != null)
            {
                switch (_moveCmd.result)
                {
                    case MoveCommandResult.Stop:
                        break;
                    case MoveCommandResult.Path:
                    case MoveCommandResult.Directional:
                        ChangeState(EntityAIState.ChaseTarget);
                        break;
                    case MoveCommandResult.Patrol:
                        ChangeState(EntityAIState.PatrolTarget);
                        break;
                }
            }
            else if (_targetingPolicy != null)
            {
                ChangeState(EntityAIState.WaitingTarget);
            }
        }

        //if (_movePolicy == null && _targetingPolicy != null)
        //{
        //    if (_FSM.Current_State != EntityAIState.AttackTarget)
        //    {
        //        ChangeState(EntityAIState.AttackTarget);
        //    }
        //}
    }


    void UpdateBlackboard()
    {
        _currentTarget = _targetingPolicy?.FindTarget(OwnerEntity);
        _moveCmd = _movePolicy != null ? _movePolicy.GetCommand(_currentTarget) : default;
        _currentDestination = _moveCmd.destination;
        _currentMoveDestEntityType = _moveCmd.destEntityType;
        _currentMoveType = _moveCmd.result;
        //_currentMovePath = _moveCmd.path;
        //_currentPathVersion = _moveCmd.pathVersion;
    }

    //void Think()
    //{
    //    switch (_FSM.Current_State)
    //    {
    //        case EntityAIState.Idle:
    //            {
    //                if (moveCmd.result == MoveCommandResult.Path)
    //                {
    //                    ChangeState(EntityAIState.MovePath);
    //                    return;
    //                }
    //            }
    //            break;
    //        case EntityAIState.MovePath:
    //            break;
    //        case EntityAIState.AttackTarget:
    //            break;
    //        default:
    //            break;
    //    }
    //    switch (moveCmd.result)
    //    {
    //        case MoveCommandResult.Stop:
    //            ChangeState(EntityAIState.Idle);
    //            break;
    //        case MoveCommandResult.Path:
    //            ChangeState(EntityAIState.MovePath);
    //            break;
    //        case MoveCommandResult.Directional:
    //            // TODO : Directional Move 스테이트 추가 ㄱㄱ
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //void HandleSkill()
    //{
    //    if (CurrentTarget != null && OwnerEntity.SkillPart.SkillCount > 0 && OwnerEntity.SkillPart.IsCasting == false)
    //    {

    //    }
    //}

    //public virtual void Release()
    //{
    //    //if (_FSM != null)
    //    //{
    //    //    _FSM.Release();
    //    //    _FSM = null;
    //    //}

    //    //OwnerEntity = null;
    //    //_movePolicy = null;
    //    //_targetingPolicy = null;
    //}

    void ChangeState(EntityAIState newState)
    {
        if (_FSM.Current_State == newState)
            return;

        _FSM.ChangeState(newState);
    }

    // * 이벤트는 안밖으로 다양하게 발생 가능함 . 다만 이 루틴을 통해서
    // 전환이 가능하다면 이루어지면됨, 즉 실제 FSM 인스턴스에 dependent 함.
    public void OnEventOccured(EntityAIStateEvent evt, params EntityAIFSMArgBase[] args)
    {
        if (_FSM.CheckEvent(evt))
        {
            _FSM.ChangeState(evt, args);
        }
        else
        {
            switch (evt)
            {
                case EntityAIStateEvent.TargetLost:
                case EntityAIStateEvent.MoveEnd_NotReachedDestination:
                case EntityAIStateEvent.TargetChangedDuringAttack:
                case EntityAIStateEvent.Cannot_Move:
                case EntityAIStateEvent.Cannot_Attack:
                    {
                        ChangeState(EntityAIState.Idle);
                    }
                    break;
                case EntityAIStateEvent.TargetInRange:
                    {
                        ChangeState(EntityAIState.AttackTarget);
                    }
                    break;
            }
        }
    }

    public void FetchPath(EntityBase mover, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        _pathProvider.FetchPath(mover, ctk, onFetched);
    }

    public void FetchPath(EntityBase mover, ulong targetEntityID, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        _pathProvider.FetchPath(mover, targetEntityID, ctk, onFetched);
    }

    public void FetchPath(EntityBase mover, Vector3 destination, in PathBuffer.Modifier modifier, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        _pathProvider.FetchPath(mover, destination, modifier, ctk, onFetched);
    }
}
