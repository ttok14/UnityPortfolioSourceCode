using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIFlushFrame")]
public class UIFlushFrame : UIBase
{
    public class Arg : UIArgBase
    {
        public string txt;
    }

    [SerializeField]
    TextMeshProUGUI _text;

    [SerializeField]
    Image _imgBg;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        var uiArg = arg as Arg;

        _text.text = uiArg.txt;
        //_imgBg.sprite = uiArg.blueOrRead ? _spriteBlue : _spriteRed;

        MainThreadDispatcher.Instance.InvokeDelay(Hide, Constants.UI.ToastDuration);
    }
}
