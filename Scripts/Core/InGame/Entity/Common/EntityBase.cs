using System;
using System.Linq;
using UnityEngine;
using GameDB;
using System.Collections.Generic;
using static EntityEventDelegates;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Text;

public abstract class EntityBase : PoolableObjectBase, IUpdatableStandard, IInteractable
{
    protected EntityTeamType _team;
    public EntityTeamType Team => _team;

    protected uint _entityTid;
    public uint EntityTID => _entityTid;

    public EntityTable TableData { get; private set; }
    public E_EntityType Type => TableData?.EntityType ?? E_EntityType.None;

    #region ====:: Parts ::====
    public EntityModel ModelPart { get; protected set; }
    public EntityAIPart AIPart { get; protected set; }
    public EntityMovePartBase MovePart { get; protected set; }
    public virtual EntityMovePartBase SubMovePart => MovePart;
    public EntityAnimationPart AnimationPart { get; protected set; }
    public EntitySkillPart SkillPart { get; protected set; }
    public EntitySpellPart SpellPart { get; protected set; }
    public EntityStatPart StatPart { get; protected set; }

    //---//
    protected EntityPartInitDataBase DefaultPartInitData;
    protected EntityAnimationPartInitData DefaultAnimationPartInitData;
    //---//

    protected virtual EntityMovePartBase CreateMovePart() => null;
    protected virtual EntityAnimationPart CreateAnimationPart(EntityAnimationPartInitData initData) =>
        InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityAnimationPart>(initData);

    protected virtual EntitySkillPart CreateSkillPart() => null;

    protected virtual EntitySpellPart CreateSpellPart() => null;

    protected virtual EntityStatPart CreateStatPart() =>
        InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityStatPart>(DefaultPartInitData);
    protected virtual EntityAIPart CreateAIPart() => null;
    #endregion

    #region ====:: Data ::====
    protected Dictionary<EntityDataCategory, EntityDataBase> _dataBase;
    static readonly Dictionary<Type, EntityDataCategory> _databaseTypeMap = new Dictionary<Type, EntityDataCategory>()
    {
        [typeof(EntityStatData)] = EntityDataCategory.Stat,
        [typeof(EntityOccupationData)] = EntityDataCategory.Occupation,
        [typeof(EntityStatisticData)] = EntityDataCategory.Statistic,
    };
    public EntityDataBase GetData(EntityDataCategory category) => _dataBase != null && _dataBase.TryGetValue(category, out var data) ? data : null;
    public T GetData<T>() where T : EntityDataBase
    {
        return _dataBase != null && _dataBase.TryGetValue(_databaseTypeMap[typeof(T)], out var data) ? data as T : null;
    }
    #endregion

    #region ====:: Events ::====
    public OnDataModified DataModifiedListener;

    public OnMovementBegin MovementBeginListener;
    public OnMoving MovementProcessingListener;
    public OnMovementEnd MovementEndListener;

    public OnHealed HealedListener;
    public OnDamaged DamagedListener;
    public OnHPChanged HpChangedListener;
    public OnLevelUp LevelUpListener;

    public OnDied DiedListener;

    public SkillTriggered SkillAniTriggerListener;

    // public OnAttackCommand AttackCommandListener;

    public OnAniamtionBegin StateAnimationBeginListener;
    public OnAniamtionEnd StateAnimationEndListener;

    public OnAnimationIKSet AnimationSkillIKSetListener;
    #endregion

    #region ====:: ETC ::====

    public bool IsAlive { get; private set; }

    public Vector3 ApproxPosition { get; private set; }

    // transform 오버헤드가 심해서 단순히 entity 의 position 이 필요할때는 이거로 쓰자
    // 대신 최신화가 항상 돼야함
    //     탈것 관련해서 최신화가 조금 애매함. 이거 잠시 보류. 하지만 추후 적용하면 퍼포먼스에 상당한 이득이 클것같음
    public void DoUpdatePositionInfo(Vector3 position)
    {
        ApproxPosition = position;
        //if (ID == 1682)
        //{
        //    Debug.LogError("UpdateApp | realPosition : " + transform.position + "  , " + ApproxPosition);
        //}
    }

    public virtual Vector3 RealForward => transform.forward;

    #endregion

    #region ====:: UniTask ::====
    private CancellationTokenSource _cancellationTokenSrc;
    #endregion

    protected virtual bool HasCameraFocusInteraction { get; } = false;
    protected virtual Type[] FocusInteractionUITypes { get; } = null;

    public bool IsInitialized { get; private set; }

    static EntityDataInitDataBase DefaultOccupationInitData = new EntityDataInitDataBase();

