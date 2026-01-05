using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

[UIAttribute(nameof(UIFloatingIcon))]
public class UIFloatingIcon : UIWorldFollowBase
{
    public class Arg : WorldFollowArg
    {
        public string spriteKey;
        public Arg(string spriteKey, Transform followTarget, Vector2 uiOffsetPos) : base(followTarget, uiOffsetPos)
        {
            this.spriteKey = spriteKey;
        }
    }

    [SerializeField]
    RectTransform _uiGroup;

    [SerializeField]
    private Image _iconImg;

    [SerializeField]
    float _floatingMoveSpeed;
    [SerializeField]
    float _displayDuration = 1.0f;

    protected override bool ShouldShow => true;
    protected override Vector2 AnchoredPositionOffset => _anchoredPosOffset;
    private Vector2 _anchoredPosOffset;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
        // transform.localScale = Vector3.zero;
    }

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        if (IsVisible(out var screenPos) == false)
        {
            Hide();
            return;
        }

        var arg = argBase as Arg;

        _iconImg.enabled = false;

        if (string.IsNullOrEmpty(arg.spriteKey) == false)
        {
            AssetManager.Instance.LoadAsyncCallBack<Sprite>(arg.spriteKey, (res) =>
            {
                if (res == null)
                    return;

                _iconImg.sprite = res;
                _iconImg.enabled = true;

                StartEffectRoutine().Forget();
            }).Forget();
        }
    }

    private async UniTaskVoid StartEffectRoutine()
    {
        var seq = DOTween.Sequence();

        seq.Append(_uiGroup.DOScale(1f, 0.25f).
            SetAutoKill(true).
            SetEase(Ease.OutBack));

        seq.Play();

        await seq.AsyncWaitForCompletion();

        await UniTask.Delay(System.TimeSpan.FromSeconds(_displayDuration));

        seq = DOTween.Sequence();

        seq.Append(_uiGroup.DOScale(0f, 0.2f).
            SetAutoKill(true).
            SetEase(Ease.InBack));

        seq.Play();
        await seq.AsyncWaitForCompletion();

        Hide();
    }

    protected override void OnHudUpdated()
    {
        base.OnHudUpdated();

        if (_floatingMoveSpeed > 0f && _iconImg.enabled)
        {
            _anchoredPosOffset += Vector2.up * _floatingMoveSpeed * Time.deltaTime;
        }
    }

    protected override void OnBecomeTransparent() { }
}
