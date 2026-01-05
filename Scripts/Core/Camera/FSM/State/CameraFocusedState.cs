using System;
using UnityEngine;

public class CameraFocusedState : CameraStateBase
{
    protected override bool NeedSyncPositionFromPrevious => true;

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        var target = args[0] as Transform;
        _cinemachineController.ChangeFollowTarget(target);

        EventManager.Instance.Register(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnUserInteractionEnded);
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);
    }

    private void OnUserInteractionEnded(EventContext cxt)
    {
        EventManager.Instance.Unregister(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnUserInteractionEnded);
        Parent.FSM.ChangeState(CinemachineCameraType.Free);
    }
}
