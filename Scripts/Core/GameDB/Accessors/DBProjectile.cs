using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBProjectile
{
    static Dictionary<string, ProjectileTable> DataByResourceKey;

    public static ProjectileTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.ProjectileTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static ProjectileTable Get(string resourceKey)
    {
        if (DataByResourceKey.TryGetValue(resourceKey, out var data))
            return data;
        return null;
    }

    public static string GetResourceKey(uint id)
    {
        var data = Get(id);
        if (data == null)
            return string.Empty;
        return data.ResourceKey;
    }

    public static void OnTableReady()
    {
        DataByResourceKey = GameDBManager.Instance.Container.ProjectileTable_data.ToDictionary((kv) => kv.Value.ResourceKey, (kv) => kv.Value);
    }

    public static void Release()
    {
    }
}
