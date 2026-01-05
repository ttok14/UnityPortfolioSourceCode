using System;
using UnityEngine;

public class UIToastArg : UIArgBase
{
    public string msg;
}

public static class UIToastSystem
{
    public enum ToastType
    {
        None = 0,

        BriefSide,
        Center_DynamicInformative,
        Center_StaticInformaitve,
        Center_StaticWarning,
        Center_Joyful,
        Center_Despair,
    }

    public static void ShowToast(ToastType type, string msg)
    {
        if (type == ToastType.BriefSide)
        {
            UIManager.Instance.ShowCallBack<UIToastSide>(UITrigger.Default, new UIToastArg() { msg = msg }).Forget();
        }
        else if (type == ToastType.Center_DynamicInformative)
        {
            UIManager.Instance.ShowCallBack<UICenterToast>(UITrigger.Default, new UICenterToast.Arg() { txt = msg }).Forget();
        }
        else if (type == ToastType.Center_Joyful)
        {
            UIManager.Instance.ShowCallBack<UIToastJoyful>(UITrigger.Default, new UIToastArg { msg = msg }).Forget();
        }
        else if (type == ToastType.Center_Despair)
        {
            UIManager.Instance.ShowCallBack<UIToastDespair>(UITrigger.Default, new UIToastArg { msg = msg }).Forget();
        }
        else if (type == ToastType.Center_StaticInformaitve)
        {
            UIManager.Instance.ShowCallBack<UICenterStaticToast>(UITrigger.Default, new UICenterStaticToast.Arg()
            {
                type = UICenterStaticToast.Type.Informative,
                txt = msg,
            }).Forget();
        }
        else if (type == ToastType.Center_StaticWarning)
        {
            UIManager.Instance.ShowCallBack<UICenterStaticToast>(UITrigger.Default, new UICenterStaticToast.Arg()
            {
                type = UICenterStaticToast.Type.Warning,
                txt = msg,
            }).Forget();
        }
        else
        {
            TEMP_Logger.Err($"not imlmented type : {type}");
        }
    }

    public static void Hide(ToastType type)
    {
        if (type == ToastType.BriefSide)
        {
            UIManager.Instance.Hide<UIToastSide>();
        }
        else if (type == ToastType.Center_DynamicInformative)
        {
            UIManager.Instance.Hide<UICenterToast>();
        }
        else
        {
            TEMP_Logger.Err($"not imlmented type : {type}");
        }
    }
}
