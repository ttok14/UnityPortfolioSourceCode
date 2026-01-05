using System;

/// <summary>
///  UI 의 정렬 설정을 위해 필요한 모든 설정값 
/// </summary>
[Serializable]
public class UISortPolicy
{
    public enum Behaviour
    {
        Default,
        Manual
    }

    public UILayer layer;
    public Behaviour behaviour;
}
