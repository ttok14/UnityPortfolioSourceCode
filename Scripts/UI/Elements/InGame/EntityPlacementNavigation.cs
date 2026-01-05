using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("EntityPlacementNavigation")]
public class EntityPlacementNavigation : UIBase
{
    public class Arg : UIArgBase
    {
        public uint entityTid;
        public Vector3 initialWorldPos;

        public bool initialCanPlace;

        public Action rotateHandler;
        public Action<EntityPlacementNavigationResult> onResultReceived;
    }

    [SerializeField]
    Image _costIcon;
    [SerializeField]
    TextMeshProUGUI _priceTxt;

    [SerializeField]
    JButton _confirmBtn;

    [SerializeField]
    RectTransform _uiGroup;

    Arg _arg;

    bool _canPlace;
    public bool CanPlace
    {
        set
        {
            _canPlace = value;
            UpdateUIActive();
        }
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        _arg = arg as Arg;
        if (_arg == null)
        {
            TEMP_Logger.Err($"Argument Invalid");
            Return();
        }

        var costInfo = DBPurchaseCost.GetByEntityID(_arg.entityTid);
        if (costInfo == null)
        {
            TEMP_Logger.Err($"Failed to get cost Info By EntityTid | EntityID : {_arg.entityTid}");
            Return();
        }

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(DBCurrency.GetSpriteKey(costInfo.CostCurrencyType), (sprite) =>
        {
            _costIcon.sprite = sprite;
        }).Forget();

        _priceTxt.text = costInfo.CostPrice.ToString();

        CanPlace = _arg.initialCanPlace;

        SetEntityPosition(_arg.initialWorldPos);
        UpdateUIActive();
    }

    public void SetEntityPosition(Vector3 entityPosition)
    {
        _uiGroup.anchoredPosition = UIHelper.GetAnchorPositionFromWorldPosition(
            CameraManager.Instance.MainCam,
            CameraManager.Instance.GetUICamera(SortPolicy.layer),
            entityPosition,
            _uiGroup.parent as RectTransform);
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        _confirmBtn.interactable = true;
        _arg = null;
        _canPlace = true;
    }

    public void OnClickConfirm()
    {
        SendResult(EntityPlacementNavigationResult.Confirm);
        if (IsEnabled)
            Hide();
    }

    public void OnClickCancel()
    {
        SendResult(EntityPlacementNavigationResult.Cancel);
        if (IsEnabled)
            Hide();
    }

    public void OnClickRotate()
    {
        _arg.rotateHandler.Invoke();
        UpdateUIActive();
    }

    void UpdateUIActive()
    {
        _confirmBtn.interactable = _canPlace;
    }

    protected void SendResult(EntityPlacementNavigationResult result)
    {
        _arg.onResultReceived?.Invoke(result);
    }
}
