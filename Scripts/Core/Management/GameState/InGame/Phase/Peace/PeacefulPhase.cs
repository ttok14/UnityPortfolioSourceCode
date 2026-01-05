using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class PeacefulPhase : InGamePhaseBaseState
{
    PeaceBattleTimer _battleSwitchTimer;

    public override void OnEnter(Action callback, params InGameFSMEnterArgBase[] args)
    {
        TEMP_Logger.Deb($"PeacefulPhase Enter");

        base.OnEnter(callback, args);

        _battleSwitchTimer = new PeaceBattleTimer();
        // _status = new PeacePhaseStatus();

        BGMManager.Instance.Play("BGM_InGame01");

        InGameManager.Instance.PublishEvent(InGameEvent.Enter, new InGameFSMStateNotify() { Phase = InGamePhase.Peace });

        CameraManager.Instance.InGameController.FSM.ChangeState(CinemachineCameraType.Free);

        UIManager.Instance.Show<UIFlushFrame>(arg: new UIFlushFrame.Arg() { txt = "평화가 찾아왔습니다." });
        UIManager.Instance.Hide<UIJoystickFrame>();

        PrepareRoutine().Forget();
    }

    private async UniTask BuildEssentialEntities()
    {
        // 플레이어 넥서스 재건 
        if (EntityManager.Instance.GetNexus(EntityTeamType.Player) == null)
        {
            var nexus = await InGameManager.Instance.PlayerCommander.RebuildNexus();
            CameraManager.Instance.InGameController.CurrentCameraTarget.transform.position = nexus.transform.position;
        }

        // 적 넥서스 재건 
        await InGameManager.Instance.EnemyCommander.RebuildStructures();

        // 플레이어 캐릭터 사망시 부활
        if (InGameManager.Instance.PlayerCommander.Player.IsAlive == false)
        {
            TEMP_Logger.Deb($"BuildEssentialEntities | Character Respawning..");

            int respawnCnt = InGameManager.Instance.PlayerCommander.Player.CreateCharacterEntity();
            if (respawnCnt > 1)
                UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticInformaitve, "부활하였습니다.");
        }
        else
        {
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticInformaitve, "체력을 회복하였습니다.");

            var playerEntity = InGameManager.Instance.PlayerCommander.Player.Entity;
            playerEntity.ApplyAffect(playerEntity.ID, 0, 999999, playerEntity.transform.position);
        }
    }

    private void Update()
    {
        if (_battleSwitchTimer != null)
            _battleSwitchTimer.Update();
    }

    async UniTaskVoid PrepareRoutine()
    {
        TEMP_Logger.Deb($"PeacefulPhase PrepareRoutine()");

        await BuildEssentialEntities();

        var aiCommander = new PeaceAICommander();
        await aiCommander.InitializeAsync();

        EntityManager.Instance.RegisterAICommander(aiCommander);

        InGameManager.Instance.PublishEvent(InGameEvent.Start, new InGameFSMStateNotify() { Phase = InGamePhase.Peace });

        EntityManager.Instance.SetAllEntityAIPartActivation(true);

        _battleSwitchTimer.SetTimer(Constants.InGame.BattleTimerSeconds, Constants.InGame.BattleTimerSeconds - Constants.InGame.BattleStartAlertBeforeEnter);

        UIManager.Instance.Show<UIPeaceModePanel>(arg: new UIPeaceModePanel.Arg(_battleSwitchTimer.StartBattleTimeAt));

        InGameManager.Instance.PlayerCommander.OnPeaceStart();
        InGameManager.Instance.EnemyCommander.OnPeaceStart();

        //InGameManager.Instance.DefensePathSystem.Refresh(new DefensePathSystem.RefreshSettings()
        //{
        //    type = DefensePathSystem.RefreshType.Setup,
        //    initSettings = new DefensePathSystem.InitializeSettings()
        //    {
        //        targetCount = 2,
        //    }
        //});
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);

        UIManager.Instance.Hide<UIPeaceModePanel>();

        InGameManager.Instance.PublishEvent(InGameEvent.End, new InGameFSMStateNotify() { Phase = InGamePhase.Peace });

        TEMP_Logger.Deb($"PeacefulPhase OnExit");
    }
}
