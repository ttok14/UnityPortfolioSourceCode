using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIEntityCommonHud")]
public class UIEntityCommonHud : UIWorldFollowBase
{
    public enum Group
    {
        None = 0,

        NameTag = 10,
        HPBar = 20,
        TargetIndicator = 30,
        SkillStatus = 40,
        ResourceGenerating = 50,
        SpawnerStats = 60,
    }

    public class Arg : WorldFollowArg
    {
        public ulong entityID;
        public Arg(ulong entityID, Transform followTarget, Vector2 uiOffsetPos) : base(followTarget, uiOffsetPos)
        {
            this.entityID = entityID;
        }
    }

    [SerializeField]
    private RectTransform _nameTagGroup;
    [SerializeField]
    private RectTransform _healthBarGroup;
    [SerializeField]
    private RectTransform _targetIndicatorGroup;
    [SerializeField]
    private RectTransform _skillStatusGroup;
    [SerializeField]
    private RectTransform _generatedResourceGroup;
    [SerializeField]
    private RectTransform _spawnerGroup;

    [SerializeField]
    private TextMeshProUGUI _nameTxt;

    [SerializeField]
    private Slider _healthBar;

    [SerializeField]
    private GameObject _spawnerGuardIcon;
    [SerializeField]
    private GameObject _spawnerAttackIcon;

    #region ===:: 최적화용 ::===
    LayoutGroup[] _allLayoutGroups;
    ContentSizeFitter[] _allContentSizeFitters;

    // 람다 캐싱용
    Action _delayedDisableLayoutAction;
    #endregion

    #region ===:: Skill Status ::===
    [SerializeField]
    private VerticalLayoutGroup _skillStatusElementsLayout;

    private List<UIEntitySkillHudElement> _skillStatusElements = new List<UIEntitySkillHudElement>();

    bool _showSkillStatus;

    bool _showSpawnerStatus;
    #endregion

    #region ===:: Resource Generator ::===
    private IResourceGenerator _resourceGenerator;

    private UICommonProgressElement _resGenElement;

    const float ResourceGenProgressUpdateThreshold = 0.1f;
    float _lastResGenProgress;
    #endregion

    #region ===:: Spawner ::===
    private SpawnerStructureEntity _spawnerEntity;
    private UIEntitySpawnerHudElement _spawnerStatusElement;
    #endregion

    bool _isUserDragging;

    protected override bool ShouldShow => InGameManager.Instance.CurrentPhaseType == InGamePhase.Battle || _isUserDragging;

    EntityBase _targetEntity;
    EntityStatData _targetEntityStatData;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _allLayoutGroups = GetComponentsInChildren<LayoutGroup>();
        _allContentSizeFitters = GetComponentsInChildren<ContentSizeFitter>();

        if (_allLayoutGroups.Length > 0 || _allContentSizeFitters.Length > 0)
        {
            _delayedDisableLayoutAction = () =>
            {
                SetLayoutComponentsEnable(false);
            };
        }
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

        _resourceGenerator = _targetEntity as IResourceGenerator;
        if (_resourceGenerator != null && _resourceGenerator.ResourceType != E_ResourceType.None)
        {
            _resourceGenerator.OnGenerated += OnResourceGenerated;
            PoolManager.Instance.RequestSpawnAsyncCallBack<UICommonProgressElement>(
                ObjectPoolCategory.UI,
                "UICommonProgressElement",
                parent: _generatedResourceGroup.transform,
                onCompleted: (res, opRes) =>
                 {
                     if (opRes != PoolOpResult.Successs)
                     {
                         TEMP_Logger.Err($"Failed to spawn UICommonProgressElement | Res : {opRes}");
                         return;
                     }

                     if (IsEnabled == false)
                     {
                         res.Return();
                         return;
                     }

                     _resGenElement = res;
                     _resGenElement.SetInfo(
                         UICommonProgressElement.VisibleFlag.Icon | UICommonProgressElement.VisibleFlag.Text | UICommonProgressElement.VisibleFlag.Slider
                         , new UICommonProgressElement.Info()
                         {
                             iconKey = DBResource.GetIconKey(_resourceGenerator.ResourceType, _resourceGenerator.DetailID),
                             progressValue = _resourceGenerator.CurrentProgress,
                             text = _resourceGenerator.Amount.ToString()
                         });
                 }).Forget();
        }

        if (_targetEntity.SkillPart != null && _targetEntity.SkillPart.SkillCount > 1)
        {
            for (int i = 1; i < _targetEntity.SkillPart.SkillCount; i++)
            {
                PoolManager.Instance.RequestSpawnAsyncCallBack<UIEntitySkillHudElement>(
                    ObjectPoolCategory.UI,
                    "UIEntitySkillHudElement",
                    parent: _skillStatusElementsLayout.transform,
                    onCompleted: (res, opRes) =>
                     {
                         if (opRes != PoolOpResult.Successs)
                         {
                             TEMP_Logger.Err($"Failed to spawn UIEntitySkillHudElement | Res : {opRes}");
                             return;
                         }

                         if (IsEnabled == false)
                         {
                             res.Return();
                             return;
                         }

                         res.gameObject.SetActive(false);
                         _skillStatusElements.Add(res);
                         UpdateGroupActive();
                     }).Forget();
            }
        }

