using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    public static bool HasInstance => _instance && _isAppQuitting == false;
    private static bool _isAppQuitting;

    public static T Instance
    {
        get
        {
            if (_isAppQuitting)
                return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
                    }

                    if (_instance.IsCreationRoutineDone == false)
                    {
                        _instance.RunCreationRoutine();
                    }
                }
            }

            return _instance;
        }
    }

    [SerializeField]
    private bool _autoInitialize;
    public bool AutoInitialize => _autoInitialize;

    public bool IsCreationRoutineDone { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (_instance == null && _isAppQuitting == false)
        {
            _instance = this as T;
            RunCreationRoutine();
            if (_instance.transform.parent == null)
                DontDestroyOnLoad(_instance.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            OnDestroyed();
        }
    }

    private void OnApplicationQuit()
    {
        _isAppQuitting = true;
        _instance = null;
    }

    public virtual void Initialize()
    {
        if (IsInitialized)
        {
            TEMP_Logger.Err($"Duplicate Initialize() call");
            return;
        }

        IsInitialized = true;
    }

    private void RunCreationRoutine()
    {
        IsCreationRoutineDone = true;
        transform.Reset();
        if (AutoInitialize)
            Initialize();
    }

    public virtual void Release()
    {
        Destroy(gameObject);
        IsInitialized = false;
        IsCreationRoutineDone = false;
    }

    protected virtual void OnDestroyed() { }
}
