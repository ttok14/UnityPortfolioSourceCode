using System;
using System.Collections;
using UnityEngine;

public class GameStateBase : BaseState<GameState, GameManager, object>
{

    public override void OnEnter(Action callback, params object[] args)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.GAME_STATE_CHANGED, new GameStateChangeEventArg(Parent.FSM.Current_State, State));
        base.OnEnter(callback, args);
    }
}
