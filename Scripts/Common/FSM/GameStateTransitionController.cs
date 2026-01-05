using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class GameStateTransitionController
{
    private MainStatetFSM _fsm;

    public GameStateTransitionController(MainStatetFSM fsm)
    {
        _fsm = fsm;
    }

    /// <summary>
    /// 로딩을 이용해 상태를 전환한다. (목적 State 로 전환되는 중간에 Loading 을 거침)
    /// </summary>
    /// <param name="nextState"></param>
    /// <param name="loadProcessor"></param>
    /// <param name="prepareTasks"></param>
    /// <param name="additionalTransitTasks"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public IEnumerator TransitionStateWithLoading(
        GameState nextState,
        ILoadProcessor loadProcessor,
        IEnumerable<IEnumerator> prepareTasks = null,
        IEnumerable<IEnumerator> additionalTransitTasks = null,
        Action onCompleted = null)
    {
        var metaData = GameManager.Instance.MetaDataBase as GameMetaData;
        var currentStateData = metaData.Find(_fsm.Current_State);
        var transitionTaskList = new List<IEnumerator>();

        AppendHideCurrentUI(transitionTaskList);

        transitionTaskList.Add(UIManager.Instance.ShowCo<UILoadingPanel>(
            arg: new UILoadingPanel.Arg(
                progressGetter: () => loadProcessor.Progress,
                statusGetter: () => loadProcessor.CurrentStatus)));

        if (additionalTransitTasks != null)
        {
            transitionTaskList.AddRange(additionalTransitTasks);
        }

        yield return FSMTransitionPlanFactory.Build(
               _fsm.Current_State,
               GameState.Loading,
               prepareTasks: prepareTasks,
               transitionTasks: transitionTaskList,
               argsForNextState: new object[] { new LoadingState.Arg(nextState, loadProcessor) },
               onCompleted: (plan) =>
               {
                   GameManager.Instance.FSM.ChangeState(GameState.TransitionState, false, new object[] { plan });
                   onCompleted?.Invoke();
               });
    }

    public IEnumerator TransitionState(
        GameState nextState,
        IEnumerable<IEnumerator> prepareTasks = null,
        IEnumerable<IEnumerator> additionalTransitTasks = null,
        Dictionary<Type, UIArgBase> uiArgs = null,
        object[] argsForNextState = null,
        Action onCompleted = null)
    {
        var metaData = GameManager.Instance.MetaDataBase as GameMetaData;
        var currentStateData = metaData.Find(_fsm.Current_State);
        var nextMetaData = metaData.Find(nextState);
        var transitionTaskList = new List<IEnumerator>();

        AppendHideCurrentUI(transitionTaskList);

        foreach (var uiType in nextMetaData.UITypes)
        {
            UIArgBase arg = null;
            if (uiArgs != null)
            {
                uiArgs.TryGetValue(uiType, out arg);
            }

            IEnumerator uniToCo = UniTask.ToCoroutine(async () =>
            {
                await UIManager.Instance.ShowAsync(uiType, UITrigger.Default, arg: arg);
            });

            transitionTaskList.Add(uniToCo);
        }

        if (additionalTransitTasks != null)
        {
            transitionTaskList.AddRange(additionalTransitTasks);
        }

        yield return FSMTransitionPlanFactory.Build(
               _fsm.Current_State,
               nextState,
               prepareTasks: prepareTasks,
               transitionTasks: transitionTaskList,
               argsForNextState: argsForNextState,
               onCompleted: (plan) =>
               {
                   GameManager.Instance.FSM.ChangeState(GameState.TransitionState, false, new object[] { plan });
                   onCompleted?.Invoke();
               });
    }

    private void AppendHideCurrentUI(List<IEnumerator> taskList)
    {
        var metaData = GameManager.Instance.MetaDataBase as GameMetaData;
        var currentStateData = metaData.Find(_fsm.Current_State);

        foreach (var type in currentStateData.UITypes)
        {
            var t = type;
            var uniToCo = UniTask.ToCoroutine(async () =>
            {
                await UIManager.Instance.HideAsync(t);
            });

            taskList.Add(uniToCo);
        }
    }
}
