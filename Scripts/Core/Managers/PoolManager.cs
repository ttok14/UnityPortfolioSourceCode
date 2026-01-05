using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

public class PoolManager : SingletonBase<PoolManager>
{
    public class PoolableMap
    {
        public Dictionary<ulong, PoolableObjectBase> ActivatedPoolByID = new Dictionary<ulong, PoolableObjectBase>(32);
        public Dictionary<string, Dictionary<ulong, PoolableObjectBase>> ActivatedPoolByKey = new Dictionary<string, Dictionary<ulong, PoolableObjectBase>>(32);
        public Dictionary<string, List<PoolableObjectBase>> InactivePool = new Dictionary<string, List<PoolableObjectBase>>(32);
        public Dictionary<string, int> LimitCountByKey = new Dictionary<string, int>();

        public Transform InactiveHolder;

        public int CurrentTotalCount => ActivatedPoolByID.Count + InactiveCount;
        public int GetTotalCountByKey(string key)
        {
            int activatedCount = 0;
            if (ActivatedPoolByKey.TryGetValue(key, out var activatedPool))
                activatedCount = activatedPool.Count;

            int inactivatedCount = 0;
            if (InactivePool.TryGetValue(key, out var inactivatedPool))
                inactivatedCount = inactivatedPool.Count;

            return activatedCount + inactivatedCount;
        }

        public int InactiveCount;

        public int LimitCount { get; private set; }

        public void SetLimitCount(int cnt) => LimitCount = cnt;
    }

    private Dictionary<ObjectPoolCategory, PoolableMap> _maps = new Dictionary<ObjectPoolCategory, PoolableMap>();

    private Transform _inactiveRoot;

    private ulong _nextID;
    private ulong NextID
    {
        get
        {
            var nextId = _nextID;
            _nextID++;
            return nextId;
        }
    }

