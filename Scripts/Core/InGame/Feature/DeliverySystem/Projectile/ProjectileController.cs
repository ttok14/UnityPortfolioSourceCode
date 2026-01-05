using UnityEngine;
using GameDB;

public class ProjectileController : PoolableObjectBase, IUpdatable, IMovementDataSource, IDeliverySource
{
    ProjectileTable _tableData;
    public uint TableDataID => _tableData.ID;

    Collider _collider;

    #region ====:: 메인 구성 요소 ::====
    MovementStrategyBase _movement;
    ImpactStrategyBase _impact;

    IDeliveryUpdateStrategy _updateStrategy;

    DeliveryActionBase _processAction;
    DeliveryActionBase _endAction;

    DeliveryContext _deliveryContext;
    #endregion

    #region ====:: IDelivery Source ::====
    public Vector3 Position => transform.position;
    public float ElapsedAliveTIme { get; private set; }
    #endregion

    #region ===:: Interface Impli ::===
    public Transform Mover => this.transform;
    public Vector3 StartPosition { get; private set; }
    public Vector3 DestOffsetPosition { get; private set; }
    public float MoveSpeed => _tableData.MoveSpeed;
    public float RotationSpeed => 5000;
    public E_AimType AimType => _tableData.AimType;
    public EntityBase Executor => EntityManager.Instance.GetEntity(_executorUniqueId);
    public EntityBase Target => EntityManager.Instance.GetEntity(_targetEntityUniqueId);
    public Vector3 Destination { get; private set; }
    #endregion

    ulong _executorUniqueId;
    ulong _targetEntityUniqueId;

    EntityTeamType _projectileTeamType;
    EntityTeamType _targetTeamType;

    float _applyStatReduction;

    uint _originalDamage;
    uint _originalHeal;

    // 충돌 레이어는 이미 entity 단에서 설정중
    int _targetLayerMask;

    TrailRenderer[] _trailRenderers;

    bool _fired;
    bool _exiting;
    float _returnAt;

    [SerializeField]
    float _delayEndTimeLength = 0;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _collider = GetComponentInChildren<Collider>();

        _tableData = DBProjectile.Get(key);

        _trailRenderers = GetComponentsInChildren<TrailRenderer>();

