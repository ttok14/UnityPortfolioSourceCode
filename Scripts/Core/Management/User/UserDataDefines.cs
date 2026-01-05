using System;
using UnityEngine;
using GameDB;
using System.Linq;

// 각종 '나' 에 대한 데이터를 모아놓은 컨테이너 클래스
// 값의 변경에 대한 이벤트도 지원해야 할듯
public static class Me
{
    public delegate void OnCurrencyChanged(uint currencyId, int prev, int current);

    // 일단 시작은 데이터 러프하게 선언하자
    [Serializable]
    public class Account
    {
        public bool IsTutorialDone;
    }

    [Serializable]
    public class Currency
    {
        public int Gold;
        public int Wood;
        public int Food;
    }

    [Serializable]
    public class EquippedSkillSet
    {
        public uint[] SkillIDs;
        public uint[] SpellIDs;

        public uint[] Get(E_SkillCategoryType category)
        {
            switch (category)
            {
                case E_SkillCategoryType.Standard:
                    return SkillIDs;
                case E_SkillCategoryType.Spell:
                    return SpellIDs;
            }

            return null;
        }
    }

    [Serializable]
    public class MeData
    {
        public Account AccountInfo;

        public Currency Currency;

        public EquippedSkillSet SkillSet;
    }

    public static MeData Data { get; private set; }

    public static OnCurrencyChanged CurrencyModifiedListener;

    public static void Initialize(string json)
    {
        Data = JsonUtility.FromJson<MeData>(json);
    }

    public static string ToJson()
    {
        return JsonUtility.ToJson(Data, prettyPrint: true);
    }

    //--------------------------------------------------------------//

    public static uint[] GetSkills()
    {
        return Data.SkillSet.SkillIDs.ToArray();
    }

    public static uint[] GetSpells()
    {
        return Data.SkillSet.SpellIDs.ToArray();
    }

    public static bool IsSkillEquipped(uint id)
    {
        return Array.Exists(Data.SkillSet.SkillIDs, t => t == id) || Array.Exists(Data.SkillSet.SpellIDs, t => t == id);
    }

    public static void SetSkills(uint[] ids)
    {
        Data.SkillSet.SkillIDs = ids.ToArray();
    }

    public static void SetSpells(uint[] ids)
    {
        Data.SkillSet.SpellIDs = ids.ToArray();
    }

    public static bool SetSkill(uint newSkillId, int idx)
    {
        var arr = Data.SkillSet.Get(DBSkill.GetCategory(newSkillId));

        if (arr == null)
            return false;

        if (idx >= arr.Length)
        {
            TEMP_Logger.Err($"Skill Array Size OutofRange | ArrLength : {arr.Length} , Idx : {idx}");
            return false;
        }

        arr[idx] = newSkillId;

        return true;
    }

    public static int GetCurrencyAmount(E_CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case E_CurrencyType.Gold:
                return Data.Currency.Gold;
            case E_CurrencyType.Wood:
                return Data.Currency.Wood;
            case E_CurrencyType.Food:
                return Data.Currency.Food;
        }

        return 0;
    }

    public static bool CanAfford(E_CurrencyType currencyType, int amount)
    {
        return GetCurrencyAmount(currencyType) >= MathF.Abs(amount);
    }

    public static void AcquireCurrency(E_CurrencyType type, int amount)
    {
        ModifyCurrency(type, amount);
    }

    public static void SpendCurrency(E_CurrencyType type, int amount)
    {
        if (CanAfford(type, amount) == false)
        {
            TEMP_Logger.Err($"send curreny when you cannot afford");
            return;
        }

        ModifyCurrency(type, amount * -1);
    }

    static void ModifyCurrency(E_CurrencyType type, int amount)
    {
        switch (type)
        {
            case GameDB.E_CurrencyType.Gold:
                ModifyGold(amount);
                break;
            case GameDB.E_CurrencyType.Wood:
                ModifyWood(amount);
                break;
            case GameDB.E_CurrencyType.Food:
                ModifyFood(amount);
                break;
            default:
                TEMP_Logger.Err($"Not implemented type : {type}");
                break;
        }
    }

    public static void AcquireItem(uint itemId, int amount)
    {
        var itemData = DBItem.Get(itemId);
        switch (itemData.ItemType)
        {
            case GameDB.E_ItemType.Currency:
                {
                    var currencyData = DBCurrency.Get(itemData.DetailID);

                    switch (currencyData.Type)
                    {
                        case GameDB.E_CurrencyType.Gold:
                            ModifyGold(amount);
                            break;
                        case GameDB.E_CurrencyType.Wood:
                            ModifyWood(amount);
                            break;
                        case GameDB.E_CurrencyType.Food:
                            ModifyFood(amount);
                            break;
                        default:
                            TEMP_Logger.Err($"Not implemented type : {itemData.ItemType}");
                            break;
                    }
                }
                break;
            default:
                TEMP_Logger.Err($"Not implmented item type : {itemId}");
                break;
        }
    }

    public static void ModifyGold(int amount)
    {
        int prev = Data.Currency.Gold;
        int newGold = Data.Currency.Gold + amount;
        Data.Currency.Gold = Math.Max(newGold, 0);
        CurrencyModifiedListener?.Invoke(DBCurrency.GetIDByType(GameDB.E_CurrencyType.Gold), prev, Data.Currency.Gold);
    }

    public static void ModifyWood(int amount)
    {
        int prev = Data.Currency.Wood;
        int newWood = Data.Currency.Wood + amount;
        Data.Currency.Wood = Math.Max(newWood, 0);
        CurrencyModifiedListener?.Invoke(DBCurrency.GetIDByType(GameDB.E_CurrencyType.Wood), prev, Data.Currency.Wood);
    }

    public static void ModifyFood(int amount)
    {
        int prev = Data.Currency.Food;
        int newFood = Data.Currency.Food + amount;
        Data.Currency.Food = Math.Max(newFood, 0);
        CurrencyModifiedListener?.Invoke(DBCurrency.GetIDByType(GameDB.E_CurrencyType.Food), prev, Data.Currency.Food);
    }
}
