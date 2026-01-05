using System;
using UnityEngine;

public class CameraStateBase : BaseState<CinemachineCameraType, InGameCameraController, object>
{
    protected CinemachineCameraController _cinemachineController;

    // A 카메라에서 B 카메라로 전환할때
    // 비활성화돼있던 B 의 위치에서 시작하는게 아닌, 기존에 월드를 찍고있던
    // A 의 위치로 B 도 시작해야할때 사용해야 하는 옵션 (e.g Structure 선택시 확대)
    protected virtual bool NeedSyncPositionFromPrevious { get; } = false;

    public override void OnEnter(Action callback, object[] args)
    {
        base.OnEnter(callback, args);

        _cinemachineController = CameraManager.Instance.InGameController.SetCurrentCinemachine(base.State, NeedSyncPositionFromPrevious);
    }

    public override void OnExit(Action callback)
    {
        base.OnExit(callback);
    }
}
