using System;
using System.Collections;

public interface ILoadProcessor
{
    float Progress { get; }
    string CurrentStatus { get; }
    LoadingProcessResult Result { get; }
    IEnumerator Process();
}