        if (_trailRenderers != null)
        {
            foreach (var r in _trailRenderers)
            {
                r.enabled = false;
            }
        }
    }

    public override void OnInactivated()
    {
        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingle(this);

        base.OnInactivated();

        ElapsedAliveTIme = 0f;

        if (_movement != null)
        {
            _movement.ReturnToPool();
            _movement = null;
        }

        if (_impact != null)
        {
            _impact = null;
        }

        if (_deliveryContext != null)
        {
            // 주의, 여기서는 바로 Return 할게 아니라 ,
            // 여전히 이 Context 를 사용중인 투사체가 있을수 있기에
            // 참조 카운트만 깍음.
            _deliveryContext.DecreaseReferenceCount(1);
            _deliveryContext = null;
        }

        if (_updateStrategy != null)
        {
            _updateStrategy.ReturnToPool();
            _updateStrategy = null;
        }

        if (_processAction != null)
        {
            _processAction = null;
        }

        if (_endAction != null)
        {
            _endAction = null;
        }


        Destination = Vector3.zero;
        StartPosition = Vector3.zero;
        DestOffsetPosition = Vector3.zero;

        _applyStatReduction = 1f;

        _originalDamage = 0;
        _originalHeal = 0;

        _executorUniqueId = 0;
        _targetEntityUniqueId = 0;

        _projectileTeamType = EntityTeamType.None;
        _targetTeamType = EntityTeamType.None;

        _returnAt = 0f;
        _exiting = false;
        _fired = false;

        if (_trailRenderers != null)
        {
            foreach (var r in _trailRenderers)
            {
                r.enabled = false;
            }
        }
    }

    public void Fire(
        EntityBase executor,
        Vector3 startPosition,
        uint damage,
        uint heal,
        EntityTeamType projectileTeamType,
        EntityTeamType targetTeamType,
        EntityBase target = null,
        Vector3 fixedPoint = default,
        DeliveryContext inheritContext = null)
    {
        _exiting = false;

        _originalDamage = damage;
        _originalHeal = heal;

        executor = EntityHelper.IsValid(executor) ? executor : null;
        target = EntityHelper.IsValid(target) ? target : null;

        _executorUniqueId = executor ? executor.ID : 0;

        ElapsedAliveTIme = 0f;

        // 먼저, 타격 지점 Indicator 표시해줄지 여부
        if (_tableData.EnableShowTargetingIndicator)
        {
            // Projectile 특성상 조건에 맞을때만 보여줄 수 있음
            bool showTargetingSpotIndicator =
                _tableData.CollisionRangeType == E_CollisionRangeType.RangeArea
                && _tableData.TargetingType == E_ProjectileTargetingType.FixedPoint;

            if (showTargetingSpotIndicator)
            {
                var rangeIndicatorPos = fixedPoint;
                rangeIndicatorPos.y = 0.1f;

                FXSystem.PlayFX_RangeIndicator(
                    new Color(0.85f, 0.56f, 0.56f, 1f),
                    _tableData.CollisionAreaRange,
                    // Duration 은 어떻게 조절하는게 자연스러울까.
                    1.5f,
                    // 무조건 바닥에 표시
                    position: rangeIndicatorPos);
            }
        }

        switch (_tableData.TargetingType)
        {
            case E_ProjectileTargetingType.FixedPoint:
                // target ID 같은 경우는, 현재 투사체가 Target 을 향해 타겟팅하는 경우에만
                // 내부적으로 가지고 있어야함 
                _targetEntityUniqueId = 0;
                // fixedPoint = new Vector3(fixedPoint.x, startPosition.y, fixedPoint.z);
                Destination = fixedPoint;
                DestOffsetPosition = Vector3.zero;
                break;
            case E_ProjectileTargetingType.Directional:
                _targetEntityUniqueId = 0;
                // max Dist 에 0 을 넣으면 그냥 그 방향으로 무제한 날아가겠다라는 걸로 . 
                float maxDist = _tableData.MaxDistance > 0f ? _tableData.MaxDistance : 9999f;
                Destination = startPosition + (fixedPoint.FlatHeight() - startPosition.FlatHeight()).normalized * maxDist;
                DestOffsetPosition = Vector3.zero;
                break;
            case E_ProjectileTargetingType.TrackingTarget:
                //if (target == null)
                //{
                //    TEMP_Logger.Err($"Given Type({E_ProjectileTargetingType.TrackingTarget} requires TargetTransform | Key : {Key}");
                //    ForceEnd();
                //    return;
                //}

                if (target)
                {
                    // target ID 같은 경우는, 현재 투사체가 Target 을 향해 타겟팅하는 경우에만
                    // 내부적으로 가지고 있어야함 
                    _targetEntityUniqueId = target.ID;
                    DestOffsetPosition = new Vector3(DestOffsetPosition.x, target.ModelPart.VolumeHeight * 0.5f, DestOffsetPosition.z);
                }
                // Tracking Target 인데 현 시점 타겟이 없는 경우에는
                // 외부에서 넣어줬을때는 Target 이 Valid 한 상황이었을거임.
                // 근데 이 시점에 갑자기 타겟이 없어진건 이 투사체를 요청? 한 시점에는 있었다가
                // 지금은 죽어서 사라진 것 일것 .
                // 이 상황에서는 갑자기 타겟이 사라졌다고 투사체를 발사 안하는게 아닌
                // 시체에다라도 발사시킨다. (기존엔 타겟이 사라지면 투사체 발사를 취소했는데
                // 이게 시각적으로 매우 어색함, 롤도보면 상대 죽어도 시체에 쏨)
                else
                {
                    _targetEntityUniqueId = 0;
                    Destination = fixedPoint;
                    DestOffsetPosition = Vector3.zero;
                }
                break;
            default:
                TEMP_Logger.Err($"Not implemented ProjectileTargetingType : {_tableData.MovementType}");
                ForceEnd();
                break;
        }

        _targetLayerMask = LayerUtils.GetLayerMask((targetTeamType, E_EntityType.Structure), (targetTeamType, E_EntityType.Character));

        StartPosition = startPosition;

        transform.position = startPosition;
        // transform.eulerAngles = startEulerRot;

        // LateUpdate 같은데로 옮겨야하나? 잔상이남지왜
        if (_trailRenderers != null)
        {
            foreach (var r in _trailRenderers)
            {
                r.enabled = true;
            }
        }

        _movement = MovementFactory.Create(_tableData.MovementType, this);
        _impact = ImpactSystemFactory.CreateStrategy(ImpactType.Standard);
        _processAction = ProjectileSystem.GetProcessAction(_tableData.ID);
        _endAction = ProjectileSystem.GetEndAction(_tableData.ID);
        _updateStrategy = DeliveryUpdateFactory.GetStrategy(_tableData.UpdateLogicType, _tableData.UpdateLogicValue);

        // Context 가 없다 => 최초 발사된 투사체 
        _deliveryContext = DeliveryActionFactory.GetDeliveryContext(
            inheritContext,
            executor ? executor.ID : 0,
            projectileTeamType,
            targetTeamType,
            _targetLayerMask,
            _tableData.CollisionRangeType,
            _tableData.CollisionAreaRange,
            _tableData.AllowMultiHit,
            _tableData.PreferMaxTargetCount,
            damage,
            heal,
            false,
            _tableData.HitSFXKeys,
            _tableData.HitFXKeys,
            _tableData.CollisionForce,
            _tableData.InheritType);

        // 물려받은 Context 는 이제 쓰임이 끝났으니 참조 카운트 하나 깍아줌
        // (물려받은 거는 이전에 물려받는 목적으로 하나가 올라가 있는 상태임)
        inheritContext?.DecreaseReferenceCount();

        _applyStatReduction = 1;

        _projectileTeamType = projectileTeamType;
        _targetTeamType = targetTeamType;

        if (_collider)
        {
            if (_tableData.CollisionActivationType != E_ProjectileCollisionActivationType.OnArriveDest)
            {
                _collider.enabled = true;
            }
            else
            {
                _collider.enabled = false;
            }
        }

        _fired = true;

        UpdateManager.Instance.RegisterSingle(this);
    }

    // private void Update()
    void IUpdatable.OnUpdate()
    {
        if (_fired == false)
            return;

        if (_exiting)
        {
            if (Time.time >= _returnAt)
                Return();

            return;
        }

        if (_tableData.LifeTime > 0f)
        {
            ElapsedAliveTIme += Time.deltaTime;

            if (ElapsedAliveTIme >= _tableData.LifeTime)
            {
                ForceEnd();
                return;
            }
        }

        if (_updateStrategy != null)
        {
            _updateStrategy.OnUpdate(this, _deliveryContext);

            if (IsActivated == false)
                return;
        }

        if (_movement != null)
        {
            MovementStrategyResult movementRes = _movement.UpdateMovement();

            if (movementRes == MovementStrategyResult.Finished)
            {
                ExecuteProcess(Target, false, true);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exiting || IsActivated == false)
            return;

        if (_tableData.CollisionActivationType == E_ProjectileCollisionActivationType.OnArriveDest)
            return;

        int otherLayerMask = 1 << other.gameObject.layer;

        if ((otherLayerMask & _targetLayerMask) == 0)
            return;

        var entity = InGameManager.Instance.CacheContainer.GetEntityFromCollider(other);
        if (entity == null)
        {
            TEMP_Logger.Err($"Projectile must collide with only entities | This Tid : {_tableData.ID} , other Layer : {other.gameObject.layer} , other Name : {other.gameObject.name}");
            return;
        }

        // 이미 충돌한 애임
        if (_tableData.AllowMultiHit == false && _deliveryContext.DeliveryHistory.ImpactedIDs.Contains(entity.ID))
            return;

        var target = EntityManager.Instance.GetEntity(entity.ID);
        if (EntityHelper.IsValid(target) == false)
            return;

        if (_tableData.CollisionActivationType == E_ProjectileCollisionActivationType.SpecificTargetOnHit)
        {
            // 애는 고유한 ID 를 상대로 검사하기 때문에 TeamType 체크할 필요가없음 
            bool isTarget = _targetEntityUniqueId == target.ID;
            if (isTarget == false)
                return;
        }
        else if (_tableData.CollisionActivationType == E_ProjectileCollisionActivationType.AnyTargetOnHit)
        {
            // 충돌은 했으나, Projectile 이 타게팅 하는 Team 이 아님
            if (target.Team != _targetTeamType)
                return;
        }
        else
        {
            TEMP_Logger.Err($"Not implemented t ype : {_tableData.CollisionActivationType}");
            return;
        }

        // 효과 발동
        ExecuteProcess(target, false, false);
    }

    public void OnDeliveryTrigger(E_UpdateLogicType type)
    {
        switch (type)
        {
            case E_UpdateLogicType.Timer:
                ForceEnd();
                break;
            case E_UpdateLogicType.Interval:
                ExecuteProcess(null, true, false);
                break;
            default:
                TEMP_Logger.Err($"Not implemneted : {type}");
                break;
        }
    }

    // 실제 효과 발동
    // 주의할건 데이터에 따라 target 이 없을 수 있음 (그냥 지정된 포인트에 쏜다거나 e.g 폭탄 날리기)
    void ExecuteProcess(EntityBase target, bool isPeriodic, bool movementFinished)
    {
        EntityTeamType targetTeam = target ? target.Team : EntityTeamType.None;

        _deliveryContext.SetPosition(transform.position);

        bool executeHitImpact =
            isPeriodic == false &&
            (target || movementFinished) &&
            // 공격형인데 투사체의 팀과 타겟으로 잡힌애의 팀이 같다면 패스한다
            // 왜냐면 돌아오는 표창 구현같은 경우 target 이 executor 임. 이런 케이스에 대한 처리.
            (_deliveryContext.Damage == 0 || targetTeam != _projectileTeamType);

        if (executeHitImpact)
        {
            _impact.Execute(this, target, _deliveryContext);
        }

        bool isProcessDestroy = false;

        if (_processAction != null)
        {
            _processAction.Execute(this, target, _deliveryContext);

            isProcessDestroy = _tableData.ProcessDestroy;
        }

        if (IsActivated)
        {
            if ((executeHitImpact && _tableData.HitDestroy) || isProcessDestroy || movementFinished)
            {
                ForceEnd();
            }
            else
            {
                if (executeHitImpact && _tableData.StatReductionRatioPerHit >= 0.01f)
                {
                    _applyStatReduction = Mathf.Max(1f - (_deliveryContext.DeliveryHistory.ImpactedIDs.Count * _tableData.StatReductionRatioPerHit), _tableData.StatReductionMinRatio);

                    _deliveryContext.Damage = (uint)(_originalDamage * _applyStatReduction);
                    _deliveryContext.Heal = (uint)(_originalHeal * _applyStatReduction);
                }
            }
        }
    }

    public void ForceEnd()
    {
        if (IsActivated == false)
            return;

        if (_endAction != null)
        {
            _endAction.Execute(this, null, _deliveryContext);
        }

        if (_collider)
            _collider.enabled = false;

        // 임의 딜레이 설정시, 바로 끝내지않음.
        if (_delayEndTimeLength > 0f)
        {
            _exiting = true;
            _returnAt = Time.time + _delayEndTimeLength;
        }
        else
        {
            Return();
        }
    }
}
