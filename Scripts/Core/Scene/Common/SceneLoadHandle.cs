using UnityEngine;

public class SceneLoadHandle
{
    public readonly AsyncOperation Operation;

    public SceneLoadHandle(AsyncOperation operation)
    {
        Operation = operation;
    }

    public float Progress => Operation.progress;
    public int ProgressPercentage => (int)(Operation.progress * 100);
    public bool IsDone => Operation.isDone;
    public bool IsReady => Progress >= 0.9f;

    /// <summary>
    /// 씬을 실제로 활성화해도 된다면 호출 해주어야함.
    /// </summary>
    public void AllowActivate()
    {
        Operation.allowSceneActivation = true;
    }
}
