using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class FSMTransitionPlanFactory
{
    public static IEnumerator Build(
        GameState prev,
        GameState next,
        IEnumerable<IEnumerator> prepareTasks = null,
        IEnumerable<IEnumerator> transitionTasks = null,
        object[] argsForNextState = null,
        Action<FSMTransitionPlan> onCompleted = null)
    {
        var metaData = GameManager.Instance.MetaDataBase as GameMetaData;
        var prevStateData = metaData.Find(prev);
        var nextStateData = metaData.Find(next);

        // 다음에 보여주어야 하는 UI 는 미리 생성 
        yield return CoroutineRunner.Instance.RunCoroutineParallel(nextStateData.UITypes.Select(t => UIManager.Instance.PrepareCo(t)).ToArray());

        onCompleted?.Invoke(
            new FSMTransitionPlan(
                from: prev,
                to: next,
                argsForNextState,
                prev != GameState.BootStrap,
                prevScene: prevStateData.scene,
                nextScene: nextStateData.scene,
                prepareTasks: prepareTasks,
                transitionTasks: transitionTasks));
    }
}
