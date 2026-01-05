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
using static UnityEngine.AddressableAssets.Addressables;

public class AddressablesPreparation
{
    private AddressablesHandleWrap _initHandle;

    public bool IsInitialized { get; private set; }
    public bool IsCatalogUpdated { get; private set; }

    public bool IsCatalogUpdating { get; private set; }
    public bool IsInitializing { get; private set; }
    public bool IsDownloadingSize { get; private set; }
    public bool IsDownloadingDependencies { get; private set; }

    public event Action<AddressablesHandleWrap> SystemInitialized;
    public event Action<AsyncOperationHandle<IResourceLocator>> SystemInitializeFailed;

    public event Action<AddressablesHandleWrap<List<IResourceLocator>>> CatalogUpdated;
    public event Action<AsyncOperationHandle<List<string>>> CheckCatalogUpdateFailed;
    public event Action<AsyncOperationHandle<List<IResourceLocator>>> CatalogUpdateFailed;

    public event Action<AddressablesHandleWrap<long>> SizeDownloaded;
    public event Action<AsyncOperationHandle<long>> SizeDownloadFailed;

    public event Action<DownloadStatus> DownloadProgressed;
    public event Action<DownloadResultReport> DependenciesDownloaded;
    public event Action<AsyncOperationHandle> DownloadFailed;

    public IEnumerator InitializeAddressablesSystem()
    {
        // Initialize 는 한번만 한다
        if (IsInitialized)
        {
            SystemInitialized?.Invoke(_initHandle);
            yield break;
        }

        if (IsInitializing)
        {
            TEMP_Logger.Wrn($"Addressables System is already in a process of initializing, Waiting ..");
            yield return new WaitUntil(() => IsInitializing == false);
            TEMP_Logger.Wrn($"Addressables System Initializing Waiting done .");
        }

        TEMP_Logger.Deb($"Begin Initializing Addressables System . .");

        IsInitializing = true;

        var handle = Addressables.InitializeAsync(autoReleaseHandle: false);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            IsInitialized = true;
            _initHandle = new AddressablesHandleWrap(handle);
            SystemInitialized?.Invoke(_initHandle);
        }
        else
        {
            TEMP_Logger.Err($"{nameof(Addressables.InitializeAsync)} Failed | OperationException : {handle.OperationException}");
            _initHandle = null;
            SystemInitializeFailed?.Invoke(handle);
        }

        IsInitializing = false;
    }

    public IEnumerator UpdateCatalogs()
    {
        if (IsInitialized == false)
        {
            TEMP_Logger.Err($"System is not initialized yet | Call {nameof(InitializeAddressablesSystem)}() First");
            yield break;
        }

        if (IsCatalogUpdating)
        {
            yield return new WaitUntil(() => IsCatalogUpdating == false);
        }

        IsCatalogUpdated = false;
        IsCatalogUpdating = true;

        var checkHandle = Addressables.CheckForCatalogUpdates(autoReleaseHandle: false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            TEMP_Logger.Deb($"{nameof(Addressables.CheckForCatalogUpdates)} Success | Catalog To Update Count : {checkHandle.Result.Count}");
        }
        else
        {
            TEMP_Logger.Err($"{nameof(Addressables.CheckForCatalogUpdates)} Failed | OperationException : {checkHandle.OperationException}");
            CheckCatalogUpdateFailed?.Invoke(checkHandle);
            IsCatalogUpdating = false;
            checkHandle.Release();
            yield break;
        }

        if (checkHandle.Result.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, autoReleaseHandle: false);
            yield return updateHandle;

            if (updateHandle.Status == AsyncOperationStatus.Succeeded)
            {
                IsCatalogUpdated = true;
                TEMP_Logger.Deb($"{nameof(Addressables.UpdateCatalogs)} Success");
                CatalogUpdated?.Invoke(new AddressablesHandleWrap<List<IResourceLocator>>(updateHandle));
            }
            else
            {
                IsCatalogUpdated = false;
                TEMP_Logger.Err($"{nameof(Addressables.UpdateCatalogs)} Failed | OperationException : {updateHandle.OperationException}");
                CatalogUpdateFailed?.Invoke(updateHandle);
            }

            updateHandle.Release();
        }
        else
        {
            IsCatalogUpdated = true;
        }

        checkHandle.Release();

        IsCatalogUpdating = false;
    }

    public IEnumerator GetDownloadSize(List<string> labels)
    {
        if (IsCatalogUpdated == false)
        {
            TEMP_Logger.Err($"Catalog Not Updated yet | call : {nameof(UpdateCatalogs)}() first");
            yield break;
        }

        if (IsDownloadingSize)
        {
            yield return new WaitUntil(() => IsDownloadingSize == false);
        }

        IsDownloadingSize = true;

        var handle = Addressables.GetDownloadSizeAsync(labels);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            TEMP_Logger.Deb($"DownloadSize Success | Size : {handle.Result}");
            SizeDownloaded?.Invoke(new AddressablesHandleWrap<long>(handle));
        }
        else
        {
            TEMP_Logger.Deb($"DownloadSize Failed | OperationException : {handle.OperationException}");
            SizeDownloadFailed?.Invoke(handle);
        }

        handle.Release();

        IsDownloadingSize = false;
    }

    public IEnumerator DownloadDependencies(IEnumerable<string> labels)
    {
        if (IsCatalogUpdated == false)
        {
            TEMP_Logger.Err($"Catalog Not Updated yet | call : {nameof(UpdateCatalogs)}() first");
            yield break;
        }

        if (IsDownloadingDependencies)
        {
            yield return new WaitUntil(() => IsDownloadingDependencies == false);
        }

        foreach (var label in labels)
        {
            TEMP_Logger.Deb($"Download Label : {label}");
        }

        IsDownloadingDependencies = true;

        float lastPercent = 0f;
        // 1% 의 변화를 Progress 이벤트 전송 기준 설정
        float notifyPercentThreshold = 0.01f;
        float startedAt = Time.time;

        var handle = Addressables.DownloadDependenciesAsync(labels, MergeMode.Union, autoReleaseHandle: false);
        while (handle.IsDone == false)
        {
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                break;
            }

            var status = handle.GetDownloadStatus();
            float percent = status.Percent;

            bool notifyEvent = percent - lastPercent > notifyPercentThreshold;
            if (notifyEvent)
            {
                lastPercent = status.Percent;
                DownloadProgressed?.Invoke(status);
            }

            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            TEMP_Logger.Deb($"DownloadDependenciesAsync Failed | OperationException : {handle.OperationException}");
            DownloadFailed?.Invoke(handle);
            handle.Release();
            IsDownloadingDependencies = false;
            yield break;
        }

        DownloadProgressed?.Invoke(handle.GetDownloadStatus());

        TEMP_Logger.Deb($"Download Addressables Finished | Total Bytes : {handle.GetDownloadStatus().TotalBytes} , Downloaded : {handle.GetDownloadStatus().DownloadedBytes}");

        float secondsTaken = Time.time - startedAt;
        DependenciesDownloaded?.Invoke(new DownloadResultReport(handle.GetDownloadStatus().DownloadedBytes, secondsTaken));
        IsDownloadingDependencies = false;

        handle.Release();
    }
}
