using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(nameof(UICenterStaticToast))]
public class UICenterStaticToast : UIBase
{
    [System.Flags]
    public enum Type
    {
        None = 0,

        Informative,
        Warning,
    }

    public class Arg : UIArgBase
    {
        public string txt;
        public Type type;
    }

    [SerializeField]
    private TextMeshProUGUI _informativeToastTxt;
    [SerializeField]
    private TextMeshProUGUI _warningToastTxt;

    [SerializeField]
    List<GameObject> _informativeObjects;

    [SerializeField]
    List<GameObject> _warningObjects;

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        var arg = argBase as Arg;

        SetActiveGameObjects(_informativeObjects, arg.type == Type.Informative);
        SetActiveGameObjects(_warningObjects, arg.type == Type.Warning);

        if (arg.type == Type.Informative)
            _informativeToastTxt.SetText(arg.txt);
        else if (arg.type == Type.Warning)
            _warningToastTxt.SetText(arg.txt);
        else
        {
            TEMP_Logger.Err($"Not implemented type : {arg.type}");
            Hide();
            return;
        }

        MainThreadDispatcher.Instance.InvokeDelay(() =>
        {
            if (IsEnabled)
                Hide();
        }, 6f);
    }

    void SetActiveGameObjects(List<GameObject> list, bool isActive)
    {
        foreach (var go in list)
        {
            if (go)
                go.SetActive(isActive);
        }
    }
}
