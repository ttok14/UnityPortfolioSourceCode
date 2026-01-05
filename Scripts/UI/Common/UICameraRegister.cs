using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

//[Serializable]
//public class UICameraBinding
//{
//    public UILayer layer;
//    public Camera camera;
//}

[RequireComponent(typeof(Camera))]
public class UICameraRegister : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;
    public Camera Camera { get; private set; }

    // 일단 하나의 UICamera 로 모든 UI 를 찍는다는 가정하에 작업

    //[SerializeField]
    //private List<UILayer> _layers;

    void Awake()
    {
        // CoroutineRunner.Instance.RunCoroutine(InitCameras());
        InitCamerasUni().Forget();
    }

    async UniTaskVoid InitCamerasUni()
    {
        await UniTask.WaitUntil(() => CameraManager.HasInstance);

        DontDestroyOnLoad(this);

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            var prevCamera = CameraManager.Instance.GetUICamera(layer);
            CameraManager.Instance.RegisterUICamera(layer, _camera);

            if (prevCamera)
            {
                Destroy(prevCamera.gameObject);
            }
        }
    }

    IEnumerator InitCameras()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            if (CameraManager.HasInstance == false)
                yield return null;

            var prevCamera = CameraManager.Instance.GetUICamera(layer);
            CameraManager.Instance.RegisterUICamera(layer, _camera);
            DontDestroyOnLoad(this);

            if (prevCamera)
            {
                Destroy(prevCamera.gameObject);
            }
        }
    }

    private void Start()
    {
        EventManager.Instance.Register(GLOBAL_EVENT.NEW_SCENE_LOADED, OnSceneLoaded);
    }

    private void OnSceneLoaded(EventContext cxt)
    {
        var sceneLoaded = cxt.Arg as SceneLoadedEventArgs;
    }
}
