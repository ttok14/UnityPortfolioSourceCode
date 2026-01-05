using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PatchEvents;

public abstract class PatchUnitBase
{
    public class Status
    {
        public bool IsInitialized { get; set; }
        public bool IsPreparedForDownload { get; set; }
        public List<string> DownloadContentsList { get; set; }
        public long CurrentDownloadedSize { get; set; }
        public long TotalSize { get; set; }
        public float CurrentDownloadProgress { get; set; }
    }

    public Status PatchStatus { get; protected set; } = new Status();

    public abstract PatchUnitType Type { get; }

    OnFailed FailedListener;
    OnPreparationCompleted PreparationCompletedListener;
    OnMetadataFetchProgressed MetadataFetchProgressedListener;
    OnMetadataFetchCompleted MetadataFetchCompletedListener;
    OnContentsDownloadProgressed ContentsDownloadProgressListener;
    OnContentsDownloadCompleted ContentsDownloadedCompletedListener;

    public virtual void Initialize() { }

    public virtual IEnumerator Prepare(OnPreparationCompleted onCompleted, OnFailed onFailed)
    {
        PreparationCompletedListener = onCompleted;
        FailedListener = onFailed;
        yield break;
    }

    public virtual IEnumerator FetchMetadata(OnMetadataFetchProgressed onProgressed, OnMetadataFetchCompleted onCompleted, OnFailed onFailed)
    {
        MetadataFetchProgressedListener = onProgressed;
        MetadataFetchCompletedListener = onCompleted;
        FailedListener = onFailed;
        yield break;
    }

    public virtual IEnumerator DownloadContents(OnContentsDownloadProgressed onProgress, OnContentsDownloadCompleted onCompleted, OnFailed onFailed)
    {
        ContentsDownloadProgressListener = onProgress;
        ContentsDownloadedCompletedListener = onCompleted;
        FailedListener = onFailed;
        yield break;
    }

    void ResetEvents()
    {
        FailedListener = null;
        PreparationCompletedListener = null;
        MetadataFetchProgressedListener = null;
        MetadataFetchCompletedListener = null;
        ContentsDownloadProgressListener = null;
        ContentsDownloadedCompletedListener = null;
    }

    #region =====:: 이벤트 호출 ::====

    protected virtual void OnPrepared()
    {
        PreparationCompletedListener?.Invoke(Type);
        ResetEvents();
    }

    protected virtual void OnPrepareFailed(Exception exception)
    {
        FailedListener?.Invoke(Type, exception);
        ResetEvents();
    }

    protected virtual void OnMetadataFetchProgressed(float progress)
    {
        MetadataFetchProgressedListener?.Invoke(Type, progress);
    }

    protected virtual void OnMetadataFetched(long downloadSize, List<string> contents)
    {
        PatchStatus.IsPreparedForDownload = true;
        PatchStatus.TotalSize = downloadSize;
        MetadataFetchCompletedListener?.Invoke(Type, downloadSize, contents);
        ResetEvents();
    }

    protected virtual void OnMetadataFetchFailed(Exception exception)
    {
        PatchStatus.IsPreparedForDownload = false;
        PatchStatus.TotalSize = 0;
        FailedListener?.Invoke(Type, exception);
        ResetEvents();
    }

    protected virtual void OnDownloadProgressed(DownloadProgressStatus status)
    {
        PatchStatus.CurrentDownloadProgress = status.progress;
        ContentsDownloadProgressListener?.Invoke(Type, status);
    }

    protected virtual void OnDownloaded(DownloadResultReport report)
    {
        PatchStatus.CurrentDownloadProgress = 1f;
        PatchStatus.TotalSize = report.downloadedBytes;
        /// StateFlags |= PatchStateFlags.AssetDownloadCompleted;
        ContentsDownloadedCompletedListener?.Invoke(Type, report);
        ResetEvents();
    }

    protected virtual void OnDownloadFailed(Exception exp)
    {
        PatchStatus.CurrentDownloadProgress = 0f;
        PatchStatus.CurrentDownloadedSize = 0;
        FailedListener?.Invoke(Type, exp);
        ResetEvents();
    }

    #endregion
}
