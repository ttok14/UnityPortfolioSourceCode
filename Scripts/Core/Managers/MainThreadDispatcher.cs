using UnityEngine;
using System;
using System.Collections.Generic;

public class MainThreadDispatcher : SingletonBase<MainThreadDispatcher>
{
    Queue<Action> _instantCallbacks = new Queue<Action>();
    List<(Action, float)> _timeDelayCallbacks = new List<(Action, float)>();
    List<(Action, int)> _frameDelayCallbacks = new List<(Action, int)>();

    object _lock = new object();

    private void Update()
    {
        while (_instantCallbacks.Count > 0)
        {
            _instantCallbacks.Dequeue().Invoke();
        }

        for (int i = _timeDelayCallbacks.Count - 1; i >= 0; i--)
        {
            var cb = _timeDelayCallbacks[i];
            if (Time.time > cb.Item2)
            {
                cb.Item1.Invoke();
                _timeDelayCallbacks.RemoveAt(i);
            }
        }

        for (int i = _frameDelayCallbacks.Count - 1; i >= 0; i--)
        {
            var cb = _frameDelayCallbacks[i];
            if (Time.frameCount >= cb.Item2)
            {
                cb.Item1.Invoke();
                _frameDelayCallbacks.RemoveAt(i);
            }
        }
    }

    public void Invoke(Action action)
    {
        if (action == null)
            return;

        lock (_lock)
        {
            _instantCallbacks.Enqueue(action);
        }
    }

    public void InvokeDelay(Action action, float delay)
    {
        if (action == null)
            return;

        lock (_lock)
        {
            _timeDelayCallbacks.Add((action, Time.time + delay));
        }
    }

    public void InvokeInFrames(Action action, int framesToJump)
    {
        if (action == null)
            return;

        lock (_lock)
        {
            _frameDelayCallbacks.Add((action, Time.frameCount + framesToJump));
        }
    }
}