    bool _useUpdate;

    // UniTask 대체 
    //protected virtual IEnumerator PlayDieCutSceneCo(Vector3 attackerPosition, float force, Action onCompleted)
    //{
    //    onCompleted.Invoke();
    //    yield break;
    //}

    protected void SetPosition(Vector3 position)
    {
        transform.position = position;
        ApproxPosition = position;
    }

    protected virtual UniTask PlayDieCutScene(Vector3 attackerPosition, float force, CancellationToken ctk)
    {
        return UniTask.CompletedTask;
    }

    public async virtual UniTask OnDie(ulong attackerId, Vector3 attackerPosition, float force)
    {
        IsAlive = false;

        gameObject.layer = LayerUtils.Layer_Dead;
        if (ModelPart != null)
        {
            ModelPart.SetLayer(LayerUtils.Layer_Dead);
            ModelPart.OnDied();
        }

        if (MovePart != null)
            MovePart.OnDie(attackerPosition);

        if (AnimationPart != null)
            AnimationPart.OnDie(attackerPosition);

        await PlayDieCutScene(attackerPosition, force, _cancellationTokenSrc.Token);

        if (EntityManager.HasInstance)
            EntityManager.Instance.RemoveEntity(this);

        //CoroutineRunner.Instance.RunCoroutine(PlayDieCutSceneCo(attackerPosition, force, () =>
        //{
        //    EntityManager.Instance.RemoveEntity(this);
        //}));
    }

    public void RefreshSkills()
    {
        if (SkillPart != null)
        {
            SkillPart.ReturnToPool();
        }

        SkillPart = CreateSkillPart();

        if (SpellPart != null)
        {
            SpellPart.ReturnToPool();
        }

        SpellPart = CreateSpellPart();
    }

    protected virtual void OnUpdateImpl()
    {
        if (IsAlive == false)
            return;

        AIPart?.DoLateUpdate();
    }

    // Update() 제거 , UpdateManager 로 대체 (최적화작업)
    //void Update()
    //{
    //    if (_useUpdate)
    //    {
    //        if (UpdateManager.Instance.ExistStandard(this) == false)
    //        {
    //            Debug.LogError($"Why not Exist ? ID : {ID}");
    //        }
    //    }
    //}

    void IUpdatable.OnUpdate()
    {
        OnUpdateImpl();

        // 이거를 해야할까?
        // 위치 이동떄마다 잘 지켜주기만 하면되는데 
        //if (MovePart != null)
        //{
        //    DoUpdatePositionInfo(transform.position);
        //}
    }

    void IUpdatableStandard.OnLateUpdate()
    {
        if (IsAlive == false)
            return;

        // SubMovePart 가 있는 경우에는 계산기 복잡해지므로
        // 버그 우려가 있어서 그냥 transform.position 접근해서 업데이트 .
        // 어쩔수없음.. 주인공 캐릭 하나니까 이정도는 패스.
        if (SubMovePart != null)
            DoUpdatePositionInfo(transform.position);
    }

    void IUpdatableStandard.OnFixedUpdate()
    {
        if (IsAlive == false)
            return;

        MovePart?.DoFixedUpdate();
    }

    void OnDestroy()
    {
        DisposeCancellationToken();
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        DefaultPartInitData = new EntityPartInitDataBase(this);
        DefaultAnimationPartInitData = new EntityAnimationPartInitData(this);
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        if (_cancellationTokenSrc == null)
            _cancellationTokenSrc = new CancellationTokenSource();
    }

    protected virtual void OnBeforeEnterNewPhase(EventContext cxt)
    {
        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;
        if (ModelPart)
        {
            ModelPart.SetPhaseActivationSocket(arg.NewPhase);
        }
    }

