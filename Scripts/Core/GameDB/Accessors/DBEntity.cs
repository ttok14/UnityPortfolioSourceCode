using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Attribute_GameDBAccessor()]
public class DBEntity
{
    static Dictionary<E_EntityType, Dictionary<uint, EntityTable>> EntitiesByType = new Dictionary<E_EntityType, Dictionary<uint, EntityTable>>();

    public static EntityTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.EntityTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static string GetName(uint id)
    {
        var data = Get(id);
        if (data == null)
            return string.Empty;
        return data.Name;
    }

    public static E_EntityType GetEntityType(uint id)
    {
        var data = Get(id);
        if (data == null)
            return E_EntityType.None;
        return data.EntityType;
    }

    public static string GetResourceKey(uint id)
    {
        var data = Get(id);
        if (data == null)
            return string.Empty;
        return data.ResourceKey;
    }

    public static string GetIconKey(uint id)
    {
        var data = Get(id);
        if (data == null)
            return string.Empty;

        return data.IconKey;

        //switch (data.EntityType)
        //{
        //    case E_EntityType.Structure:
        //        return DBStructure.GetIconKey(data.DetailTableID);
        //    case E_EntityType.Character:
        //        return DBCharacter.GetIconKey(data.DetailTableID);
        //}

        //return string.Empty;
    }

    public static IReadOnlyDictionary<uint, EntityTable> GetByType(E_EntityType type)
    {
        return EntitiesByType[type];
    }

    // 에디터 툴용으로 쓸까햿는데 . .. 
    //    public static void InitializeBootstrap()
    //    {
    //#if UNITY_EDITOR && DEVELOPMENT
    //        EntitiesByType.Clear();

    //        foreach (var objectData in GameDBManager.Instance.Container.EntityTable_data)
    //        {
    //            if (EntitiesByType.TryGetValue(objectData.Value.EntityType, out var dic) == false)
    //            {
    //                dic = new Dictionary<uint, EntityTable>();
    //                EntitiesByType.Add(objectData.Value.EntityType, dic);
    //            }

    //            dic.Add(objectData.Key, objectData.Value);
    //        }
    //#endif
    //    }

    public static void OnTableReady()
    {
        EntitiesByType.Clear();

        foreach (var objectData in GameDBManager.Instance.Container.EntityTable_data)
        {
            if (EntitiesByType.TryGetValue(objectData.Value.EntityType, out var dic) == false)
            {
                dic = new Dictionary<uint, EntityTable>();
                EntitiesByType.Add(objectData.Value.EntityType, dic);
            }

            dic.Add(objectData.Key, objectData.Value);
        }
    }

    public static Vector2Int[] GetOccupationPosData(uint id)
    {
        var data = Get(id);
        if (data == null)
            return null;
        return data.OccupyOffsets;
    }

    public static void Release()
    {
        EntitiesByType.Clear();
    }
}
