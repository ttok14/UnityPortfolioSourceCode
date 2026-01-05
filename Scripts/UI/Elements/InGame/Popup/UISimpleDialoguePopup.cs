using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UISimpleDialoguePopup")]
public class UISimpleDialoguePopup : PopupBase
{
    public enum Result
    {
        Confirm,
        Cancel
    }

    [System.Flags]
    public enum ButtonFlags
    {
        None = 0,
        Confirm = 0x1,
        Cancel = 0x1 << 1,
        Close = 0x1 << 2,

        All = Confirm | Cancel
    }

    [SerializeField]
    private JButton _closeBtn;
    [SerializeField]
    private JButton _confirmBtn;
    [SerializeField]
    private JButton _cancelBtn;
    [SerializeField]
    private TextMeshProUGUI _titleTxt;
    [SerializeField]
    private TextMeshProUGUI _contentTxt;

    public class Arg : PopupShowArgBase
    {
        public string title;
        public string content;
        public ButtonFlags btnFlags;

        public Arg(string title, string content, ButtonFlags btnFlags = ButtonFlags.All, Action<PopupResultBase> resultReceiver = null) : base(resultReceiver)
        {
            this.title = title;
            this.content = content;
            this.btnFlags = btnFlags;
        }
    }

    public class ResultArg : PopupResultBase
    {
        public Result result;

        public ResultArg(Result result)
        {
            this.result = result;
        }
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        var popupArg = arg as Arg;
        _titleTxt.text = popupArg.title;
        _contentTxt.text = popupArg.content;
        _confirmBtn.gameObject.SetActive(popupArg.btnFlags.HasFlag(ButtonFlags.Confirm));
        _cancelBtn.gameObject.SetActive(popupArg.btnFlags.HasFlag(ButtonFlags.Cancel));
        _closeBtn.gameObject.SetActive(popupArg.btnFlags.HasFlag(ButtonFlags.Close));
    }

    public void OnClickXButton()
    {
        UIManager.Instance.Hide<UISimpleDialoguePopup>();
        SendResult(new ResultArg(Result.Cancel));
    }

    public void OnClickConfirmBtn()
    {
        UIManager.Instance.Hide<UISimpleDialoguePopup>();
        SendResult(new ResultArg(Result.Confirm));
    }

    public void OnClickCancelBtn()
    {
        UIManager.Instance.Hide<UISimpleDialoguePopup>();
        SendResult(new ResultArg(Result.Cancel));
    }
}
