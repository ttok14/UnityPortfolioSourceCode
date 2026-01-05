using System;
using UnityEngine;

[Serializable]
public struct EntityObjectData
{
    public Vector3 worldPosition;
    public int eulerRotY;
    public uint entityId;

    public EntityTeamType teamType;

    public EntityObjectData(Vector3 worldPosition, int eulerRotY, uint entityId, EntityTeamType teamType)
    {
        this.worldPosition = worldPosition;
        this.eulerRotY = eulerRotY;
        this.entityId = entityId;
        this.teamType = teamType;
    }

    public EntityObjectData Copy()
    {
        return (EntityObjectData)MemberwiseClone();
    }
}
