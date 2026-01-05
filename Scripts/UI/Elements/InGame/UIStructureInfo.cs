using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[UIAttribute("UIStructureInfo")]
public class UIStructureInfo : UIBase
{
    [Serializable]
    public class Stat
    {
        public Transform root;
        public Image imgIcon;
        public TextMeshProUGUI txtAmount;
    }

    [SerializeField]
    private TextMeshProUGUI _title;

    [SerializeField]
    private GameObject _spawnerRoot;
    //[SerializeField]
    //private Image _imgUpgrade;
    //[SerializeField]
    //private TextMeshProUGUI _upgradeCost;
    [SerializeField]
    private Image _spawnerModeBg;
    [SerializeField]
    private TextMeshProUGUI _spawnerModeTxt;

    [SerializeField]
    private Image _imgIcon;

    [SerializeField]
    private TextMeshProUGUI _subTitle;

    [SerializeField]
    private TextMeshProUGUI _Description;

    [SerializeField]
    private Stat[] _stats;

    StructureEntity _entity;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
    }

    public override void Hide()
    {
        base.Hide();

        _entity = null;
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        var uiArg = arg as EntityInteractionUIArg;
        if (uiArg == null)
        {
            TEMP_Logger.Err($"Wrong argument : {arg}");
            Hide();
            return;
        }

        InGameManager.Instance.EventListener += OnInGameEvent;

        for (int i = 0; i < _stats.Length; i++)
        {
            _stats[i].root.gameObject.SetActive(false);
        }

        _entity = uiArg.entity as StructureEntity;
        var entityData = DBEntity.Get(uiArg.entity.EntityTID);
        var statData = uiArg.entity.GetData(EntityDataCategory.Stat) as EntityStatData;
        var structureData = DBStructure.Get(entityData.DetailTableID);
        // var statData = DBStat.Get(structureData.StatID);
        DBStat.GetFinalStatAtLevel(entityData.StatTableID, statData.Level, out uint atk, out var atkSpeed, out uint maxHp);

        UpdateSpawnerUI();

        //if (structureData.UpgradeCostCurrencyType != GameDB.E_CurrencyType.None)
        //{
        //    AssetManager.Instance.LoadAsyncCallBack<Sprite>(DBCurrency.GetSpriteKey(structureData.UpgradeCostCurrencyType), (sprite) =>
        //    {
        //        _imgUpgrade.sprite = sprite;
        //        _upgradeCost.text = structureData.UpgradeCost.ToString();
        //        _upgradeRoot.SetActive(true);
        //    }).Forget();
        //}

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(entityData.IconKey, (sprite) =>
        {
            _imgIcon.sprite = sprite;
        }).Forget();

        _title.text = entityData.Name;
        _subTitle.text = TempLocale.To(structureData.StructureType);
        _Description.text = structureData.Description;

        int statIdx = 0;

        if (statData.CurrentHP > 0)
            ShowStat(statIdx++, "Icon_HP", statData.CurrentHP.ToString());

        if (atk > 0)
            ShowStat(statIdx++, "Icon_Atk", atk.ToString());

        if (structureData.GenResourceBaseAmount > 0)
        {
            ShowStat(statIdx++,
                DBCurrency.GetSpriteKey(GameDB.E_CurrencyType.Gold),
                DBStructure.GetFinalGenCurrencyAmountAtLevel(entityData.DetailTableID, statData.Level).ToString());
        }
    }

    public override void OnHide(UIArgBase arg)
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;

        base.OnHide(arg);
    }

    private void UpdateSpawnerUI()
    {
        bool isSpawner = _entity.StructureData.StructureType == GameDB.E_StructureType.Spawner;

        if (isSpawner == false)
        {
            if (_spawnerRoot)
                _spawnerRoot.SetActive(false);

            return;
        }

        _spawnerRoot.SetActive(true);

        var spawner = _entity as SpawnerStructureEntity;

        switch (spawner.SpawnerMode)
        {
            case SpawnerMode.Defensive:
                _spawnerModeTxt.text = "수비 모드";
                _spawnerModeBg.color = Color.green;
                break;
            case SpawnerMode.Aggresive:
                _spawnerModeTxt.text = "공격 모드";
                _spawnerModeBg.color = Color.red;
                break;
            default:
                TEMP_Logger.Err($"Not Implemented Type : {spawner.SpawnerMode}");
                break;
        }
    }

    void ShowStat(int idx, string spriteKey, string amount)
    {
        AssetManager.Instance.LoadAsyncCallBack<Sprite>(spriteKey, (sprite) =>
        {
            _stats[idx].imgIcon.sprite = sprite;
            _stats[idx].txtAmount.text = amount;
            _stats[idx].root.gameObject.SetActive(true);
        }).Forget();
    }

    void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.Enter)
        {
            Hide();
        }
    }

    public void OnClickUpgradeBtn()
    {

    }

    public void OnClickSpawnerMode()
    {
        var spawner = _entity as SpawnerStructureEntity;
        spawner.SetSpawnerMode(spawner.SpawnerMode == SpawnerMode.Defensive ? SpawnerMode.Aggresive : SpawnerMode.Defensive);
        UpdateSpawnerUI();
    }
}
