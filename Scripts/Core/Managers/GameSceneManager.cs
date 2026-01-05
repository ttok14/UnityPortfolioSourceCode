using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : SingletonBase<GameSceneManager>
{
    private SCENES _currentScene = SCENES.None;
    public SCENES CurrentScene => _currentScene;

    Dictionary<SCENES, Scene> _loadedScenes = new Dictionary<SCENES, Scene>();

    // private Dictionary<SCENES, SceneBase<>

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var prevScene = _currentScene;

        try
        {
            _currentScene = Enum.Parse<SCENES>(scene.name);
            _loadedScenes.Add(_currentScene, scene);
            SceneManager.SetActiveScene(scene);
        }
        catch (Exception exp)
        {
            TEMP_Logger.Err($"Scene 파싱중 에러 발생 (씬 이름과 Enum 요소가 일치하는지 체크해볼 것) : {exp}");
        }

        EventManager.Instance.Publish(GLOBAL_EVENT.NEW_SCENE_LOADED, new SceneLoadedEventArgs(prevScene, _currentScene));
    }

    private void OnSceneUnloaded(Scene scene)
    {
        _loadedScenes.Remove(Enum.Parse<SCENES>(scene.name));
        EventManager.Instance.Publish(GLOBAL_EVENT.SCENE_UNLOADED, new SceneUnloadedEventArgs(Enum.Parse<SCENES>(scene.name)));
    }

    public void LoadSceneSync(SCENES newScene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.NEW_SCENE_LOAD_ENTER, new SceneLoadEnterEventArgs(_currentScene, newScene));
        SceneManager.LoadScene(newScene.ToString(), mode);
    }

    public IEnumerator LoadSceneAsyncCo(SCENES newScene, LoadSceneMode mode = LoadSceneMode.Single, Action onCompleted = null)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.NEW_SCENE_LOAD_ENTER, new SceneLoadEnterEventArgs(_currentScene, newScene));
        var op = SceneManager.LoadSceneAsync(newScene.ToString(), mode);

        while (op.isDone == false)
        {
            yield return null;
        }

        onCompleted?.Invoke();
    }

    public async Task LoadSceneAsync(SCENES newScene, LoadSceneMode mode = LoadSceneMode.Single, Action onCompleted = null)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.NEW_SCENE_LOAD_ENTER, new SceneLoadEnterEventArgs(_currentScene, newScene));
        await SceneManager.LoadSceneAsync(newScene.ToString(), mode);
        onCompleted?.Invoke();
    }

    /// <summary>
    /// 씬 비동기 로드 , *주의* 반드시 리턴 <see cref="SceneLoadHandle.AllowActivate"/> 를 호출해주어야만 다음으로 넘어감.
    /// </summary>
    /// <param name="newScene"></param>
    /// <param name="mode"></param>
    /// <returns> 비동기 제어 객체 </returns>
    public SceneLoadHandle LoadSceneAsyncWithHandle(SCENES newScene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.NEW_SCENE_LOAD_ENTER, new SceneLoadEnterEventArgs(_currentScene, newScene));
        var operation = SceneManager.LoadSceneAsync(newScene.ToString(), mode);
        operation.allowSceneActivation = false;
        return new SceneLoadHandle(operation);
    }

    public AsyncOperation UnloadAsync(SCENES scene)
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.SCENE_UNLOAD_ENTER, new SceneUnloadEnterEventArgs(scene));
        return SceneManager.UnloadSceneAsync(scene.ToString());
    }
}
