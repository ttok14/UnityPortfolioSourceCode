using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSimulationProcessor : ILoadProcessor
{
    float _progress;
    string _curStatus = "Loading..";
    LoadingProcessResult _result = LoadingProcessResult.None;

    public float Progress => _progress;

    public string CurrentStatus => _curStatus;

    public LoadingProcessResult Result => _result;

    public IEnumerator Process()
    {
        _progress = 0f;

        int randomStep = UnityEngine.Random.Range(3, 10);
        for (int i = 0; i < randomStep; i++)
        {
            float interval = UnityEngine.Random.Range(0f, 1f);
            yield return new WaitForSeconds(interval);
            _progress = i / (float)randomStep;

            _curStatus = $"Loading Data{i} . .";
        }

        _result = LoadingProcessResult.Success;
        _progress = 1f;
    }
}
