using System;
using UnityEngine;

public class FXRangeIndicator : FXBase
{
    [SerializeField]
    private SpriteRenderer _renderer;

    [SerializeField]
    private string _rangeSpriteKey;

    Color _color;
    float _range;

    bool _infoSetUp;

    float _duration = 0f;
    float _enabledTimeAt;

    public override bool ActivateLateUpdate => true;

    private void Awake()
    {
        _renderer.enabled = false;
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (_duration > 0f && _enabledTimeAt + _duration < Time.time)
        {
            Return();
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        _renderer.enabled = false;
        _renderer.transform.localScale = Vector3.zero;
        _duration = 0f;
        _enabledTimeAt = 0f;
        _infoSetUp = false;
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(_rangeSpriteKey, (sprite) =>
        {
            if (sprite == null)
            {
                TEMP_Logger.Err($"Failed to load Sprite | key : {_rangeSpriteKey}");
                return;
            }

            _renderer.sprite = sprite;

            UpdateInfo();
        }).Forget();
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        UpdateInfo();
    }

    public void SetInfo(Color color, float range, float duration = 0f)
    {
        _infoSetUp = true;

        _color = color;
        _range = range;
        _duration = duration;

        UpdateInfo();
    }

    void UpdateInfo()
    {
        bool activate = _infoSetUp && _renderer.sprite != null;

        if (activate)
        {
            _renderer.color = _color;
            _renderer.transform.localScale = new Vector3(_range, _range, 1f);
        }

        if (_renderer.enabled == false && activate)
            _enabledTimeAt = Time.time;

        _renderer.enabled = activate;
    }

    public void SetColor(Color color)
    {
        _renderer.color = color;
    }
}
