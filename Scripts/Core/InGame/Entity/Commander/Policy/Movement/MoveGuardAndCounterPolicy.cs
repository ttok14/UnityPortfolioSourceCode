using System.Threading;
using UnityEngine;
using GameDB;

public class MoveGuardAndCounterPolicyInitData : MovePolicyInitDataBase
{
    public IObjectiveProvider ObjectiveProvider;
    public MoveGuardAndCounterPolicy.Mode InitialMode;

    public void Set(EntityBase mover, IObjectiveProvider objectiveProvider, SpawnerMode spawnMode)
    {
        base.Set(mover);

        ObjectiveProvider = objectiveProvider;
        InitialMode = spawnMode == SpawnerMode.None || spawnMode == SpawnerMode.Defensive ? MoveGuardAndCounterPolicy.Mode.Guard : MoveGuardAndCounterPolicy.Mode.Counter;
    }
}

public class MoveGuardAndCounterPolicy : MovePolicyBase
{
    public enum Mode
    {
        None = 0,

        // 수비 
        Guard,

        // 공격
        Counter
    }

    const float GuardDistanceOffset = 4f;
    const float GuardSpreadRadius = 3f;
    const float SqrGuardArriveCheckThresholdDistance = 1.5f;
    const float GuardNextArriveCheckInterval = 1.5f;

    // 공격 모드에서 상대가 아래 거리 이상을 이동하면 다시 찾음
    // 이거는 좀 짧게해야하는게 , 지금 도착했는데 거리가 안돼서 공격을 못하는 경우에
    // Path 재요청을 하는 것 말고 이 케이스에 대한 방어가 없는데
    // 최소한 Path 재요청시 최대한 타겟에 근접해서 공격 모드로 전환할수있도록 애초에
    // 이 MovePolicy 에서 최대한 가까이 줘야함.
    // (성능은 recheck interval 로 해야할듯 이거로 했다가는 타겟을 공격안하는 버그발생가능)
    const float SqrCounterRefindPathThresholdDistance = 0.2f * 0.2f;
    // 위에서 언급한 타겟에 대한 길찾기 재시도 대기시간 
    const float CounterRefindPathInterval = 2f;

    Mode _currentMode;

    ulong _lastGuardEntityId;
    ulong _lastCounterEntityId;
    Vector3 _lastDestinationWorldPos;
    E_EntityType _lastDestEntityType;
    Vector3 _lastGuardArriveCheckPosition;
    MoveCommandResult _moveResult;

    float _guardNextArriveCheckTimeAt;
    float _guardIdleElapsedTime;
    float _counterNextPathCheckTimeAt;



    IObjectiveProvider _objectiveProvider;

    public override MoveCommand GetCommand(EntityBase target)
    {

        switch (_currentMode)
        {
            case Mode.None:
                return MoveCommand.Stop;
            case Mode.Guard:
                bool doGuard = UpdateGuardModeState();
                if (doGuard)
                {
                    return new MoveCommand()
                    {
                        result = _moveResult,
                        destination = _lastDestinationWorldPos,
                        destEntityType = _lastDestEntityType

                    };
                }
                else
                {
                    return MoveCommand.Stop;
                }
            case Mode.Counter:
                if (UpdateCounterState())
                {
                    return new MoveCommand()
                    {
                        result = _moveResult,
                        destination = _lastDestinationWorldPos,
                        destEntityType = _lastDestEntityType
                    };
                }
                return MoveCommand.Stop;
        }

        return MoveCommand.Stop;
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as MoveGuardAndCounterPolicyInitData;
        _objectiveProvider = data.ObjectiveProvider;

        // _lastDestinationPos = default;
        ResetDestPos();

        _currentMode = data.InitialMode;

        switch (_currentMode)
        {
            case Mode.Guard:
                UpdateGuardModeState();
                break;
            case Mode.Counter:
                SwitchToCounterMode();
                break;
        }
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        _objectiveProvider = null;
        _currentMode = Mode.None;
        _lastGuardEntityId = 0;
        _lastCounterEntityId = 0;
        ResetDestPos();
        _guardIdleElapsedTime = 0;
        _lastGuardArriveCheckPosition = default;
        _guardNextArriveCheckTimeAt = 0;
        _counterNextPathCheckTimeAt = 0;
    }

