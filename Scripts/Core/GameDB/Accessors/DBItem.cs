using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBItem
{
    public static ItemTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.ItemTable_data.TryGetValue(id, out var data) == false)
        {
            return null;
        }
        return data;
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
