using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.U2D;

public class UIManager : SingletonBase<UIManager>
{
    private Dictionary<Type, UIAttribute> _attributeCache = new Dictionary<Type, UIAttribute>(64);

    [SerializeField]
    private List<UILayerGroupEntry> _layerEntries;

    private UIStack _uiStack = new UIStack();
    private UISortSystem _sortSystem = new UISortSystem();

    public override void Initialize()
    {
        base.Initialize();

        EventManager.Instance.Register(GLOBAL_EVENT.NEW_SCENE_LOADED, OnSceneLoaded);
        EventManager.Instance.Register(GLOBAL_EVENT.SCENE_UNLOADED, OnSceneUnloaded);

        SpriteAtlasManager.atlasRequested += OnAtlasRequested;

        #region ===:: Attribute 캐싱하기 ::===

        var allUITypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(UIBase)) && type.IsAbstract == false)
            .ToList();

        foreach (var type in allUITypes)
        {
            if (type.IsDefined(typeof(UIAttribute), false))
            {
                _attributeCache.Add(type, type.GetCustomAttribute<UIAttribute>());
            }
            else
            {
                TEMP_Logger.Err($"{nameof(UIBase)} 를 상속받는 클래스({type})는 {nameof(UIAttribute)} 속성을 선언해야 합니다.");
            }
        }

        #endregion

        var layerEntryDict = new Dictionary<UILayer, UILayerGroupEntry>(_layerEntries.Count);

        foreach (var entry in _layerEntries)
        {
            layerEntryDict.Add(entry.layer, entry.Clone());
        }

        _sortSystem.Initialize(layerEntryDict);

        #region ===:: 카메라 가져와 세팅하기 ::===

        CoroutineRunner.Instance.RunCoroutine(FetchCamera());

        #endregion
    }

    public override void Release()
    {
        base.Release();

        SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
    }

    private void OnAtlasRequested(string key, Action<SpriteAtlas> onCompleted)
    {
        TEMP_Logger.Deb($"Atlas Requested | key : {key}");

        AssetManager.Instance.LoadAsyncCallBack<SpriteAtlas>(key, (res) =>
        {
            onCompleted?.Invoke(res);
        }).Forget();
    }

    public void Show<T>(UITrigger trigger = UITrigger.Default, UIArgBase arg = null) where T : UIBase
    {
        ShowCallBack<T>(trigger, arg).Forget();
    }

    public async UniTaskVoid ShowCallBack<T>(UITrigger trigger = UITrigger.Default, UIArgBase arg = null, Action<T> onCompleted = null) where T : UIBase
    {
        var res = await ShowAsync<T>(trigger, arg);

        onCompleted?.Invoke(res);
    }

    //public void Show<T>(UITrigger trigger = UITrigger.Default, UIArgBase arg = null, Action<T> onCompleted = null) where T : UIBase
    //{
    //    if (CoroutineRunner.HasInstance)
    //        CoroutineRunner.Instance.RunCoroutine(ShowCo<T>(trigger, arg, onCompleted));
    //}

    //public void Show(string typeName, UITrigger trigger = UITrigger.Default, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    if (CoroutineRunner.HasInstance)
    //        CoroutineRunner.Instance.RunCoroutine(ShowCo(Type.GetType(typeName), trigger, arg, onCompleted));
    //}

    public async UniTaskVoid Show(Type type, UITrigger trigger = UITrigger.Default, UIArgBase arg = null)
    {
        await ShowAsync(type, trigger, arg);
    }

    public IEnumerator ShowCo<T>(UITrigger trigger = UITrigger.Default, UIArgBase arg = null) where T : UIBase
    {
        bool done = false;
        ShowAsyncCallBack(typeof(T), trigger, arg, (res) =>
        {
            done = true;
        }).Forget();

        while (done == false)
        {
            yield return null;
        }
    }

    public IEnumerator ShowCo(Type type, UITrigger trigger, UIArgBase arg = null)
    {
        bool done = false;
        ShowAsyncCallBack(type, trigger, arg, (res) =>
        {
            done = true;
        }).Forget();

        while (done == false)
        {
            yield return null;
        }
    }

    public async UniTaskVoid ShowAsyncCallBack(Type type, UITrigger trigger, UIArgBase arg, Action<UIBase> onCompleted)
    {
        var res = await ShowAsync(type, trigger, arg);
        onCompleted?.Invoke(res);
    }

    public async UniTask<T> ShowAsync<T>(UITrigger trigger, UIArgBase arg = null) where T : UIBase
    {
        return await ShowAsync(typeof(T), trigger, arg) as T;
    }

    public async UniTask<UIBase> ShowAsync(Type type, UITrigger trigger, UIArgBase arg = null)
    {
        var result = await PoolManager.Instance.RequestSpawnAsync<UIBase>(ObjectPoolCategory.UI, _attributeCache[type].key, null);

        var resultUI = result.instance;

        _uiStack.Push(resultUI);
        RunSort(resultUI);
        resultUI.OnShow(trigger, arg);

        await resultUI.Enter();

        return resultUI;
    }

    //public IEnumerator ShowCo(Type type, UITrigger trigger, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    UIBase resultUI = null;

    //    yield return PoolManager.Instance.RequestSpawnCo(ObjectPoolCategory.UI, _attributeCache[type].key, null, onCompleted: (go, opRes) =>
    //    {
    //        resultUI = go as UIBase;

    //        _uiStack.Push(resultUI);
    //        RunSort(resultUI);
    //        resultUI.OnShow(trigger, arg);
    //    });

    //    yield return resultUI.Enter();

    //    onCompleted?.Invoke(resultUI);
    //}

    //public IEnumerator Prepare<T>(Action<T> onCompleted = null) where T : UIBase
    //{
    //    var type = typeof(T);

    //    yield return PoolManager.Instance.PrepareCo<T>(ObjectPoolCategory.UI, _attributeCache[type].key, 1, onCompleted: (results) =>
    //      {
    //          onCompleted?.Invoke(results[0]);
    //      });
    //}

    public IEnumerator PrepareCo(Type type, Action onCompleted = null)
    {
        yield return PrepareCo(new Type[] { type }, onCompleted);
    }

    public IEnumerator PrepareCo(Type[] types, Action onCompleted = null)
    {
        if (types == null || types.Length == 0)
        {
            onCompleted?.Invoke();
            yield break;
        }

        // var coroutines = new IEnumerator[types.Length];
        var tasks = new UniTask[types.Length];
        UIBase[] results = new UIBase[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            tasks[i] = PoolManager.Instance.PrepareAsync(
                ObjectPoolCategory.UI,
                _attributeCache[types[i]].key,
                1);
        }

        yield return UniTask.WhenAll(tasks).ToCoroutine();

        onCompleted?.Invoke();
    }

    public async UniTask HideAsyncCallBack<T>(UIArgBase arg = null, Action<T> onCompleted = null) where T : UIBase
    {
        var ui = _uiStack.Pop<T>();
        if (ui)
            await HideAsyncCallBack(ui, arg, (result) => onCompleted?.Invoke(result as T));
    }

    public void Hide<T>(UIArgBase arg = null, Action<T> onCompleted = null) where T : UIBase
    {
        var ui = _uiStack.Pop<T>();
        if (ui)
            HideAsyncCallBack(ui, arg, (res) => onCompleted?.Invoke(res as T)).Forget();
    }

    public void Hide(UIBase ui, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    {
        _uiStack.Remove(ui);
        HideAsyncCallBack(ui, arg, onCompleted).Forget();
    }

    public async UniTask HideAsync(UIBase ui, UIArgBase arg = null)
    {
        _uiStack.Remove(ui);
        await HideInternal(ui, arg);

    }

    public async UniTask HideAsyncCallBack<T>(T ui, UIArgBase arg = null, Action<T> onCompleted = null) where T : UIBase
    {
        await HideInternal(ui, arg);
        onCompleted?.Invoke(ui);
    }

    public async UniTask HideAsyncCallBack(UIBase ui, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    {
        await HideInternal(ui, arg);
        onCompleted?.Invoke(ui);
    }

    public async UniTask HideAsyncCallBack(string key, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    {
        var ui = _uiStack.Pop(key);
        await HideInternal(ui, arg);
        onCompleted?.Invoke(ui);
    }

    public void Hide(Type type, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    {
        var ui = FindUI(type);
        Hide(ui, arg, onCompleted);
    }

    public void HideAll<T>(UIArgBase arg = null) where T : UIBase
    {
        int count = _uiStack.GetCount<T>();

        for (int i = 0; i < count; i++)
        {
            Hide<T>(arg);
        }
    }

    public async UniTask HideAsync(Type type, UIArgBase arg = null)
    {
        var ui = FindUI(type);
        await HideAsync(ui, arg);
    }

    public bool Remove<T>(UIArgBase hideArg = null) where T : UIBase
    {
        var ui = _uiStack.Pop<T>();

        // 현재 Show 중인애가 있으면 걔를 먼저 삭제 
        if (ui)
        {
            _sortSystem.Unregister(ui);

            ui.OnHide(hideArg);

            if (PoolManager.HasInstance)
                return PoolManager.Instance.Remove(ui);
        }
        // 현재 Show 중인애가 없으면 
        else
        {
            if (PoolManager.HasInstance)
                return PoolManager.Instance.Remove(ObjectPoolCategory.UI, _attributeCache[typeof(T)].key);
        }

        return false;
    }

    //public IEnumerator HideCo(Type type, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    var ui = FindUI(type);
    //    yield return HideCo(ui, arg, onCompleted);
    //}

    //public void HideNext(UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    var ui = _uiStack.Pop();
    //    CoroutineRunner.Instance.RunCoroutine(HideInternal(ui, arg, onCompleted));
    //}

    //public IEnumerator HideNextCo(UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    var ui = _uiStack.Pop();
    //    yield return CoroutineRunner.Instance.RunCoroutine(HideInternal(ui, arg, onCompleted));
    //}

    //public void HideFromToEnd<T>() where T : UIBase
    //{
    //    HideByCount(_uiStack.CountFrom(typeof(T)));
    //}

    public T FindUI<T>() where T : UIBase
    {
        return _uiStack.Find<T>();
    }

    public UIBase FindUI(Type type)
    {
        return _uiStack.Find(type);
    }

    //public void HideAll()
    //{
    //    while (_uiStack.Count > 0)
    //    {
    //        HideNext();
    //    }
    //}

    //private void HideAll(Predicate<UIBase> condition)
    //{
    //    var uis = _uiStack.PopWithCondition(condition);
    //    if (uis != null)
    //    {
    //        CoroutineRunner.Instance.RunCoroutineParallel(uis.Select(t => HideInternal(t)).ToArray());
    //    }
    //}

    private async UniTask HideInternal(UIBase ui, UIArgBase arg = null)
    {
        if (ui == null)
            return;

        await ui.Exit();
        //yield return ui.Exit();

        _sortSystem.Unregister(ui);
        ui.OnHide(arg);

        if (PoolManager.HasInstance)
            PoolManager.Instance.Return(ui);
    }

    //private IEnumerator HideInternal(UIBase ui, UIArgBase arg = null, Action<UIBase> onCompleted = null)
    //{
    //    if (ui == null)
    //    {
    //        onCompleted?.Invoke(null);
    //        yield break;
    //    }

    //    yield return ui.Exit();

    //    _sortSystem.Unregister(ui);
    //    ui.OnHide(arg);
    //    PoolManager.Instance.Return(ui);

    //    onCompleted?.Invoke(ui);
    //}

    private void OnSceneLoaded(EventContext cxt)
    {
        //var arg = args as SceneLoadedEventArgs;
    }

    private void OnSceneUnloaded(EventContext cxt)
    {
        // var arg = args as SceneUnloadedEventArgs;
        // 그 다음 UI 가 뜰 시간을 적절히 벌어줌
        // TODO: 조금더 명확한 시스템을 만드는게 좋을듯.
        // CoroutineRunner.Instance.RunCoroutineFrameDelay(2, () => CleanSceneBoundUIs(arg.scene));
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        EventManager.Instance.Unregister(GLOBAL_EVENT.NEW_SCENE_LOADED, OnSceneLoaded);
        EventManager.Instance.Unregister(GLOBAL_EVENT.SCENE_UNLOADED, OnSceneLoaded);
    }

    IEnumerator FetchCamera()
    {
        yield return new WaitUntil(() => CameraManager.Instance.IsInitialized);

        // 현재 시스템상, CameraManager 에 카메라가 등록되는 시점이 이 시점에 
        // 완료됐을 거란 보장이 없기 때문에 , 가져다 쓰는 입장에서는 
        // 대기하는 것이 적절하다고 판단. 
        foreach (var entry in _layerEntries)
        {
            var uiCamera = CameraManager.Instance.GetUICamera(entry.layer);
            if (uiCamera)
            {
                entry.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                entry.canvas.worldCamera = uiCamera;

                TEMP_Logger.Deb($"Fetched Camera Successfully : {entry.layer}");
            }
            else
            {
                yield return null;
            }
        }
    }

    //private void CleanSceneBoundUIs(SCENES scene)
    //{
    //    HideAll((ui) => ui.LifeCyclePolicy.lifeCycleType == UILifeCycleType.SceneBound && ui.LifeCyclePolicy.sceneBoundTo == scene);
    //}

    //private void HideByCount(int count)
    //{
    //    for (int i = 0; i < count; i++)
    //    {
    //        CoroutineRunner.Instance.RunCoroutine(HideNextCo());
    //    }
    //}

    private void RunSort(UIBase ui)
    {
        _sortSystem.Register(ui);
    }
}