        if (_targetEntity.Type == E_EntityType.Structure)
        {
            var structureEntity = _targetEntity as StructureEntity;
            if (structureEntity.StructureData.StructureType == E_StructureType.Spawner && structureEntity.StructureData.EnableSpawning)
            {
                _spawnerEntity = structureEntity as SpawnerStructureEntity;
                _spawnerEntity.SpawnerModeChangedListener += OnSpawnModeChanged;

                PoolManager.Instance.RequestSpawnAsyncCallBack<UIEntitySpawnerHudElement>(
                    ObjectPoolCategory.UI,
                    "UIEntitySpawnerHudElement",
                    parent: _spawnerGroup.transform,
                    onCompleted: (res, opRes) =>
                    {
                        if (opRes != PoolOpResult.Successs)
                        {
                            TEMP_Logger.Err($"Failed to spawn UIEntitySpawnerHudElement | Res : {opRes}");
                            return;
                        }

                        if (IsEnabled == false)
                        {
                            res.Return();
                            return;
                        }

                        // res.gameObject.SetActive(false);
                        _spawnerStatusElement = res;
                        UpdateGroupActive();
                    }).Forget();
            }
        }

        _targetEntityStatData = _targetEntity.GetData(EntityDataCategory.Stat) as EntityStatData;

        _nameTxt.text = _targetEntity.TableData.Name;

        UpdateGroupActive();

        EventManager.Instance.Register(GLOBAL_EVENT.USER_INPUT, OnUserInput);

