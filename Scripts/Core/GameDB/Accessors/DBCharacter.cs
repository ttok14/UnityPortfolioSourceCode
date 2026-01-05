using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBCharacter
{
    public static CharacterTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.CharacterTable_data.TryGetValue(id, out var data) == false)
            return null;
        return data;
    }

    public static float GetShadowScale(uint id)
    {
        if (GameDBManager.Instance.Container.CharacterTable_data.TryGetValue(id, out var data) == false)
            return 0f;
        return data.ShadowScale;
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
