using UnityEngine;
using DG.Tweening;

public class FXFollowTargetTweenScale : FXBase
{
    private Tweener _tween;
    private Transform _followTarget;
    private Vector3 _currentTargetScale;

    [SerializeField]
    private float _duration = 0.5f;

    [SerializeField]
    private Ease _ease = Ease.OutQuad;

    public override bool ActivateLateUpdate => true;

    public void SetTargetScale(Vector3 targetScale, Transform followTarget = null)
    {
        if (followTarget == null)
        {
            Return();
            return;
        }

        _followTarget = followTarget;
        transform.position = _followTarget.position;

        if (_tween != null && _tween.IsActive() && _currentTargetScale == targetScale)
        {
            return;
        }

        _currentTargetScale = targetScale;

        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill();
        }

        transform.localScale = targetScale * 0.7f;

        _tween = transform.DOScale(targetScale, _duration)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (IsActivated == false)
            return;

        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
        }
        else
        {
            Return();
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        _followTarget = null;
        _currentTargetScale = Vector3.zero;

        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill();
            _tween = null;
        }

        transform.localScale = Vector3.one;
    }
}
