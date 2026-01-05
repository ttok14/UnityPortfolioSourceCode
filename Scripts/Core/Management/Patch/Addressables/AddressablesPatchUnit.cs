using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using static PatchEvents;

public class AddressablesPatchUnit : PatchUnitBase
{
    bool _waitForRetryAddressables;

    public override PatchUnitType Type => PatchUnitType.Addressables;

    public override IEnumerator Prepare(OnPreparationCompleted onCompleted, OnFailed onFailed)
    {
        if (AddressablesManager.Instance.Preparation.IsInitialized &&
            AddressablesManager.Instance.Preparation.IsCatalogUpdated)
        {
            onCompleted?.Invoke(PatchUnitType.Addressables);
            yield break;
        }

        yield return base.Prepare(onCompleted, onFailed);

        TEMP_Logger.Deb($"[PatchManager] Addressables Init,UpdateCatalog Begin!");

        _waitForRetryAddressables = false;

        AddressablesManager.Instance.Preparation.SystemInitializeFailed += OnAddressableInitializeFailed;
        while (AddressablesManager.Instance.Preparation.IsInitialized == false)
        {
            if (_waitForRetryAddressables)
                yield return null;
            else
                yield return AddressablesManager.Instance.Preparation.InitializeAddressablesSystem();
        }
        AddressablesManager.Instance.Preparation.SystemInitializeFailed -= OnAddressableInitializeFailed;

        TEMP_Logger.Deb($"[PatchManager] Addressables Init Completed!");

        //-------------------------------------------------

        _waitForRetryAddressables = false;

        AddressablesManager.Instance.Preparation.CheckCatalogUpdateFailed += OnCheckCatalogUpdateFailed;
        AddressablesManager.Instance.Preparation.CatalogUpdateFailed += OnCatalogUpdateFailed;
        while (AddressablesManager.Instance.Preparation.IsCatalogUpdated == false)
        {
            if (_waitForRetryAddressables)
                yield return null;
            else
                yield return AddressablesManager.Instance.Preparation.UpdateCatalogs();
        }
        AddressablesManager.Instance.Preparation.CheckCatalogUpdateFailed -= OnCheckCatalogUpdateFailed;
        AddressablesManager.Instance.Preparation.CatalogUpdateFailed -= OnCatalogUpdateFailed;

        TEMP_Logger.Deb($"[PatchManager] Addressables UpdateCatalog Completed!");
    }

    public override IEnumerator FetchMetadata(OnMetadataFetchProgressed onProgressed, OnMetadataFetchCompleted onCompleted, OnFailed onFailed)
    {
        var labels = GameManager.Instance.MetaData.AddressablesLabelsToDownload;

        if (labels.Count == 0)
        {
            TEMP_Logger.Deb($"[UIAuthPanel] No Addressable Labels to download");
            yield break;
        }

        yield return base.FetchMetadata(onProgressed, onCompleted, onFailed);

        PatchStatus.DownloadContentsList = labels.ToList();

        TEMP_Logger.Deb($"[UIAuthPanel] Detected Addressable Labels to download Count : {labels.Count}");

        AddressablesManager.Instance.Preparation.SizeDownloaded += OnAddressablesSizeDownloaded;
        AddressablesManager.Instance.Preparation.SizeDownloadFailed += OnAddressablesSizeDownloadFailed;

        yield return AddressablesManager.Instance.Preparation.GetDownloadSize(labels.ToList());

        AddressablesManager.Instance.Preparation.SizeDownloaded -= OnAddressablesSizeDownloaded;
        AddressablesManager.Instance.Preparation.SizeDownloadFailed -= OnAddressablesSizeDownloadFailed;
    }

    public override IEnumerator DownloadContents(OnContentsDownloadProgressed onProgress, OnContentsDownloadCompleted onCompleted, OnFailed onFailed)
    {
        if (PatchStatus.DownloadContentsList == null ||
            PatchStatus.DownloadContentsList.Count == 0 ||
            PatchStatus.TotalSize == 0)
        {
            TEMP_Logger.Deb($"Nothing to download");
            yield break;
        }

        yield return base.DownloadContents(onProgress, onCompleted, onFailed);

        AddressablesManager.Instance.Preparation.DownloadProgressed += OnDownloadProgressed;
        AddressablesManager.Instance.Preparation.DependenciesDownloaded += OnAddressablesDownloaded;
        AddressablesManager.Instance.Preparation.DownloadFailed += OnDownloadFailed;

        yield return AddressablesManager.Instance.Preparation.DownloadDependencies(PatchStatus.DownloadContentsList);

        AddressablesManager.Instance.Preparation.DownloadProgressed -= OnDownloadProgressed;
        AddressablesManager.Instance.Preparation.DependenciesDownloaded -= OnAddressablesDownloaded;
        AddressablesManager.Instance.Preparation.DownloadFailed -= OnDownloadFailed;
    }

    private void OnAddressableInitializeFailed(AsyncOperationHandle<IResourceLocator> handle)
    {
        _waitForRetryAddressables = true;
        base.OnPrepareFailed(handle.OperationException);
    }

    private void OnCheckCatalogUpdateFailed(AsyncOperationHandle<List<string>> handle)
    {
        _waitForRetryAddressables = true;
        base.OnPrepareFailed(handle.OperationException);
    }

    private void OnCatalogUpdateFailed(AsyncOperationHandle<List<IResourceLocator>> handle)
    {
        _waitForRetryAddressables = true;
        base.OnPrepareFailed(handle.OperationException);
    }

    private void OnAddressablesSizeDownloaded(AddressablesHandleWrap<long> handle)
    {
        base.OnMetadataFetched(handle.Handle.Result, null);
    }

    private void OnAddressablesSizeDownloadFailed(AsyncOperationHandle<long> handle)
    {
        base.OnMetadataFetchFailed(handle.OperationException);
    }

    private void OnDownloadProgressed(DownloadStatus status)
    {
        base.OnDownloadProgressed(new DownloadProgressStatus(status.DownloadedBytes, status.Percent));
    }

    private void OnAddressablesDownloaded(DownloadResultReport report)
    {
        base.OnDownloaded(report);
    }

    private void OnDownloadFailed(AsyncOperationHandle handle)
    {
        PatchStatus.CurrentDownloadProgress = 0f;
        PatchStatus.CurrentDownloadedSize = 0;
        base.OnDownloadFailed(handle.OperationException);
    }
}
