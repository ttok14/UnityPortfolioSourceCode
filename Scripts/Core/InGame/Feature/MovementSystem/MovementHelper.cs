using GameDB;
using UnityEngine;

public static class MovementHelper 
{
    public static Vector3 ApplyAimedPosition(E_AimType type, Vector3 srcTargetPos, Vector3 startPos, EntityBase target)
    {
        switch (type)
        {
            case E_AimType.None:
                return srcTargetPos;
            case E_AimType.FixHeight:
                srcTargetPos.y = startPos.y;
                return srcTargetPos;
            case E_AimType.Ground:
                srcTargetPos.y = 0;
                return srcTargetPos;
            case E_AimType.TransformPosition:
                return srcTargetPos;
            case E_AimType.Head:
                if (EntityHelper.IsValid(target))
                {
                    var headSocket = target.ModelPart.GetSocket(EntityModelSocket.Head);
                    if (headSocket)
                        return headSocket.position;
                }
                return srcTargetPos;
            default:
                return srcTargetPos;
        }
    }
}
