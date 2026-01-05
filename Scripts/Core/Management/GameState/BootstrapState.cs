using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class BootstrapState : GameStateBase
{
    bool _isPatchSuccessed;
    bool _waitRetry;

    public override void OnInitialize(GameManager _parent, GameState _state)
    {
        base.OnInitialize(_parent, _state);
    }

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);
        CoroutineRunner.Instance.RunCoroutine(BootStrappingRoutine());
    }

    private IEnumerator BootStrappingRoutine()
    {
        //--------------------------------------//

        // 얘는 빌트인 + Sync 로 그냥 바로 로드/띄움
        UIManager.Instance.ShowCallBack<UIBootstrapPanel>().Forget();

        _isPatchSuccessed = false;
        _waitRetry = false;

        while (_isPatchSuccessed == false)
        {
            if (_waitRetry)
                yield return null;
            else
                yield return PatchManager.Instance.Prepare(PatchUnitType.Table, OnPatchPrepared, OnPatchPrepareFailed);
        }

        _isPatchSuccessed = false;
        _waitRetry = false;

        while (_isPatchSuccessed == false)
        {
            if (_waitRetry)
                yield return null;
            else
                yield return PatchManager.Instance.Prepare(PatchUnitType.Addressables, OnPatchPrepared, OnPatchPrepareFailed);
        }

        _isPatchSuccessed = false;
        _waitRetry = false;

        while (_isPatchSuccessed == false)
        {
            if (_waitRetry)
                yield return null;
            else
                yield return PatchManager.Instance.Prepare(PatchUnitType.MapData, OnPatchPrepared, OnPatchPrepareFailed);
        }

        yield return new WaitForSeconds(2f);

        // 몇몇 미리 만들어놓기 
        yield return PoolManager.Instance.PrepareAsync(ObjectPoolCategory.Audio_Critical, "AudioPlayer", 5).ToCoroutine();
        yield return PoolManager.Instance.PrepareAsync(ObjectPoolCategory.Audio_Normal, "AudioPlayer", 5).ToCoroutine();
        yield return UIManager.Instance.PrepareCo(new Type[] { typeof(UISimpleDialoguePopup), typeof(UIDownloadPopup) });

        yield return GameManager.Instance.FSM.TransitionController.TransitionStateWithLoading(GameState.Auth, new LoadSimulationProcessor());
    }

    private void OnPatchPrepared(PatchUnitType type)
    {
        _isPatchSuccessed = true;
    }

    private void OnPatchPrepareFailed(PatchUnitType type, Exception exp)
    {
        _waitRetry = true;
        ShowErrorNoti($"{type} Prepare Failed : {type}", exp);
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);

        // UIManager.Instance.Hide<UIBootstrapPanel>();
    }

    private void ShowErrorNoti(string contents, Exception operationException)
    {
        PopupSystem.ShowSimpleDialoguePopup(new UISimpleDialoguePopup.Arg(
            "Notification",
            $"{contents} | {operationException.Message}",
            UISimpleDialoguePopup.ButtonFlags.Confirm | UISimpleDialoguePopup.ButtonFlags.Close,
            (result) =>
            {
                _waitRetry = false;
            }));
    }
}
