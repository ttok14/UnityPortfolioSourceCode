using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UICenterToast")]
public class UICenterToast : UIBase
{
    public class Arg : UIArgBase
    {
        public string txt;
    }

    [SerializeField]
    private TextMeshProUGUI _toastTxt;

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        _toastTxt.text = (arg as Arg).txt;
    }
}
