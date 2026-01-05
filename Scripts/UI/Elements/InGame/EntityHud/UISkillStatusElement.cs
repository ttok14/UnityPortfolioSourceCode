using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(nameof(UISkillStatusElement))]
public class UISkillStatusElement : UIBase
{
    public class Arg : UIArgBase
    {
        public EntityBase owner;
        public EntitySkillBase skillRef;

        public Arg(EntityBase owner, EntitySkillBase skillRef)
        {
            this.owner = owner;
            this.skillRef = skillRef;
        }
    }

    [SerializeField]
    private RectTransform _uiGroup;

    [SerializeField]
    Image _iconImg;

    [SerializeField]
    private List<Graphic> _skillReadyElements;

    [SerializeField]
    private List<Graphic> _skillNotReadyElements;

    [SerializeField]
    private Image _cooltimeFillImg;

    [SerializeField]
    private Image _lockIcon;

    [SerializeField]
    private TextMeshProUGUI _skillCooltimeTxt;

    [SerializeField]
    private LayoutElement _layoutElement;

    [SerializeField]
    private JButton _triggerButton;

    bool _lastSkillReady;
    float _lastSkillCooltime;

    EntitySkillBase _skillRef;
    EntityBase _owner;

    const int UpdateFrameInterval = 3;

    uint _skillValidCheckId;

    bool IsValid => _skillRef != null && _skillRef.PoolableInstanceValidID == _skillValidCheckId && EntityHelper.IsValid(_owner);

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        var arg = argBase as Arg;

        _skillRef = arg.skillRef;
        _owner = arg.owner;

        _layoutElement.preferredWidth = _skillRef.TableData.SkillCategory == GameDB.E_SkillCategoryType.Standard ? 200 : 300;
        _layoutElement.preferredHeight = _skillRef.TableData.SkillCategory == GameDB.E_SkillCategoryType.Standard ? 200 : 300;

        _skillValidCheckId = arg.skillRef.PoolableInstanceValidID;

        _lockIcon.enabled = _skillRef.TableData.SkillCategory == GameDB.E_SkillCategoryType.Standard;

        _triggerButton.enabled = _skillRef.TableData.SkillCategory == GameDB.E_SkillCategoryType.Spell;

        _uiGroup.gameObject.SetActive(false);

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(_skillRef.TableData.IconKey, (res) =>
        {
            if (IsEnabled == false)
            {
                Hide();
                return;
            }

            _iconImg.sprite = res;

            UpdateInfo();

            _uiGroup.gameObject.SetActive(true);
        }).Forget();
    }

    void Update()
    {
        // 지연 업데이트 최적화
        // 하지만 ready 상태가 달라졌다면 즉각 업데이트 해주어야함
        if (Time.frameCount % UpdateFrameInterval != 0 || IsValid == false)
            return;

        UpdateInfo();
    }

    public override void OnHide(UIArgBase argBase)
    {
        base.OnHide(argBase);

        _uiGroup.gameObject.SetActive(false);

        _skillRef = null;
        _owner = null;
        _lastSkillReady = false;
        _lastSkillCooltime = 0f;
        _skillValidCheckId = 0;
        _cooltimeFillImg.fillAmount = 0f;
        _skillCooltimeTxt.SetText("0");

        foreach (var elem in _skillReadyElements)
        {
            if (elem)
                elem.enabled = true;
        }

        foreach (var elem in _skillNotReadyElements)
        {
            if (elem)
                elem.enabled = false;
        }

        //UpdateInfo();
    }

    void UpdateInfo()
    {
        if (_skillValidCheckId != _skillRef.PoolableInstanceValidID)
        {
            _skillRef = null;
            SetReadyUI(false);
            return;
        }

        bool readyChanged = _lastSkillReady != _skillRef.IsAvailable;
        _lastSkillReady = _skillRef.IsAvailable;

        if (_lastSkillReady)
        {
            _lastSkillCooltime = 0f;
        }
        else
        {
            int cooltimeRemained = (int)_skillRef.CooltimeLeft;
            if ((int)_lastSkillCooltime != cooltimeRemained)
            {
                bool cooltimeFinished = cooltimeRemained <= 0;

                _skillCooltimeTxt.enabled = cooltimeFinished == false;

                if (cooltimeFinished == false)
                    _skillCooltimeTxt.SetText(cooltimeRemained.ToString());

                _lastSkillCooltime = _skillRef.CooltimeLeft;
            }

            _cooltimeFillImg.fillAmount = 1f - _skillRef.CooltimeProgress;
        }

        if (readyChanged)
        {
            SetReadyUI(_lastSkillReady);

            if (_triggerButton.enabled)
                _triggerButton.interactable = _lastSkillReady;
        }
    }

    void SetReadyUI(bool isReady)
    {
        foreach (var elem in _skillReadyElements)
        {
            if (elem)
                elem.enabled = isReady;
        }

        foreach (var elem in _skillNotReadyElements)
        {
            if (elem)
                elem.enabled = isReady == false;
        }
    }

    public void OnClickSkill()
    {
        if (IsValid == false || _skillRef.TableData.SkillCategory != GameDB.E_SkillCategoryType.Spell)
            return;

        var spell = _skillRef as EntitySpellStandard;

        _owner.SpellPart.RequestUse(new EntitySkillTriggerContext
        {
            Executor = _owner,
            Target = spell.TargetGetter.Invoke(),
            SlotIdx = _skillRef.SkillIdx,
        });
    }
}
