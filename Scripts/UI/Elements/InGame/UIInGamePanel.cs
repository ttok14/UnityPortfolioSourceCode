using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using GameDB;
using Cysharp.Threading.Tasks;

[UIAttribute("UIInGamePanel")]
public class UIInGamePanel : UIBase
{
    [Serializable]
    public class CurrencyGroup
    {
        public Image icon;
        public TextMeshProUGUI amount;
    }

    [SerializeField]
    List<GameObject> activatelistOnEnter;

    [SerializeField]
    CurrencyGroup _gold;
    [SerializeField]
    CurrencyGroup _wood;
    [SerializeField]
    CurrencyGroup _food;

    LayoutGroup[] _allLayoutGroups;
    Action _disableAllLayoutAction;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _allLayoutGroups = GetComponentsInChildren<LayoutGroup>();

        _disableAllLayoutAction = () => SetEnableLayoutGroups(false);
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg = null)
    {
        base.OnShow(trigger, arg);
        activatelistOnEnter.ForEach(t => t.SetActive(false));

        SetEnableLayoutGroups(true);

        InitCurrency(E_CurrencyType.Gold, _gold, Me.Data.Currency.Gold);
        InitCurrency(E_CurrencyType.Wood, _wood, Me.Data.Currency.Wood);
        InitCurrency(E_CurrencyType.Food, _food, Me.Data.Currency.Food);

        Me.CurrencyModifiedListener += OnCurrencyModified;

        MainThreadDispatcher.Instance.InvokeInFrames(_disableAllLayoutAction, 2);
    }

    void InitCurrency(E_CurrencyType type, CurrencyGroup group, int amount)
    {
        AssetManager.Instance.LoadAsyncCallBack<Sprite>(DBCurrency.GetSpriteKey(type), (sprite) =>
        {
            group.icon.sprite = sprite;
        }).Forget();

        UpdateCurrencyAmount(group.amount, amount);
    }

    void UpdateCurrencyAmount(TextMeshProUGUI text, int amount)
    {
        text.SetText("{0}", amount);
    }

    void OnCurrencyModified(uint currencyId, int prev, int current)
    {
        var type = DBCurrency.GetCurrencyType(currencyId);
        var group = GetCurrencyGroup(type);

        UpdateCurrencyAmount(group.amount, current);
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        ReleaseCurrency(_gold);
        ReleaseCurrency(_wood);
        ReleaseCurrency(_food);

        Me.CurrencyModifiedListener -= OnCurrencyModified;
    }

    CurrencyGroup GetCurrencyGroup(E_CurrencyType type)
    {
        switch (type)
        {
            case E_CurrencyType.Gold:
                return _gold;
            case E_CurrencyType.Wood:
                return _wood;
            case E_CurrencyType.Food:
                return _food;
            default:
                TEMP_Logger.Err($"Not implmented Type : {type}");
                break;
        }

        return null;
    }

    void SetEnableLayoutGroups(bool enabled)
    {
        if (_allLayoutGroups == null)
            return;

        foreach (var lg in _allLayoutGroups)
        {
            if (lg)
            {
                lg.enabled = enabled;
            }
        }
    }

    private void ReleaseCurrency(CurrencyGroup group)
    {
        group.icon.sprite = null;
    }
}
