using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoroutineRunner : SingletonBase<CoroutineRunner>
{
    Dictionary<float, WaitForSeconds> _waitForSecondsCache = new Dictionary<float, WaitForSeconds>();
    public WaitForSeconds WaitForSeconds(float seconds)
    {
        if (_waitForSecondsCache.TryGetValue(seconds, out var wfs))
            return wfs;
        wfs = new WaitForSeconds(seconds);
        _waitForSecondsCache[seconds] = wfs;
        return wfs;
    }

    public Coroutine RunCoroutine(IEnumerator coroutine, float delay = 0f, Action onCompleted = null)
    {
        //TEMP_Logger.Deb($"RunCoroutine");
        return StartCoroutine(WrapDelayDuration(coroutine, delay, onCompleted));
    }

    public Coroutine RunCoroutineFrameDelay(int delayCount, Action routine, Action onCompleted = null)
    {
        //TEMP_Logger.Deb($"RunCoroutineFrameDelay");
        return StartCoroutine(WrapDelayFrameCount(delayCount, routine, onCompleted));
    }

    public Coroutine RunCoroutineParallel(IEnumerable<IEnumerator> coroutines)
    {
        //TEMP_Logger.Deb($"RunCoroutineParallel");
        return StartCoroutine(RunCoroutineParallelInternal(null, coroutines));
    }

    public Coroutine RunCoroutineParallel(Action onCompleted, IEnumerable<IEnumerator> coroutines)
    {
        //TEMP_Logger.Deb($"RunCoroutineParallel");
        return StartCoroutine(RunCoroutineParallelInternal(onCompleted, coroutines));
    }

    public void Stop(params Coroutine[] coroutines)
    {
        foreach (var co in coroutines)
        {
            StopCoroutine(co);
        }
    }

    #region ===:: Internal ::===

    private IEnumerator RunCoroutineParallelInternal(Action onCompleted, IEnumerable<IEnumerator> coroutines)
    {
        if (coroutines == null || coroutines.Count() == 0)
        {
            onCompleted?.Invoke();
            yield break;
        }

        int cnt = coroutines.Count();
        var coArr = new Coroutine[cnt];
        var done = new bool[cnt];

        for (int i = 0; i < cnt; i++)
        {
            int idx = i;
            coArr[i] = RunCoroutine(Wrap(coroutines.ElementAt(i), () => done[idx] = true));
        }

        while (done.Any(t => t == false))
        {
            yield return null;
        }

        onCompleted?.Invoke();
    }

    private IEnumerator Wrap(IEnumerator routine, Action onCompleted)
    {
        yield return routine;
        onCompleted?.Invoke();
    }

    private IEnumerator WrapDelayDuration(IEnumerator coroutine, float duration, Action onCompleted)
    {
        yield return WaitForSeconds(duration);
        yield return coroutine;
        onCompleted?.Invoke();
    }

    private IEnumerator WrapDelayFrameCount(int delayCount, Action routine, Action onCompleted)
    {
        int remained = delayCount;
        while (remained > 0)
        {
            remained--;
            yield return null;
        }

        routine();
        onCompleted?.Invoke();
    }

    #endregion
}
