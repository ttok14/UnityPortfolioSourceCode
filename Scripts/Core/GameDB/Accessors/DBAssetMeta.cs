using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBAssetMeta
{
    public static Dictionary<string, AssetMetaTable> MetaByKey;

    public static AssetMetaTable Get(string key)
    {
        if (MetaByKey.TryGetValue(key, out var target))
            return target;
        return null;
    }

    public static void InitializeBootstrap()
    {
        var bin = Resources.Load<TextAsset>("Table/AssetMetaTable");
        GameDBManager.Instance.Container.AssetMetaTable_data = GameDBHelper.LoadTableBinary(nameof(GameDBContainer.AssetMetaTable_data), bin.bytes) as Dictionary<string, AssetMetaTable>;
        MetaByKey = GameDBManager.Instance.Container.AssetMetaTable_data;
    }

    public static void OnTableReady()
    {
        MetaByKey = GameDBManager.Instance.Container.AssetMetaTable_data;
    }

    public static void Release()
    {
        MetaByKey = null;
    }
}
