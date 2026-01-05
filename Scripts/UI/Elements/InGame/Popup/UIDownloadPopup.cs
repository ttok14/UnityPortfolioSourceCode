using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIDownloadPopup")]
public class UIDownloadPopup : PopupBase
{
    public enum Result
    {
        Confirm,
        Cancel
    }

    public class Arg : PopupShowArgBase
    {
        public string title;
        public string description;
        public string downloadSizeTxt;

        public Arg(string title, string description, string downloadSizeTxt, Action<PopupResultBase> resultReceiver) : base(resultReceiver)
        {
            this.title = title;
            this.description = description;
            this.downloadSizeTxt = downloadSizeTxt;
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

    [SerializeField]
    private TextMeshProUGUI _titleTxt;
    [SerializeField]
    private TextMeshProUGUI _descriptionTxt;
    [SerializeField]
    private TextMeshProUGUI _downloadSizeTxt;

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);
        var popupArg = arg as Arg;
        _titleTxt.text = popupArg.title;
        _descriptionTxt.text = popupArg.description;
        _downloadSizeTxt.text = popupArg.downloadSizeTxt;
    }

    public void OnClickXButton()
    {
        UIManager.Instance.Hide<UIDownloadPopup>();
        SendResult(new ResultArg(Result.Cancel));
    }

    public void OnClickConfirmBtn()
    {
        UIManager.Instance.Hide<UIDownloadPopup>();
        SendResult(new ResultArg(Result.Confirm));
    }
}
