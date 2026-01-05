using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDB;

public enum UIEntitySelectionTask
{
    None = 0,

    Purchase,
}

public class UIEntitySelectionElement : MonoBehaviour
{
    [Serializable]
    public class Group
    {
        public UIEntitySelectionTask taskType;
        public RectTransform root;
    }

    [SerializeField]
    private Image _imgIcon;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private List<Group> _groups;

    [SerializeField]
    private Image _imgCurrencyIcon;
    [SerializeField]
    private TextMeshProUGUI _txtPrice;
    [SerializeField]
    private TextMeshProUGUI _txtName;

    public uint EntityTID { get; private set; }
    public int Idx { get; private set; }

    Action<UIEntitySelectionElement> _onClicked;

    public void ShowPurchaseItem(uint entityTid, string iconKey, E_CurrencyType currencyType, int price, int idx, Action<UIEntitySelectionElement> onClicked)
    {
        EntityTID = entityTid;
        Idx = idx;

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(iconKey, (sprite) => _imgIcon.sprite = sprite).Forget();

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(DBCurrency.GetSpriteKey(currencyType), (sprite) => _imgCurrencyIcon.sprite = sprite).Forget();

        _txtName.text = DBEntity.GetName(entityTid);

        _txtPrice.text = price.ToString();

        for (int i = 0; i < _groups.Count; i++)
        {
            _groups[i].root.gameObject.SetActive(_groups[i].taskType == UIEntitySelectionTask.Purchase);
        }

        bool canAfford = Me.CanAfford(currencyType, price);
        _button.interactable = canAfford;
        _txtPrice.color = canAfford ? Color.white : Color.red;

        _onClicked = onClicked;
    }

    public void OnClicked()
    {
        if (_onClicked == null)
        {
            TEMP_Logger.Err($"No Event Handler linked");
            return;
        }

        _onClicked.Invoke(this);
    }

    public void Release()
    {
        EntityTID = 0;
        Idx = -1;

        _onClicked = null;
    }
}
