using System;
using System.Collections.Generic;

public class PatchEvents
{
    public delegate void OnFailed(PatchUnitType type, Exception exp);
    public delegate void OnPreparationCompleted(PatchUnitType type);
    public delegate void OnMetadataFetchProgressed(PatchUnitType type, float progress);
    public delegate void OnMetadataFetchCompleted(PatchUnitType type, long totalSize, List<string> contents);
    public delegate void OnContentsDownloadProgressed(PatchUnitType type, DownloadProgressStatus status);
    public delegate void OnContentsDownloadCompleted(PatchUnitType type, DownloadResultReport report);
}

public struct DownloadProgressStatus
{
    public long downloadedBytes;
    public float progress;

    public DownloadProgressStatus(long downloadedBytes, float progress)
    {
        this.downloadedBytes = downloadedBytes;
        this.progress = progress;
    }
}

public struct DownloadResultReport
{
    public long downloadedBytes;
    public float secondsTaken;

    public DownloadResultReport(long downloadedBytes, float secondsTaken)
    {
        this.downloadedBytes = downloadedBytes;
        this.secondsTaken = secondsTaken;
    }
}
