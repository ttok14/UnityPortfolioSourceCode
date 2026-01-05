using System;
using UnityEngine;
using GameDB;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public static class EntityFactory
{
    private static ulong _EntityDataBaseIDCounter = 0;
    public static ulong NextDataBaseID => ++_EntityDataBaseIDCounter;

    public static async UniTask<(EntityBase entity, IEnumerable<EntityDataBase> database)> CreateEntity(
        EntityObjectData entityData,
        Transform parent)
    {
        var tableData = DBEntity.Get(entityData.entityId);

        switch (tableData.EntityType)
        {
            case E_EntityType.Environment:
                return await Create<EnvironmentEntity>(tableData, entityData, parent);
            case E_EntityType.Structure:
                {
                    var structureData = DBStructure.Get(tableData.DetailTableID);
                    if (structureData.StructureType == E_StructureType.Defense)
                        return await Create<DefenseStructureEntity>(tableData, entityData, parent);
                    else if (structureData.StructureType == E_StructureType.Spawner)
                        return await Create<SpawnerStructureEntity>(tableData, entityData, parent);
                    else return await Create<StructureEntity>(tableData, entityData, parent);
                }
            case E_EntityType.Weapon:
                return await Create<WeaponEntity>(tableData, entityData, parent);
            case E_EntityType.Food:
                return await Create<FoodEntity>(tableData, entityData, parent);
            case E_EntityType.Prop:
                return await Create<PropEntity>(tableData, entityData, parent);
            case E_EntityType.Character:
                return await Create<CharacterEntity>(tableData, entityData, parent);
            case E_EntityType.Animal:
                return await Create<AnimalEntity>(tableData, entityData, parent);
            case E_EntityType.StaticStructure:
                return await Create<StaticStructureEntity>(tableData, entityData, parent);
            case E_EntityType.Item:
                return await Create<ItemEntity>(tableData, entityData, parent);
            default:
                TEMP_Logger.Err($"Not implemented : {tableData.EntityType}");
                break;
        }

        return (null, null);
    }

    static async UniTask<(EntityBase entity, IEnumerable<EntityDataBase> database)> Create<EntityType>(
       EntityTable tableData,
       EntityObjectData entityData,
       Transform parent) where EntityType : EntityBase
    {
        var res = await PoolManager.Instance.RequestSpawnAsync<EntityType>(ObjectPoolCategory.Entity, typeof(EntityType).ToString(), parent: parent);

        if (res.instance == null)
        {
            TEMP_Logger.Err($"Failed to Spawn Entity | Key : {tableData.EntityType}");
            return (null, null);
        }

#if UNITY_EDITOR
        res.instance.gameObject.name = tableData.ResourceKey;
#endif

        var database = CreateDataBase(res.instance, tableData);
        return await res.instance.Initialize(tableData.EntityType, database, entityData);
    }

    //static Dictionary<EntityPartType, EntityPartBase> CreateParts<T>(EntityTable tableData)
    //{
    //    if (tableData.EntityType == E_EntityType.Character)
    //    {
    //        return new Dictionary<EntityPartType, EntityPartBase>()
    //        {
    //            [EntityPartType.Move] = new EntityPointsMovePart()
    //        };
    //    }
    //    else
    //    {
    //        TEMP_Logger.Wrn($"TODO Make Parts !!");
    //        return new Dictionary<EntityPartType, EntityPartBase>();
    //    }
    //}

    static EntityDataInitDataBase StatInitData = new EntityDataInitDataBase();
    static IEnumerable<EntityDataBase> CreateDataBase(EntityBase owner, EntityTable tableData)
    {
        var list = new List<EntityDataBase>();

        // TODO : 돌,산 이런 부피만 차지하고 상호작용없는 애들 등 모두 데이터 생성하지 않아도 무방할듯.
        // 추후 이 부분 정책정해지면 최적화 ㄱ
        // list.Add(new EntityBaseData(NextID, EntityDataCategory.EntityBase, tableData.ID));

        if (tableData.EntityType == E_EntityType.Structure ||
            tableData.EntityType == E_EntityType.Character ||
            tableData.EntityType == E_EntityType.Animal)
        {
            StatInitData.SetBaseInitData(owner, NextDataBaseID, EntityDataCategory.Stat, tableData.ID);
            list.Add(InGameManager.Instance.CacheContainer.EntityDataPool.GetOrCreate<EntityStatData>(StatInitData));
        }

        return list.Count > 0 ? list : null;
    }
}
