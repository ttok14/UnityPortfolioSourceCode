using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICommonProgressElement : PoolableObjectBase
{
    public enum VisibleFlag
    {
        None = 0,

        Icon = 0x1,
        Text = 0x1 << 1,
        Slider = 0x1 << 2
    }

    public struct Info
    {
        public string iconKey;
        public string text;
        public float progressValue;
    }

    [SerializeField]
    Image _iconImg;
    [SerializeField]
    TextMeshProUGUI _numberTxt;
    [SerializeField]
    Slider _slider;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _iconImg.enabled = false;
        _numberTxt.enabled = false;
        _slider.gameObject.SetActive(false);
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        _slider.value = 1f;
    }

    public override void OnInactivated()
    {
        base.OnInactivated();
    }

    public void SetInfo(VisibleFlag flags, Info info)
    {
        _iconImg.enabled = false;
        if (flags.HasFlag(VisibleFlag.Icon))
        {
            if (string.IsNullOrEmpty(info.iconKey) == false)
            {
                AssetManager.Instance.LoadAsyncCallBack<Sprite>(info.iconKey, (sprite) =>
                {
                    _iconImg.sprite = sprite;
                    _iconImg.enabled = true;
                }).Forget();
            }
            else
            {
                TEMP_Logger.Err($"Given IconKey is not valid");
            }
        }

        if (flags.HasFlag(VisibleFlag.Text))
        {
            _numberTxt.text = info.text;
            _numberTxt.enabled = true;
        }
        else
        {
            _numberTxt.enabled = false;
        }

        if (flags.HasFlag(VisibleFlag.Slider))
        {
            _slider.value = info.progressValue;
            _slider.gameObject.SetActive(true);
        }
        else
        {
            _slider.gameObject.SetActive(false);
        }
    }

    public void SetSliderValue(float value)
    {
        _slider.value = value;
    }
}