    bool UpdateGuardModeState()
    {
        var guardEntity = _objectiveProvider.GetTargetEntity(EntityHelper.ToOpponentTeamType(_mover.Team));
        if (EntityHelper.IsValid(guardEntity) == false)
        {
            SwitchToCounterMode();
            return false;
        }

        Vector3 moverPosition = _mover.ApproxPosition.FlatHeight();

        if (_lastGuardEntityId != 0 && Time.time >= _guardNextArriveCheckTimeAt)
        {
            _guardNextArriveCheckTimeAt = Time.time + GuardNextArriveCheckInterval;
            float sqrDistRemained = Vector3.SqrMagnitude(_lastDestinationWorldPos - moverPosition);

            // 1차적으로 도착 체크를, 예상되는 도착 지점과의 거리로 체크
            // (하지만 불안정, 실제 유닛의 위치가 거기로 갔을거란 보장 없음)
            if (sqrDistRemained <= SqrGuardArriveCheckThresholdDistance)
            {
                SwitchToCounterMode();
                return false;
            }
            else
            {
                // 2차로는 현재 위치가 마지막 체크 위치로부터 어느정도 이상 움직였는지 확인
                // 근데 스킬 사용중이라면 (싸우는중이라면, 조건 대체해야하나?)
                // 이거는 idle 로 보면안됨
                if (Vector3.SqrMagnitude(moverPosition - _lastGuardArriveCheckPosition) >= 0.2f || _mover.SkillPart.IsCasting)
                {
                    // 움직였음 -> 위치 갱신 및 대기 시간 초기화
                    _lastGuardArriveCheckPosition = moverPosition;
                    _guardIdleElapsedTime = 0f;
                }
                else
                {
                    _guardIdleElapsedTime += GuardNextArriveCheckInterval;

                    // 1초 이상(2번의 체크 주기 동안) 제자리라면 도착으로 간주하고 공격 모드로 전환
                    if (_guardIdleElapsedTime >= GuardNextArriveCheckInterval)
                    {
                        SwitchToCounterMode();
                        return false;
                    }
                }
            }
        }

        if (_lastGuardEntityId == guardEntity.ID)
        {
            return true;
        }

        _lastGuardEntityId = guardEntity.ID;

        switch (guardEntity.Type)
        {
            case GameDB.E_EntityType.Structure:
                {
                    Vector3 opponentPosition = GetOpponentPosition();
                    opponentPosition.y = 0;
                    var guardEntityPos = guardEntity.ApproxPosition.FlatHeight();

                    Vector3 dir = (opponentPosition - guardEntityPos).normalized;

                    var occupationData = guardEntity.GetData(EntityDataCategory.Occupation) as EntityOccupationData;
                    // 여기서는 단지 가장 가까운 위치를 구할거기 때문에
                    // 의도적으로 Flag 에 None 을 넘김 
                    occupationData.GetClosestPositionFrom(opponentPosition, GameDB.E_EntityFlags.None, out Vector3 destPos);

                    // Offset 만큼 거리를 벌려준 상태에서
                    // 양옆 사이드로 랜덤하게 Radius 만큼 띄어줌
                    destPos += (dir * GuardDistanceOffset) + (Vector3.Cross(Vector3.up, dir).normalized * UnityEngine.Random.Range(-GuardSpreadRadius, GuardSpreadRadius));

                    // 선정된 destPos 가 위치할 수 없는 타일이라면 
                    if (MapManager.Instance.CanPlace(destPos, _mover.TableData.EntityFlags) == false)
                    {
                        bool failedGetDestPos = false;

                        var occupyingEntityId = MapManager.Instance.GetTileOccupierID(MapUtils.WorldPosToTilePos(destPos));

                        // 해당 위치를 점유중인 엔티티가 발견되면 
                        if (occupyingEntityId != 0)
                        {
                            occupationData = EntityManager.Instance.GetEntity(occupyingEntityId).GetData(EntityDataCategory.Occupation) as EntityOccupationData;
                            if (occupationData.GetClosestPositionFrom(destPos, _mover.TableData.EntityFlags, out destPos) == false)
                            {
                                failedGetDestPos = true;
                            }
                        }
                        else
                        {
                            failedGetDestPos = true;
                        }

                        if (failedGetDestPos)
                        {
                            SwitchToCounterMode();
                            return false;
                        }
                    }

                    SetDestPos(destPos, E_EntityType.Structure);
                }
                break;
            default:
                SwitchToCounterMode();
                TEMP_Logger.Err($"Not Implemented Type : {guardEntity.Type}");
                return false;
        }

        return true;
    }

