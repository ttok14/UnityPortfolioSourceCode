using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class BattlePhase : InGamePhaseBaseState
{
    BattleModeBase _mode;

    public InGameBattleMode CurrentMode { get; private set; }

    bool _isEnding;

    public override void OnEnter(Action callback, params InGameFSMEnterArgBase[] args)
    {
        TEMP_Logger.Deb($"BattlePhase Enter");

        base.OnEnter(callback, args);

        BGMManager.Instance.Play("BGM_InGameBattle");

        InGameManager.Instance.PublishEvent(InGameEvent.Enter, new InGameFSMStateNotify() { Phase = InGamePhase.Battle });

        var arg = args[0] as BattleStateEnterArg;
        CurrentMode = arg.mode;

        PrepareRoutine().Forget();
    }

    // 테스트용
    public void TEST_ForceFinish()
    {
        _mode.ForceFinish();
    }

    async UniTaskVoid PrepareRoutine()
    {
        //await InGameManager.Instance.DefenseStrategySystem.RefreshAsync(new DefenseStrategySystem.RefreshSettings()
        //{
        //    type = DefenseStrategySystem.RefreshType.RegisterVariants
        //});

        TEMP_Logger.Deb($"BattlePhase PrepareRoutine");

        //// 각종 설정
        //CameraManager.Instance.InGameController.FSM.ChangeState(CinemachineCameraType.FollowTarget, false, new object[] { Parent.Player.Entity.transform });

        // base.OnEnter 전에 미리 Mode 설정해놓아야함. (Commander 인스턴스가 Mode 인스턴스에 의존적임..)
        // 수정가능하면 수정하자 (보통 base.XX() 가 가장 먼저 호출되므로)
        if (CurrentMode == InGameBattleMode.Defense)
            _mode = new DefenseMode();

        await _mode.EnterAsync(this);

        EntityManager.Instance.SetAllEntityAIPartActivation(false);

        var aiCommander = CreateCommander();
        await aiCommander.InitializeAsync();

        EntityManager.Instance.RegisterAICommander(aiCommander);

        UIManager.Instance.ShowCallBack<UIFlushFrame>(arg: new UIFlushFrame.Arg() { txt = "곧 전투가 시작됩니다." }).Forget();

        UIManager.Instance.ShowCallBack<UIBattleModePanel>(arg: new UIBattleModePanel.Arg()
        {
            objectiveEntityIDs = InGameManager.Instance.EnemyCommander.BattleStatus.OrderedTargetIDs.ToArray()
        }).Forget();

        await UniTask.WaitForSeconds(3f);

        StartBattle();
    }

    public EntityAICommanderBase CreateCommander(params InGameFSMEnterArgBase[] args)
    {
        if (CurrentMode == InGameBattleMode.Defense)
        {
            var commander = new DefenseModeAICommander(_mode as DefenseMode);
            return commander;
        }
        else
        {
            TEMP_Logger.Err($"Not implemented type : {CurrentMode}");
        }
        return null;
    }

    public EntityAICommanderBase CreateCommander()
    {
        if (CurrentMode == InGameBattleMode.Defense)
        {
            var commander = new DefenseModeAICommander(_mode as DefenseMode);
            return commander;
        }
        else
        {
            TEMP_Logger.Err($"Not implemented type : {CurrentMode}");
        }
        return null;
    }


    private void Update()
    {
        if (_mode != null && _isEnding == false)
        {
            if (_mode.CheckFinished())
            {
                _isEnding = true;
                InGameManager.Instance.PublishEvent(InGameEvent.BattleEnding);
                PauseBattleSystem();
                _mode.FinishCutSceneRoutine(_mode.GetWinner(), OnEndingCutSceneFinished).Forget();
                return;
            }

            _mode.Update();
        }
    }

    void PauseBattleSystem()
    {
        EntityManager.Instance.SetAllEntityAIPartActivation(false);

        UIManager.Instance.HideAll<UICharacterHud>();
    }

    public override void OnRelease()
    {
        base.OnRelease();
    }

    void OnEndingCutSceneFinished()
    {
        InGameManager.Instance.RequestChangePhase(InGamePhase.Peace, new EnterPeacePhaseEventArg(InGamePhase.Battle));
    }

    void StartBattle()
    {
        TEMP_Logger.Deb($"BattlePhase StartBattle");

        InGameManager.Instance.PublishEvent(InGameEvent.Start, new InGameFSMStateNotify() { Phase = InGamePhase.Battle });

        EntityManager.Instance.SetAllEntityAIPartActivation(true);

        // SpawnManager.Instance.StartBattle();
        InGameManager.Instance.OnStartBattle();

        AudioManager.Instance.Play("SFX_BattleStart");
    }

    public override void OnExit(Action callback)
    {
        TEMP_Logger.Deb($"BattlePhase OnExit");

        WaveManager.Instance.Finish();
        // SpawnManager.Instance.Clean();

        InGameManager.Instance.OnEndBattle();

        //SpawnManager.Instance.EndSpawning(
        //Up: true);

        UIManager.Instance.Hide<UIBattleModePanel>();

        if (_mode != null)
            _mode.Release();

        _mode = null;

        _isEnding = false;
        CurrentMode = InGameBattleMode.None;

        InGameManager.Instance.PublishEvent(InGameEvent.End, new InGameFSMStateNotify() { Phase = InGamePhase.Battle });

        base.OnExit(callback);
    }
}
