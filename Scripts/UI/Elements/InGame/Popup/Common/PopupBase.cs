using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class PopupShowArgBase : UIArgBase
{
    public Action<PopupResultBase> onResultReceived;

    public PopupShowArgBase(Action<PopupResultBase> resultReceiver)
    {
        onResultReceived = resultReceiver;
    }
}
public class PopupResultBase { }

[RequireComponent(typeof(RectTransform))]
public abstract class PopupBase : UIBase
{
    protected PopupShowArgBase _arg;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);
        _arg = arg != null ? arg as PopupShowArgBase : null;
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);
    }

    protected void SendResult(PopupResultBase resultArg)
    {
        _arg.onResultReceived?.Invoke(resultArg);
    }
}
