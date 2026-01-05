using System;
using System.Collections;
using System.Collections.Generic;

public class FSMTransitionPlan
{
    public GameState from;
    public GameState to;
    public object[] argsForNextState;

    public bool unloadPrevScene;

    public SCENES prevScene;
    public SCENES nextScene;

    public IEnumerable<IEnumerator> prepareTasks;
    public IEnumerable<IEnumerator> transitionTasks;

    public FSMTransitionPlan(
        GameState from,
        GameState to,
        object[] argsForNextState,
        bool unloadPrevScene,
        SCENES prevScene,
        SCENES nextScene,
        IEnumerable<IEnumerator> prepareTasks,
        IEnumerable<IEnumerator> transitionTasks)
    {
        this.from = from;
        this.to = to;
        this.argsForNextState = argsForNextState;
        this.unloadPrevScene = unloadPrevScene;
        this.prevScene = prevScene;
        this.nextScene = nextScene;
        this.prepareTasks = prepareTasks;
        this.transitionTasks = transitionTasks;
    }
}

public class TransitionState : GameStateBase
{
    public override void OnInitialize(GameManager _parent, GameState _state)
    {
        base.OnInitialize(_parent, _state);
    }

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        CoroutineRunner.Instance.RunCoroutine(TransitionRoutine(args[0] as FSMTransitionPlan));
    }

    IEnumerator TransitionRoutine(FSMTransitionPlan param)
    {
        // 씬 로드 처리 
        SceneLoadHandle sceneHandle = null;
        if (param.nextScene != SCENES.None)
        {
            sceneHandle = GameSceneManager.Instance.LoadSceneAsyncWithHandle(param.nextScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        // 다음 State 의 필요한 부분들 처리
        if (param.prepareTasks != null)
        {
            yield return CoroutineRunner.Instance.RunCoroutineParallel(param.prepareTasks);
        }

        // 씬 준비(90%) 와 PrepareTask 준비가 다 끝날때까지 대기
        while (sceneHandle != null && sceneHandle.IsReady == false)
        {
            yield return null;
        }

        if (param.transitionTasks != null)
        {
            // 실제 전환 작업 진행 (e.g UI의 등장, 퇴장 연출 등..)
            yield return CoroutineRunner.Instance.RunCoroutineParallel(param.transitionTasks);
        }

        if (sceneHandle != null)
        {
            sceneHandle.AllowActivate();
            yield return sceneHandle.Operation;
        }

        if (param.unloadPrevScene)
        {
            yield return GameSceneManager.Instance.UnloadAsync(param.prevScene);
        }

        yield return null;

        Parent.FSM.ChangeState(param.to, false, param.argsForNextState);
    }
}
