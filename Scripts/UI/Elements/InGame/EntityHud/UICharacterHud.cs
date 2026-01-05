using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UICharacterHud")]
public class UICharacterHud : UIWorldFollowBase
{
    public class Arg : WorldFollowArg
    {
        public ulong entityID;
        public bool autoHide;

        public Arg(ulong entityID, bool autoHide, Transform followTarget, Vector2 uiOffsetPos) : base(followTarget, uiOffsetPos)
        {
            this.entityID = entityID;
            this.autoHide = autoHide;
        }
        public Arg() { }
    }

    [SerializeField]
    private Slider _healthBar;

    [SerializeField]
    Sprite _playerTeamFillSprite;
    [SerializeField]
    Sprite _enemyTeamFillSprite;

    [SerializeField]
    Image _fillImg;

    EntityBase _targetEntity;

    ulong _validId;

    bool _shouldShow;
    protected override bool ShouldShow => _shouldShow;

    float _hpNotUpdatedElapsedTime;
    float _autoHideTimeAt;

    //---------//
    EntityEventDelegates.OnHPChanged _hpChangedAction;
    EntityEventDelegates.OnDied _targetDiedAction;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _hpChangedAction = OnHPChanged;
        _targetDiedAction = OnTargetDied;
    }

    private void OnTargetDied(ulong attackerId, Vector3 attackerPosition)
    {
        if (IsEnabled)
            Hide();
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        var uiArg = arg as Arg;
        if (uiArg == null)
        {
            TEMP_Logger.Err($"Wrong argument : {arg}");
            Hide();
            return;
        }

        _targetEntity = EntityManager.Instance.GetEntity(uiArg.entityID);

        if (_targetEntity == null)
        {
            Hide();
            return;
        }

#if UNITY_EDITOR
        gameObject.name = $"{nameof(UICharacterHud)}_{uiArg.entityID}";
#endif

        _validId = uiArg.entityID;

        _autoHideTimeAt = uiArg.autoHide ? 4f : 0f;

        _fillImg.sprite = _targetEntity.Team == EntityTeamType.Player ? _playerTeamFillSprite : _enemyTeamFillSprite;

        var stat = _targetEntity.GetData(EntityDataCategory.Stat) as EntityStatData;
        float hpRatio = (float)stat.CurrentHP / stat.MaxHp;
        SetHpValue(hpRatio);

        _shouldShow = hpRatio > 0 && hpRatio < 1f;

        _targetEntity.HpChangedListener += _hpChangedAction;
        _targetEntity.DiedListener += _targetDiedAction;
    }

    protected override void OnPersistentUpdate()
    {
        base.OnPersistentUpdate();

        if (_autoHideTimeAt > 0f)
        {
            _hpNotUpdatedElapsedTime += Time.deltaTime;

            if (_hpNotUpdatedElapsedTime >= _autoHideTimeAt)
                Hide();
        }
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        if (_targetEntity)
        {
            (_targetEntity as CharacterEntity).HudRemoved();
            _targetEntity.HpChangedListener -= _hpChangedAction;
            _targetEntity.DiedListener -= _targetDiedAction;
        }

        SetHpValue(1f);
        _targetEntity = null;
        _hpNotUpdatedElapsedTime = 0f;
        _autoHideTimeAt = 0f;
        _shouldShow = false;
    }

    void SetHpValue(float value)
    {
        _healthBar.value = value;
    }

    void OnHPChanged(int maxHP, int currentHP, int diff)
    {
        if (IsEnabled == false)
            return;

        _hpNotUpdatedElapsedTime = 0f;

        if (currentHP == 0)
        {
            Hide();
            return;
        }

        if (currentHP == maxHP)
        {
            _shouldShow = false;
        }
        else
        {
            _shouldShow = true;

            SetHpValue((float)currentHP / maxHP);
        }
    }
}
