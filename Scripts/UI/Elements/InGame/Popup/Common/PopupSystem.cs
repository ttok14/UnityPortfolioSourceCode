using System;
using System.Collections;
using UnityEngine;

public static class PopupSystem
{
    public static void Show<T>(PopupShowArgBase arg = null, Action<T> onCompleted = null) where T : PopupBase
    {
        if (UIManager.HasInstance == false)
        {
            onCompleted?.Invoke(null);
            return;
        }

        UIManager.Instance.ShowCallBack<T>(UITrigger.Default, arg, onCompleted).Forget();
    }

    public static void ShowSimpleDialoguePopup(UISimpleDialoguePopup.Arg arg, Action<UISimpleDialoguePopup> onCompleted = null)
    {
        if (UIManager.HasInstance == false)
        {
            return;
        }

        UIManager.Instance.ShowCallBack<UISimpleDialoguePopup>(UITrigger.Default, arg, onCompleted).Forget();
    }

    public static void ShowDownloadAskPopup(UIDownloadPopup.Arg arg, Action<UIDownloadPopup> onCompleted = null)
    {
        if (UIManager.HasInstance == false)
        {
            return;
        }

        UIManager.Instance.ShowCallBack<UIDownloadPopup>(UITrigger.Default, arg, onCompleted).Forget();
    }
}
