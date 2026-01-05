using System;
using UnityEngine;

public class AudioListenerController : MonoBehaviour, IUpdatable
{
    [SerializeField]
    AudioListener _listener;

    Plane _terrainPlane;

    Camera _mainCamRef;

    // 이거 사이드이펙트 크게 없겟지 ..? 
    const int UpdateIntervalFrame = 3;

    public void Initialize()
    {
        if (EventManager.HasInstance)
            EventManager.Instance.Register(GLOBAL_EVENT.MAIN_CAMERA_REGISTERED, OnMainCameraRegistered);

        _terrainPlane = new Plane(Vector3.up, Vector3.zero);

        UpdateManager.Instance.RegisterSingleLateUpdatable(this);
    }

    // void LateUpdate()
    // 실제 호출 시점은 Late 업데이트임
    void IUpdatable.OnUpdate()
    {
        if (Time.frameCount % UpdateIntervalFrame != 0)
            return;

        if (!_mainCamRef)
            return;

        var ray = _mainCamRef.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (_terrainPlane.Raycast(ray, out var enter))
        {
            var point = ray.GetPoint(enter);
            point.y = 5f;
            _listener.transform.position = point;
        }
    }

    private void OnDestroy()
    {
        if (EventManager.HasInstance)
            EventManager.Instance.Unregister(GLOBAL_EVENT.MAIN_CAMERA_REGISTERED, OnMainCameraRegistered);

        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingleLateUpdatable(this);
    }

    private void OnMainCameraRegistered(EventContext cxt)
    {
        var cam = (cxt.Arg as MainCamRegisteredEventArg).camera;
        _mainCamRef = cam;
    }

}
