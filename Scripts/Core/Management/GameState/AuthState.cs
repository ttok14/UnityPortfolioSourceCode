using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuthState : GameStateBase
{
    bool _isFetchSuccessed;

    bool _isEnteringLobby;

    bool _isTableReadyForDownload;
    long _tableDownloadSize;

    bool _isAddressablesSizeDownloaded;
    long _addressablesDownloadSize;

    bool _isMapDataReadyForDownloaded;
    long _mapDataDownloadSize;

    public override void OnInitialize(GameManager _parent, GameState _state)
    {
        base.OnInitialize(_parent, _state);
    }

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        _isEnteringLobby = false;

        EventManager.Instance.Register(GLOBAL_EVENT.REQUEST_GO_LOBBY_START_ROUTINE, OnRequestGoLobbyRoutine);
    }

    public override void OnExit(Action callback)
    {
        EventManager.Instance.Unregister(GLOBAL_EVENT.REQUEST_GO_LOBBY_START_ROUTINE, OnRequestGoLobbyRoutine);
        base.OnExit(callback);
    }

    private void OnRequestGoLobbyRoutine(EventContext cxt)
    {
        if (_isEnteringLobby)
        {
            TEMP_Logger.Deb($"Already Entering Lobby...");
            return;
        }

        CoroutineRunner.Instance.RunCoroutine(GoLobbyRoutine());
    }

    IEnumerator GoLobbyRoutine()
    {
        //------ 가상의 계정정보 간단하게 세팅하자 ------
        Me.Initialize(JsonUtility.ToJson(new Me.MeData()
        {
            AccountInfo = new Me.Account()
            {
                IsTutorialDone = false,
            },
            Currency = new Me.Currency()
            {
                Gold = 10000,
                Wood = 50000,
                Food = 2000
            },
            SkillSet = new Me.EquippedSkillSet()
            {
                SkillIDs = new uint[]
                {
                    1,15,12,
                },
                SpellIDs = new uint[]
                {
                    22,23,24
                }
            }
        }));
        //--------------------------------//

        yield return PatchManager.Instance.FetchMetadata(PatchUnitType.Table, null, OnMetadataFetched, OnMetadataFetchFailed);
        if (_isFetchSuccessed == false)
            yield break;

        yield return PatchManager.Instance.FetchMetadata(PatchUnitType.Addressables, null, OnMetadataFetched, OnMetadataFetchFailed);
        if (_isFetchSuccessed == false)
            yield break;

        yield return PatchManager.Instance.FetchMetadata(PatchUnitType.MapData, null, OnMetadataFetched, OnMetadataFetchFailed);
        if (_isFetchSuccessed == false)
            yield break;

        if (_isTableReadyForDownload == false || _isAddressablesSizeDownloaded == false || _isMapDataReadyForDownloaded == false)
            yield break;

        long totalDownloadSize = _tableDownloadSize + _addressablesDownloadSize + _mapDataDownloadSize;
        bool shouldDownload = totalDownloadSize > 0;
        if (shouldDownload)
        {
            PopupSystem.ShowDownloadAskPopup(new UIDownloadPopup.Arg(
                "Download Data",
                "Would you like to download data?",
                $"{totalDownloadSize}Byte",
                (result) =>
                {
                    if ((result as UIDownloadPopup.ResultArg).result == UIDownloadPopup.Result.Confirm)
                    {
                        CoroutineRunner.Instance.RunCoroutine(GameManager.Instance.FSM.TransitionController.TransitionStateWithLoading(GameState.Lobby,
                            new CompositeLoadProcessor(
                                new CompositeLoadProcessor.SubProcessor(70, new DownloadProcessor(_tableDownloadSize, _addressablesDownloadSize, _mapDataDownloadSize, GameManager.Instance.FSM.Current_State)),
                                new CompositeLoadProcessor.SubProcessor(10, new GameDBLoadProcessor()),
                                new CompositeLoadProcessor.SubProcessor(10, new GameDBAccessorLoadProcessor()),
                                new CompositeLoadProcessor.SubProcessor(10, new MapDataLoadProcessor()))));
                    }
                    else
                    {
                        PopupSystem.ShowSimpleDialoguePopup(new UISimpleDialoguePopup.Arg("Error", "Must download contents for advance."));
                    }
                }));
        }
        else
        {
            CoroutineRunner.Instance.RunCoroutine(GameManager.Instance.FSM.TransitionController.TransitionStateWithLoading(GameState.Lobby,
                new CompositeLoadProcessor(
                    new CompositeLoadProcessor.SubProcessor(50, new GameDBLoadProcessor()),
                    new CompositeLoadProcessor.SubProcessor(40, new GameDBAccessorLoadProcessor()),
                    new CompositeLoadProcessor.SubProcessor(10, new MapDataLoadProcessor()))));
        }
    }

    private void OnMetadataFetched(PatchUnitType type, long totalSize, List<string> contents)
    {
        if (type == PatchUnitType.Table)
        {
            _isFetchSuccessed = true;
            _tableDownloadSize = totalSize;
            _isTableReadyForDownload = true;
        }
        else if (type == PatchUnitType.Addressables)
        {
            _isFetchSuccessed = true;
            _isAddressablesSizeDownloaded = true;
            _addressablesDownloadSize = totalSize;
        }
        else if (type == PatchUnitType.MapData)
        {
            _isFetchSuccessed = true;
            _isMapDataReadyForDownloaded = true;
            _mapDataDownloadSize = totalSize;
        }
        else
        {
            TEMP_Logger.Err($"Not implmented type : {type}");
        }
    }

    private void OnMetadataFetchFailed(PatchUnitType type, Exception exp)
    {
        _isFetchSuccessed = false;

        _isTableReadyForDownload = false;
        _tableDownloadSize = 0;

        _isAddressablesSizeDownloaded = false;
        _addressablesDownloadSize = 0;

        _isMapDataReadyForDownloaded = false;
        _mapDataDownloadSize = 0;

        PopupSystem.ShowSimpleDialoguePopup(new UISimpleDialoguePopup.Arg(
            $"{type} Download Failed",
            "Failed to get size , please retry.",
            UISimpleDialoguePopup.ButtonFlags.Confirm | UISimpleDialoguePopup.ButtonFlags.Close,
            (result) =>
            {
            }));
    }
}