    private void Awake()
    {
        _inactiveRoot = new GameObject("InactivePoolRoot").transform;
        _inactiveRoot.SetParent(transform);
        _inactiveRoot.gameObject.SetActive(false);
        _inactiveRoot.transform.position = new Vector3(10000, 0, 0);
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public void CreatePoolMap(ObjectPoolCategory category, int limitCount = 0)
    {
        if (_maps.TryGetValue(category, out var map))
        {
            map.SetLimitCount(limitCount);
            return;
        }

        map = new PoolableMap();
        map.InactiveHolder = new GameObject(category.ToString()).transform;
        map.InactiveHolder.SetParent(_inactiveRoot);
        map.InactiveHolder.gameObject.SetActive(false);
        map.InactiveHolder.localPosition = Vector3.zero;
        map.SetLimitCount(limitCount);

        _maps.Add(category, map);
    }

    public bool SetInstanceLimitCount(ObjectPoolCategory category, string key, int limitCount)
    {
        if (_maps.TryGetValue(category, out var map) == false)
        {
            CreatePoolMap(category, 0);
            map = _maps[category];
        }

        map.LimitCountByKey[key] = limitCount;

        int instanceCount = map.GetTotalCountByKey(key);
        if (instanceCount > limitCount)
        {
            TEMP_Logger.Wrn($"Existing Key Instance Count ({instanceCount}) is greater than new Limit Count ({limitCount})");
        }

        return true;
    }

    // Spawn 은 결국 이미 있으면 바로 Activate, 없으면 prepare->activate 하란건데. 함수명 다시 생각해볼까.
    public async UniTaskVoid RequestSpawnAsyncCallBack<T>(
        ObjectPoolCategory category,
        string key,
        Vector3? worldPos = null,
        Quaternion? rotation = null,
        Transform parent = null,
        Action<T, PoolOpResult> onCompleted = null) where T : PoolableObjectBase
    {
        var result = await RequestSpawnAsync<T>(category, key, worldPos, rotation, parent);
        onCompleted?.Invoke(result.instance, result.opRes);
    }

    public async UniTask<(T instance, PoolOpResult opRes)> RequestSpawnAsync<T>(
        ObjectPoolCategory category,
        string key,
        Vector3? worldPos = null,
        Quaternion? rotation = null,
        Transform parent = null) where T : PoolableObjectBase
    {
        if (HasInactive(category, key) == false)
        {
            var result = await PrepareAsync<T>(category, key, 1);

            if (result != PoolOpResult.Successs)
                return (null, result);

            var reactivated = ActivateInternal(category, key, worldPos, rotation, parent);
            return (reactivated as T, result);
        }

        var activated = ActivateInternal(category, key, worldPos, rotation, parent);
        return (activated as T, PoolOpResult.Successs);
    }

    //public void RequestSpawn<T>(
    //    ObjectPoolCategory category,
    //    string key,
    //    Vector3? worldPos = null,
    //    Transform parent = null,
    //    Action<T, PoolOpResult> onCompleted = null) where T : PoolableObjectBase
    //{
    //    if (HasInactive(category, key) == false)
    //    {
    //        Prepare<T>(category, key, 1, (opRes) =>
    //        {
    //            if (opRes != PoolOpResult.Successs)
    //            {
    //                onCompleted?.Invoke(null, opRes);
    //                return;
    //            }

    //            var activated = ActivateInternal(category, key, worldPos, parent);
    //            onCompleted?.Invoke(activated as T, opRes);
    //        });
    //        return;
    //    }

    //    var activated = ActivateInternal(category, key, worldPos, parent);
    //    onCompleted?.Invoke(activated as T, PoolOpResult.Successs);
    //}

    //public IEnumerator RequestSpawnCo<T>(ObjectPoolCategory category, string key, Vector3? worldPos = null, Transform parent = null, Action<T, PoolOpResult> onCompleted = null) where T : PoolableObjectBase
    //{
    //    yield return RequestSpawnCo(category, key, worldPos, parent, (res, opRes) =>
    //    {
    //        onCompleted?.Invoke(res as T, opRes);
    //    });
    //}

    //// 비동기 작업 대비해서 *코루틴* 으로 설계
    //public async UniTask<(T instance, PoolOpResult opRes)> RequestSpawnCo<T>(ObjectPoolCategory category, string key, Vector3? worldPos = null, Transform parent = null)
    //    where T : PoolableObjectBase
    //{
    //    bool wait = true;
    //    RequestSpawnAsync<PoolableObjectBase>(category, key, worldPos, parent, (res, opRes) =>
    //    {
    //        onCompleted?.Invoke(res, opRes);
    //        wait = false;
    //    });

    //    while (wait)
    //    {
    //        yield return null;
    //    }
    //}

    public async UniTaskVoid PrepareAsyncCallBack(ObjectPoolCategory category, string key, int count, Action<PoolOpResult> onCompleted)
    {
        var opRes = await PrepareAsync<PoolableObjectBase>(category, key, count);
        onCompleted.Invoke(opRes);
    }

    public async UniTask<PoolOpResult> PrepareAsync(ObjectPoolCategory category, string key, int count)
    {
        return await PrepareAsync<PoolableObjectBase>(category, key, count);
    }

    public async UniTask<PoolOpResult> PrepareAsync<T>(ObjectPoolCategory category, string key, int count) where T : PoolableObjectBase
    {
        if (GetInactiveCount(category, key) >= count)
        {
            return PoolOpResult.Fail_NoExistInPool;
        }

        if (_maps.TryGetValue(category, out var map))
        {
            if (map.LimitCount > 0 && map.CurrentTotalCount >= map.LimitCount)
            {
                return PoolOpResult.Fail_ReachedPoolLimit;
            }

            if (map.LimitCountByKey.TryGetValue(key, out var keyLimitCount))
            {
                if (map.GetTotalCountByKey(key) >= keyLimitCount)
                    return PoolOpResult.Fail_ReachedInstanceLimit;
            }
        }

        var result = await SpawnInternal<T>(category, key);
        return result.opRes;
    }

    // 차후에 Addressable 비동기 로드 대비해서 일단은 비동기로 만듬
    // TODO : Addressables 도 로드 가능해야하게 수정 (책임은 조금 더 고려)
    //public async UniTask<PoolOpResult> PrepareAsync<T>(ObjectPoolCategory category, string key, int count, Action<PoolOpResult> onCompleted = null) where T : PoolableObjectBase
    //{
    //    bool wait = true;
    //    Prepare<T>(category, key, count, (opRes) =>
    //    {
    //        onCompleted?.Invoke(opRes);
    //        wait = false;
    //    });

    //    while (wait)
    //    {
    //        yield return null;
    //    }
    //}

    //public IEnumerator PrepareCo(ObjectPoolCategory category, string key, int count, Action<PoolOpResult> onCompleted = null)
    //{
    //    yield return PrepareAsync<PoolableObjectBase>(category, key, count, onCompleted);
    //}

    public bool Return(PoolableObjectBase poolable)
    {
        return Return(poolable.Category, poolable.ID);
    }

    public bool Return(ObjectPoolCategory category, ulong id)
    {
        if (_maps.TryGetValue(category, out var map) == false)
        {
            TEMP_Logger.Err($"0. The Poolable Category does not exist | Category : {category} | ID : {id}");
            return false;
        }

        if (map.ActivatedPoolByID.TryGetValue(id, out var poolByID) == false)
        {
            TEMP_Logger.Err($"1. The Poolable does not exist in the pool | Category : {category} | ID : {id}");
            return false;
        }

        map.ActivatedPoolByID.Remove(id);
        map.ActivatedPoolByKey[poolByID.Key].Remove(id);
        Inactivate(poolByID);

        return true;
    }

    public bool Remove(ObjectPoolCategory category, string key)
    {
        var poolable = GetActivePoolable(category, key);
        if (poolable == null)
            poolable = GetInactivePoolable(category, key);

        if (poolable)
            return Remove(poolable);

        return false;
    }

    public bool Remove(PoolableObjectBase poolable, bool immediately = false)
    {
        if (_maps.TryGetValue(poolable.Category, out var map) == false)
            return false;

        if (poolable.IsActivated)
        {
            map.ActivatedPoolByID.Remove(poolable.ID);
            map.ActivatedPoolByKey[poolable.Key].Remove(poolable.ID);
        }
        else
        {
            map.InactivePool[poolable.Key].Remove(poolable);
            map.InactiveCount--;
        }

        poolable.OnRemoved();

        AssetManager.Instance.ReleaseInstance(poolable.gameObject, immediately);

        return true;
    }

    public int GetTotalPoolableCount(ObjectPoolCategory category)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return 0;
        return map.CurrentTotalCount;
    }

