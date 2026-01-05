using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(nameof(UIFloatingText))]
public class UIFloatingText : UIWorldFollowBase
{
    public class Arg : WorldFollowArg
    {
        public Color color;
        public string text;
        public string spriteKey;

        public Arg(Color color, string text, string spriteKey, Transform followTarget, Vector2 uiOffsetPos) : base(followTarget, uiOffsetPos)
        {
            this.color = color;
            this.spriteKey = spriteKey;
            this.text = text;
        }
    }

    [SerializeField]
    Image _iconImg;
    [SerializeField]
    TextMeshProUGUI _txt;

    [SerializeField]
    private float _floatingMoveSpeed;

    protected override bool ShouldShow => Time.time <= _startFadeAt;
    protected override Vector2 AnchoredPositionOffset => _anchoredPosOffset;
    Vector2 _anchoredPosOffset;

    float _startFadeAt;

    protected override void OnBecomeTransparent()
    {
        Hide();
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _iconImg.enabled = false;
        _txt.enabled = false;
    }

    public override void OnShow(UITrigger trigger, UIArgBase argBase)
    {
        base.OnShow(trigger, argBase);

        _txt.enabled = true;

        var arg = argBase as Arg;
        if (arg == null)
        {
            _txt.color = Color.white;
            _iconImg.enabled = false;
        }
        else
        {
            _txt.color = arg.color;
            _txt.SetText(arg.text);

            if (string.IsNullOrEmpty(arg.spriteKey))
            {
                _iconImg.enabled = false;
            }
            else
            {
                AssetManager.Instance.LoadAsyncCallBack<Sprite>(arg.spriteKey, (res) =>
                {
                    _iconImg.sprite = res;
                    _iconImg.enabled = true;
                }).Forget();
            }
        }

        _anchoredPosOffset = Vector2.zero;
        _startFadeAt = Time.time + 1f;
    }

    protected override void OnHudUpdated()
    {
        base.OnHudUpdated();

        _anchoredPosOffset += Vector2.up * _floatingMoveSpeed * Time.deltaTime;
    }
}
