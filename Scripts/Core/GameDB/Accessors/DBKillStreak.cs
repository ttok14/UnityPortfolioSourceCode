using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBKillStreak
{
    public static KillStreakTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.KillStreakTable_data.TryGetValue(id, out var data) == false)
            return null;

        return data;
    }

    public static void InitializeBootstrap()
    {
    }

    public static void OnTableReady()
    {
    }


    public static void Release()
    {
    }
}
