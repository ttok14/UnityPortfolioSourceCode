using System;
using System.Collections.Generic;
using UnityEngine;

public class InGameCacheContainer
{
    #region ====:: Entity ::====
    // TODO : 이거 별도 시스템으로 좀 체계적으로 관리하자. 일단 이런거 필요는 함 
    private Dictionary<Collider, EntityBase> _colliderToEntityDic;

    private Collider[] _colliderCacheArray_single = new Collider[1];
    private Collider[] _colliderCacheArray_small = new Collider[10];
    private Collider[] _colliderCacheArray_medium = new Collider[20];
    private Collider[] _colliderCacheArray_big = new Collider[30];
    private Collider[] _colliderCacheArray_massive = new Collider[100];

    public Collider[] ColliderCacheArray_Single => _colliderCacheArray_single;
    public Collider[] ColliderCacheArray_Small => _colliderCacheArray_small;
    public Collider[] ColliderCacheArray_Medium => _colliderCacheArray_medium;
    public Collider[] ColliderCacheArray_Big => _colliderCacheArray_big;
    public Collider[] ColliderCacheArray_Massive => _colliderCacheArray_massive;
    Dictionary<int, Collider[]> _collidersCacheByCount;
    public Collider[] GetColliderCacheByCount(int count)
    {
        if (_collidersCacheByCount.TryGetValue(count, out var colArr))
        {
            return colArr;
        }

        colArr = new Collider[count];
        _collidersCacheByCount.Add(count, colArr);
        return colArr;
    }

    public EntityPartsInstancePool EntityPartsPool { get; private set; } = new EntityPartsInstancePool();
    public EntityDataInstancePool EntityDataPool { get; private set; } = new EntityDataInstancePool();

    public EntityAIInstancePool EntityAIPool { get; private set; } = new EntityAIInstancePool();
    public EntitySkillSubPool EntitySkillSubPool { get; private set; } = new EntitySkillSubPool();

    public PartType GetPartOrCreateParts<PartType>(IInstancePoolInitData initData) where PartType : EntityPartBase
    {
        return EntityPartsPool.GetOrCreate<PartType>(initData);
    }

    #endregion

    #region ====:: Movement Strategy ::====
    public MovementInstancePool MovementStrategyInstancePool { get; private set; } = new MovementInstancePool();
    #endregion

    #region ====:: Animation ::====
    private Dictionary<int, Dictionary<int, AnimatorControllerParameter>> _animatorParameters;
    #endregion

    #region ====:: ETC ::====
    public PathBuffer PathBufferCache { get; private set; }
    #endregion

    public void Initialize()
    {
        _colliderToEntityDic = new Dictionary<Collider, EntityBase>(64);
        _animatorParameters = new Dictionary<int, Dictionary<int, AnimatorControllerParameter>>(8);
        PathBufferCache = new PathBuffer();
        PathBufferCache.Initialize();

        _collidersCacheByCount = new Dictionary<int, Collider[]>()
        {
            // 0 은 그냥 제한없음 취급
            [0] = ColliderCacheArray_Big,
            [ColliderCacheArray_Single.Length] = ColliderCacheArray_Single,
            [ColliderCacheArray_Small.Length] = ColliderCacheArray_Small,
            [ColliderCacheArray_Medium.Length] = ColliderCacheArray_Medium,
            [ColliderCacheArray_Big.Length] = ColliderCacheArray_Big,
            [ColliderCacheArray_Massive.Length] = ColliderCacheArray_Massive
        };
    }

    public void Release()
    {
        if (_colliderToEntityDic != null)
        {
            _colliderToEntityDic.Clear();
            _colliderToEntityDic = null;
        }

        if (_collidersCacheByCount != null)
        {
            _collidersCacheByCount.Clear();
            _collidersCacheByCount = null;
        }

        if (_animatorParameters != null)
        {
            _animatorParameters.Clear();
            _animatorParameters = null;
        }

        if (PathBufferCache != null)
        {
            PathBufferCache.Release();
            PathBufferCache = null;
        }
    }

    //====================//

    public EntityBase GetEntityFromCollider(Collider collider)
    {
        var colParent = collider.transform.parent;

        // 1차적으로 일단 EntityManager 에서 이미 존재한다면 바로 돌려줌
        // 대부분은 캐릭터를 위주로 쓰기에 여기서 거의 걸러짐 
        EntityBase entity = EntityManager.Instance.GetCharacterByTransform(colParent);
        if (entity)
            return entity;

        // 여기부터는 내부 캐시로 관리 
        bool refresh = _colliderToEntityDic.TryGetValue(collider, out var cache) == false || colParent != cache.transform;

        if (refresh)
        {
            entity = colParent.GetComponent<EntityBase>();

            if (!entity)
            {
                TEMP_Logger.Err($"Failed to get EntityBase from given collider. Must Check! | ColliderName : {collider.name} | RootName : {collider.transform.root.name}");
                return null;
            }

            _colliderToEntityDic[collider] = entity;

            cache = entity;
        }

        return cache;
    }

    public IReadOnlyDictionary<int, AnimatorControllerParameter> GetAnimatorParameterDic(Animator animator)
    {
        if (animator == null)
        {
            TEMP_Logger.Err($"Given animator is null");
            return null;
        }

        var controller = animator.runtimeAnimatorController;

        if (controller == null)
        {
            TEMP_Logger.Err($"Given animator runtimeANimatorController is null ({animator.gameObject.name})");
            return null;
        }

        if (_animatorParameters.TryGetValue(controller.GetInstanceID(), out var parameters))
            return parameters;

        parameters = new Dictionary<int, AnimatorControllerParameter>(animator.parameterCount);
        foreach (var param in animator.parameters)
            parameters.Add(param.nameHash, param);

        _animatorParameters.Add(controller.GetInstanceID(), parameters);

        return parameters;
    }
}
