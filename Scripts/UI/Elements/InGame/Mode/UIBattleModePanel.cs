using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameDB;

[UIAttribute(nameof(UIBattleModePanel))]
public class UIBattleModePanel : UIBase
{
    public class Arg : UIArgBase
    {
        public ulong[] objectiveEntityIDs;
    }

    class EntityHPProgress
    {
        public ulong id;
        public UITransCommonProgressElement progressBar;
    }

    [SerializeField]
    RectTransform _objectiveElementsRoot;

    [SerializeField]
    RectTransform _skillStatusElementRoot;

    [SerializeField]
    RectTransform _spellStatusElementRoot;

    LayoutGroup[] _allLayoutGroups;

    List<EntityHPProgress> _objectiveProgressBarList = new List<EntityHPProgress>();

    List<UISkillStatusElement> _skillStatusElements = new List<UISkillStatusElement>();
    List<UISkillStatusElement> _spellStatusElements = new List<UISkillStatusElement>();

    float _lastUpdatedNormalizedHP;

    int _rebuildRefCount;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _allLayoutGroups = GetComponentsInChildren<LayoutGroup>();
    }

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        var arg = argBase as Arg;
        if (arg == null)
        {
            TEMP_Logger.Err($"Arg Invalid Error");
            Hide();
            return;
        }

        _objectiveProgressBarList.Clear();

        if (arg.objectiveEntityIDs != null)
        {
            _objectiveProgressBarList.Resize(arg.objectiveEntityIDs.Length);
            for (int i = 0; i < _objectiveProgressBarList.Count; i++)
            {
                _objectiveProgressBarList[i] = new EntityHPProgress();
            }

            for (int i = 0; i < arg.objectiveEntityIDs.Length; i++)
            {
                int idx = i;
                var entity = EntityManager.Instance.GetEntity(arg.objectiveEntityIDs[idx]);
                if (entity == null)
                {
                    TEMP_Logger.Err($"Given Entity is invalid | ID : {arg.objectiveEntityIDs[idx]}");
                    Hide();
                    return;
                }

                // [0] 이, 이 시점 타겟일테니?
                entity.HpChangedListener += OnHPChanged;

                PoolManager.Instance.RequestSpawnAsyncCallBack<UITransCommonProgressElement>(
                    ObjectPoolCategory.UI,
                    "UITransCommonProgressElement",
                    parent: _objectiveElementsRoot,
                    onCompleted: (res, opRes) =>
                    {
                        res.SetInfo(UICommonProgressElement.VisibleFlag.Icon | UICommonProgressElement.VisibleFlag.Slider, new UICommonProgressElement.Info()
                        {
                            iconKey = "StructureIcon_Residential",
                            progressValue = (entity.GetData(EntityDataCategory.Stat) as EntityStatData).CurrentHPNormalized
                        });

                        _objectiveProgressBarList[idx].progressBar = res;

                        if (_objectiveProgressBarList.TrueForAll(t => t.progressBar != null))
                        {
                            _objectiveProgressBarList.Sort((lhs, rhs) =>
                            {
                                return lhs.progressBar.transform.GetSiblingIndex().CompareTo(rhs.progressBar.transform.GetSiblingIndex());
                            });

                            var targetIds = InGameManager.Instance.EnemyCommander.BattleStatus.OrderedTargetIDs;
                            for (int i = 0; i < targetIds.Count; i++)
                            {
                                _objectiveProgressBarList[i].id = targetIds[i];
                            }

                            UpdateObjectiveProgressBars(true);
                        }
                        else
                        {
                            res.SetAlpha(0f);
                        }
                    }).Forget();
            }
        }

        var playerEntity = InGameManager.Instance.PlayerCommander.Player.Entity;

        var skillCount = playerEntity.SkillPart.SkillCount;
        for (int i = 0; i < skillCount; i++)
        {
            var skill = playerEntity.SkillPart.GetSkill(i);

            if (string.IsNullOrEmpty(skill.TableData.IconKey))
                continue;

            E_SkillCategoryType category = skill.TableData.SkillCategory;

            UIManager.Instance.ShowCallBack<UISkillStatusElement>(UITrigger.Default, new UISkillStatusElement.Arg(playerEntity, skill),
                onCompleted: (ui) =>
                {
                    if (IsEnabled == false)
                    {
                        ui.Hide();
                        return;
                    }

                    _skillStatusElements.Add(ui);
                    ui.transform.SetParent(_skillStatusElementRoot);

                    RebuildLayout();
                }).Forget();
        }

        int spellCount = playerEntity.SpellPart.SpellCount;
        for (int i = 0; i < spellCount; i++)
        {
            var spell = playerEntity.SpellPart.GetSpell(i);

            if (string.IsNullOrEmpty(spell.TableData.IconKey))
                continue;

            E_SkillCategoryType category = spell.TableData.SkillCategory;

            UIManager.Instance.ShowCallBack<UISkillStatusElement>(UITrigger.Default, new UISkillStatusElement.Arg(playerEntity, spell),
                onCompleted: (ui) =>
                {
                    if (IsEnabled == false)
                    {
                        ui.Hide();
                        return;
                    }

                    _spellStatusElements.Add(ui);
                    ui.transform.SetParent(_spellStatusElementRoot);

                    RebuildLayout();
                }).Forget();
        }
    }

    private void OnHPChanged(int maxHP, int currentHP, int diff)
    {
        float normalized = (float)currentHP / maxHP;
        if (Mathf.Abs(normalized - _lastUpdatedNormalizedHP) < 0.05f)
            return;

        _lastUpdatedNormalizedHP = normalized;

        UpdateObjectiveProgressBars();
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        _rebuildRefCount = 0;
        _lastUpdatedNormalizedHP = 0;

        foreach (var elemUI in _objectiveProgressBarList)
        {
            if (elemUI.progressBar)
            {
                elemUI.progressBar.Return();
                elemUI.progressBar = null;
            }
        }

        for (int i = 0; i < _skillStatusElements.Count; i++)
        {
            if (_skillStatusElements[i].IsEnabled)
            {
                UIManager.Instance.Hide(_skillStatusElements[i]);
            }
        }
        _skillStatusElements.Clear();

        for (int i = 0; i < _spellStatusElements.Count; i++)
        {
            if (_spellStatusElements[i].IsEnabled)
            {
                UIManager.Instance.Hide(_spellStatusElements[i]);
            }
        }
        _spellStatusElements.Clear();

        _objectiveProgressBarList.Clear();
    }

    void UpdateObjectiveProgressBars(bool rebuildLayout = false)
    {
        var battleStatus = InGameManager.Instance.EnemyCommander.BattleStatus;
        ulong currentTargetId = battleStatus.CurrentTargetID;
        // var curTargetIdx = battleStatus.CurrentTargetIndex;

        for (int i = 0; i < _objectiveProgressBarList.Count; i++)
        {
            bool isAlive = EntityHelper.IsValid(EntityManager.Instance.GetEntity(_objectiveProgressBarList[i].id));

            // 이미 사망한 타겟의 bar (제거)
            if (isAlive == false)
            {
                if (_objectiveProgressBarList[i].progressBar)
                {
                    _objectiveProgressBarList[i].progressBar.Return();
                    _objectiveProgressBarList[i] = null;
                    rebuildLayout = true;
                }

                _objectiveProgressBarList.RemoveAt(i);
                i--;
            }
            // 현재 타겟팅된 bar (불투명)
            else if (_objectiveProgressBarList[i].id == currentTargetId)
            {
                if (_objectiveProgressBarList[i].progressBar)
                {
                    var curStatData = EntityManager.Instance.GetEntity(battleStatus.CurrentTargetID).GetData(EntityDataCategory.Stat) as EntityStatData;

                    _objectiveProgressBarList[i].progressBar.SetAlpha(1f);
                    _objectiveProgressBarList[i].progressBar.SetSliderValue(curStatData.CurrentHPNormalized);
                }
            }
            // 앞으로 타겟팅이 될 bar (반투명)
            else
            {
                if (_objectiveProgressBarList[i].progressBar)
                    _objectiveProgressBarList[i].progressBar.SetAlpha(0.6f);
            }
        }

        if (rebuildLayout)
        {
            RebuildLayout();
        }
    }

    void RebuildLayout()
    {
        _rebuildRefCount++;

        SetLayoutGroupEnabled(true);

        MainThreadDispatcher.Instance.InvokeInFrames(() =>
        {
            if (IsEnabled == false)
                return;

            _rebuildRefCount--;

            if (_rebuildRefCount <= 0)
            {
                SetLayoutGroupEnabled(false);
                _rebuildRefCount = 0;
            }
        }, 2);
    }

    void SetLayoutGroupEnabled(bool enabled)
    {
        foreach (var group in _allLayoutGroups)
        {
            group.enabled = enabled;
        }
    }
}
