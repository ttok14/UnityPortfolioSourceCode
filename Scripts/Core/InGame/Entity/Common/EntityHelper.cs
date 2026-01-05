using UnityEngine;
using GameDB;

public static class EntityHelper
{
    public static bool IsValid(EntityBase entity, ulong? expectedEntityID = null)
    {
        return entity &&
            entity.IsActivated &&
            entity.IsAlive &&
            entity.ModelPart &&
            (expectedEntityID.HasValue == false ||
            expectedEntityID.Value == entity.ID);
    }

    public static bool CanPlace(uint id, Vector3 transformWorldPos, float transformEulerY)
    {
        var data = DBEntity.Get(id);
        if (data.EntityFlags.HasFlag(E_EntityFlags.Requires_Walkable_Ground) == false &&
            data.EntityFlags.HasFlag(E_EntityFlags.Require_Jumpable) == false &&
            data.EntityFlags.HasFlag(E_EntityFlags.Require_Walkable_Air) == false)
            return true;

        foreach (var offset in data.OccupyOffsets)
        {
            var worldPos = MapUtils.TransformTilePos(offset, transformWorldPos, transformEulerY);
            if (MapManager.Instance.CanPlace(worldPos, data.EntityFlags) == false)
                return false;
        }

        return true;
    }

    public static EntityTeamType ToOpponentTeamType(EntityTeamType team)
    {
        switch (team)
        {
            case EntityTeamType.Player:
                return EntityTeamType.Enemy;
            case EntityTeamType.Enemy:
                return EntityTeamType.Player;
            default:
                return EntityTeamType.None;
        }
    }

    public static float ApplySizeToForce(float force, E_SizeType sizeType)
    {
        switch (sizeType)
        {
            case E_SizeType.Small:
                return force * 1f;
            case E_SizeType.Medium:
                return force * 0.8f;
            case E_SizeType.Large:
                return force * 0.5f;
            default:
                TEMP_Logger.Err($"Not Implemented Type : {sizeType}");
                return force;
        }
    }

    //public static int GetOpponentLayer(EntityTeamType team)
    //{
    //    if(team == EntityTeamType.Player)
    //    {
    //        return LayerUtils.GetLayer(E_EntityType.)
    //    }
    //}
}
