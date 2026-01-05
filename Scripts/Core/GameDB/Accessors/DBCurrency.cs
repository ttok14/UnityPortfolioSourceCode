using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Attribute_GameDBAccessor()]
public class DBCurrency
{
    static Dictionary<E_CurrencyType, CurrencyTable> CurrenciesByType = new Dictionary<E_CurrencyType, CurrencyTable>();

    public static CurrencyTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.CurrencyTable_data.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public static CurrencyTable Get(E_CurrencyType type)
    {
        if (CurrenciesByType.TryGetValue(type, out var data) == false)
            return null;
        return data;
    }

    public static E_CurrencyType GetCurrencyType(uint id)
    {
        var data = Get(id);
        if (data == null)
            return E_CurrencyType.None;
        return data.Type;
    }

    public static uint GetIDByType(E_CurrencyType type)
    {
        var data = Get(type);
        if (data == null)
            return 0;
        return data.ID;
    }

    public static string GetSpriteKey(E_CurrencyType type)
    {
        var data = Get(type);
        if (data == null)
            return string.Empty;
        return data.SpriteKey;
    }

    public static string GetSpriteKeyByID(uint id)
    {
        var data = Get(id);
        if (data == null)
            return string.Empty;
        return data.SpriteKey;
    }

    public static void OnTableReady()
    {
        foreach (var data in GameDBManager.Instance.Container.CurrencyTable_data.Values)
        {
            CurrenciesByType.Add(data.Type, data);
        }
    }

    public static void Release()
    {
        CurrenciesByType.Clear();
    }
}
