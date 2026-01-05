using System.Collections.Generic;
using UnityEngine;
using GameDB;

public class LayerUtils
{
    //readonly static Dictionary<string, int> NameToLayer = new Dictionary<string, int>(32);
    //readonly static Dictionary<E_EntityType, int> EntityTypeToLayer = new Dictionary<E_EntityType, int>();
    readonly static Dictionary<(EntityTeamType, E_EntityType), int> EntityToLayer = new Dictionary<(EntityTeamType, E_EntityType), int>();

    //readonly static Dictionary<string, int> NameToLayerMask = new Dictionary<string, int>(32);
    //readonly static Dictionary<E_EntityType, int> EntityTypeToLayerMask = new Dictionary<E_EntityType, int>();
    readonly static Dictionary<(EntityTeamType, E_EntityType), int> EntityToLayerMask = new Dictionary<(EntityTeamType, E_EntityType), int>();

    readonly static Dictionary<(EntityTeamType, string), int> StringToLayer = new Dictionary<(EntityTeamType, string), int>();

    readonly static Dictionary<(EntityTeamType, string), int> StringToLayerMask = new Dictionary<(EntityTeamType, string), int>();

    public readonly static int Layer_Dead = LayerMask.NameToLayer("Dead");
    public readonly static int Layer_Invincible = LayerMask.NameToLayer("Invincible");

    //public static int GetLayerMask((EntityTeamType, E_EntityType) teamAndEntityTypePairs)
    //{
    //    int res = 0x0;

    //    for (int i = 0; i < teamAndEntityTypePairs.Length; i++)
    //    {
    //        if (EntityToLayerMask.TryGetValue((teamAndEntityTypePairs), out var mask) == false)
    //        {
    //            mask = LayerMask.GetMask(entityTypes[i].ToString());
    //            EntityTypeToLayerMask.Add(entityTypes[i], mask);
    //        }
    //        res |= mask;
    //    }

    //    return res;
    //}

    //public static int GetLayerMask(params E_EntityType[] entityTypes)
    //{
    //    int res = 0x0;

    //    for (int i = 0; i < entityTypes.Length; i++)
    //    {
    //        if (EntityTypeToLayerMask.TryGetValue(entityTypes[i], out var mask) == false)
    //        {
    //            mask = LayerMask.GetMask(entityTypes[i].ToString());
    //            EntityTypeToLayerMask.Add(entityTypes[i], mask);
    //        }
    //        res |= mask;
    //    }

    //    return res;
    //}


    static string PairToLayerName_Entity((EntityTeamType, E_EntityType) pair)
    {
        string prefix = pair.Item1.ToString();
        string suffix = pair.Item2 == E_EntityType.Character || pair.Item2 == E_EntityType.Structure ?
            $"_{pair.Item2}" : string.Empty;
        return prefix + suffix;
    }

    static string PairToLayerName_String((EntityTeamType prefix, string suffix) pair)
    {
        string prefix = pair.prefix.ToString();
        return $"{prefix}_{pair.suffix}";
    }

    public static int GetLayer((EntityTeamType, E_EntityType) pair)
    {
        if (EntityToLayer.TryGetValue(pair, out var layer) == false)
        {
            layer = LayerMask.NameToLayer(PairToLayerName_Entity(pair));
            if (layer == -1)
            {
                TEMP_Logger.Err($"Given layer does not exist : {pair}");
            }
            EntityToLayer.Add(pair, layer);
        }

        return layer;
    }

    public static int GetLayer_String((EntityTeamType prefix, string suffix) pair)
    {
        if (StringToLayer.TryGetValue(pair, out var layer) == false)
        {
            layer = LayerMask.NameToLayer(PairToLayerName_String(pair));
            if (layer == -1)
            {
                TEMP_Logger.Err($"Given layer does not exist : {pair}");
            }
            StringToLayer.Add(pair, layer);
        }

        return layer;
    }

    //public static int GetLayer(string name)
    //{
    //    if (NameToLayer.TryGetValue(name, out var layer) == false)
    //    {
    //        layer = LayerMask.NameToLayer(name);
    //        if (layer == -1)
    //        {
    //            TEMP_Logger.Err($"Given layer does not exist : {name}");
    //        }
    //        NameToLayer.Add(name, layer);
    //    }

    //    return layer;
    //}

    //public static int GetLayer(E_EntityType entityType)
    //{
    //    if (EntityTypeToLayer.TryGetValue(entityType, out var layer) == false)
    //    {
    //        layer = LayerMask.NameToLayer(entityType.ToString());
    //        if (layer == -1)
    //        {
    //            TEMP_Logger.Err($"Given layer does not exist : {entityType}");
    //        }
    //        EntityTypeToLayer.Add(entityType, layer);
    //    }

    //    return layer;
    //}

    //public static int GetLayerMask(params string[] names)
    //{
    //    int res = 0x0;

    //    for (int i = 0; i < names.Length; i++)
    //    {
    //        if (NameToLayerMask.TryGetValue(names[i], out var mask) == false)
    //        {
    //            mask = LayerMask.GetMask(names[i]);
    //            NameToLayerMask.Add(names[i], mask);
    //        }
    //        res |= mask;
    //    }

    //    return res;
    //}

    public static int GetLayerMask(params (EntityTeamType, E_EntityType)[] pairs)
    {
        int res = 0x0;

        for (int i = 0; i < pairs.Length; i++)
        {
            if (EntityToLayerMask.TryGetValue(pairs[i], out var mask) == false)
            {
                mask = LayerMask.GetMask(PairToLayerName_Entity(pairs[i]));
                EntityToLayerMask.Add(pairs[i], mask);
            }
            res |= mask;
        }

        return res;
    }

    public static int GetLayerMask_String(params (EntityTeamType, string)[] pairs)
    {
        int res = 0x0;

        for (int i = 0; i < pairs.Length; i++)
        {
            if (StringToLayerMask.TryGetValue(pairs[i], out var mask) == false)
            {
                mask = LayerMask.GetMask(PairToLayerName_String(pairs[i]));
                StringToLayerMask.Add(pairs[i], mask);
            }
            res |= mask;
        }

        return res;
    }
}
