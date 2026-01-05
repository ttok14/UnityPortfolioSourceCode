using UnityEngine;

public class CameraControlFSM : FSM<CameraControlTransitionEvent, CinemachineCameraType, object, InGameCameraController>
{
    public CameraControlFSM(InGameCameraController parent) : base(parent) { }
}