    public override void OnInactivated()
    {
        if (_useUpdate)
        {
            //bool success = false;

            if (UpdateManager.HasInstance)
                UpdateManager.Instance.UnregisterStandard(this);

            //if (success)
            //{
            //    //#if UNITY_EDITOR
            //    //                UpdatableDebugHistory.AppendLine($"{Time.frameCount} Unregister Updatable ID : {ID} (SUCCESS)");
            //    //#endif
            //}
            //else
            //{
            //    TEMP_Logger.Err($"[UpdateError] UpdateManager Unregister Fail | Name : {name} | id : {ID}");

            //    //#if UNITY_EDITOR
            //    //                UpdatableDebugHistory.AppendLine($"{Time.frameCount} Unregister Updatable ID : {ID} (FAIL)");

            //    //                System.IO.File.WriteAllText(@"D:\Projects\JayceDefense\Log.md", UpdatableDebugHistory.ToString());
            //    //#endif
            //}
        }

        _useUpdate = false;

        base.OnInactivated();

        // 이때는 할 필요가없지않을까? 이미
        // 정상적으로 루틴을 탄 상태에서 Inactivate 되는 거라면?
        // DisposeCancellationToken();

        EventManager.Instance.Unregister(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnBeforeEnterNewPhase);

        IsInitialized = false;

        DataModifiedListener = null;
        MovementBeginListener = null;
        MovementProcessingListener = null;
        MovementEndListener = null;
        HealedListener = null;
        DamagedListener = null;
        LevelUpListener = null;
        DiedListener = null;
        SkillAniTriggerListener = null;
        StateAnimationBeginListener = null;
        StateAnimationEndListener = null;
        //AnimationSkillIKSetListener = null;

        if (_dataBase != null)
        {
            foreach (var data in _dataBase)
            {
                data.Value.ReturnToPool();
            }

            _dataBase = null;
        }

        if (ModelPart)
        {
            ModelPart.Return();
            ModelPart = null;
        }
        if (MovePart != null)
        {
            // MovePart.Release();
            MovePart.ReturnToPool();
            MovePart = null;
        }
        if (AnimationPart != null)
        {
            AnimationPart.ReturnToPool();
            AnimationPart = null;
        }
        if (SkillPart != null)
        {
            SkillPart.ReturnToPool();
            SkillPart = null;
        }
        if (SpellPart != null)
        {
            SpellPart.ReturnToPool();
            SpellPart = null;
        }
        if (StatPart != null)
        {
            StatPart.ReturnToPool();
            StatPart = null;
        }
        if (AIPart != null)
        {
            AIPart.ReturnToPool();
            AIPart = null;
        }

        // EntityManager.Instance.OnEntityInactivated(this);
    }

    public override void OnRemoved()
    {
        base.OnRemoved();

        if (ModelPart)
        {
            PoolManager.Instance.Remove(ModelPart);
            ModelPart = null;
        }
    }

    //#if UNITY_EDITOR
    //    public static StringBuilder UpdatableDebugHistory { get; private set; } = new StringBuilder(64 * 1000);

    //    string IUpdatable.DebugText()
    //    {
    //        return $"Updater Debug ID : {ID}";
    //    }
    //#endif

    public virtual void OnInitializeFinished() { }
    public virtual async UniTask<(EntityBase, IEnumerable<EntityDataBase>)> Initialize(
        E_EntityType entityType,
        IEnumerable<EntityDataBase> entityDatabase,
        EntityObjectData objectData)
    {
        IsInitialized = false;

        _entityTid = objectData.entityId;
        TableData = DBEntity.Get(objectData.entityId);
        bool isInvincible = TableData.StatTableID != 0 ? DBStat.Get(TableData.StatTableID).IsInvincible : false;

        _team = objectData.teamType;

#if UNITY_EDITOR
        gameObject.name = $"{TableData.ResourceKey}_{ID}";
#endif

        int layer = isInvincible ? LayerUtils.Layer_Invincible : LayerUtils.GetLayer((objectData.teamType, entityType));

        gameObject.layer = layer;

        transform.position = objectData.worldPosition;
        DoUpdatePositionInfo(objectData.worldPosition);

        transform.eulerAngles = new Vector3(0, objectData.eulerRotY, 0);

        if (_dataBase != null && _dataBase.Count > 0)
            TEMP_Logger.Err($"Should not be preInitialized, add data after this otherwise overwritten..");

        _dataBase = entityDatabase != null ? entityDatabase.ToDictionary((d) => d.Category, (d) => d) : new Dictionary<EntityDataCategory, EntityDataBase>();

        ModelPart = await CreateModelPart();

        if (ModelPart == null)
            return (null, null);

        var modelTs = ModelPart.transform;

        ModelPart.SetLayer(layer);

        modelTs.localPosition = Vector3.zero;
        modelTs.localRotation = Quaternion.identity;

        ModelPart.UniqueID = base.ID;

        SkillPart = CreateSkillPart();

        if (ModelPart.Animator)
        {
            DefaultAnimationPartInitData.Set(ModelPart.Animator, ModelPart.AnimatorStateBehaviours);
            AnimationPart = CreateAnimationPart(DefaultAnimationPartInitData);
        }

        MovePart = CreateMovePart();
        if (TableData.StatTableID != 0)
            StatPart = CreateStatPart();
        AIPart = CreateAIPart();

        // Spell 은 유저 조작이기 때문에 의도적으로
        // AI 파트 뒤에서 생성 (AI에 의존적일 수 있음)
        SpellPart = CreateSpellPart();

        IsInitialized = true;
        IsAlive = true;

        OnInitializeFinished();

        if (GetData(EntityDataCategory.Occupation) == null && TableData.OccupyOffsets != null && TableData.OccupyOffsets.Length > 0)
        {
            DefaultOccupationInitData.SetBaseInitData(this, EntityFactory.NextDataBaseID, EntityDataCategory.Occupation, EntityTID);
            AddDataBase(EntityDataCategory.Occupation, InGameManager.Instance.CacheContainer.EntityDataPool.GetOrCreate<EntityOccupationData>(DefaultOccupationInitData));
        }

        EventManager.Instance.Register(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnBeforeEnterNewPhase);

        _useUpdate = Type == E_EntityType.Character ||
            Type == E_EntityType.Animal ||
            Type == E_EntityType.Item ||
            Type == E_EntityType.Structure;

        if (_useUpdate)
        {
            bool success = UpdateManager.Instance.RegisterStandard(this);

            if (success)
            {
                //#if UNITY_EDITOR
                //                UpdatableDebugHistory.AppendLine($"{Time.frameCount} | Register Updatable ID : {ID} (SUCCESS)");
                //#endif
            }
            else
            {
                TEMP_Logger.Err($"[UpdateError] UpdateManager Register Fail | Name : {name} | id : {ID}");

                //#if UNITY_EDITOR
                //                UpdatableDebugHistory.AppendLine($"{Time.frameCount} Register Updatable ID : {ID} (FAIL)");

                //                System.IO.File.WriteAllText(@"D:\Projects\JayceDefense\Log.md", UpdatableDebugHistory.ToString());
                //#endif
            }
        }

        // Debug.LogError($"[UpdateInfo] Register: {ID}");

        return (this, entityDatabase);
    }

