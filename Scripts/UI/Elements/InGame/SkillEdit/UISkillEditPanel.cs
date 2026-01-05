using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameDB;
using TMPro;
using System.Linq;

[UIAttribute("UISkillEditPanel")]
public class UISkillEditPanel : UIBase
{
    enum Status
    {
        None = 0,

        Browse,
        Switching
    }

    public class Arg : UIArgBase
    {
    }

    [Serializable]
    class Group
    {
        public RectTransform ElementRoot;

        [HideInInspector]
        public List<UISkillEditElement> UIs = new List<UISkillEditElement>();
        public uint[] IDs;

        public UISkillEditElement TryGet(uint id)
        {
            return UIs.Find(t => t.SkillTableId == id);
        }

        public bool ChangeID(uint oriId, uint newId)
        {
            for (int i = 0; i < IDs.Length; i++)
            {
                if (IDs[i] == oriId)
                {
                    IDs[i] = newId;
                    return true;
                }
            }

            return false;
        }

        public void SetInteractableById(uint id)
        {
            for (int i = 0; i < UIs.Count; i++)
            {
                UIs[i].SetInteractable(UIs[i].SkillTableId == id);
            }
        }

        public void SetInteractableByCategoryAndBlockDuplicate(E_SkillCategoryType ctg, params uint[] duplicate)
        {
            for (int i = 0; i < UIs.Count; i++)
            {
                UIs[i].SetInteractable(
                    DBSkill.GetCategory(UIs[i].SkillTableId) == ctg &&
                    (duplicate == null || Array.Exists(duplicate, t => t == UIs[i].SkillTableId) == false));
            }
        }

        public void SetAllInteractable(bool interactable)
        {
            for (int i = 0; i < UIs.Count; i++)
            {
                UIs[i].SetInteractable(interactable);
            }
        }

        public void Release()
        {
            IDs = null;
            if (UIs != null)
            {
                foreach (var ui in UIs)
                {
                    GameObject.Destroy(ui.gameObject);
                }
                UIs.Clear();
            }
        }
    }

    [SerializeField]
    UISkillEditElement _elementSrc;

    [SerializeField]
    Group _equippedSkillGroup;
    [SerializeField]
    Group _equippedSpellGroup;
    [SerializeField]
    Group _inventoryGroup;

    [SerializeField]
    Image _descImg;

    [SerializeField]
    TextMeshProUGUI _descTxt;

    [SerializeField]
    ScrollRect _scroll;

    Status _status;

    UISkillEditElement _currentSwitchingElement;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        InGameManager.Instance.EventListener += OnInGameEvent;

        _status = Status.None;

        _equippedSkillGroup.IDs = Me.GetSkills();
        _equippedSpellGroup.IDs = Me.GetSpells();
        var allSkills = DBSkill.GetEquippableSkills();

        //var skills = DBSkill.GetSkillsByCategory(GameDB.E_SkillCategoryType.Standard);
        //var spells = DBSkill.GetSkillsByCategory(GameDB.E_SkillCategoryType.Spell);

        for (int i = 0; i < _equippedSkillGroup.IDs.Length; i++)
        {
            CreateElement(_equippedSkillGroup, UISkillEditElement.Type.CurrentEquippedSkill, _equippedSkillGroup.IDs[i]);
        }

        for (int i = 0; i < _equippedSpellGroup.IDs.Length; i++)
        {
            CreateElement(_equippedSpellGroup, UISkillEditElement.Type.CurrentEquippedSpell, _equippedSpellGroup.IDs[i]);
        }

        for (int i = 0; i < allSkills.Count; i++)
        {
            CreateElement(_inventoryGroup, UISkillEditElement.Type.InventoryElement, allSkills[i].ID);
        }

        _descImg.enabled = false;

        SetStatus(Status.Browse);

