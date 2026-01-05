using System;
using System.Threading;
using UnityEngine;

public class AIFSMState_ChaseTarget : AIFSMState
{
    const float CheckTargetIntervalTime = 0.5f;
    const float RequestPathInterval = 2f;
    const float UpdateDirectionInterval = 0.5f;
    const int RefindPathDistanceThreshold = 0;
    const float UpdateDirectionalTargetThresholdDist = 0.2f;

    ICombatBlackboard _bb;
    IPathProvider _pathProvider;

    // uint _lastPathUniqueId;

    Vector2Int _lastDestTilePos;
    Vector3 _lastDestWorldPos;
    // MoveCommandResult _lastMoveType;
    CancellationTokenSource _ctkSrc;

    EntityEventDelegates.OnMovementEnd _onMoveEndAction;

    float _nextPathFindTimeAt;
    float _nextUpdateDirectionTimeAt;
    float _checkTargetTimeAt;

    Action<PathListPoolable> _onPathResponded;

    public override void OnInitialize(EntityAIBehaviour _parent, EntityAIState state)
    {
        base.OnInitialize(_parent, state);
        _onMoveEndAction = OnMoveEnd;

        _onPathResponded = OnPathResponeded;
    }

    public override void OnEnter(Action callback, params EntityAIFSMArgBase[] args)
    {
        base.OnEnter(callback, args);

        if (_owner.AnimationPart != null && _owner.AnimationPart.IsPlayingSkillAnimation)
        {
            SendEvent(EntityAIStateEvent.Cannot_Move);
            return;
        }

        if (_bb == null)
            _bb = Parent;
        if (_pathProvider == null)
            _pathProvider = Parent;

        _owner.MovementEndListener += _onMoveEndAction;

        switch (_bb.CurrentMoveType)
        {
            case MoveCommandResult.Path:
                RequestPath();
                break;
            case MoveCommandResult.Directional:
                MoveDirectly();
                break;
        }

        _checkTargetTimeAt = Time.time;
    }

#if UNITY_EDITOR
    int _requestCount;
#endif
    void RequestPath()
    {
        CancelRequestPath();

        if (_ctkSrc == null)
            _ctkSrc = new CancellationTokenSource();

#if UNITY_EDITOR
        _requestCount++;

        // Debug.Log($"Owner : {_owner.gameObject.name} | PathRequestCount : {_requestCount}");
#endif

        _lastDestWorldPos = _bb.CurrentDestination;
        _lastDestTilePos = MapUtils.WorldPosToTilePos(_bb.CurrentDestination);

        // 타겟이 지금 건물로 잡혀있으면 Destination 을 갈때 modifier 적용 (캐릭터는 적용안한다는 것)
        bool applyModifier = _bb.CurrentTarget?.Type == GameDB.E_EntityType.Structure;
        _pathProvider.FetchPath(_owner, _bb.CurrentDestination, new PathBuffer.Modifier(applyModifier), _ctkSrc.Token, _onPathResponded);
    }

    void MoveDirectly()
    {
        CancelRequestPath();

        _lastDestWorldPos = _bb.CurrentDestination;
        _lastDestTilePos = MapUtils.WorldPosToTilePos(_bb.CurrentDestination);

        _owner.MovePart.MoveToDestDirectional(_bb.CurrentDestination, 1f);
    }

    public override void OnExit(Action callback)
    {
        _bb = null;
        _pathProvider = null;

        _owner.MovementEndListener -= OnMoveEnd;
        _owner.MovePart.Stop();

        CancelRequestPath();

        _lastDestTilePos = Vector2Int.zero;
        _lastDestWorldPos = Vector3.zero;

        //_lastMoveType = MoveCommandResult.Stop;

        _nextUpdateDirectionTimeAt = 0;
        _nextPathFindTimeAt = 0;

        base.OnExit(callback);
    }

