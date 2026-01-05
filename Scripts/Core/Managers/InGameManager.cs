using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public class InGameManager : SingletonBase<InGameManager>
{
    private FSM<InGamePhaseEvent, InGamePhase, InGameFSMEnterArgBase, InGameManager> _phaseFSM { get; set; }
    public InGamePhaseBaseState CurrentPhase => _phaseFSM.Current as InGamePhaseBaseState;
    public InGamePhase CurrentPhaseType => _phaseFSM.Current_State;
    public InGameBattleMode BattleMode { get; private set; }

    public PlayerTeamCommander PlayerCommander { get; private set; }
    public EnemyTeamCommander EnemyCommander { get; private set; }

    public DefensePathSystem DefensePathSystem { get; private set; }

    public InGameCacheContainer CacheContainer { get; private set; }

    public event Action<InGameEvent, InGameEventArgBase> EventListener;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void OnStartBattle()
    {
        PlayerCommander.OnBattleStart();
        EnemyCommander.OnBattleStart();

        // DefensePathSystem.StartBattle();
    }

    public void OnEndBattle()
    {
        EnemyCommander.OnBattleEnd();
        DefensePathSystem.OnBattleEnd();
    }

    // 테스트용 임시 코드 
    private void Update()
    {
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            InGamePhase newPhase = InGameManager.Instance.CurrentPhaseType == InGamePhase.Peace ? InGamePhase.Battle : InGamePhase.Peace;
            EnterInGamePhaseEventArgBase arg = InGameManager.Instance.CurrentPhaseType == InGamePhase.Peace ?
                new EnterBattlePhaseEventArg(InGameManager.Instance.CurrentPhaseType, InGameBattleMode.Defense) : new EnterPeacePhaseEventArg(InGameManager.Instance.CurrentPhaseType);

            if (newPhase == InGamePhase.Battle)
                InGameManager.Instance.RequestChangePhase(newPhase, arg);
            else if (newPhase == InGamePhase.Peace)
                (CurrentPhase as BattlePhase).TEST_ForceFinish();
        }
    }

    public void PublishEvent(InGameEvent evt, InGameEventArgBase arg = null)
    {
        EventListener?.Invoke(evt, arg);
    }

    public void EnterInGame()
    {
        PrepareRoutine().Forget();
    }

    async UniTaskVoid PrepareRoutine()
    {
        MovementFactory.Initialize();
        ProjectileSystem.Initialize();

        CameraManager.Instance.PrepareInGame(FindAnyObjectByType<InGameCameraController>());

        CacheContainer = new InGameCacheContainer();
        CacheContainer.Initialize();

        InitializeFSM();

        await PreparePool();

        string mapDataName = "MainMap01";

        await MapManager.Instance.GenerateMap(mapDataName);

        await EntityPlacementManager.Instance.PrepareGame();

        DefensePathSystem = new DefensePathSystem();
        DefensePathSystem.Initialize();

        PlayerCommander = new PlayerTeamCommander();
        PlayerCommander.Initialize();

        EnemyCommander = new EnemyTeamCommander();
        EnemyCommander.Initialize();

        UIManager.Instance.Show(typeof(UIInGamePanel)).Forget();

        await UniTask.WaitForSeconds(2f);

        RequestChangePhase(InGamePhase.Peace, new EnterPeacePhaseEventArg(InGamePhase.None));

        UIToastSystem.ShowToast(UIToastSystem.ToastType.BriefSide, "아침이 밝았습니다");

        TEMP_Logger.Deb($"** Map Load DONE !**");
    }

    private void InitializeFSM()
    {
        _phaseFSM = new FSM<InGamePhaseEvent, InGamePhase, InGameFSMEnterArgBase, InGameManager>(this);

        _phaseFSM.AddState(InGamePhase.Peace, gameObject.AddComponent<PeacefulPhase>());
        _phaseFSM.AddState(InGamePhase.Battle, gameObject.AddComponent<BattlePhase>());
    }

    async UniTask PreparePool()
    {
        PoolManager.Instance.CreatePoolMap(ObjectPoolCategory.Audio_Normal, 10);

        PoolManager.Instance.SetInstanceLimitCount(ObjectPoolCategory.Audio_Normal, "Heavy_FooStep01", 2);

        PoolManager.Instance.SetInstanceLimitCount(ObjectPoolCategory.Fx, "SpriteFX_SlashSprite", 6);
        PoolManager.Instance.SetInstanceLimitCount(ObjectPoolCategory.Fx, "FX_Hit_Red", 8);
        PoolManager.Instance.SetInstanceLimitCount(ObjectPoolCategory.Fx, "FX_Hit_Blue", 8);
        PoolManager.Instance.SetInstanceLimitCount(ObjectPoolCategory.Fx, "SpriteFX_BloodSplatter", 13);

        var taskList = new List<UniTask<PoolOpResult>>()
        {
            PoolManager.Instance.PrepareAsync(ObjectPoolCategory.Default, "PathIndicator", 1),
            PoolManager.Instance.PrepareAsync(ObjectPoolCategory.Fx, "FX_Explosion01", 1)
        };

        await UniTask.WhenAll(taskList);
    }

    public void RequestChangePhase(InGamePhase newPhase, EnterInGamePhaseEventArgBase arg)
    {
        if (_phaseFSM.Current_State == newPhase)
        {
            return;
        }

        EventManager.Instance.Publish(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, arg);

        // 여기서 Manager 단에서 알아서 조립해 보내주자
        if (newPhase == InGamePhase.Peace)
        {
            BattleMode = InGameBattleMode.None;

            if (_phaseFSM.Current_State == InGamePhase.None)
                _phaseFSM.Enable(InGamePhase.Peace);
            else _phaseFSM.ChangeState(newPhase, false, null);
        }
        else if (newPhase == InGamePhase.Battle)
        {
            // TODO: 적절한 모드 선택 기능 도입시 수정
            BattleMode = InGameBattleMode.Defense;
            _phaseFSM.ChangeState(newPhase, false, new InGameFSMEnterArgBase[] { new BattleStateEnterArg() { mode = BattleMode } });
        }
    }

    public override void Release()
    {
        base.Release();

        PlayerCommander.Release();
        EnemyCommander.Release();
        CacheContainer.Release();
        DefensePathSystem.Release();
        _phaseFSM.Release();
        _phaseFSM = null;
    }
}
