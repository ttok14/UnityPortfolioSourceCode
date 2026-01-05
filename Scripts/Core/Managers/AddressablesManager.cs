using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Linq;

public class AddressablesManager : SingletonBase<AddressablesManager>
{
    public AddressablesPreparation Preparation { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        Preparation = new AddressablesPreparation();
    }

    public AsyncOperationHandle<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        return Addressables.LoadAssetAsync<T>(key);
    }

    public IEnumerator LoadCo<T>(string key, Action<AsyncOperationHandle<T>> onCompleted) where T : UnityEngine.Object
    {
        var handle = Addressables.LoadAssetAsync<T>(key);
        yield return handle;
        onCompleted?.Invoke(handle);
    }

    public AsyncOperationHandle<GameObject> InstantiateAsync(string key)
    {
        return Addressables.InstantiateAsync(key);
    }

    public void ReleaseAsset(AsyncOperationHandle handle)
    {
        Addressables.Release(handle);
    }

    public void ReleaseInstance(AsyncOperationHandle handle)
    {
        Addressables.ReleaseInstance(handle);
    }

    public void ReleaseInstance(GameObject instance)
    {
        Addressables.ReleaseInstance(instance);
    }
}
