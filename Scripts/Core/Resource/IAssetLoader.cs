using System;

public interface IAssetLoader
{
    T Load<T>(string key) where T : UnityEngine.Object;
    UnityEngine.Object Load(string key, Type type);
}
