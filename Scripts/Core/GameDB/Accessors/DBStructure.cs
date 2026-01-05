using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBStructure
{
    // static Dictionary<E_StructureType, Dictionary<uint, EntityTable>> StructureByType = new Dictionary<E_StructureType, Dictionary<uint, EntityTable>>();

    public static StructureTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.StructureTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static StructureTable GetByEntityID(uint id)
    {
        var entityData = DBEntity.Get(id);
        if (entityData == null)
        {
            return null;
        }

        return Get(entityData.DetailTableID);
    }

    public static E_StructureType GetStructureType(uint id)
    {
        var data = Get(id);
        if (data == null)
            return E_StructureType.None;
        return data.StructureType;
    }

    public static uint GetFinalGenCurrencyAmountAtLevel(uint id, uint level)
    {
        if (GameDBManager.Instance.Container.StructureTable_data.TryGetValue(id, out var data) == false)
            return 0;

        return data.GenResourceBaseAmount + ((level - 1) * data.GenCurrencyGrowthPerLevel);
    }

    public static void OnTableReady()
    {
        // StructureByType.Clear();


    }

    public static void Release()
    {
        // StructureByType.Clear();
    }
}