    public override void ManualLateUpdate()
    {
        if (EntityHelper.IsValid(_bb.CurrentTarget) == false)
        {
            SendEvent(EntityAIStateEvent.TargetLost);
            return;
        }

        // 바뀐 Path 에 대한 갱신
        switch (_bb.CurrentMoveType)
        {
            case MoveCommandResult.Path:
                {
                    if (Time.time >= _nextPathFindTimeAt)
                    {
                        _nextPathFindTimeAt = Time.time + RequestPathInterval;

                        bool request = MapUtils.GetDistance(MapUtils.WorldPosToTilePos(_bb.CurrentDestination), _lastDestTilePos) > RefindPathDistanceThreshold;
                        if (request)
                        {
                            RequestPath();
                        }
                    }
                }
                break;
            case MoveCommandResult.Directional:
                {
                    if (Time.time >= _nextUpdateDirectionTimeAt)
                    {
                        _nextUpdateDirectionTimeAt = Time.time + UpdateDirectionInterval;

                        bool update = Vector3.SqrMagnitude(_bb.CurrentDestination - _lastDestWorldPos) > UpdateDirectionalTargetThresholdDist;
                        if (update)
                        {
                            MoveDirectly();
                        }
                    }
                }
                break;
        }

        // RequestPath 에서 곧 바로 OnExit 루틴을 탈 수 있으므로
        // 여기서 이 케이스에는 종료시켜줌
        // (현 State 종료된거임)
        if (_bb == null)
        {
            return;
        }

        if (Time.time >= _checkTargetTimeAt)
        {
            _checkTargetTimeAt = Time.time + CheckTargetIntervalTime;

            if (_owner.SkillPart.CheckIfTargetIsInRange(_bb.CurrentTarget))
            {
                SendEvent(EntityAIStateEvent.TargetInRange);
            }
        }
    }

    private void OnPathResponeded(PathListPoolable path)
    {
        if (path == null)
            return;

        // 대부분의 상황에서는 Cancel 이 되면 Path 루틴이 null 을 넘겨줘서 캔슬시
        // 여기까지 오지 않는 것이 맞지만 타이밍 이슈가 있을지 모르니 방어코드 추가
        if (_ctkSrc == null || _ctkSrc.IsCancellationRequested)
        {
            path.ReturnToPool();
            return;
        }

        if (_bb == null)
            return;

        _owner.MovePart.MoveAlongPaths(path, new MoveContext(_bb.CurrentMoveDestEntityType));
    }

    void OnMoveEnd(EntityBase executor, bool reachedDest)
    {
        if (EntityHelper.IsValid(_bb.CurrentTarget) == false)
        {
            SendEvent(EntityAIStateEvent.TargetLost);
            return;
        }

        // 이동이 끝났을때 사정거리 체크후 유효하다면 이벤트로 알리고
        // 모자라다면 Directional 로 마저 이동함
        float rangeDistanceRemained = _owner.SkillPart.GetDistanceRemainedToTarget(_bb.CurrentTarget);
        if (rangeDistanceRemained <= 0f)
        {
            SendEvent(EntityAIStateEvent.TargetInRange);
            return;
        }

        // 기존에 도착을 했는데 거리가 부족해 간헐적으로 공격을 못하는 현상 대응.
        // 기존에는 강제로 이 위치에서 MoveDirectional 타겟으로 그냥 다이렉트로 이동하는 방식을 썼지만
        // Target 이 있지만 MovePolicy 에서 Target 을 공격하라는 의미로 위치를 내려준게아닐수있으므로
        // 현 FSM State 에서 이걸 단정해 MoveDirectional 하는것은 안티패턴임.
        // 즉 현 시점에는 이 이슈를 , 이 위치에서 path 를 다시 요청하는거로 수정 및
        // 이렇게해도 거리가 안된다면 해당 유닛의 Range 체크를 하는 변수 요소들 체크해야함
        _nextPathFindTimeAt = Time.time;


        //Debug.LogError("Need to go more : " + rangeDistanceRemained + " , " + gameObject.name);

        //if (_bb.CurrentTarget != null)
        //{
        //    _owner.MovePart.MoveDirectional((_bb.CurrentTarget.transform.position - _owner.transform.position).normalized, 1f);
        //}
    }

    void CancelRequestPath()
    {
        if (_ctkSrc != null)
        {
            _ctkSrc.Cancel();
            _ctkSrc.Dispose();
            _ctkSrc = null;
        }
    }
}
