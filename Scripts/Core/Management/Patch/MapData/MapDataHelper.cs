using System;
using System.IO;
using UnityEngine;

public static class MapDataHelper
{
    public static MapData LoadMapDataReadingFile(string mapName)
    {
#if USE_REMOTE
        string json = File.ReadAllText(Path.Combine(Constants.Paths.MapDataJsonFileDirectory, $"{mapName}.json"));
#else
        string path = Constants.Paths.MapDataBaseRelativeDirectory + "/" + mapName;
        var txtAsset = Resources.Load<TextAsset>(path);
        if (txtAsset == null)
        {
            TEMP_Logger.Err($"[BuiltIn] Failed to load mapData | Path : {path}");
            return null;
        }
        string json = txtAsset.text;
#endif

        if (string.IsNullOrEmpty(json))
        {
            TEMP_Logger.Err($"MapData Json is not valid | Path : {json}");
            return null;
        }
        return JsonUtility.FromJson<MapData>(json);
    }
}
