using GameDB;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(nameof(UIPeaceModePanel))]
public class UIPeaceModePanel : UIBase
{
    public class Arg : UIArgBase
    {
        public float startBattleTimeAt;

        public Arg(float startBattleTimeAt)
        {
            this.startBattleTimeAt = startBattleTimeAt;
        }
    }

    [SerializeField]
    TextMeshProUGUI _txtStructureCount;

    [SerializeField]
    TextMeshProUGUI _txtCharacterCount;

    [SerializeField]
    TextMeshProUGUI _txtBattleTimer;

    [SerializeField]
    JButton _constructionBtn;

    Coroutine _coBattleTimer;

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        var arg = argBase as Arg;

        InGameManager.Instance.EventListener += OnInGameEvent;

        UpdateUI();

        _coBattleTimer = CoroutineRunner.Instance.StartCoroutine(StartBattleTimer(arg.startBattleTimeAt));
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        if (_coBattleTimer != null)
        {
            CoroutineRunner.Instance.StopCoroutine(_coBattleTimer);
            _coBattleTimer = null;
        }

        UIManager.Instance.Remove<UIEntitySelectionListPopup>();
        UIManager.Instance.Remove<UISkillEditPanel>();
    }

    IEnumerator StartBattleTimer(float startBattleTimeAt)
    {
        int lastRemainedSec = 0;
        while (Time.time < startBattleTimeAt)
        {
            int remained = (int)(Time.time - startBattleTimeAt);
            if (lastRemainedSec != remained)
            {
                _txtBattleTimer.SetText(TimeSpan.FromSeconds(remained).ToString(@"m\:ss"));
            }
            yield return null;
        }

        _txtBattleTimer.SetText("0:00");

        _coBattleTimer = null;
    }

    void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.EntityCreated)
        {
            var arg = argBase as EntityCreatedEventArg;
            if (arg.Entity.Team == EntityTeamType.Player)
                UpdateUI();
        }
        else if (evt == InGameEvent.EntityRemoved)
        {
            var arg = argBase as EntityRemovedEventArg;
            if (arg.Team == EntityTeamType.Player)
                UpdateUI();
        }
    }

    void UpdateUI()
    {
        _txtStructureCount.SetText("{0}", EntityManager.Instance.GetStructureCount(EntityTeamType.Player));
        _txtCharacterCount.SetText("{0}", EntityManager.Instance.GetCharacterCount(EntityTeamType.Player));
    }

    public override void Hide()
    {
        base.Hide();
    }

    public void OnClickBattleStart()
    {
        var arg = new EnterBattlePhaseEventArg(
                InGameManager.Instance.CurrentPhaseType,
                InGameBattleMode.Defense);

        InGameManager.Instance.RequestChangePhase(InGamePhase.Battle, arg);
    }

    public void OnClickConstructionBtn()
    {
        UIManager.Instance.ShowCallBack<UIEntitySelectionListPopup>(
            arg: new UIEntitySelectionListPopup.Arg(UIEntitySelectionTask.Purchase,
            (entityTid) =>
            {
                return DBEntity.GetEntityType(entityTid) == E_EntityType.Structure;
            },
            (resBase) =>
            {
                var res = resBase as UIEntitySelectionListPopup.ResultArg;
                if (res.result == UIEntitySelectionListPopup.Result.Confirm)
                {
                    EntityPlacementManager.Instance.StartPlacement(res.selectedEntityTid);
                }
            })).Forget();
    }

    public void OnClickSkillEditBtn()
    {
        UIManager.Instance.ShowCallBack<UISkillEditPanel>().Forget();
    }
}
