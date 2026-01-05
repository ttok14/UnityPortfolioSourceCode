using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIBubbleFrame")]
public class UIBubbleFrame : UIWorldFollowBase
{
    public class Arg : WorldFollowArg
    {
        public string txt;

        public Arg(string txt, Vector2 uiOffsetPos, Transform followTarget) : base(followTarget, uiOffsetPos)
        {
            this.txt = txt;
        }
    }

    [SerializeField]
    private RectTransform _bubbleUiGroup;

    [SerializeField]
    private TextMeshProUGUI _bubbleTxt;

    LayoutGroup[] _allLayoutGroups;
    ContentSizeFitter[] _allContentSizeFitters;

    Action _delayedDisableLayoutAction;

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

        SetLayoutComponentsEnable(true);

        _bubbleUiGroup.transform.localScale = Vector3.zero;
        _bubbleTxt.text = uiArg.txt;

        if (_allLayoutGroups.Length > 0 || _allContentSizeFitters.Length > 0)
        {
            MainThreadDispatcher.Instance.InvokeInFrames(_delayedDisableLayoutAction, 1);
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

    public override void OnInactivated()
    {
        base.OnInactivated();

        _bubbleTxt.text = string.Empty;
    }

    protected override void OnHudUpdated()
    {
        if (LengthSinceActivated > Constants.UI.AutoBubbleTextDuration)
        {
            Hide();
        }
    }
}
