using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Attribute_GameDBAccessor()]
public class DBSkill
{
    static List<SkillTable> _allSkillList = new List<SkillTable>();
    static List<SkillTable> _allEquippableSkillList = new List<SkillTable>();
    static List<SkillTable> _skillsBySkillCtg = new List<SkillTable>();
    static List<SkillTable> _spellsBySpellCtg = new List<SkillTable>();

    public static SkillTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.SkillTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static SkillTable GetByEntityID(uint id)
    {
        var entityData = DBEntity.Get(id);
        if (entityData == null)
            return null;

        if (entityData.EntityType == E_EntityType.Character)
        {
            var characterData = DBCharacter.Get(entityData.DetailTableID);
            if (characterData == null || characterData.SkillSet == null || characterData.SkillSet.Length == 0)
                return null;
            return Get(characterData.SkillSet[0]);
        }
        else if (entityData.EntityType == E_EntityType.Structure)
        {
            var structureData = DBStructure.Get(entityData.DetailTableID);
            if (structureData == null || structureData.SkillSet == null || structureData.SkillSet.Length == 0)
                return null;
            return Get(structureData.SkillSet[0]);
        }

        return null;
    }

    public static IReadOnlyList<SkillTable> GetAllSkills()
    {
        return _allSkillList;
    }

    public static IReadOnlyList<SkillTable> GetEquippableSkills()
    {
        return _allEquippableSkillList;
    }

    public static bool IsEquippable(SkillTable data)
    {
        // TODO : 이건 추후 바꿔야할듯 . 
        return string.IsNullOrEmpty(data.ProjectileKey);
    }

    public static bool IsEquippable(uint id)
    {
        return IsEquippable(Get(id));
    }

    public static IReadOnlyList<SkillTable> GetSkillsByCategory(E_SkillCategoryType category)
    {
        switch (category)
        {
            case E_SkillCategoryType.Standard:
                return _skillsBySkillCtg;
            case E_SkillCategoryType.Spell:
                return _spellsBySpellCtg;
        }
        return null;
    }

    public static E_SkillCategoryType GetCategory(uint id)
    {
        var data = Get(id);
        if (data == null)
            return E_SkillCategoryType.None;

        return data.SkillCategory;
    }

    public static void OnTableReady()
    {
        _allSkillList = GameDBManager.Instance.Container.SkillTable_data.Values.ToList();

        foreach (var data in GameDBManager.Instance.Container.SkillTable_data)
        {
            if (data.Value.SkillCategory == E_SkillCategoryType.Standard)
                _skillsBySkillCtg.Add(data.Value);
            else if (data.Value.SkillCategory == E_SkillCategoryType.Spell)
                _spellsBySpellCtg.Add(data.Value);

            if (string.IsNullOrEmpty(data.Value.ProjectileKey) == false)
                _allEquippableSkillList.Add(data.Value);
        }
    }

    public static void Release()
    {
    }
}
