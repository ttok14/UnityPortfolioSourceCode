using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFreeState : CameraStateBase
{
    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        EventManager.Instance.Register(GLOBAL_EVENT.USER_INPUT, OnUserInput);
        InGameManager.Instance.EventListener += OnInGameEvent;

        if (args != null)
        {
            if (args[0] is Vector3)
            {
                SetTargetPosition((Vector3)args[0]);
            }
        }
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);

        EventManager.Instance.Unregister(GLOBAL_EVENT.USER_INPUT, OnUserInput);
        InGameManager.Instance.EventListener -= OnInGameEvent;
    }

    private void OnUserInput(EventContext cxt)
    {
        var baseArg = cxt.Arg as InputEventBaseArg;

        if (baseArg.InputType == UserInputType.Dragging)
        {
            var inputArg = baseArg as InputDraggingEventData;
            _cinemachineController.MoveTarget(inputArg.Delta * -1, Constants.CameraControl.CameraMoveSpeed * Time.deltaTime);
            cxt.Use();
        }
        else if (baseArg.InputType == UserInputType.MouseScroll)
        {
            var inputArg = baseArg as InputMouseScrollEventData;
            _cinemachineController.Zoom(inputArg.Delta * -1 * Constants.CameraControl.CameraPinchingZoomSensitivity * Time.deltaTime);
            cxt.Use();
        }
        else if (baseArg.InputType == UserInputType.Pinching)
        {
            TEMP_Logger.Err($"pinching not implemented ");
        }
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.PlayerCharacterSpawned)
        {
            var arg = argBase as PlayerCharacterRespawnEventArg;
            SetTargetPosition(arg.Entity.ApproxPosition.FlatHeight());
        }
    }

    public void SetTargetPosition(Vector3 position)
    {
        _cinemachineController.SetTargetPosition(position);
    }
}
