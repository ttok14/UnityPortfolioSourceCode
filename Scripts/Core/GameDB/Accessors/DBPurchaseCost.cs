using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Attribute_GameDBAccessor()]
public class DBPurchaseCost
{
    static Dictionary<uint, PurchaseCostTable> CostDataByType = new Dictionary<uint, PurchaseCostTable>();

    public static PurchaseCostTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.PurchaseCostTable_data.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public static PurchaseCostTable GetByEntityID(uint entityId)
    {
        if (CostDataByType.TryGetValue(entityId, out var data))
            return data;
        return null;
    }

    public static void OnTableReady()
    {
        foreach (var costData in GameDBManager.Instance.Container.PurchaseCostTable_data)
        {
            CostDataByType.Add(costData.Value.EntityID, costData.Value);
        }
    }

    public static void Release()
    {
    }
}