    public int GetActivePoolableCount(ObjectPoolCategory category)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return 0;
        return map.ActivatedPoolByID.Count;
    }

    public int GetInactivePoolableCount(ObjectPoolCategory category)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return 0;
        return map.InactivePool.Count;
    }

    public int GetActivePoolableCount(ObjectPoolCategory category, string key)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return 0;

        if (map.ActivatedPoolByKey.TryGetValue(key, out var dic) == false)
            return 0;

        return dic.Count;
    }

    #region =====:: PRIVATE ::=====

    private int GetInactiveCount(ObjectPoolCategory category, string key)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return 0;

        if (map.InactivePool.TryGetValue(key, out var pool) == false)
            return 0;

        return pool.Count;
    }

    private PoolableObjectBase GetInactivePoolable(ObjectPoolCategory category, string key)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return null;

        if (map.InactivePool.TryGetValue(key, out var poolable) && poolable.Count > 0)
            return poolable[0];

        return null;
    }

    private bool HasInactive(ObjectPoolCategory category, string key)
    {
        return GetInactiveCount(category, key) > 0;
    }

    // Linq 로 가져오니까 잦은 호출 X 
    private PoolableObjectBase GetActivePoolable(ObjectPoolCategory category, string key)
    {
        if (_maps.TryGetValue(category, out var map) == false)
            return null;

        if (map.ActivatedPoolByKey.TryGetValue(key, out var poolable))
        {
            if (poolable.Count > 0)
                return poolable.Values.First();
        }

        return null;
    }

    private async UniTask<(T instance, PoolOpResult opRes)> SpawnInternal<T>(ObjectPoolCategory category, string key) where T : PoolableObjectBase
    {
        T loaded = AssetManager.Instance.GetLoadedAsset<T>(key);

        if (loaded == null)
            loaded = await AssetManager.Instance.LoadAsync<T>(key);

        if (loaded == null)
            return (null, PoolOpResult.Fail_LoadAsset);

        if (_maps.TryGetValue(category, out var map) == false)
        {
            CreatePoolMap(category);
            map = _maps[category];
        }

        if (map.InactivePool.TryGetValue(key, out var inactiveStack) == false)
        {
            inactiveStack = new List<PoolableObjectBase>(32);
            map.InactivePool.Add(key, inactiveStack);
        }

        /// 만약 해당 프리팹이 <see cref="PoolableObjectBase"/> 를 상속받지 않는다면
        /// 런타임 에러가 날 것임.
        var instance = AssetManager.Instance.Instantiate<T>(key, map.InactiveHolder);
        var result = instance;
        inactiveStack.Add(instance);
        instance.OnSpawned(category, key);
        map.InactiveCount++;

        return (result, PoolOpResult.Successs);
    }

    private PoolableObjectBase ActivateInternal(ObjectPoolCategory category, string key, Vector3? worldPos, Quaternion? rotation, Transform parent = null)
    {
        if (_maps.TryGetValue(category, out var map) &&
            map.InactivePool.TryGetValue(key, out var pool) && pool.Count > 0)
        {
            var target = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            ActivateInternal(target, worldPos, rotation, parent);
            return target;
        }

        return null;
    }

    private bool ActivateInternal(PoolableObjectBase poolable, Vector3? worldPos = null, Quaternion? rotation = null, Transform parent = null)
    {
        if (_maps.TryGetValue(poolable.Category, out var map) == false)
            return false;

        ulong newId = NextID;

        map.ActivatedPoolByID.Add(newId, poolable);
        map.InactiveCount--;

        if (map.ActivatedPoolByKey.TryGetValue(poolable.Key, out var pool) == false)
        {
            pool = new Dictionary<ulong, PoolableObjectBase>(64);
            map.ActivatedPoolByKey.Add(poolable.Key, pool);
        }

        // 필요한 설정들을 다 하고
        if (parent != poolable.transform)
            poolable.transform.SetParent(parent, false);
        if (worldPos.HasValue)
            poolable.transform.position = worldPos.Value;
        if (rotation.HasValue)
            poolable.transform.rotation = rotation.Value;

        poolable.OnActivated(newId);
        pool.Add(newId, poolable);

        // Activate 시켜주는게 맞을듯 ? 
        poolable.gameObject.SetActive(true);

        return true;
    }

    private bool Inactivate(PoolableObjectBase poolable)
    {
        if (_maps.TryGetValue(poolable.Category, out var map) == false)
            return false;

        // 일단 빨리 안보이게 처리함
        // poolable.transform.SetParent(map._inactiveHolder, false);
        poolable.gameObject.SetActive(false);
        poolable.OnInactivated();

        map.InactivePool[poolable.Key].Add(poolable);

        map.InactiveCount++;

        return true;
    }

    //private async UniTask<T> Load<T>(string key) where T : PoolableObjectBase
    //{
    //    var prefab = AssetManager.Instance.GetLoadedAsset<T>(key);

    //    if (prefab)
    //        return prefab;

    //    return await AssetManager.Instance.LoadAsync<T>(key);
    //}

    #endregion
}
