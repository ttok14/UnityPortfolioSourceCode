using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraManualState : CameraStateBase
{
    protected override bool NeedSyncPositionFromPrevious => true;

    public override void OnEnter(Action callback, params object[] args)
    {
        base.OnEnter(callback, args);

        if (args != null && args.Length > 0)
        {
            if (args[0] is Vector3 position)
            {
                SetPosition(position);
            }

            if (args.Length > 1 && args[1] is float fov)
            {
                SetFov(fov);
            }
        }
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);
    }

    public void SetPosition(Vector3 position)
    {
        base._cinemachineController.SetTargetPosition(position);
    }

    public void SetFov(float fov)
    {
        base._cinemachineController.SetFov(fov);
    }

    public void SetPositionAndFov(Vector3 position, float fov)
    {
        base._cinemachineController.SetTargetPositionAndFov(position, fov);
    }
}