        _targetEntity.HpChangedListener += OnHPChanged;
        _targetEntity.DiedListener += OnEntityDied;
        InGameManager.Instance.EventListener += OnInGameEvent;
    }

    void OnSpawnModeChanged(SpawnerMode newMode)
    {
        UpdateSpawnerModeUI();
    }

    private void OnResourceGenerated(E_ResourceType type, uint id, uint amount)
    {
        UIManager.Instance.ShowCallBack<UIFloatingText>(UITrigger.Default, new UIFloatingText.Arg(
            Color.green, "+" + amount.ToString(), DBResource.GetIconKey(type, id), _targetEntity.transform, new Vector2(0, 200))).Forget();
    }

    public override void OnRemoved()
    {
        foreach (var elem in _skillStatusElements)
        {
            if (elem)
            {
                elem.Return();
            }
        }
        _skillStatusElements.Clear();
        _delayedDisableLayoutAction = null;

        base.OnRemoved();
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        if (_targetEntity)
        {
            _targetEntity.HpChangedListener -= OnHPChanged;
            _targetEntity.DiedListener -= OnEntityDied;
        }

        _targetEntity = null;
        if (_resourceGenerator != null)
        {
            _resourceGenerator.OnGenerated -= OnResourceGenerated;
            _resourceGenerator = null;
        }
        if (_resGenElement)
        {
            _resGenElement.Return();
            _resGenElement = null;
        }

        if (_spawnerStatusElement)
        {
            _spawnerStatusElement.Return();
            _spawnerStatusElement = null;
        }

        if (_spawnerEntity)
        {
            _spawnerEntity.SpawnerModeChangedListener -= OnSpawnModeChanged;
            _spawnerEntity = null;
        }

        foreach (var skillHud in _skillStatusElements)
        {
            if (skillHud)
            {
                skillHud.transform.SetParent(null, false);
                skillHud.Return();
            }
        }

        _skillStatusElements.Clear();

        _targetEntityStatData = null;
        _showSkillStatus = false;
        _showSpawnerStatus = false;
        _nameTxt.text = string.Empty;
        EventManager.Instance.Unregister(GLOBAL_EVENT.USER_INPUT, OnUserInput);
        InGameManager.Instance.EventListener -= OnInGameEvent;
    }

    protected override void OnHudUpdated()
    {
        if (IsEnabled == false)
            return;

        if (_showSkillStatus)
        {
            for (int skillIdx = 1; skillIdx < _targetEntity.SkillPart.SkillCount; skillIdx++)
            {
                int elemIdx = skillIdx - 1;
                if (elemIdx >= _skillStatusElements.Count)
                    break;

                _skillStatusElements[elemIdx].SetSliderValue(_targetEntity.SkillPart.GetCooltimeProgress(skillIdx));
            }
        }

        if (_showSpawnerStatus)
        {
            if (_spawnerStatusElement && _spawnerEntity.IsSpawnerEnabled)
            {
                _spawnerStatusElement.SetSliderValue(_spawnerEntity.SpawnerPart.Progress);
            }
        }

        if (_resourceGenerator != null &&
            _resourceGenerator.IsEnabled &
            _resGenElement)
        {
            var progress = _resourceGenerator.CurrentProgress;
            if (MathF.Abs(progress - _lastResGenProgress) >= ResourceGenProgressUpdateThreshold)
            {
                _lastResGenProgress = progress;

                _resGenElement.SetSliderValue(progress);
            }
        }
    }

    void UpdateGroupActive()
    {
        SetLayoutComponentsEnable(true);

        _nameTagGroup.gameObject.SetActive(true);

        var phase = InGameManager.Instance.CurrentPhaseType;

        UpdateSpawnerModeUI();

        if (phase == InGamePhase.Peace)
        {
            _healthBarGroup.gameObject.SetActive(false);
            _targetIndicatorGroup.gameObject.SetActive(false);
            _spawnerStatusElement?.gameObject.SetActive(false);
            _showSpawnerStatus = false;
        }
        else if (InGameManager.Instance.CurrentPhaseType == InGamePhase.Battle)
        {
            var bs = InGameManager.Instance.EnemyCommander.BattleStatus;
            bool isTarget = bs.IsTarget(_targetEntity.ID);
            bool showHpBar = isTarget || (_targetEntity.Team == EntityTeamType.Enemy && _targetEntity.StatPart.StatData.StatTableData.IsInvincible == false);

            // 이게 혹시 너무 깊이 알고있는 걸까
            _targetIndicatorGroup.gameObject.SetActive(bs.IsTarget(_targetEntity.ID));

            _healthBarGroup.gameObject.SetActive(showHpBar);

            //-- Skill --//
            _showSkillStatus = _targetEntity.SkillPart != null && _targetEntity.SkillPart.SkillCount > 1;
            if (_showSkillStatus)
            {
                _skillStatusGroup.gameObject.SetActive(true);

                for (int i = 0; i < _skillStatusElements.Count; i++)
                {
                    var elem = _skillStatusElements[i];
                    int skillSlotIdx = i + 1;
                    elem.SetInfo(_targetEntity.SkillPart.GetSkill(skillSlotIdx).TableID);
                    elem.gameObject.SetActive(true);
                }
            }
            else
            {
                _skillStatusGroup.gameObject.SetActive(false);
            }

            //-- spawner --//
            if (_spawnerEntity)
            {
                _showSpawnerStatus = _spawnerEntity;
                _spawnerGroup.gameObject.SetActive(_showSpawnerStatus);

                if (_showSpawnerStatus)
                {
                    if (_spawnerStatusElement)
                    {
                        var iconKey = DBEntity.GetIconKey(_spawnerEntity.SpawnerPart.SpawnEntityId);

                        if (string.IsNullOrEmpty(iconKey) == false)
                        {
                            _spawnerStatusElement.gameObject.SetActive(true);
                            _spawnerStatusElement.SetInfo(iconKey);
                        }
                        else
                        {
                            _spawnerStatusElement.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        if (_resourceGenerator != null)
        {
            _generatedResourceGroup.gameObject.SetActive(_resourceGenerator.IsEnabled);
        }
        else
        {
            _generatedResourceGroup.gameObject.SetActive(false);
        }

        UpdateHPBarValue();

        if (_allLayoutGroups.Length > 0 || _allContentSizeFitters.Length > 0)
        {
            MainThreadDispatcher.Instance.InvokeInFrames(_delayedDisableLayoutAction, 1);
        }
    }

    void UpdateSpawnerModeUI()
    {
        if (_spawnerEntity)
        {
            _spawnerGroup.gameObject.SetActive(true);
            _spawnerGuardIcon.gameObject.SetActive(_spawnerEntity.SpawnerMode == SpawnerMode.Defensive);
            _spawnerAttackIcon.gameObject.SetActive(_spawnerEntity.SpawnerMode == SpawnerMode.Aggresive);
        }
        else
        {
            _spawnerGroup.gameObject.SetActive(false);
        }
    }

    void UpdateHPBarValue()
    {
        if (_healthBarGroup.gameObject.activeSelf && _targetEntityStatData != null)
        {
            _healthBar.value = _targetEntityStatData.CurrentHP / (float)_targetEntityStatData.MaxHp;
        }
    }

    void SetLayoutComponentsEnable(bool enable)
    {
        if (_allLayoutGroups != null)
        {
            foreach (var group in _allLayoutGroups)
            {
                group.enabled = enable;
            }
        }

        if (_allContentSizeFitters != null)
        {
            foreach (var fitter in _allContentSizeFitters)
            {
                fitter.enabled = enable;
            }
        }
    }

    private void OnUserInput(EventContext cxt)
    {
        var baseArg = cxt.Arg as InputEventBaseArg;
        if (baseArg.InputType == UserInputType.DragBegin)
        {
            _isUserDragging = true;
        }
        else if (baseArg.InputType == UserInputType.DragEnd)
        {
            _isUserDragging = false;
        }
    }

    void OnInGameEvent(InGameEvent evt, InGameEventArgBase arg)
    {
        if (evt == InGameEvent.Start)
        {
            UpdateGroupActive();
        }
    }

    void OnHPChanged(int maxHP, int currentHP, int diff)
    {
        UpdateHPBarValue();
    }

    void OnEntityDied(ulong attackerId, Vector3 attackerPosition)
    {
        if (IsEnabled == false)
            return;

        Hide();
    }
}