        MainThreadDispatcher.Instance.InvokeInFrames(() =>
        {
            if (IsEnabled == false)
                return;

            _scroll.verticalNormalizedPosition = 1f;
        }, 2);
    }

    public override void OnHide(UIArgBase arg)
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;

        _equippedSkillGroup.Release();
        _equippedSpellGroup.Release();
        _inventoryGroup.Release();

        SetStatus(Status.Browse);
        _currentSwitchingElement = null;

        base.OnHide(arg);
    }

    void ToSwitchMode(UISkillEditElement switchingElement)
    {
        _currentSwitchingElement = switchingElement;
        SetStatus(Status.Switching);
    }

    void ToBrowseMode()
    {
        _currentSwitchingElement = null;
        SetStatus(Status.Browse);
    }

    void SetStatus(Status newStatus)
    {
        if (_status == newStatus)
            return;

        _status = newStatus;

        switch (newStatus)
        {
            case Status.Browse:
                {
                    _equippedSkillGroup.SetAllInteractable(true);
                    _equippedSpellGroup.SetAllInteractable(true);
                    _inventoryGroup.SetAllInteractable(true);
                }
                break;
            case Status.Switching:
                {
                    // 스위칭하려는 Element 가 선택이 안되어있는데? 
                    if (_currentSwitchingElement == null)
                    {
                        TEMP_Logger.Err($"Invalid switching Element");
                        return;
                    }

                    _equippedSkillGroup.SetInteractableById(_currentSwitchingElement.SkillTableId);
                    _equippedSpellGroup.SetInteractableById(_currentSwitchingElement.SkillTableId);
                    _inventoryGroup.SetInteractableByCategoryAndBlockDuplicate(
                        DBSkill.GetCategory(_currentSwitchingElement.SkillTableId),
                        _equippedSkillGroup.IDs.Concat(_equippedSpellGroup.IDs).ToArray());
                }
                break;
            default:
                break;
        }
    }

    UISkillEditElement CreateElement(Group group, UISkillEditElement.Type type, uint skillId)
    {
        var data = DBSkill.Get(skillId);
        if (data == null)
        {
            TEMP_Logger.Err($"Failed to get SkillData | ID : {skillId}");
            return null;
        }

        var element = GameObject.Instantiate(_elementSrc, group.ElementRoot);

        if (element == null)
        {
            TEMP_Logger.Err($"Failed to create element UI | parent : {group.ElementRoot} , SkillId : {skillId}");
            return null;
        }

        element.Set(type, skillId, OnClickElement);

        group.UIs.Add(element);

        element.gameObject.SetActive(true);

        return element;
    }

    bool ApplyData()
    {
        var playerEntity = InGameManager.Instance.PlayerCommander.Player.Entity;
        if (playerEntity == null)
        {
            TEMP_Logger.Err($"Player Entity is null !");
            return false;
        }

        Me.SetSkills(_equippedSkillGroup.IDs);
        Me.SetSpells(_equippedSpellGroup.IDs);

        playerEntity.RefreshSkills();

        return true;
    }

    UISkillEditElement GetElementBySkillId(uint id)
    {
        var element = _equippedSkillGroup.TryGet(id);
        if (element)
            return element;

        element = _equippedSpellGroup.TryGet(id);
        if (element)
            return element;

        element = _inventoryGroup.TryGet(id);
        if (element)
            return element;

        return null;
    }

    void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.Start)
        {
            Hide();
        }
    }

    void OnClickElement(UISkillEditElement clickedElement)
    {
        var data = DBSkill.Get(clickedElement.SkillTableId);

        if (string.IsNullOrEmpty(data.IconKey) == false)
        {
            AssetManager.Instance.LoadAsyncCallBack<Sprite>(data.IconKey, (res) =>
            {
                _descImg.enabled = true;
                _descImg.sprite = res;
            }).Forget();
        }
        else
        {
            TEMP_Logger.Err($"NoIconKey | ID : {data.ID}");
        }

        _descTxt.SetText(data.Description);

        switch (_status)
        {
            case Status.Browse:
                if (clickedElement.ElementType == UISkillEditElement.Type.CurrentEquippedSkill ||
                    clickedElement.ElementType == UISkillEditElement.Type.CurrentEquippedSpell)
                {
                    ToSwitchMode(clickedElement);
                }
                else if (clickedElement.ElementType == UISkillEditElement.Type.InventoryElement)
                {

                }
                else
                {
                    TEMP_Logger.Err($"Not imeplemented Type : {clickedElement.ElementType}");
                }
                break;
            case Status.Switching:
                {
                    if (_currentSwitchingElement != clickedElement)
                    {
                        if (_equippedSkillGroup.ChangeID(_currentSwitchingElement.SkillTableId, clickedElement.SkillTableId)
                             || _equippedSpellGroup.ChangeID(_currentSwitchingElement.SkillTableId, clickedElement.SkillTableId))
                        {
                            _currentSwitchingElement.SetSkillID(clickedElement.SkillTableId);
                        }
                        else
                        {
                            TEMP_Logger.Err($"Failed to Set SkillID | CurrentSwitchingID : {_currentSwitchingElement.SkillTableId} | ClickedID : {clickedElement.SkillTableId}");
                        }
                    }

                    ToBrowseMode();
                }
                break;
            default:
                break;
        }
    }

    public void OnClickConfirm()
    {
        ApplyData();
        Hide();
    }

    public void OnClickCancel()
    {
        Hide();
    }
}
