using UnityEngine;

public class TimeManager : SingletonBase<TimeManager>
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
    }
}
