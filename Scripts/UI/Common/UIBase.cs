using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// *참고* : 하위 클래스는 반드시 <see cref="UIAttribute"/> 를 정의해서 
/// 자신의 정보를 등록해야함
public abstract class UIBase : PoolableObjectBase, ITransition
{
    //[SerializeField]
    //private UILifeCyclePolicy _lifeCyclePolicy;
    //public UILifeCyclePolicy LifeCyclePolicy => _lifeCyclePolicy;

    [SerializeField]
    private UISortPolicy _sortPolicy;
    public UISortPolicy SortPolicy => _sortPolicy;

    [SerializeField]
    protected int _showSfxId;
    [SerializeField]
    protected string _showSfxKey;

    [SerializeField]
    protected int _hideSfxId;
    [SerializeField]
    protected string _hideSfxKey;

    [SerializeField]
    protected CanvasGroup _tweenCanvasGroup;

    public RectTransform RectTf { get; private set; }

    [SerializeField]
    protected TweenSequenceRunner[] _enterTweenSequenceRunners;
    [SerializeField]
    protected TweenSequenceRunner[] _exitTweenSequenceRunners;

    protected virtual Vector3 InitialPosition => Vector3.zero;

    bool _isEntering;

    protected UITrigger _trigger;

    public bool IsEnabled { get; protected set; }

    public bool IsHideableBackButton { get; private set; } = true;

    List<UniTask> _tasks = new List<UniTask>();

    //public virtual async Task Enter()
    //{
    //    await Task.CompletedTask;
    //}

    //public async virtual Task Exit()
    //{
    //    await Task.CompletedTask;
    //}

    // 이 시점에는 보여지기 전임. 보이기 직전에 해야하는 처리는
    // 이 함수에서 처리
    public virtual void OnShow(UITrigger trigger, UIArgBase arg)
    {
        _isEntering = true;

        _trigger = trigger;

        if (_showSfxId != 0)
            AudioManager.Instance.Play((uint)_showSfxId, Vector3.zero, AudioTrigger.UI);

        // 내가 Player - Entity 간 Interaction 으로 생성되었는가
        // 그렇다면 이 Interaction 이 종료되면 나도 종료 
        if (trigger == UITrigger.EntityInteraction)
        {
            EventManager.Instance.Register(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnEntityInteractionEnded);
        }

        if (_tweenCanvasGroup)
        {
            _tweenCanvasGroup.alpha = 0;
        }

        IsEnabled = true;
    }

    private void OnEntityInteractionEnded(EventContext cxt)
    {
        Hide();
    }

    public virtual void OnHide(UIArgBase arg)
    {
        EventManager.Instance.Unregister(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnEntityInteractionEnded);

        if (_hideSfxId != 0)
            AudioManager.Instance.Play((uint)_hideSfxId, Vector3.zero, AudioTrigger.UI);
        else if (string.IsNullOrEmpty(_hideSfxKey) == false)
            AudioManager.Instance.Play(_hideSfxKey, Vector3.zero, AudioTrigger.UI);
    }

    public virtual void Hide()
    {
        if (IsEnabled == false)
        {
            TEMP_Logger.Wrn($"Is Already Hiding ! Duplicate | Key : {Key} ID(Pool) : {ID}");
            return;
        }

        IsEnabled = false;
        UIManager.Instance.Hide(this);
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
        RectTf = GetComponent<RectTransform>();
    }

    public async virtual UniTask Enter()
    {
        if (_showSfxId != 0)
            AudioManager.Instance.Play((uint)_showSfxId, Vector3.zero, AudioTrigger.UI);
        else if (string.IsNullOrEmpty(_showSfxKey) == false)
            AudioManager.Instance.Play(_showSfxKey, Vector3.zero, AudioTrigger.UI);

        await RunTweenRunnerParrelleCo(_enterTweenSequenceRunners);

        _isEntering = false;
    }

    public async virtual UniTask Exit()
    {
        // onShow -> Enter 루틴중일때는 막자
        while (_isEntering)
            await UniTask.Yield();
        await RunTweenRunnerParrelleCo(_exitTweenSequenceRunners);
    }

    protected async UniTask RunTweenRunnerParrelleCo(params TweenSequenceRunner[] runners)
    {
        if (runners.Length > 0)
        {
            _tasks.Clear();

            for (int i = 0; i < runners.Length; i++)
            {
                if (runners[i] == null)
                {
                    TEMP_Logger.Wrn($"Empty Field , Remove this | Key : {Key}");
                    continue;
                }
                _tasks.Add(runners[i].PlaySequenceAsync());
            }

            await UniTask.WhenAll(_tasks);
            // await CoroutineRunner.Instance.RunCoroutineParallel(coroutines);
        }
    }

    //protected IEnumerator RunTweenRunnerParrelleCo(params TweenSequenceRunner[] runners)
    //{
    //    if (runners.Length > 0)
    //    {
    //        var coroutines = new IEnumerator[runners.Length];
    //        for (int i = 0; i < runners.Length; i++)
    //        {
    //            coroutines[i] = runners[i].PlaySequenceCo();
    //        }

    //        yield return CoroutineRunner.Instance.RunCoroutineParallel(coroutines);
    //    }
    //}
}
