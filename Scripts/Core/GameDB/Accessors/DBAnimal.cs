using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBAnimal
{
    public static PetTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.PetTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static bool IsRidable(uint id)
    {
        var data = Get(id);
        if (data == null)
            return false;
        return data.IsRidable;
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
