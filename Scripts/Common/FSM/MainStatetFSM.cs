
using System;
using System.Collections;

public class MainStatetFSM : FSM<GameStateEvent, GameState, object, GameManager>
{
    public GameStateTransitionController TransitionController { get; private set; }

    public MainStatetFSM(GameManager gameManager) : base(gameManager) { }

    protected override void OnInit()
    {
        base.OnInit();
        TransitionController = new GameStateTransitionController(this);
    }

    public void AddEnterEventHandler(GameState state, Action handler)
    {
        StateMap[state].EnterEventHandle += handler;
    }

    public void RemoveEnterEventHandler(GameState state, Action handler)
    {
        StateMap[state].EnterEventHandle -= handler;
    }

    public void AddExitEventHandler(GameState state, Action handler)
    {
        StateMap[state].ExitEventHandler += handler;
    }

    public void RemoveExitEventHandler(GameState state, Action handler)
    {
        StateMap[state].ExitEventHandler -= handler;
    }


}
