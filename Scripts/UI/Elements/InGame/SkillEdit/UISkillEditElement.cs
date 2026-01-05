using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISkillEditElement : MonoBehaviour
{
    public enum Type
    {
        None = 0,

        CurrentEquippedSkill,
        CurrentEquippedSpell,
        InventoryElement
    }

    [SerializeField]
    Button _btn;

    [SerializeField]
    Image _iconImg;

    [SerializeField]
    TextMeshProUGUI _nameTxt;

    public uint SkillTableId { get; private set; }

    public Type ElementType { get; private set; }

    Action<UISkillEditElement> _onClickHandler;

    public bool Set(Type type, uint skillId, Action<UISkillEditElement> onClickHandler)
    {
        if (SetSkillID(skillId) == false)
            return false;

        ElementType = type;

        _onClickHandler = onClickHandler;

        return true;
    }

    public bool SetSkillID(uint id)
    {
        var data = DBSkill.Get(id);

        if (data == null)
        {
            TEMP_Logger.Err($"Failed to get SkillData | ID : {id}");
            return false;
        }

        if (string.IsNullOrEmpty(data.IconKey) == false)
        {
            AssetManager.Instance.LoadAsyncCallBack<Sprite>(data.IconKey, (res) =>
            {
                if (gameObject)
                {
                    _iconImg.sprite = res;
                }
            }).Forget();
        }
        else
        {
            TEMP_Logger.Err($"Given SKill has no IconKey | SkillID : {id}");
            return false;
        }

        _nameTxt.text = data.Name;

        SkillTableId = id;

        return true;
    }

    public void SetInteractable(bool interactable)
    {
        _btn.interactable = interactable;
    }

    public void OnClick()
    {
        _onClickHandler?.Invoke(this);
    }
}
