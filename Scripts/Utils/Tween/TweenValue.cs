using System;
using DG.Tweening;
using DG.Tweening.Core;

public class TweenValue
{
    public float CurrentValue { get; private set; }

    public float BeginValue;
    public float EndValue;
    public float Duration;
    public Ease EaseEffect;

    private Action<float> _onUpdatedListener;
    private Action _onCompletedListener;

    private Tweener _tweener;

    DOGetter<float> _getCurrentValueLambda;
    DOSetter<float> _setCurrentValueLambda;
    TweenCallback _onUpdateCallBack;
    TweenCallback _onCompleteCallBack;

    public TweenValue()
    {
        _getCurrentValueLambda = GetCurrentValue;
        _setCurrentValueLambda = SetCurrentValue;
        _onUpdateCallBack = OnUpdateValue;
        _onCompleteCallBack = OnCompleted;
    }
    public TweenValue(float beginValue, float endValue, float duration, Ease easeEffect, Action<float> onUpdated, Action onCompletedListener)
    {
        CurrentValue = beginValue;
        BeginValue = beginValue;
        EndValue = endValue;
        Duration = duration;
        EaseEffect = easeEffect;
        _onUpdatedListener = onUpdated;
        _onCompletedListener = onCompletedListener;

        _getCurrentValueLambda = GetCurrentValue;
        _setCurrentValueLambda = SetCurrentValue;
        _onUpdateCallBack = OnUpdateValue;
        _onCompleteCallBack = OnCompleted;
    }

    public void StartTween()
    {
        StartTweenInternal();
    }

    public void StartTween(float beginValue, float endValue, float duration, Ease ease, Action<float> onUpdated = null, Action onCompletedListener = null)
    {
        BeginValue = beginValue;
        EndValue = endValue;
        Duration = duration;
        EaseEffect = ease;
        CurrentValue = beginValue;
        _onUpdatedListener = onUpdated;
        _onCompletedListener = onCompletedListener;

        StartTweenInternal();
    }

    public void StopTween()
    {
        if (_tweener != null && _tweener.IsActive())
        {
            _tweener.Kill();
            _tweener = null;
        }
    }

    public bool IsActive()
    {
        return _tweener != null && _tweener.IsActive();
    }

    //public bool IsPlaying()
    //{
    //    return _tweener != null && _tweener.IsActive() && _tweener.IsPlaying();
    //}

    private void StartTweenInternal()
    {
        if (_tweener != null && _tweener.IsActive())
            _tweener.Kill();

        _tweener = DOTween.To(
            _getCurrentValueLambda,
            _setCurrentValueLambda,
            EndValue,
            Duration
        )
        .SetAutoKill(true)
        .SetEase(EaseEffect)
        .OnComplete(_onCompleteCallBack);

        if (_onUpdatedListener != null)
            _tweener.OnUpdate(_onUpdateCallBack);

        _tweener.Play();
    }


    void OnUpdateValue()
    {
        _onUpdatedListener?.Invoke(CurrentValue);
    }

    float GetCurrentValue() => CurrentValue;
    void SetCurrentValue(float value)
    {
        CurrentValue = value;
    }
    void OnCompleted()
    {
        if (_tweener != null && _tweener.IsActive())
            _tweener.Kill();

        // 이 시점에 이미 AutoKill 로 Kill 됐겠지? 별도 Kill 호출 안해도될듯
        _tweener = null;
        _onCompletedListener?.Invoke();
    }

    public void Release()
    {
        if (_tweener != null && _tweener.IsActive())
            _tweener.Kill();
        _tweener = null;
        _onCompletedListener = null;
    }
}
