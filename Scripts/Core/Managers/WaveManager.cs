using UnityEngine;
using GameDB;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

public class WaveManager : SingletonBase<WaveManager>
{
    private CancellationTokenSource _cancellationTokenSrc;
    private CancellationTokenSource _linkedTokenSrc;

    private List<RuntimeWaveCmdBase> _cachedSequence;

    public bool IsProcessing { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        _cachedSequence = new List<RuntimeWaveCmdBase>();
    }

    public override void Release()
    {
        base.Release();

        if (_cachedSequence != null)
            _cachedSequence.Clear();
    }

    public void StartWave(uint waveID)
    {
        // 가장 하드난이도임. 테스트용
        waveID = 1000;

        var sequence = DBWave.GetSequence(waveID);
        if (sequence == null)
        {
            TEMP_Logger.Err($"Failed to get WaveSequenceData | WaveID : {waveID}");
            return;
        }

        InGameManager.Instance.EventListener += OnInGameEvent;

        // TEST
        // sequence.Clear();
        // sequence.RemoveRange(1, sequence.Count - 1);

        _cancellationTokenSrc = new CancellationTokenSource();
        _linkedTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSrc.Token, Application.exitCancellationToken);

        if (_cachedSequence.Capacity < sequence.Count)
            _cachedSequence.Capacity = sequence.Count;

        foreach (var data in sequence)
        {
            RuntimeWaveCmdBase newCmd = null;

            switch (data.CmdType)
            {
                case E_WaveCommandType.Wait:
                case E_WaveCommandType.WaitForClear:
                    newCmd = new RuntimeWaitCmd(data);
                    break;
                case E_WaveCommandType.Spawn:
                    newCmd = new RuntimeSpawnCmd(data);
                    break;
                case E_WaveCommandType.Camera:
                    newCmd = new RuntimeCameraCmd(data);
                    break;
                case E_WaveCommandType.Notification:
                    newCmd = new RuntimeNotificationCmd(data);
                    break;
                case E_WaveCommandType.Sound:
                    newCmd = new RuntimeSoundCmd(data);
                    break;
                case E_WaveCommandType.FX:
                    newCmd = new RuntimeFXCmd(data);
                    break;
                default:
                    TEMP_Logger.Err($"Not implemented type : {data.CmdType}");
                    break;
            }

            _cachedSequence.Add(newCmd);
        }

        ProcessSequence(_cachedSequence, _linkedTokenSrc.Token).Forget();
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.BattleEnding)
        {
            Finish();
        }
    }

    public void Finish()
    {
        bool wasProcessing = IsProcessing;
        IsProcessing = false;

        if (_cancellationTokenSrc != null)
        {
            _cancellationTokenSrc.Cancel();
            _cancellationTokenSrc.Dispose();
            _cancellationTokenSrc = null;
        }

        _linkedTokenSrc = null;

        InGameManager.Instance.EventListener -= OnInGameEvent;

        if (wasProcessing)
            InGameManager.Instance.PublishEvent(InGameEvent.WaveEnd);
    }

    async UniTaskVoid ProcessSequence(List<RuntimeWaveCmdBase> cmdList, CancellationToken ctk)
    {
        IsProcessing = true;

        InGameManager.Instance.PublishEvent(InGameEvent.WaveStart);

        int limitConcurrentCmdCnt = 20;
        int cmdExecuteCountWithoutYield = 0;

        int idx = 0;
        while (idx < cmdList.Count)
        {
            var cmd = cmdList[idx];
            idx++;

            // 확률이 존재하면 체크 후 꽝이면 이번 Cmd 는 그냥 패스 처리
            if (cmd.Chance > 0 && UnityEngine.Random.Range(0, 100) > cmd.Chance)
                continue;

            // 특수 조건부 대기 커맨드 
            if (cmd.IsWaitForClear)
            {
                await WaitUntil_ClearEnemy(ctk);
                cmdExecuteCountWithoutYield = 0;
                if (ctk.IsCancellationRequested)
                    break;
            }
            else
            {
                cmd.Execute();
                // ExecuteCommand(cmd);
            }

            cmdExecuteCountWithoutYield++;

            if (cmd.ResumeDelay > 0f)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(cmd.ResumeDelay), cancellationToken: ctk);
                    cmdExecuteCountWithoutYield = 0;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception exp)
                {
                    TEMP_Logger.Err(exp.Message);
                    break;
                }
            }

            if (cmdExecuteCountWithoutYield >= limitConcurrentCmdCnt)
            {
                await UniTask.Yield();
                cmdExecuteCountWithoutYield = 0;
                if (_cancellationTokenSrc.IsCancellationRequested)
                    break;
            }
        }

        // Wave 는 커맨드없으면 일단 탈출 , 끝 (게임 끝 의미가 아님)

        Finish();
    }

    //void ExecuteCommand(WaveSequenceTable cmd)
    //{
    //    switch (cmd.CmdType)
    //    {
    //        case E_WaveCommandType.Wait:
    //            {
    //                // 패스
    //            }
    //            break;
    //        case E_WaveCommandType.Spawn:
    //            {
    //                InGameManager.Instance.EnemyCommander.SpawnController.SpawnEnemyWave(cmd.ToSpawnData());
    //                // SpawnManager.Instance.SpawnEnemyWave(cmd.ToSpawnData());
    //            }
    //            break;
    //        case E_WaveCommandType.Camera:
    //            {
    //                var data = cmd.ToCameraData();
    //                var position = SpawnManager.Instance.GetSpawnPosition((int)data.TargetID);

    //                if (data.FOV > 0)
    //                    CameraManager.Instance.InGameController.SetManualStatePositionAndFov(position, data.FOV);
    //                else
    //                    CameraManager.Instance.InGameController.SetManualStatePosition(position);
    //            }
    //            break;
    //        case E_WaveCommandType.Notification:
    //            {
    //                var data = cmd.ToNotificationData();
    //                if (data.MessageLevel == 0)
    //                    UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticInformaitve, data.Message);
    //                else if (data.MessageLevel == 1)
    //                    UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticWarning, data.Message);
    //                else
    //                {
    //                    TEMP_Logger.Err($"Not Implmented MessageLevel : {data.MessageLevel}");
    //                    UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticWarning, data.Message);
    //                }
    //            }
    //            break;
    //        case E_WaveCommandType.Sound:
    //            {
    //                var data = cmd.ToSoundData();
    //                AudioManager.Instance.Play(data.AudioKey);
    //            }
    //            break;
    //        case E_WaveCommandType.FX:
    //            {
    //                var data = cmd.ToFXData();
    //                FXSystem.PlayFX(data.FXKey, startPosition: SpawnManager.Instance.GetSpawnPosition((int)data.SpawnPointID)).Forget();
    //            }
    //            break;
    //        default:
    //            TEMP_Logger.Err($"Not implemented Wave Sequence Cmd Type : {cmd.CmdType}");
    //            break;
    //    }
    //}

    async UniTask WaitUntil_ClearEnemy(CancellationToken ctk)
    {
        while (EntityManager.Instance.GetCharacterCount(EntityTeamType.Enemy) > 0 ||
            InGameManager.Instance.EnemyCommander.SpawnController.SpawningEntityCount > 0)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ctk);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exp)
            {
                TEMP_Logger.Err(exp.Message);
            }
        }
    }
}
