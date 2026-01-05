using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Attribute_GameDBAccessor()]
public class DBResource
{
    public static ResourceTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.ResourceTable_data.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public static string GetIconKey(E_ResourceType resourceType, uint detailID)
    {
        switch (resourceType)
        {
            case E_ResourceType.Currency:
                return DBCurrency.GetSpriteKeyByID(detailID);
            case E_ResourceType.Entity:
                return DBEntity.GetIconKey(detailID);
            default:
                TEMP_Logger.Err($"Not implmented Type : {resourceType}");
                return string.Empty;
        }
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
