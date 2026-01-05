using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class StructureEntity : EntityBase, IResourceGenerator
{
    public StructureTable StructureData { get; private set; }

    protected override bool HasCameraFocusInteraction => true;
    protected override Type[] FocusInteractionUITypes => new Type[] { typeof(UIStructureInfo) };

    #region ====:: 자원 생성 관련 ::====
    protected EntityResourceGeneratePart ResourceGeneratorPart { get; private set; }

    bool IResourceGenerator.IsEnabled => ResourceGeneratorPart?.IsEnabled ?? false;
    E_ResourceType IResourceGenerator.ResourceType => ResourceGeneratorPart?.ResourceType ?? E_ResourceType.None;
    uint IResourceGenerator.DetailID => ResourceGeneratorPart?.DetailResourceId ?? 0;
    uint IResourceGenerator.Amount => ResourceGeneratorPart?.Amount ?? 0;
    float IResourceGenerator.GenerateInterval => ResourceGeneratorPart?.Interval ?? 0f;
    float IResourceGenerator.CurrentProgress => ResourceGeneratorPart?.Progress ?? 0f;

    public event EntityEventDelegates.OnResourceGenerated OnGenerated;
    #endregion

    bool _isShowingLowHealth;
    FXBase _lowHealthFx;

    [SerializeField]
    DOTweenAnimation _tweenAnimation;

    EntityResourceGeneratorInitData _defResGenInitData;

    //static EntityDataInitDataBase DefaultOccupationInitData = new EntityDataInitDataBase();

    public override async UniTask<(EntityBase, IEnumerable<EntityDataBase>)> Initialize(
        E_EntityType entityType,
        IEnumerable<EntityDataBase> entityDatabase,
        EntityObjectData objectData)
    {
        StructureData = DBStructure.GetByEntityID(objectData.entityId);
        if (StructureData == null)
        {
            TEMP_Logger.Err($"Failed to get StructureEntityData | EntityTID : {EntityTID}");
        }

        if (StructureData.GenResourceType != E_ResourceType.None)
        {
            InitializeResourceGenerator();
        }

        var res = await base.Initialize(entityType, entityDatabase, objectData);

        DamagedListener += OnDamaged;
        HpChangedListener += OnHpChanged;

        _tweenAnimation.DOPlay();

        _isShowingLowHealth = false;

        return res;
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _defResGenInitData = new EntityResourceGeneratorInitData(this);
    }

    void InitializeResourceGenerator()
    {
        _defResGenInitData.Set(
            StructureData.GenResourceType,
            StructureData.GenResourceID,
            StructureData.GenResourceBaseAmount,
            StructureData.GenResourceInterval);

        ResourceGeneratorPart = InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityResourceGeneratePart>(_defResGenInitData);

        ResourceGeneratorPart.OnGeneratedListener += OnResourceGenerated;

        ResourceGeneratorPart.IsEnabled = InGameManager.Instance.CurrentPhaseType == InGamePhase.Peace;
    }

    private void OnResourceGenerated(E_ResourceType type, uint id, uint amount)
    {
        OnGenerated?.Invoke(type, id, amount);
    }

    protected override void OnUpdateImpl()
    {
        base.OnUpdateImpl();

        if (ResourceGeneratorPart != null)
            ResourceGeneratorPart.Update();
    }

    //public override void OnInitializeFinished()
    //{
    //    base.OnInitializeFinished();
    //}

    public async override UniTask OnDie(ulong attackerId, Vector3 attackerPos, float force)
    {
        AudioManager.Instance.Play("Structure_Destroyed", ApproxPosition, AudioTrigger.Default);

        var fxPos = ApproxPosition + new Vector3(0, 2f, 0);
        PoolManager.Instance.RequestSpawnAsync<FXParticleSystem>(
                ObjectPoolCategory.Fx,
                StructureData.DestroyEffectKey,
                fxPos).Forget();

        await base.OnDie(attackerId, attackerPos, force);
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        ReleaseInternal();
    }

    void OnDamaged(ulong executorId, int damaged, Vector3 effectPos, float effectForce = 0f)
    {
        // 이거를 테이블로 뺄 필요가 있을까? 
        //FXSystem.PlayFX("SpriteFX_DustShattered",
        //    positionGetter: () =>
        //    {
        //        // 비동기 방어코드
        //        if (this)
        //            return effectPos;
        //        return default;
        //    },
        //    rotationGetter: () =>
        //    {
        //        if (this)
        //            return transform.rotation;
        //        return default;
        //    }, (res) =>
        //    {
        //        if (!this)
        //        {
        //            res.Return();
        //        }
        //    });
    }

    protected override void OnBeforeEnterNewPhase(EventContext cxt)
    {
        base.OnBeforeEnterNewPhase(cxt);

        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;
        if (arg.NewPhase == InGamePhase.Battle)
        {
            if (ResourceGeneratorPart != null)
                ResourceGeneratorPart.IsEnabled = false;

            (GetData(EntityDataCategory.Occupation) as EntityOccupationData).Refresh();
        }
        else if (arg.NewPhase == InGamePhase.Peace)
        {
            if (ResourceGeneratorPart != null)
                ResourceGeneratorPart.IsEnabled = true;
        }
    }

    void OnHpChanged(int maxHP, int currentHP, int diff)
    {
        float hpRatio = (float)currentHP / maxHP;
        bool isLowHealthMode = hpRatio <= 0.5f;

        if (isLowHealthMode == _isShowingLowHealth)
            return;

        _isShowingLowHealth = isLowHealthMode;

        if (isLowHealthMode)
        {
            // 여기 비동기 처리에서 버그가 난다면 ,
            // ulong 으로 비동기 처리 들어간 시점에서의 entity unique id 를 체크를 하던
            // 아니면 구조를 그냥 뒤집던 해야할듯. 지금 state 가 너무 많은거같아 ㅡㅡ

            FXSystem.PlayFXCallBack("FX_DustZone",
                startPosition: ModelPart.GetSocket(EntityModelSocket.LowHealthEffect).position,
                scale: ModelPart.VolumeRadius,
                onCompleted: (fx) =>
                {
                    if (EntityHelper.IsValid(this))
                        _lowHealthFx = fx;
                    else
                        PoolManager.Instance.Return(fx);
                }).Forget();
        }
        else
        {
            if (_lowHealthFx)
            {
                _lowHealthFx.Return();
                _lowHealthFx = null;
            }
        }
    }

    public override bool OnInteract(IInteractor interactor, InteractionContext context)
    {
        bool baseInteracted = base.OnInteract(interactor, context);

        if (context is PlayerInteractContext)
        {
            AudioManager.Instance.Play(
                StructureData.SoundKey,
                Vector3.zero,
                AudioTrigger.EntityInteraction,
                0,
                new AudioSettings()
                {
                    loop = true,
                    enableAutoStop = false
                });
        }

        return baseInteracted;
    }

    void ReleaseInternal()
    {
        DamagedListener -= OnDamaged;
        HpChangedListener -= OnHpChanged;

        if (ResourceGeneratorPart != null)
        {
            ResourceGeneratorPart.OnPoolReturned();
            ResourceGeneratorPart = null;
        }

        if (_isShowingLowHealth)
        {
            _isShowingLowHealth = false;
        }

        if (_lowHealthFx)
        {
            _lowHealthFx.Return();
            _lowHealthFx = null;
        }
    }
}
