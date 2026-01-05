using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBStat
{
    public static StatTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.StatTable_data.TryGetValue(id, out var data) == false)
        {
            return null;
        }
        return data;
    }
    public static void GetFinalStatAtLevel(
        uint id,
        uint level,
        out uint outAttackPower,
        out float outAttackSpeed,
        out uint outHp)
    {
        outAttackPower = 0;
        outAttackSpeed = 0;
        outHp = 0;

        var data = Get(id);
        if (data == null)
        {
            TEMP_Logger.Wrn($"Failed to find stat Data");
            return;
        }

        outAttackPower = data.BaseAttackPower + ((level - 1) * data.AttackGrowthPerLevel);
        outAttackSpeed = data.AttackSpeed + ((level - 1) * data.AttackSpeedGrowthPerLevel);
        outHp = data.BaseHP + ((level - 1) * data.HPGrowthPerLevel);
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
