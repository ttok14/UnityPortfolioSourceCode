using System;
using UnityEngine;

public class AssetLoader : IAssetLoader
{
    T IAssetLoader.Load<T>(string key)
    {
#if UNITY_EDITOR
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(key);
        if (asset)
        {
            return asset;
        }
#endif

        return Resources.Load<T>(key);
    }

    UnityEngine.Object IAssetLoader.Load(string key, Type type)
    {
#if UNITY_EDITOR
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(key, type);
        if (asset)
        {
            return asset;
        }
#endif

        return Resources.Load(key, type);
    }

    // TODO : 어드레서블 로드 추가해야함 . 이미 다운로드는 돼있어야함.

}
