using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GameDBAccessorLoadProcessor : ILoadProcessor
{
    int _totalCount;
    int _currentCount;
    float _progress;
    LoadingProcessResult _result;

    public float Progress
    {
        get
        {
            if (_result == LoadingProcessResult.Success)
            {
                return 1;
            }

            return _progress;
        }
    }

    public string CurrentStatus
    {
        get
        {
            return $"Loading GameDBAccessors.. {_currentCount}/{_totalCount}";
        }
    }

    public LoadingProcessResult Result
    {
        get
        {
            return _result;
        }
    }

    public IEnumerator Process()
    {
        if (GameDBManager.Instance == null)
        {
            yield break;
        }

        yield return GameDBManager.Instance.LoadAccessorsTableReady((progressedCount) =>
        {
            _currentCount = progressedCount;
        });

        _result = LoadingProcessResult.Success;
    }
}
