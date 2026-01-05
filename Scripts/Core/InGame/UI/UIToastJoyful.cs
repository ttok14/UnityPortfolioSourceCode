using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIToastJoyful")]
public class UIToastJoyful : UIBase
{
    [SerializeField]
    TextMeshProUGUI _text;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
    }

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        var arg = argBase as UIToastArg;

        _text.text = arg.msg;

        // 중간에 누구도 끄지 못해야하는데. 
        MainThreadDispatcher.Instance.InvokeDelay(Hide, Constants.UI.LongToastDuration);
    }
}
