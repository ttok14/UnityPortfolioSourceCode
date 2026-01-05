using System;
using System.Collections;
using UnityEngine;

public class LoadingState : GameStateBase
{
    public class Arg
    {
        public GameState nextState;
        public ILoadProcessor loadProcessor;

        public Arg(GameState nextState, ILoadProcessor loadProcessor)
        {
            this.nextState = nextState;
            this.loadProcessor = loadProcessor;
        }
    }

    public override void OnInitialize(GameManager _parent, GameState _state)
    {
        base.OnInitialize(_parent, _state);
    }

    /// <summary>
    /// * args[0] 에는 반드시 최종 State 가 들어와야 한다 *
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    public override void OnEnter(Action callback, params object[] args)
    {
        if (args == null || args.Length == 0 || (args[0] is Arg) == false)
        {
            TEMP_Logger.Err($"Invalid arg");
            base.OnEnter(callback, args);
            return;
        }

        base.OnEnter(callback, args);
        CoroutineRunner.Instance.RunCoroutine(LoadingRoutine(args));
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);
    }

    private IEnumerator LoadingRoutine(params object[] args)
    {
        var arg = args[0] as Arg;

        // 실제 로드 진행
        yield return arg.loadProcessor.Process();

        if (arg.loadProcessor.Result == LoadingProcessResult.Success)
        {
            TEMP_Logger.Deb($"LoadingRoutine | Next : {arg.nextState}");
            yield return Parent.FSM.TransitionController.TransitionState(arg.nextState,
                onCompleted: () =>
                {
                    TEMP_Logger.Deb($"ChangeState From Loading (TO) : {arg.nextState}");
                });
        }
        else
        {
            TEMP_Logger.Err($"TODO: Error handling !");
        }
    }
}
