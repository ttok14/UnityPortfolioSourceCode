using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class InGameCameraController : MonoBehaviour
{
    private Camera _mainCam;

    private CinemachineBrain _brainCam;
    private CinemachineBrainEvents _brainCamEvents;
    // TODO : 추후 여러개 관리 가능한 시스템으로 변경할것 
    private Dictionary<CinemachineCameraType, CinemachineCameraController> _cinemachineControllers = new Dictionary<CinemachineCameraType, CinemachineCameraController>();

    public Transform CurrentCameraTarget => _cinemachineControllers[FSM.Current_State].Target;

    bool _eventBlocked;

    public bool IsBlending => _brainCam.IsBlending;

    public CameraControlFSM FSM { get; private set; }

    public void Initialize(Camera mainCam)
    {
        _mainCam = mainCam;

        _brainCam = mainCam.GetComponent<CinemachineBrain>();
        _brainCamEvents = mainCam.GetComponent<CinemachineBrainEvents>();

        _brainCamEvents.BlendCreatedEvent.AddListener(OnBlendCreated);
        // _brainCamEvents.BrainUpdatedEvent.AddListener(OnBlendUpdated);
        _brainCamEvents.BlendFinishedEvent.AddListener(OnBlendFinished);

        var cinControllers = FindObjectsByType<CinemachineCameraController>(FindObjectsSortMode.None);
        foreach (var controller in cinControllers)
        {
            RegisterCinemachineCameraController(controller);
        }

        //---------------------------------------//

        FSM = new CameraControlFSM(this);

        FSM.AddState(CinemachineCameraType.Free, gameObject.AddComponent<CameraFreeState>());
        FSM.AddState(CinemachineCameraType.Focused, gameObject.AddComponent<CameraFocusedState>());
        FSM.AddState(CinemachineCameraType.FollowTarget, gameObject.AddComponent<CameraFollowTargetState>());
        FSM.AddState(CinemachineCameraType.Manual, gameObject.AddComponent<CameraManualState>());

        FSM.Enable(CinemachineCameraType.Free);
    }

    private void OnBlendCreated(CinemachineCore.BlendEventParams arg0)
    {
        if (_eventBlocked)
            return;

        _eventBlocked = true;

        InputManager.Instance.BlockEventCount++;
    }

    //private void OnBlendUpdated(CinemachineBrain brain)
    //{

    //}
    private void OnBlendFinished(ICinemachineMixer mixer, ICinemachineCamera camera)
    {
        if (_eventBlocked == false)
            return;

        _eventBlocked = false;

        InputManager.Instance.BlockEventCount--;
    }

    public void Release()
    {
        FSM.Release();
        _brainCamEvents.BlendCreatedEvent.RemoveAllListeners();
        _brainCamEvents.BlendFinishedEvent.RemoveAllListeners();
        _cinemachineControllers.Clear();
    }

    public void RequestCameraFocus(Transform transform)
    {
        // TODO : 추후 현재 게임 Phase 에 따라서 막기도 해야할까? 아니면
        // 막을 필요없이 Phase 에 따라서 애초에 Interact 가 발생하면 안됐을까? 후자일거같긴함.
        FSM.ChangeState(CinemachineCameraType.Focused, args: new object[] { transform });
    }

    public void SetManualStatePosition(Vector3 position)
    {
        if (FSM.Current_State == CinemachineCameraType.Manual)
            (FSM.Current as CameraManualState).SetPosition(position);
        else FSM.ChangeState(CinemachineCameraType.Manual, false, new object[] { position });
    }

    public void SetManualStatePositionAndFov(Vector3 position, float fov)
    {
        if (FSM.Current_State == CinemachineCameraType.Manual)
            (FSM.Current as CameraManualState).SetPositionAndFov(position, fov);
        else FSM.ChangeState(CinemachineCameraType.Manual, false, new object[] { position, fov });
    }

    public void RegisterCinemachineCameraController(CinemachineCameraController camera)
    {
        if (_cinemachineControllers.ContainsKey(camera.Type))
        {
            _cinemachineControllers[camera.Type] = camera;
        }
        else
        {
            _cinemachineControllers.Add(camera.Type, camera);
        }
    }

    // 쓸일없을듯?
    //public void UnregisterCinemachineCameraController(CinemachineCameraType type)
    //{
    //    _cinemachineControllers[type] = null;
    //}

    public CinemachineCameraController SetCurrentCinemachine(CinemachineCameraType type, bool syncPosition)
    {
        foreach (var controller in _cinemachineControllers.Values)
        {
            bool isTarget = type == controller.Type;

            if (syncPosition && isTarget)
            {
                controller.SetTransformPosRot(_mainCam.transform);
            }

            controller.SetPriority(isTarget ? 1 : 0);
        }

        return _cinemachineControllers[type];
    }
}
