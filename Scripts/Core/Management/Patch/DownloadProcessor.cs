using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PatchEvents;

public class DownloadProcessor : ILoadProcessor
{
    GameState _prevState;

    string _curStatus = "Preparing..";
    float _progress;
    LoadingProcessResult _result = LoadingProcessResult.None;

    long _totalDownloadSize;

    long _tableSizeToDownload;
    long _tableDownloadedSize;

    long _addressablesSizeToDownload;
    long _addressablesDownloadedSize;

    long _mapdataSizeToDownload;
    long _mapdataDownloadedSize;

    bool _waitForNextActionAfterFail;

    public long CurrentTotalDownloadedSize => _tableDownloadedSize + _addressablesDownloadedSize + _mapdataDownloadedSize;

    public float Progress => _result == LoadingProcessResult.Success ? 1 : _progress;

    public string CurrentStatus => _curStatus;

    public LoadingProcessResult Result => _result;

    public DownloadProcessor(long tableDownloadSize, long addressablesDownloadSize, long mapdataDownloadSize, GameState prevState)
    {
        _tableSizeToDownload = tableDownloadSize;
        _addressablesSizeToDownload = addressablesDownloadSize;
        _mapdataSizeToDownload = mapdataDownloadSize;
        _totalDownloadSize = tableDownloadSize + addressablesDownloadSize + mapdataDownloadSize;
        _prevState = prevState;
    }

    public IEnumerator Process()
    {
        if (_tableSizeToDownload > 0)
        {
            yield return Process(PatchUnitType.Table);

            if (_result != LoadingProcessResult.Success)
            {
                yield break;
            }
        }

        if (_addressablesSizeToDownload > 0)
        {
            yield return Process(PatchUnitType.Addressables);

            if (_result != LoadingProcessResult.Success)
            {
                yield break;
            }
        }

        if (_mapdataSizeToDownload > 0)
            yield return Process(PatchUnitType.MapData);
    }

    public IEnumerator Process(PatchUnitType type)
    {
        _result = LoadingProcessResult.None;

        _curStatus = $"Please wait ...";

        while (_result != LoadingProcessResult.Success && _result != LoadingProcessResult.Cancelled)
        {
            yield return PatchManager.Instance.DownloadContents(type, OnDownloadProgressed, OnDownloaded, OnDownloadFailed);

            if (_result == LoadingProcessResult.Success)
            {
                break;
            }

            yield return new WaitUntil(() => _waitForNextActionAfterFail == false);
        }
    }

    private void OnDownloadProgressed(PatchUnitType type, DownloadProgressStatus status)
    {
        if (type == PatchUnitType.Table)
        {
            _tableDownloadedSize = status.downloadedBytes;
        }
        else if (type == PatchUnitType.Addressables)
        {
            _addressablesDownloadedSize = status.downloadedBytes;
        }
        else if (type == PatchUnitType.MapData)
        {
            _mapdataDownloadedSize = status.downloadedBytes;
        }

        _progress = (float)(((double)CurrentTotalDownloadedSize) / _totalDownloadSize);
        // _curStatus = $"{_tableDownloadedSize + _addressablesDownloadedSize}/{_totalDownloadSize}Bytes";
        _curStatus = $"{CurrentTotalDownloadedSize}/{_totalDownloadSize}";
    }

    private void OnDownloaded(PatchUnitType type, DownloadResultReport report)
    {
        _progress = 1f;
        _result = LoadingProcessResult.Success;
    }

    private void OnDownloadFailed(PatchUnitType type, Exception exp)
    {
        _result = LoadingProcessResult.Failed;
        // 실패하면 일단은 재시도에 대한 선택을 해야하기에 대기시킴 
        _waitForNextActionAfterFail = true;

        string failMsg = exp.Message;

        PopupSystem.ShowSimpleDialoguePopup(new UISimpleDialoguePopup.Arg(
            "Download Failed",
            failMsg,
            UISimpleDialoguePopup.ButtonFlags.All,
            (result) =>
            {
                if ((result as UISimpleDialoguePopup.ResultArg).result == UISimpleDialoguePopup.Result.Confirm)
                {
                    _waitForNextActionAfterFail = false;
                }
                else
                {
                    _waitForNextActionAfterFail = false;
                    _result = LoadingProcessResult.Cancelled;

                    // 돌아가기
                    CoroutineRunner.Instance.RunCoroutine(GameManager.Instance.FSM.TransitionController.TransitionState(_prevState));
                }
            }));
    }
}
