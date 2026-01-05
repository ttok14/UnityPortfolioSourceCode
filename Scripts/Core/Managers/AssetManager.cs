using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class AssetManager : SingletonBase<AssetManager>
{
    class AssetLoadReference
    {
        public string key;
        public E_AssetType assetType;
        public E_LoaderType loadType;
        public UnityEngine.Object asset;
        public Component componentForInstantiate;
        public AsyncOperationHandle handle;

        public AssetLoadReference(string key, E_AssetType assetType, E_LoaderType loadType, UnityEngine.Object asset, Component componentForInstantiate, AsyncOperationHandle handle)
        {
            this.key = key;
            this.assetType = assetType;
            this.loadType = loadType;
            this.asset = asset;
            this.componentForInstantiate = componentForInstantiate;
            this.handle = handle;
        }

        public T To<T>() where T : UnityEngine.Object
        {
            switch (assetType)
            {
                case E_AssetType.Prefab:
                    return componentForInstantiate as T;
                case E_AssetType.Other:
                    return asset as T;
            }

            return null;
        }
    }

    class InstanceReference
    {
        public GameObject instance;
        public AssetLoadReference src;

        public InstanceReference(GameObject instance, AssetLoadReference src)
        {
            this.instance = instance;
            this.src = src;
        }
    }

    public interface IWaiter
    {
        public Action<T> Get<T>() where T : UnityEngine.Object;
    }

    struct WaitReceiver<T> : IWaiter
        where T : UnityEngine.Object
    {
        public Action<T> receiver;

        public WaitReceiver(Action<T> receiver)
        {
            this.receiver = receiver;
        }

        public Action<T1> Get<T1>() where T1 : UnityEngine.Object
        {
            return receiver as Action<T1>;
        }
    }


    // private IAssetLoader _loader = new AssetLoader();
    // private Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>();

    // Load 된 에셋들
    private Dictionary<string, AssetLoadReference> _loadedRef = new Dictionary<string, AssetLoadReference>();
    private Dictionary<UnityEngine.Object, AssetLoadReference> _loadedRefLookup = new Dictionary<UnityEngine.Object, AssetLoadReference>();

    // 비동기 로드시, 로딩중인 에셋을 여러군데서 요청할 수 있기때문에 하나의 에셋에 대해 로딩 로직을
    // 한번만 돌리면서 그 뒤에 오는 요청들은 Queue 로 로딩 완료시 전달하기 위한 딕셔너리 
    private Dictionary<string, Queue<IWaiter>> _assetsOnLoading = new Dictionary<string, Queue<IWaiter>>();

    private Dictionary<GameObject, InstanceReference> _instances = new Dictionary<GameObject, InstanceReference>();
    private Dictionary<string, uint> _instanceCount = new Dictionary<string, uint>();

    //public T Load<T>(string key) where T : UnityEngine.Object
    //{
    //    if (_assets.TryGetValue(key, out var result))
    //    {
    //        return result as T;
    //    }

    //    var loaded = _loader.Load<T>(key);

    //    if (loaded)
    //    {
    //        _assets.Add(key, loaded);
    //    }

    //    return loaded;
    //}

    //public T 이거 콜백형 !Load<T>(string key, Action<T> onCompleted) where T : UnityEngine.Object
    //{
    //    if (_assets.TryGetValue(key, out var result))
    //    {
    //        return result as T;
    //    }

    //    var loaded = _loader.Load<T>(key);

    //    if (loaded)
    //    {
    //        _assets.Add(key, loaded);
    //    }

    //    return loaded;
    //}

    //public UnityEngine.Object Load(string key, Type type)
    //{
    //    if (_assets.TryGetValue(key, out var result))
    //    {
    //        return result;
    //    }

    //    var loaded = _loader.Load(key, type);

    //    if (loaded)
    //    {
    //        _assets.Add(key, loaded);
    //    }

    //    return loaded;
    //}

    public string GetKeyByObject(UnityEngine.Object loadedAsset)
    {
        if (_loadedRefLookup.TryGetValue(loadedAsset, out var @ref))
        {
            return @ref.key;
        }
        return null;
    }

    public bool IsLoaded(string key)
    {
        return _loadedRef.ContainsKey(key);
    }

    //public async UniTask<T> Load<T>(string key, Action<T> onCompleted = null) where T : UnityEngine.Object
    //{
    //    if (_loadedRef.TryGetValue(key, out var res))
    //    {
    //        if (res.assetType == E_AssetType.Prefab)
    //            onCompleted?.Invoke(res.componentForInstantiate as T);
    //        else if (res.assetType == E_AssetType.Other)
    //            onCompleted?.Invoke(res.asset as T);

    //        return;
    //    }

    //    var meta = DBAssetMeta.Get(key);
    //    E_LoaderType loaderType = E_LoaderType.None;

    //    if (meta == null)
    //    {
    //        TEMP_Logger.Err($"Could not find AssetMetadata({key}) | Please add the key to {nameof(AssetMetaTable)} | Default: Addressables");
    //        onCompleted?.Invoke(null);
    //        return;
    //    }
    //    else
    //    {
    //        loaderType = meta.LoaderType;
    //    }

    //    if (loaderType == E_LoaderType.Resources)
    //    {
    //        var loaded = LoadFromResources<T>(key, meta.AssetType);
    //        onCompleted?.Invoke(loaded);
    //    }
    //    else if (loaderType == E_LoaderType.Addressables)
    //    {
    //        if (meta.AssetType == E_AssetType.Prefab)
    //        {
    //            // 프리팹이라면 GameObject 로 로드해야 만 한다.. 어드레서블에서는
    //            // 부착된 컴포넌트 형태로 로드되지 않음 
    //            LoadFromAddressables_Prefab<T>(key, onCompleted);
    //        }
    //        else
    //        {
    //            LoadFromAddressables<T>(key, onCompleted);
    //        }
    //    }
    //    else
    //    {
    //        TEMP_Logger.Err($"Not supported Asset Load Type : {loaderType}");
    //    }
    //}

    //public void Load<T>(AssetReferenceT<T> assetReference, Action<T> onCompleted = null) where T : UnityEngine.Object
    //{
    //    if (assetReference == null)
    //    {
    //        onCompleted?.Invoke(null);
    //        return;
    //    }

    //    Load<T>(assetReference.RuntimeKey.ToString(), onCompleted);
    //}

    Dictionary<string, UniTaskCompletionSource<AssetLoadReference>> _completionSources = new Dictionary<string, UniTaskCompletionSource<AssetLoadReference>>();

    public async UniTaskVoid LoadAsyncCallBack<T>(string key, Action<T> onCompleted) where T : UnityEngine.Object
    {
        var result = await LoadAsync<T>(key);
        onCompleted.Invoke(result);
    }

    public async UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        if (_loadedRef.TryGetValue(key, out var res))
            return res.To<T>();

        if (key == "EnvironmentEntity")
        {
            int n = 0;
        }

        var meta = DBAssetMeta.Get(key);
        E_LoaderType loaderType = E_LoaderType.None;

        if (meta == null)
        {
            TEMP_Logger.Err($"Could not find AssetMetadata({key}) | Please add the key to {nameof(AssetMetaTable)} | Default: Addressables");
            return null;
        }
        else
        {
            loaderType = meta.LoaderType;
        }

        switch (loaderType)
        {
            case E_LoaderType.Resources:
                return LoadFromResources<T>(key, meta.AssetType);
            case E_LoaderType.Addressables:
                if (_completionSources.TryGetValue(key, out var src) == false)
                {
                    src = new UniTaskCompletionSource<AssetLoadReference>();
                    _completionSources.Add(key, src);

                    switch (meta.AssetType)
                    {
                        case E_AssetType.Prefab:
                            // 프리팹이라면 GameObject 로 로드해야 만 한다.. 어드레서블에서는
                            // 부착된 컴포넌트 형태로 로드되지 않음 
                            LoadFromAddressables_Prefab<T>(key, src);
                            break;
                        case E_AssetType.Other:
                            LoadFromAddressables<T>(key, src);
                            break;
                    }
                }

                var result = await src.Task;

                if (_completionSources.ContainsKey(key))
                    _completionSources.Remove(key);

                return result.To<T>();
        }

        TEMP_Logger.Err($"Failed to load asset | Key : {key} | AssetType : {meta.AssetType} | LoadType : {meta.LoaderType}");
        return null;

        //while (wait)
        //    yield return null;
    }

    //public IEnumerator LoadCo<T>(string key, Action<T> onCompleted = null) where T : UnityEngine.Object
    //{
    //    bool wait = true;

    //    Load<T>(key, (res) =>
    //    {
    //        onCompleted?.Invoke(res);
    //        wait = false;
    //    });

    //    while (wait)
    //        yield return null;
    //}

    void CallLoadWaiter<T>(string key, T res) where T : UnityEngine.Object
    {
        var waiterQueue = _assetsOnLoading[key];
        while (waiterQueue.Count > 0)
        {
            var receiver = waiterQueue.Dequeue();

            receiver.Get<T>().Invoke(res);
        }

        _assetsOnLoading.Remove(key);
    }

    //public IEnumerator LoadCo<T>(string key, Action<T> onCompleted) where T : UnityEngine.Object
    //{
    //    bool wait = true;
    //    Load<T>(key, (res) =>
    //    {
    //        onCompleted.Invoke(res);
    //        wait = false;
    //    });

    //    while (wait)
    //    {
    //        yield return null;
    //    }
    //}

    public T Instantiate<T>(string key, Transform parent) where T : Component
    {
        if (_loadedRef.TryGetValue(key, out var asset) == false)
        {
            TEMP_Logger.Err($"Please Load Asset First | Key : {key}");
            return null;
        }

        var src = asset.asset as GameObject;
        if (src == null)
        {
            TEMP_Logger.Err($"Source Object Is not Prefab, so can not instantiate | Key : {key}");
            return null;
        }

        var created = GameObject.Instantiate<T>(asset.componentForInstantiate as T /*asset.componentForInstantiate as T*/ /*src.GetComponent<T>()*/, parent);
        if (created == null)
        {
            TEMP_Logger.Err($"Instantiated prefab({key}) but has no component of type : {typeof(T).Name}");
            return null;
        }

        _instances.Add(created.gameObject, new InstanceReference(created.gameObject, asset));

        if (_instanceCount.ContainsKey(key))
            _instanceCount[key]++;
        else
            _instanceCount.Add(key, 1);

        return created;
    }

    public bool ReleaseInstance(GameObject instance, bool immediately = false)
    {
        if (_instances.TryGetValue(instance, out var inst) == false)
        {
            TEMP_Logger.Err($"Instance({instance}({instance.name})) does not exist");
            return false;
        }

        if (instance)
        {
            if (immediately)
                GameObject.DestroyImmediate(instance);
            else
                GameObject.Destroy(instance);
        }
        else
        {
            TEMP_Logger.Err($"Instance to destroy is not valid");
        }

        var key = inst.src.key;
        _instanceCount[key]--;
        if (_instanceCount[key] == 0)
        {
            _instanceCount.Remove(key);
        }

        return _instances.Remove(instance);
    }

    public bool ReleaseAsset(UnityEngine.Object loadedAsset)
    {
        if (_loadedRefLookup.TryGetValue(loadedAsset, out var @ref) == false)
        {
            // 현 매니저를 통해서 로드하지 않은 에셋을 뜬금없이 여기에 Release 해주세요라고 하면 이 에러가 날것.
            TEMP_Logger.Err($"target({loadedAsset}) does not exist, duplicate release or never loaded . Must check.");
            return false;
        }

        if (_instanceCount[@ref.key] > 0)
        {
            TEMP_Logger.Err($"Please Release All the left instances before release asset ! | Asset : {loadedAsset} , Key : {@ref.key}");
            return false;
        }

        if (@ref.loadType == E_LoaderType.Resources)
        {
            Resources.UnloadAsset(loadedAsset);
        }
        else if (@ref.loadType == E_LoaderType.Addressables)
        {
            if (@ref.handle.IsValid())
            {
                AddressablesManager.Instance.ReleaseAsset(@ref.handle);
            }
            else
            {
                // 이미 삭제됐거나 하면 여기에 들어올 수 있는데, 현재 handle 을 의도적으로 반환하지 않는
                // 상황에 외부에서 실수할 수는 없을거고 AssetManager 자체의 오류일 확률이 클 듯.
                TEMP_Logger.Err($"Handle is not valid | Asset : {loadedAsset} | Check if already released or..");
            }
        }

        _loadedRef.Remove(@ref.key);
        _loadedRefLookup.Remove(loadedAsset);

        return true;
    }

    //---------------------------------------//

    private T LoadFromResources<T>(string key, E_AssetType assetType) where T : UnityEngine.Object
    {
        if (_loadedRef.TryGetValue(key, out var target))
        {
            return target.asset as T;
        }

        var result = Resources.Load<T>(key);
        if (result)
        {
            var @ref = new AssetLoadReference(key, assetType, E_LoaderType.Resources, result, null, default);
            _loadedRef.Add(key, @ref);
            _loadedRefLookup.Add(result, @ref);
        }

        return result;
    }

    private void LoadFromAddressables<T>(string key, UniTaskCompletionSource<AssetLoadReference> completionSrc) where T : UnityEngine.Object
    {
        var handle = AddressablesManager.Instance.LoadAsync<T>(key);
        handle.Completed += (res) =>
        {
            if (res.Status == AsyncOperationStatus.Succeeded)
            {
                var @ref = new AssetLoadReference(key, E_AssetType.Other, E_LoaderType.Addressables, res.Result, null, handle);
                _loadedRef.Add(key, @ref);
                _loadedRefLookup.Add(res.Result, @ref);

                completionSrc.TrySetResult(@ref);
                // CallLoadWaiter(key, handle.Result);
            }
            else
            {
                completionSrc.TrySetException(null);
                // CallLoadWaiter<T>(key, default);
            }

        };
        return;
    }


    private void LoadFromAddressables_Prefab<T>(string key, UniTaskCompletionSource<AssetLoadReference> completionSrc) where T : UnityEngine.Object
    {
        var handle = AddressablesManager.Instance.LoadAsync<GameObject>(key);
        handle.Completed += (res) =>
        {
            if (res.Status == AsyncOperationStatus.Succeeded)
            {
                T componentForInstantiate = res.Result.GetComponent<T>();
                if (componentForInstantiate == null)
                {
                    TEMP_Logger.Err($"Failed to get component : {typeof(T)} | Key : {key}");
                    completionSrc.TrySetException(new Exception("No Component For Instantiate"));
                }
                else
                {
                    var @ref = new AssetLoadReference(
                        key,
                        E_AssetType.Prefab,
                        E_LoaderType.Addressables,
                        res.Result,
                        componentForInstantiate as Component,
                        handle);

                    _loadedRef.Add(key, @ref);
                    _loadedRefLookup.Add(res.Result, @ref);

                    completionSrc.TrySetResult(@ref);
                    // CallLoadWaiter(key, componentForInstantiate);
                }
            }
            else
            {
                completionSrc.TrySetException(null);
                // CallLoadWaiter<T>(key, default);
            }
        };
    }

    bool EnqueueWaiter<T>(string key, Action<T> onCompleted) where T : UnityEngine.Object
    {
        bool alreadyLoading = false;
        if (_assetsOnLoading.ContainsKey(key))
        {
            alreadyLoading = true;
        }
        else
        {
            _assetsOnLoading.Add(key, new Queue<IWaiter>());
        }

        _assetsOnLoading[key].Enqueue(new WaitReceiver<T>(onCompleted));

        return alreadyLoading;
    }

    public T GetLoadedAsset<T>(string key) where T : UnityEngine.Object
    {
        if (_loadedRef.TryGetValue(key, out var asset))
        {
            return asset.asset as T;
        }
        return null;
    }
}