    public void AddDataBase(EntityDataCategory category, EntityDataBase database)
    {
        if (_dataBase.ContainsKey(category))
            _dataBase[category] = database;
        else
            _dataBase.Add(category, database);
    }

    protected virtual async UniTask<EntityModel> CreateModelPart()
    {
        var res = await PoolManager.Instance.RequestSpawnAsync<EntityModel>(
            ObjectPoolCategory.Model,
            TableData.ResourceKey,
            parent: transform.transform);

        if (res.instance == null)
        {
            TEMP_Logger.Err($"Failed to Spawn EntityModel | Key : {TableData.ResourceKey}");
            return null;
        }

        ModelPart = res.instance;

        res.instance.SetPhaseActivationSocket(InGameManager.Instance.CurrentPhaseType);

        return res.instance;
    }

    public virtual bool OnInteract(IInteractor interactor, InteractionContext context)
    {
        if (context is PlayerInteractContext)
        {
            PlayerInteractionManager.Instance.BeginInteraction(PlayerInteractionType.WithEntity);

            if (HasCameraFocusInteraction)
            {
                CameraManager.Instance.InGameController.RequestCameraFocus(transform);
            }

            if (FocusInteractionUITypes != null)
            {
                for (int i = 0; i < FocusInteractionUITypes.Length; i++)
                {
                    UIManager.Instance.Show(FocusInteractionUITypes[i], UITrigger.EntityInteraction, new EntityInteractionUIArg(this)).Forget();
                }
            }

            return true;
        }

        return false;
    }

    public virtual void ApplyAffect(IEntityAffecter affector)
    {
        ApplyAffect(affector.ExecutorID, (int)affector.Damage, (int)affector.Heal, affector.Position, affector.PhysicalForce);
    }

    public virtual void ApplyAffect(ulong executorId, int damage, int heal, Vector3 effectPos, float effectForce = 0f)
    {
        int healDmgDiff = heal - damage;
        if (healDmgDiff > 0)
        {
            HealedListener?.Invoke(executorId, healDmgDiff, effectPos);
        }
        else
        {
            DamagedListener?.Invoke(executorId, Mathf.Abs(healDmgDiff), effectPos, effectForce);
        }
    }

    public virtual void AcquireItem(uint itemId, int amount)
    {
        if (Team == EntityTeamType.Player)
        {
            Me.AcquireItem(itemId, amount);
        }
        // 적이 아이템을 얻는 경우, 일단 없음?
        else
        {
            TEMP_Logger.Err($"Not Implemented Type : {Team}");
        }
    }

    void DisposeCancellationToken()
    {
        if (_cancellationTokenSrc != null)
        {
            _cancellationTokenSrc.Cancel();
            _cancellationTokenSrc.Dispose();
            _cancellationTokenSrc = null;
        }
    }
}
