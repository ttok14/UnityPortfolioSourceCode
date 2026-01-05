//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.AddressableAssets.ResourceLocators;
//using UnityEngine.ResourceManagement.AsyncOperations;

//public class AddressablesPatcher
//{
//    [Flags]
//    public enum TaskFlag
//    {
//        None = 0,

//        Initialize = 0x1,
//        CatalogUpdate = 0x1 << 1,
//        SizeDownload = 0x1 << 2,
//        DownloadDependencies = 0x1 << 3,

//        All = Initialize | CatalogUpdate | SizeDownload | DownloadDependencies
//    }

//    public event Action<AddressablesHandleWrap> AddressableInitializedListener;
//    public event Action<AsyncOperationHandle<IResourceLocator>> AddressableInitializeFailedListener;

//    public event Action<AddressablesHandleWrap<List<IResourceLocator>>> CatalogUpdatedListener;
//    public event Action<AsyncOperationHandle<List<string>>> CheckCatalogUpdateFailedListener;
//    public event Action<AsyncOperationHandle<List<IResourceLocator>>> CatalogUpdateFailedListener;

//    public event Action<AddressablesHandleWrap<long>> SizeDownloadedListener;
//    public event Action<AsyncOperationHandle<long>> SizeDownloadFailedListener;

//    public event Action<DownloadStatus> DownloadProgressedListener;
//    public event Action<DownloadResultReport> DownloadedListener;
//    public event Action<AsyncOperationHandle> DownloadFailedListener;

//    public bool WaitForRetryResponse { get; private set; }
//    public bool ShouldRetry { get; private set; }

//    private TaskFlag _internalSuccessFlags = TaskFlag.None;
//    private bool IsTaskSuccessed(TaskFlag task) => _internalSuccessFlags.HasFlag(task);

//    public readonly TaskFlag RetriableTaskFlags = TaskFlag.All;
//    private bool IsRetriable(TaskFlag flag) => RetriableTaskFlags.HasFlag(flag);

//    public void SetWaitForRetryReponse(bool wait)
//    {
//        WaitForRetryResponse = wait;
//    }

//    public void SetShouldRetry(bool retry)
//    {
//        ShouldRetry = retry;
//    }

//    public IEnumerator InitializeSystem()
//    {
//        AddressablesManager.Instance.Preparation.SystemInitialized += OnAddressablesInitialized;
//        AddressablesManager.Instance.Preparation.SystemInitializeFailed += OnAddressableInitializeFailed;

//        while (AddressablesManager.Instance.Preparation.IsInitialized == false)
//        {
//            yield return AddressablesManager.Instance.Preparation.InitializeAddressablesSystem();

//            if (IsTaskSuccessed(TaskFlag.Initialize) || IsRetriable(TaskFlag.Initialize) == false)
//            {
//                break;
//            }

//            yield return new WaitUntil(() => WaitForRetryResponse == false);

//            if (ShouldRetry == false)
//            {
//                break;
//            }
//        }

//        AddressablesManager.Instance.Preparation.SystemInitialized -= OnAddressablesInitialized;
//        AddressablesManager.Instance.Preparation.SystemInitializeFailed -= OnAddressableInitializeFailed;
//    }

//    public IEnumerator UpdateCatalogs()
//    {
//        AddressablesManager.Instance.Preparation.CatalogUpdated += OnCatalogUpdated;
//        AddressablesManager.Instance.Preparation.CheckCatalogUpdateFailed += OnCheckCatalogUpdateFailed;
//        AddressablesManager.Instance.Preparation.CatalogUpdateFailed += OnCatalogUpdateFailed;

//        while (AddressablesManager.Instance.Preparation.IsCatalogUpdated == false)
//        {
//            yield return AddressablesManager.Instance.Preparation.UpdateCatalogs();

//            if (IsTaskSuccessed(TaskFlag.CatalogUpdate) || IsRetriable(TaskFlag.CatalogUpdate) == false)
//            {
//                break;
//            }

//            yield return new WaitUntil(() => WaitForRetryResponse == false);

//            if (ShouldRetry == false)
//            {
//                break;
//            }
//        }

//        AddressablesManager.Instance.Preparation.CatalogUpdated -= OnCatalogUpdated;
//        AddressablesManager.Instance.Preparation.CheckCatalogUpdateFailed -= OnCheckCatalogUpdateFailed;
//        AddressablesManager.Instance.Preparation.CatalogUpdateFailed -= OnCatalogUpdateFailed;
//    }

//    public IEnumerator DownloadSize(List<string> labels)
//    {
//        // 재시도 허용하기 위해 기존 성공 상태를 release.
//        _internalSuccessFlags &= ~TaskFlag.SizeDownload;

