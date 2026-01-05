using Cysharp.Threading.Tasks;
using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public class StaticStructureEntity : EntityBase
{
    public override async UniTask<(EntityBase, IEnumerable<EntityDataBase>)> Initialize(
        E_EntityType entityType,
        IEnumerable<EntityDataBase> entityDatabase,
        EntityObjectData objectData)
    {
        var res = await base.Initialize(entityType, entityDatabase, objectData);
        enabled = false;
        ModelPart.enabled = false;
        return res;
    }
}
