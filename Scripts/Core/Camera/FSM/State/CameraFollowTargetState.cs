using System;
using UnityEngine;

public class CameraFollowTargetState : CameraStateBase
{
    protected override bool NeedSyncPositionFromPrevious => true;

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        var target = args[0] as Transform;
        if (target == null)
        {
            TEMP_Logger.Err($"Need Target Transform to follow");
        }
        else
        {
            _cinemachineController.ChangeFollowTarget(target);
        }
    }

    public override void OnExit(Action callback)
    {
        _cinemachineController.ChangeFollowTarget(null);

        base.OnExit(callback);
    }
}