    private void SetDestPos(Vector3 destPos, E_EntityType destEntityType)
    {
        _lastDestinationWorldPos = destPos;
        _lastDestEntityType = destEntityType;

        // 분기로 건물같은 경우 무조건 PathFind 쓰고
        // 캐릭터같이 추적하는 용도면 분기
        switch (destEntityType)
        {
            case E_EntityType.Structure:
                _moveResult = MoveCommandResult.Path;
                break;
            case E_EntityType.Character:
                bool canGoDirectly =
                    MapManager.Instance.CanPlaceFromTo(
                    _mover.ApproxPosition.FlatHeight(),
                    destPos,
                    _mover.TableData.EntityFlags);

                // 근데 혹시 Directional Move 로 타겟을 추격하다가
                // NotWalkable 쪽으로 꺽이면서 들어가면 이때 처리는 돼있나?
                // 잘못하면 길찾기 PathMove 일떄 Fail 될텐데 간헐적으로
                _moveResult = canGoDirectly ?
                    MoveCommandResult.Directional :
                    MoveCommandResult.Path;
                break;
        }
    }

    void ResetDestPos()
    {
        _lastDestinationWorldPos = default;
        _lastDestEntityType = E_EntityType.None;
        _moveResult = MoveCommandResult.Stop;
    }

    private bool UpdateCounterState()
    {
        var target = _objectiveProvider.GetTargetEntity(_mover.Team);
        if (target == null)
        {
            _lastCounterEntityId = 0;
            return false;
        }

        if (_lastCounterEntityId == target.ID)
        {
            if (Time.time >= _counterNextPathCheckTimeAt)
            {
                _counterNextPathCheckTimeAt = Time.time + CounterRefindPathInterval;

                float sqrDist = Vector3.SqrMagnitude(target.ApproxPosition.FlatHeight() - _lastDestinationWorldPos);

                if (sqrDist <= SqrCounterRefindPathThresholdDistance)
                    return true;
            }
            else
            {
                return true;
            }
        }

        // _lastDestinationPos = MapUtils.WorldPosToTilePos(pos);
        SetDestPos(target.ApproxPosition.FlatHeight(), target.Type);

        _lastCounterEntityId = target.ID;

        return true;
    }

    void SwitchToCounterMode()
    {
        _currentMode = Mode.Counter;
        UpdateCounterState();
    }

    Vector3 GetOpponentPosition()
    {
        if (_mover.Team == EntityTeamType.Player)
        {
            return InGameManager.Instance.EnemyCommander.SpawnController.GetCenterPosition();
        }
        else if (_mover.Team == EntityTeamType.Enemy)
        {
            return InGameManager.Instance.PlayerCommander.Player.Entity.ApproxPosition;
        }
        return default;
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }
}
