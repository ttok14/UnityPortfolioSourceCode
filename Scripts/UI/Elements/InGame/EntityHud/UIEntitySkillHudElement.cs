using UnityEngine;
using UnityEngine.UI;

public class UIEntitySkillHudElement : PoolableObjectBase
{
    [SerializeField]
    Image _iconImg;
    [SerializeField]
    Slider _slider;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _iconImg.gameObject.SetActive(false);
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

        _iconImg.gameObject.SetActive(false);
        _slider.gameObject.SetActive(false);
    }

    public void SetInfo(uint skillId)
    {
        var data = DBSkill.Get(skillId);
        if (data == null)
        {
            TEMP_Logger.Err($"Failed to get SkillData | id : {skillId}");
            return;
        }

        AssetManager.Instance.LoadAsyncCallBack<Sprite>(data.IconKey, (sprite) =>
        {
            _iconImg.sprite = sprite;

            if (_iconImg.gameObject.activeSelf == false)
                _iconImg.gameObject.SetActive(true);
            if (_slider.gameObject.activeSelf == false)
                _slider.gameObject.SetActive(true);
        }).Forget();
    }

    public void SetSliderValue(float value)
    {
        _slider.value = value;
    }
}
