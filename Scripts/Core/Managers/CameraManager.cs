using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class CameraManager : SingletonBase<CameraManager>
{
    private Dictionary<UILayer, Camera> _uiCameras = new Dictionary<UILayer, Camera>();
    private Camera _mainCamera;

    CameraRenderType _uiCamOriRenderType;

    public InGameCameraController InGameController { get; private set; }

    public Camera MainCam
    {
        get
        {
            if (!_mainCamera)
            {
                _mainCamera = Camera.main;

                if (_mainCamera)
                {
                    EventManager.Instance.Publish(GLOBAL_EVENT.MAIN_CAMERA_REGISTERED, new MainCamRegisteredEventArg() { camera = _mainCamera });
                }
            }

            return _mainCamera;
        }
    }

    public override void Initialize()
    {
        CoroutineRunner.Instance.RunCoroutine(InitUICamera());
    }

    public void PrepareInGame(InGameCameraController controller)
    {
        ResolveUrpSettings(MainCam, GetUICamera(UILayer.Panel));

        InGameController = controller;
        InGameController.Initialize(MainCam);
    }

    public override void Release()
    {
        base.Release();
    }

    IEnumerator InitUICamera()
    {
        TEMP_Logger.Deb("UICamera 초기화를 대기합니다.");

        var layers = (UILayer[])Enum.GetValues(typeof(UILayer));

        yield return new WaitUntil(() =>
        {
            if (_uiCameras.Count != layers.Length)
            {
                return false;
            }

            foreach (var cam in _uiCameras)
            {
                if (cam.Value == null)
                {
                    return false;
                }
            }

            return true;
        });

        base.Initialize();

        TEMP_Logger.Deb("UICamera 초기화가 완료됐습니다.");
    }

    public void ExitInGame()
    {
        InGameController.Release();

        var uiCam = GetUICamera(UILayer.Panel);
        uiCam.GetUniversalAdditionalCameraData().renderType = _uiCamOriRenderType;
    }

    public Camera GetUICamera(UILayer layer)
    {
        if (_uiCameras.TryGetValue(layer, out var camera))
        {
            return camera;
        }
        return null;
    }

    public void RegisterMainCamera(Camera camera)
    {
        _mainCamera = camera;
        EventManager.Instance.Publish(GLOBAL_EVENT.MAIN_CAMERA_REGISTERED, new MainCamRegisteredEventArg() { camera = camera });
    }

    public void RegisterUICamera(UILayer layer, Camera camera)
    {
        if (_uiCameras.ContainsKey(layer))
        {
            _uiCameras[layer] = camera;
        }
        else
        {
            _uiCameras.Add(layer, camera);
        }
    }

    public void UnregisterUICamera(UILayer layer)
    {
        _uiCameras[layer] = null;
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        //EventManager.Instance.Unregister(EVENTS.NEW_SCENE_LOAD_ENTER, OnSceneLoadEnter);
    }

    private void ResolveUrpSettings(Camera mainCamera, Camera uiCam)
    {
        // URP 에서 Base 와 Overlay 설정 
        var mainData = mainCamera.GetUniversalAdditionalCameraData();
        mainData.renderType = CameraRenderType.Base;
        if (mainData.cameraStack.Contains(uiCam) == false)
        {
            mainData.cameraStack.Add(uiCam);
        }
        _uiCamOriRenderType = uiCam.GetUniversalAdditionalCameraData().renderType;
        uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
    }
}
