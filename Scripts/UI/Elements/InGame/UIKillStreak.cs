using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

[UIAttribute(nameof(UIKillStreak))]
public class UIKillStreak : UIBase, IUpdatable
{
    class Cmd
    {
        public uint TriggerKillCount;
        public uint TableID;
    }

    [SerializeField]
    RectTransform _uiGroup;

    [SerializeField]
    TextMeshProUGUI _messageTxt;

    [SerializeField]
    float _tweenDuration;

    List<Cmd> _cmds;

    uint _currentUpdatedKillCount;

    CharacterEntity _player;

    float _finishAt;

    LayoutGroup _layoutGroup;
    ContentSizeFitter _sizeFitter;
    Action _layoutDisableAction;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        // 한번실행. 성능무관.
        _layoutGroup = GetComponentInChildren<LayoutGroup>();
        _sizeFitter = GetComponentInChildren<ContentSizeFitter>();

        _layoutDisableAction = () => SetLayoutEnable(false);
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        _player = InGameManager.Instance.PlayerCommander.Player.Entity;
        if (_player == null)
        {
            TEMP_Logger.Err($"Could not get PlayerEntity!");
            Hide();
            return;
        }

        var statistic = _player.GetData(EntityDataCategory.Statistic) as EntityStatisticData;

        if (statistic == null)
        {
            TEMP_Logger.Err($"Player Entity does not have Statistic Data , Erorr");
            Hide();
            return;
        }

        _uiGroup.transform.localScale = Vector3.zero;

        _currentUpdatedKillCount = statistic.KillCount;

        _player.DataModifiedListener += OnPlayerDataModified;

        _cmds = GameDBManager.Instance.Container.KillStreakTable_data.
            Select(t => new Cmd()
            {
                TableID = t.Key,
                TriggerKillCount = t.Value.KillCount
            }).ToList();

        _cmds.Sort((lhs, rhs) =>
        {
            return lhs.TriggerKillCount.CompareTo(rhs.TriggerKillCount);
        });

        UpdateManager.Instance.RegisterSingleLateUpdatable(this);
    }

    public override void OnHide(UIArgBase arg)
    {
        if (EntityHelper.IsValid(_player))
            _player.DataModifiedListener -= OnPlayerDataModified;

        _player = null;
        _currentUpdatedKillCount = 0;
        _finishAt = 0;

        base.OnHide(arg);
    }

    void SetLayoutEnable(bool enable)
    {
        _layoutGroup.enabled = enable;
        _sizeFitter.enabled = enable;
    }

    private void OnPlayerDataModified(EntityDataCategory category, EntityDataBase data)
    {
        if (category == EntityDataCategory.Statistic)
        {
            var statistic = data as EntityStatisticData;

            _currentUpdatedKillCount = statistic.KillCount;
        }
    }

    // LateUpdate
    void IUpdatable.OnUpdate()
    {
        if (InGameManager.Instance.CurrentPhaseType != InGamePhase.Battle)
            return;

        if (_cmds.Count > 0)
        {
            CheckAndExecute();
        }

        if (_finishAt != 0 && Time.time >= _finishAt)
        {
            _finishAt = 0;
            _uiGroup.transform.localScale = Vector3.zero;
        }
    }

    public override void Hide()
    {
        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingleLateUpdatable(this);

        base.Hide();

        if (_cmds != null)
        {
            _cmds.Clear();
            _cmds = null;
        }
    }

    bool CheckAndExecute()
    {
        var tableId = TryPopCommand();

        if (tableId == 0)
            return false;

        var data = DBKillStreak.Get(tableId);

        if (data == null)
        {
            TEMP_Logger.Err($"Failed to get KillStreak Data | ID : {tableId}");
            return false;
        }

        SetLayoutEnable(true);

        _uiGroup.DOKill(false);

        if (ColorUtility.TryParseHtmlString(data.ColorHex, out var color))
            _messageTxt.color = color;
        else _messageTxt.color = Color.white;

        _messageTxt.SetText($"적 {_currentUpdatedKillCount} 마리 처치달성 ! {data.NotificationText}");
        _messageTxt.enabled = true;

        _uiGroup.transform.localScale = Vector3.zero;

        _uiGroup.DOScale(Vector3.one, _tweenDuration).SetEase(Ease.OutBack);

        if (data.AudioKeys != null)
        {
            for (int i = 0; i < data.AudioKeys.Length; i++)
            {
                if (string.IsNullOrEmpty(data.AudioKeys[i]) == false)
                {
                    AudioManager.Instance.Play(data.AudioKeys[i]);
                }
            }
        }

        _finishAt = Time.time + Mathf.Max(_tweenDuration, data.DisplayDuration);

        if (data.DoImpulse)
            InGameManager.Instance.PublishEvent(InGameEvent.RequestCameraImpulse);

        MainThreadDispatcher.Instance.InvokeInFrames(_layoutDisableAction, 1);

        return true;
    }

    uint TryPopCommand()
    {
        if (_cmds.Count == 0)
            return 0;

        int targetIndex = -1;

        for (int i = 0; i < _cmds.Count; i++)
        {
            if (_currentUpdatedKillCount >= _cmds[i].TriggerKillCount)
            {
                targetIndex = i;
            }
        }

        if (targetIndex == -1)
            return 0;

        var resultId = _cmds[targetIndex].TableID;

        _cmds.RemoveRange(0, targetIndex + 1);

        return resultId;
    }
}
