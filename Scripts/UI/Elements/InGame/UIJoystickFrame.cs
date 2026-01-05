using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIJoystickFrame")]
public class UIJoystickFrame : UIBase
{
    public class Arg : UIArgBase
    {
        // Direction, Speed (0~1)
        public Action<Vector2, float> onTouched;
        public Action onReleased;
    }

    public enum BallDirection
    {
        LeftTop,
        RightTop,
        Rightbot,
        LeftBot
    }

    [Serializable]
    public class FocusEdge
    {
        public BallDirection direction;
        public CanvasGroup canvasGroup;
    }

    [SerializeField]
    private RectTransform _joystickGroup;

    [SerializeField]
    private Image _imgControlBall;

    [Header("계산상의 효율을 위해 반드시 Enum 순서대로 요소 넣어줘야함"), SerializeField]
    private List<FocusEdge> _focusEdgeList;

    [SerializeField]
    private RectTransform _touchAllowedAreaDef;

    Camera _uiCam;

    bool _isAppeared;

    float _radius;

    RectTransform _joystickGroupParent;

    Arg _arg;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _radius = _joystickGroup.sizeDelta.x * 0.5f;
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        _arg = arg as Arg;
        if (_arg == null)
        {
            TEMP_Logger.Err("Joystick UI Arg cannot be null.");
            Hide();
        }

        _uiCam = CameraManager.Instance.GetUICamera(base.SortPolicy.layer);
        EventManager.Instance.Register(GLOBAL_EVENT.USER_INPUT, OnUserInput, GLOBAL_EVENT_PRIORITY.Medium);
        _joystickGroupParent = _joystickGroup.parent as RectTransform;
    }

    public override void OnHide(UIArgBase arg)
    {
        base.OnHide(arg);

        Disappear();
        EventManager.Instance.Unregister(GLOBAL_EVENT.USER_INPUT, OnUserInput, GLOBAL_EVENT_PRIORITY.Medium);
        _arg = null;
    }

    void OnUserInput(EventContext cxt)
    {
        var userInputArg = cxt.Arg as InputEventBaseArg;

        if (userInputArg.InputType == UserInputType.FirstPressDown ||
            userInputArg.InputType == UserInputType.DragBegin ||
            userInputArg.InputType == UserInputType.Dragging)
        {
            if (_isAppeared == false && userInputArg.InputType != UserInputType.Dragging)
            {
                bool appear = RectTransformUtility.RectangleContainsScreenPoint(_touchAllowedAreaDef, userInputArg.ScreenPosition, _uiCam);
                if (appear)
                    Appear(userInputArg.ScreenPosition);
            }

            else if (userInputArg.InputType == UserInputType.Dragging)
            {
                UpdateBallPosition(userInputArg.ScreenPosition);
            }
        }
        else
        {
            Disappear();
        }
    }

    void Appear(Vector2 screenPosition)
    {
        if (ScreenToAnchoredPos(_joystickGroupParent, screenPosition, out var anchoredPos) == false)
            return;

        _isAppeared = true;
        _tweenCanvasGroup.alpha = 1f;
        _joystickGroup.anchoredPosition = anchoredPos;
        _imgControlBall.rectTransform.anchoredPosition = Vector2.zero;
    }

    void UpdateBallPosition(Vector2 screenPosition)
    {
        if (ScreenToAnchoredPos(_joystickGroup, screenPosition, out var anchoredPos) == false)
            return;

        float dist = Mathf.Clamp(Vector2.Distance(anchoredPos, Vector2.zero), 0, _radius);
        Vector2 dir = anchoredPos.normalized;
        _imgControlBall.rectTransform.anchoredPosition = dir * dist;

        BallDirection activatedDir;
        if (dir.x < 0 && dir.y > 0)
            activatedDir = BallDirection.LeftTop;
        else if (dir.x > 0 && dir.y > 0)
            activatedDir = BallDirection.RightTop;
        else if (dir.x > 0 && dir.y < 0)
            activatedDir = BallDirection.Rightbot;
        else
            activatedDir = BallDirection.LeftBot;

        for (int i = 0; i < _focusEdgeList.Count; i++)
        {
            _focusEdgeList[i].canvasGroup.alpha = i == (int)activatedDir ? 1f : 0;
        }

        _arg.onTouched.Invoke(dir, dist / _radius);
    }

    bool ScreenToAnchoredPos(RectTransform parent, Vector2 screenPosition, out Vector2 anchoredPos)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, _uiCam, out anchoredPos);
    }

    void Disappear()
    {
        _isAppeared = false;
        _tweenCanvasGroup.alpha = 0f;
        _arg.onReleased.Invoke();
    }
}
