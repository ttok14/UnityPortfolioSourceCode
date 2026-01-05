using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class EntityModel : PoolableObjectBase
{
    public enum ShadowOption
    {
        NoShadow,
        CircleShadow_Height,
        CircleShadow_NoHeight
    }

    [Serializable]
    public class Socket
    {
        public EntityModelSocket socket;
        public Transform ts;
    }

    [Serializable]
    public class Equipment
    {
        public EntityEquipmentType type;
        public Transform ts;

        public EntityModelSocket defaultSocket;

        [HideInInspector]
        public EntityModelSocket currentSocket;
    }

    class RendererCache
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    [SerializeField]
    private Socket[] _sockets;
    Dictionary<EntityModelSocket, Socket> _socketDic = new Dictionary<EntityModelSocket, Socket>();

    [SerializeField]
    private Equipment[] _equipments;
    Dictionary<EntityEquipmentType, Equipment> _equipmentsDic = new Dictionary<EntityEquipmentType, Equipment>();

    //[SerializeField]
    //IKController _ik;
    //public IKController IK => _ik;

    [SerializeField]
    private SpriteRenderer _skillRangeIndicator;

    Collider _collider;

    // 굳이 생성마다 찾을 건 아니고 , 특수한 경우에만 (ghost 표현 등)
    List<RendererCache> _renderers;
    bool _isRendererDirty;

    // TODO : Entity 프리팹에 가장 근접한 컴포넌트기에
    // 모델 자체에 속한 특수 정보같은거를
    // 이곳에서 EntityBase 에게 제공해줄수 있음
    //--------------------------//

    public event Action OnSkillAnimationEventReceivedListener;

    // public event Action<int> OnAnimationEventReceivedIntListener;
    //public event Action<string> OnAnimationEventReceivedStringListener;

    private ulong _uniqueID;
    public ulong UniqueID
    {
        get => _uniqueID;
        set => _uniqueID = value;
    }

    [SerializeField]
    private ShadowOption _shadowOption;
    [SerializeField]
    private float _shadowScale = 1f;
    CircleShadow _circleShadow;

    [SerializeField]
    private TweenSequenceRunner _tweenRunner;

    #region ====:: Animations ::====
    public Animator Animator { get; private set; }
    public Dictionary<EntityAnimationStateID, float> AnimationTriggerData { get; private set; }
    public EntityAnimationStateBeh[] AnimatorStateBehaviours { get; private set; }
    #endregion

    public Vector3 TopPosition
    {
        get
        {
            var pos = transform.position;
            pos.y = VolumeHeight;
            return pos;
        }
    }

    float? _volumeRadius;

    // 모든 model 들이 volume 이 필요하지 않을수있기에 Lazy 로 전환
    public float VolumeRadius
    {
        get
        {
            if (_volumeRadius.HasValue)
                return _volumeRadius.Value;

            if (_collider == null)
            {
                _volumeRadius = 1f * transform.lossyScale.x;
                return _volumeRadius.Value;
            }

            switch (_collider)
            {
                case SphereCollider c:
                    _volumeRadius = c.radius * c.transform.lossyScale.x;
                    break;
                case BoxCollider c:
                    float maxHorizontalSize = Mathf.Max(c.size.x, c.size.z);
                    _volumeRadius = maxHorizontalSize * 0.5f * c.transform.lossyScale.x;
                    break;
                default:
                    _volumeRadius = 1f * transform.lossyScale.x;
                    TEMP_Logger.Err($"Model Volume Radius is not measureable");
                    break;

            }

            return _volumeRadius.Value;
        }
    }

    float? _volumeHeight;
    public float VolumeHeight
    {
        get
        {
            if (_volumeHeight.HasValue)
                return _volumeHeight.Value;

            var headSocket = GetSocket(EntityModelSocket.Head);
            if (headSocket)
            {
                _volumeHeight = headSocket.position.y;
                return _volumeHeight.Value;
            }

            if (_collider == null)
            {
                _volumeHeight = 1f * transform.lossyScale.x;
                return _volumeHeight.Value;
            }

            switch (_collider)
            {
                case SphereCollider c:
                    _volumeHeight = c.radius * c.transform.lossyScale.x;
                    break;
                case BoxCollider c:
                    _volumeHeight = c.size.y * c.transform.lossyScale.x;
                    break;
                default:
                    _volumeHeight = 1f * transform.lossyScale.x;
                    TEMP_Logger.Err($"Model Volume Height is not measureable");
                    break;

            }

            return _volumeHeight.Value;
        }
    }

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();

        if (Animator)
        {
            if (Animator.runtimeAnimatorController)
            {
                AnimationTriggerData = EntityManager.Instance.AnimationTriggerData.GetData(Animator.runtimeAnimatorController);
            }
            else
            {
                TEMP_Logger.Err($"AnimatorController is empty | Name : {gameObject.name}");
            }
        }
    }

    private void OnEnable()
    {
        if (Animator)
        {
            // WARNING : Animator 가 부착돼있는 게임오브젝트가 False 가 되는 순간
            // EntityAnimationStateBeh 에 대한 레퍼런스들이 fake null 로
            // 자동으로 처리가 되는 유니티 동작이 있어서 Active 가 켜질때 한번
            // 다시 가져와야함 (풀링 시스템 라이프사이클에 관련하여)
            AnimatorStateBehaviours = Animator.GetBehaviours<EntityAnimationStateBeh>();
        }
    }

    public async UniTaskVoid CreateSkillRange(string indicatorKey, float range)
    {
        if (_skillRangeIndicator == null)
        {
            TEMP_Logger.Err($"Require SpriteRenderer for indicator");
            return;
        }

        _skillRangeIndicator.gameObject.SetActive(false);

        var sprite = await AssetManager.Instance.LoadAsync<Sprite>(indicatorKey);

        _skillRangeIndicator.sprite = sprite;
        ChangeSkillRange(range);
        SetSkillRangeSpriteVisible(true);
    }

    public void OnDied()
    {
        if (_circleShadow)
        {
            _circleShadow.Return();
            _circleShadow = null;
        }

        if (_collider)
            _collider.enabled = false;
    }

    public void SetSkillRangeSpriteVisible(bool show)
    {
        if (_skillRangeIndicator == null)
        {
            return;
        }

        _skillRangeIndicator.gameObject.SetActive(show);
    }

    public void ChangeSkillRange(float range)
    {
        _skillRangeIndicator.GetComponent<ScaleFixer>().SetScale(new Vector3(range, range, range));
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _collider = GetComponentInChildren<Collider>();

        if (_equipments != null)
        {
            foreach (var eq in _equipments)
            {
                eq.currentSocket = eq.defaultSocket;
            }
        }

        //var collider = GetComponent<Collider>();
        //if (collider)
        //{
        //    switch (collider)
        //    {
        //        case SphereCollider c:
        //            VolumeRadius = c.radius * transform.localScale.x;
        //            break;
        //        case BoxCollider c:
        //            VolumeRadius = c.size.x * 0.5f * transform.localScale.x;
        //            break;
        //        default:
        //            TEMP_Logger.Err($"Model Volume Radius is not measureable");
        //            break;

        //    }
        //}
    }

    public override void OnRemoved()
    {
        base.OnRemoved();

        if (_circleShadow)
        {
            PoolManager.Instance.Remove(_circleShadow);
            _circleShadow = null;
        }
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        if (_shadowOption == ShadowOption.CircleShadow_Height ||
            _shadowOption == ShadowOption.CircleShadow_NoHeight)
        {
            CreateShadow();
        }

        if (_collider)
        {
            _collider.enabled = true;
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        if (_circleShadow)
        {
            _circleShadow.Return();
            _circleShadow = null;
        }

        if (_equipments != null)
        {
            foreach (var eq in _equipments)
            {
                if (eq.ts && eq.currentSocket != eq.defaultSocket)
                {
                    eq.ts.SetParent(GetSocket(eq.defaultSocket), false);
                }
            }
        }

        if (_isRendererDirty)
        {
            _isRendererDirty = false;

            foreach (var r in _renderers)
            {
                r.renderer.materials = r.originalMaterials;
            }
        }
    }

    public void SetLayer(int layer)
    {
        gameObject.layer = layer;

        if (_collider)
        {
            _collider.gameObject.layer = layer;
        }
    }

    public void SetPhaseActivationSocket(InGamePhase newPhase)
    {
        var peaceSocket = GetSocket(EntityModelSocket.PeaceModeActivationGroup);
        var battleSocket = GetSocket(EntityModelSocket.BattleModeActivationGroup);

        if (peaceSocket)
            peaceSocket.gameObject.SetActive(newPhase == InGamePhase.Peace);
        if (battleSocket)
            battleSocket.gameObject.SetActive(newPhase == InGamePhase.Battle);
    }

    public Transform GetSocket(EntityModelSocket socket)
    {
        if (_socketDic.TryGetValue(socket, out var target))
        {
            return target.ts;
        }

        if (_sockets == null || _sockets.Length == 0)
            return null;

        for (int i = 0; i < _sockets.Length; i++)
        {
            if (_sockets[i].socket == socket)
            {
                _socketDic.Add(socket, _sockets[i]);
                return _sockets[i].ts;
            }
        }
        return null;
    }

    public Transform GetEquimentTransform(EntityEquipmentType type)
    {
        var eq = GetEquiment(type);
        if (eq == null)
            return transform;
        return eq.ts;
    }

    public void SetEquipmentActive(EntityEquipmentType type, bool active)
    {
        var eq = GetEquiment(type);
        if (eq == null)
            return;
        eq.ts.gameObject.SetActive(active);
    }

    Equipment GetEquiment(EntityEquipmentType type)
    {
        if (_equipmentsDic.TryGetValue(type, out var target))
        {
            return target;
        }

        if (_equipments == null || _equipments.Length == 0)
            return null;

        for (int i = 0; i < _equipments.Length; i++)
        {
            if (_equipments[i].type == type)
            {
                _equipmentsDic.Add(type, _equipments[i]);
                return _equipments[i];
            }
        }
        return null;
    }

    public void SwitchEquipmentSocket(EntityEquipmentType equipmentType, EntityModelSocket socket)
    {
        var eq = GetEquiment(equipmentType);
        var sk = GetSocket(socket);
        if (eq == null || sk == null)
            return;

        if (eq.currentSocket == socket)
            return;

        eq.ts.SetParent(sk, false);
        eq.currentSocket = socket;
    }

    public void OnSkillAnimationEventReceived()
    {
        OnSkillAnimationEventReceivedListener?.Invoke();
    }

    public void RunTweenRunner()
    {
        if (_tweenRunner == null)
        {
            return;
        }

        _tweenRunner.PlaySequenceAsyncCallBack().Forget();
    }

    protected void CreateShadow()
    {
        PoolManager.Instance.RequestSpawnAsyncCallBack<CircleShadow>(ObjectPoolCategory.Fx, "CircleShadow", parent: transform,
            onCompleted: (resShadow, opRes) =>
            {
                if (IsActivated == false)
                {
                    resShadow.Return();
                    return;
                }

                resShadow.UseDynamicHeight = _shadowOption == ShadowOption.CircleShadow_Height;
                resShadow.SetScaleScalar(_shadowScale);
                resShadow.SetFollowTarget(transform);

                _circleShadow = resShadow;
            }).Forget();
    }

    public void ChangeAllMaterials(Material material)
    {
        if (_renderers == null)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            _renderers = new List<RendererCache>(renderers.Length);

            foreach (var r in renderers)
            {
                _renderers.Add(new RendererCache()
                {
                    renderer = r,
                    originalMaterials = r.materials
                });
            }
        }

        if (_renderers.Count == 0)
            return;

        _isRendererDirty = true;

        foreach (var r in _renderers)
        {
            var mats = r.renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = material;
            }
            r.renderer.materials = mats;
        }
    }

    //public void OnAnimationEventReceived_Int(int value)
    //{
    //    OnAnimationEventReceivedIntListener?.Invoke(value);
    //}

    //public void OnAnimationEventReceived_String(string value)
    //{
    //    OnAnimationEventReceivedStringListener?.Invoke(value);
    //}
}
