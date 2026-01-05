using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameState : GameStateBase
{
    //private FSM<InGamePhaseEvent, InGamePhase, InGameState> _phaseFSM { get; set; }
    // public FSM<InGamePhaseEvent, InGamePhase, InGameState> PhaseFSM => _phaseFSM;

    //public InGamePhase CurrentPhase => _phaseFSM.Current_State;

    //private PlayerController _player;
    //public PlayerController Player => _player;

    public override void OnInitialize(GameManager _parent, GameState _state)
    {
        base.OnInitialize(_parent, _state);
    }

    //public void ChangePhase(InGamePhase newPhase, params object[] args)
    //{
    //    if (_phaseFSM.Current_State == newPhase)
    //    {
    //        return;
    //    }

    //    _phaseFSM.ChangeState(newPhase, false, args);
    //}

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        InGameManager.Instance.EnterInGame();
    }

    public override void OnExit(Action callback)
    {
        EntityManager.Instance.Release();
        MapManager.Instance.Release();
        PlayerInteractionManager.Instance.Release();
        PathFindingManager.Instance.Release();
        CameraManager.Instance.ExitInGame();
        LightManager.Instance.Release();
        // SpawnManager.Instance.Release();
        InGameManager.Instance.Release();
        EntityPlacementManager.Instance.Release();

        // Warning: 이 인풋 매니저의 IsEventPublishingEnabled 복원 처리는
        // 항상 여기에 영향을 줄 수 있는 이벤트/처리 등이 해지된 후에 마지막에 호출돼야함
        // 그렇지 않으면 잠재적으로 이게 바뀐후에 아직 인게임의 잔재가 이 값을 바꿔버릴 수 있음
        // (e.g CinemachineCameraController 의 Blend 스테이트에 따른 enable 설정 등)
        InputManager.Instance.BlockEventCount = 0;

        base.OnExit(callback);
    }
}
