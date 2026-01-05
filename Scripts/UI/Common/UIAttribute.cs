using System;

// 런타임에 stripping 방지 . 
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[UnityEngine.Scripting.Preserve]
public class UIAttribute : Attribute
{
    public string key;
    // public bool useBackBtn;

    public UIAttribute(string key/*, bool useBackBtn */)
    {
        this.key = key;
        // this.useBackBtn = useBackBtn;
    }
}
