using System;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraController : MonoBehaviour
{
    [SerializeField]
    CinemachineCamera _cinemachine;

    [SerializeField]
    CinemachineCameraType _type;
    public CinemachineCameraType Type => _type;

    public float CurrentFov => _cinemachine.Lens.FieldOfView;

    public Transform Target => _cinemachine.Target.TrackingTarget;

    //private void Awake()
    //{
    //    if (CameraManager.HasInstance)
    //    {
    //        CameraManager.Instance.InGameController.RegisterCinemachineCameraController(_type, this);
    //    }
    //}

    //private void OnDestroy()
    //{
    //    if (CameraManager.HasInstance)
    //    {
    //        CameraManager.Instance.InGameController.UnregisterCinemachineCameraController(_type);
    //    }
    //}

    public void SetPriority(int priority)
    {
        _cinemachine.Priority = priority;
    }

    public void SetTransformPosRot(Transform ts)
    {
        _cinemachine.ForceCameraPosition(ts.position, ts.rotation);
    }

    public void MoveTarget(Vector2 dir, float amount)
    {
        if (_cinemachine.Follow == null)
        {
            TEMP_Logger.Err($"Cinmeachine No Target Set | CamType : {_type} , Go name : {gameObject.name}");
            return;
        }

        var movement = dir * amount;
        _cinemachine.Follow.transform.position += new Vector3(movement.x, 0, movement.y);
    }

    public void SetTargetPosition(Vector3 position)
    {
        if (_cinemachine.Follow == null)
        {
            TEMP_Logger.Err($"Cinmeachine No Target Set | CamType : {_type} , Go name : {gameObject.name}");
            return;
        }

        _cinemachine.Follow.transform.position = position;
    }

    public void ChangeFollowTarget(Transform target)
    {
        _cinemachine.Target.TrackingTarget = target;
    }

    public void SetTargetPositionAndFov(Vector3 position, float fov)
    {
        SetTargetPosition(position);
        _cinemachine.Lens.FieldOfView = fov;
    }

    public void SetFov(float fov)
    {
        _cinemachine.Lens.FieldOfView = fov;
    }

    public void Zoom(float amount)
    {
        _cinemachine.Lens.FieldOfView += amount;
    }
}