//        AddressablesManager.Instance.Preparation.SizeDownloaded += OnSizeDownloaded;
//        AddressablesManager.Instance.Preparation.SizeDownloadFailed += OnSizeDownloadFailed;

//        while (IsTaskSuccessed(TaskFlag.SizeDownload) == false)
//        {
//            yield return AddressablesManager.Instance.Preparation.GetDownloadSize(labels);

//            if (IsTaskSuccessed(TaskFlag.SizeDownload) || IsRetriable(TaskFlag.SizeDownload) == false)
//            {
//                break;
//            }

//            yield return new WaitUntil(() => WaitForRetryResponse == false);

//            if (ShouldRetry == false)
//            {
//                break;
//            }
//        }

//        AddressablesManager.Instance.Preparation.SizeDownloaded -= OnSizeDownloaded;
//        AddressablesManager.Instance.Preparation.SizeDownloadFailed -= OnSizeDownloadFailed;
//    }

//    public IEnumerator DownloadDependencies(List<string> labels)
//    {
//        _internalSuccessFlags &= ~TaskFlag.DownloadDependencies;

//        AddressablesManager.Instance.Preparation.DownloadProgressed += OnDownloadProgressed;
//        AddressablesManager.Instance.Preparation.DependenciesDownloaded += OnDependenciesDownloaded;
//        AddressablesManager.Instance.Preparation.DownloadFailed += OnDownloadFailed;

//        // while (IsTaskSuccessed(TaskFlag.DownloadDependencies) == false)
//        {
//            yield return AddressablesManager.Instance.Preparation.DownloadDependencies(labels);

//          //  if (IsTaskSuccessed(TaskFlag.DownloadDependencies) || IsRetriable(TaskFlag.DownloadDependencies) == false)
//            {
//            //    break;
//            }

//          //  yield return new WaitUntil(() => WaitForRetryResponse == false);

//          //  if (ShouldRetry == false)
//            {
//          //      break;
//            }

//            TEMP_Logger.Deb($"Retrying DownloadDependencies");
//        }

//        AddressablesManager.Instance.Preparation.DownloadProgressed -= OnDownloadProgressed;
//        AddressablesManager.Instance.Preparation.DownloadFailed -= OnDownloadFailed;
//        AddressablesManager.Instance.Preparation.DependenciesDownloaded -= OnDependenciesDownloaded;
//    }

//    #region ===:: Private ::===

//    private void OnAddressablesInitialized(AddressablesHandleWrap handle)
//    {
//        TEMP_Logger.Deb($"Initialized Addressable System");
//        _internalSuccessFlags |= TaskFlag.Initialize;
//        AddressableInitializedListener?.Invoke(handle);
//    }

//    private void OnAddressableInitializeFailed(AsyncOperationHandle<IResourceLocator> handle)
//    {
//        TEMP_Logger.Err($"Initialized Addressable Failed, Retry . .");

//        AddressableInitializeFailedListener?.Invoke(handle);
//    }

//    private void OnCatalogUpdated(AddressablesHandleWrap<List<IResourceLocator>> handle)
//    {
//        TEMP_Logger.Deb($"Catalog Updated | Count : {handle.Handle.Result.Count}");
//        _internalSuccessFlags |= TaskFlag.CatalogUpdate;
//        CatalogUpdatedListener?.Invoke(handle);
//    }

//    private void OnCheckCatalogUpdateFailed(AsyncOperationHandle<List<string>> handle)
//    {
//        TEMP_Logger.Err($"CheckCatalog Failed , Retry . . . ");
//        CheckCatalogUpdateFailedListener?.Invoke(handle);
//    }

//    private void OnCatalogUpdateFailed(AsyncOperationHandle<List<IResourceLocator>> handle)
//    {
//        TEMP_Logger.Err($"CatalogUpdate Failed , Retry . . . ");
//        CatalogUpdateFailedListener?.Invoke(handle);
//    }

//    private void OnSizeDownloadFailed(AsyncOperationHandle<long> handle)
//    {
//        SizeDownloadFailedListener?.Invoke(handle);
//    }

//    private void OnSizeDownloaded(AddressablesHandleWrap<long> handle)
//    {
//        _internalSuccessFlags |= TaskFlag.SizeDownload;
//        SizeDownloadedListener?.Invoke(handle);
//    }

//    private void OnDownloadProgressed(DownloadStatus handle)
//    {
//        DownloadProgressedListener?.Invoke(handle);
//    }

//    private void OnDependenciesDownloaded(DownloadResultReport handle)
//    {
//        _internalSuccessFlags |= TaskFlag.DownloadDependencies;
//        DownloadedListener?.Invoke(handle);
//    }

//    private void OnDownloadFailed(AsyncOperationHandle handle)
//    {
//        DownloadFailedListener?.Invoke(handle);
//    }

//    #endregion
//}
