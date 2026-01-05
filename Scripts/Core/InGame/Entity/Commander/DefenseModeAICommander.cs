using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameDB;

public class DefenseModeAICommander : EntityAICommanderBase
{
    DefenseMode _mode;

    //FixedTargetingWithAggroPolicy _fixedTargetingWithAggroPolicy_playerUnit;
    //FixedTargetingWithAggroPolicy _fixedTargetingWithAggroPolicy_enemy;
    // FixedTargetingPolicy _fixedTargetingPolicy;
    //StaticPatrolPolicy _patrolPolicy;

    StandardAggroSystem _standardAggroSystem_actor;
    StandardAggroSystem _standardAggroSystem_player_npc;
    StandardAggroSystem _standardAggroSystem_enemy_npc;
    StandardAggroSystem _standardAggroSystem_enemy_structure;

    EntityAIBehaviourInitData _behaviourInitData = new EntityAIBehaviourInitData();
    AggroTargetingPolicyInitData _aggroSystemInitData = new AggroTargetingPolicyInitData();
    FixedTargetingWithAggroPolicyInitData _fixedTargetingWithAggreoInitData = new FixedTargetingWithAggroPolicyInitData();

    MovePolicyInitDataBase _movePolicyInitData = new MovePolicyInitDataBase();
    MoveGuardAndCounterPolicyInitData _moveGuardAndCounterPolicyInitData = new MoveGuardAndCounterPolicyInitData();

    public DefenseModeAICommander(DefenseMode mode)
    {
        _mode = mode;
    }

    public override async UniTask InitializeAsync()
    {
        await base.InitializeAsync();

        //_fixedTargetingPolicy = new FixedTargetingPolicy(_mode);
        // invaldate 옵션 일단은 끄자 , 최적화 .
        // 만약에 사정거리 밖을 넘어간  상대를 멍하니 쳐다보는게 빈번하거나 티가나면 true 로.
        _standardAggroSystem_actor = new StandardAggroSystem(30, 0.7f, 1.4f, false, E_EntityType.Character, E_EntityType.Structure);
        _standardAggroSystem_actor.Initialize();

        _standardAggroSystem_player_npc = new StandardAggroSystem(1, 1.5f, 2f, false, E_EntityType.Character);
        _standardAggroSystem_player_npc.Initialize();

        _standardAggroSystem_enemy_npc = new StandardAggroSystem(1, 1.5f, 1.5f, false, E_EntityType.Character);
        _standardAggroSystem_enemy_npc.Initialize();

        _standardAggroSystem_enemy_structure = new StandardAggroSystem(1, 1f, 2f, false, E_EntityType.Character);
        _standardAggroSystem_enemy_structure.Initialize();

        //_fixedTargetingWithAggroPolicy_playerUnit = new FixedTargetingWithAggroPolicy(_mode, _standardAggroSystem_player_npc);
        //_fixedTargetingWithAggroPolicy_enemy = new FixedTargetingWithAggroPolicy(_mode, _standardAggroSystem_enemy_npc);
    }

    public override void Release()
    {
        base.Release();

        if (_standardAggroSystem_actor != null)
        {
            _standardAggroSystem_actor.Release();
        }
    }

    public override void Update()
    {
        base.Update();
    }

    //void FindPath(Vector3 from, Action<List<Vector3>> onReceived)
    //{
    //    // Mode 레벨에서 Cache 된 Path 가 있는지 먼저 체크 (주로 SpawnPoint 로부터 Target까지의 Path)
    //    _mode.GetPathCacheToTarget(from, onReceived);
    //}

    public override EntityAIBehaviour CreateBehaviour(EntityBase entity, in EntitySetupContext context)
    {
        switch (entity.Team)
        {
            case EntityTeamType.Player:
                if (entity.Type == GameDB.E_EntityType.Character)
                {
                    var charType = (entity as CharacterEntity).CharacterType;

                    if (charType == GameDB.E_CharacterType.Actor)
                    {
                        _aggroSystemInitData.Set(_standardAggroSystem_actor);
                        var policy = InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<AggroTargetingPolicy>(_aggroSystemInitData);
                        _behaviourInitData.Set(entity, policy, null, _mode);
                        return InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
                    }
                    else if (charType == GameDB.E_CharacterType.NPC)
                    {
                        var aiPool = InGameManager.Instance.CacheContainer.EntityAIPool;

                        _moveGuardAndCounterPolicyInitData.Set(entity, _mode, context.SpawnMode);
                        _fixedTargetingWithAggreoInitData.Set(_mode, _standardAggroSystem_player_npc);

                        _behaviourInitData.Set(entity,
                            aiPool.GetOrCreate<FixedTargetingWithAggroPolicy>(_fixedTargetingWithAggreoInitData),
                            aiPool.GetOrCreate<MoveGuardAndCounterPolicy>(_moveGuardAndCounterPolicyInitData),
                            _mode);

                        return aiPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);

                        //_movePolicyInitData.Set(entity);
                        //_behaviourInitData.Set(entity,
                        //    _fixedTargetingWithAggroPolicy_playerUnit,
                        //    InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<ToTargetMovePolicy>(_movePolicyInitData),
                        //    _mode);
                        //return InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
                    }
                }
                else if (entity.Type == GameDB.E_EntityType.Structure)
                {
                    var structure = entity as StructureEntity;
                    if (structure.StructureData.StructureType == GameDB.E_StructureType.Defense)
                    {
                        MovePolicyBase movePolicy = null;
                        var aiPool = InGameManager.Instance.CacheContainer.EntityAIPool;

                        if (entity.MovePart != null)
                        {
                            _movePolicyInitData.Set(entity);
                            if (entity.MovePart is EntityPatrolMovePart)
                                movePolicy = aiPool.GetOrCreate<StaticPatrolPolicy>(_movePolicyInitData);
                            else TEMP_Logger.Err($"Not Implemented MovePart To MovePolicy : {entity.MovePart}");
                        }

                        _aggroSystemInitData.Set(_standardAggroSystem_actor);

                        _behaviourInitData.Set(
                            entity,
                            aiPool.GetOrCreate<AggroTargetingPolicy>(_aggroSystemInitData),
                            movePolicy,
                            _mode);

                        return aiPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
                    }
                }
                return null;
            case EntityTeamType.Enemy:
                if (entity.Type == E_EntityType.Character)
                {
                    var aiPool = InGameManager.Instance.CacheContainer.EntityAIPool;

                    _movePolicyInitData.Set(entity);
                    _fixedTargetingWithAggreoInitData.Set(_mode, _standardAggroSystem_enemy_npc);

                    _behaviourInitData.Set(
                        entity,
                        aiPool.GetOrCreate<FixedTargetingWithAggroPolicy>(_fixedTargetingWithAggreoInitData),
                        aiPool.GetOrCreate<ToTargetMovePolicy>(_movePolicyInitData),
                        _mode);

                    return aiPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
                }
                else if (entity.Type == E_EntityType.Structure)
                {
                    var aiPool = InGameManager.Instance.CacheContainer.EntityAIPool;
                    var structure = entity as StructureEntity;

                    if (structure.StructureData.StructureType == GameDB.E_StructureType.Defense)
                    {
                        MovePolicyBase movePolicy = null;

                        if (entity.MovePart != null)
                        {
                            _movePolicyInitData.Set(entity);
                            if (entity.MovePart is EntityPatrolMovePart)
                                movePolicy = aiPool.GetOrCreate<StaticPatrolPolicy>(_movePolicyInitData);
                            else TEMP_Logger.Err($"Not Implemented MovePart To MovePolicy : {entity.MovePart}");
                        }

                        _aggroSystemInitData.Set(_standardAggroSystem_enemy_structure);

                        _behaviourInitData.Set(
                            entity,
                            aiPool.GetOrCreate<AggroTargetingPolicy>(_aggroSystemInitData),
                            movePolicy,
                            _mode);

                        return aiPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
                    }
                }
                else
                {
                    TEMP_Logger.Err($"Not Implemented TeamType For AI Behaviour : {entity.Team}");
                }
                return null;
        }

        return null;
    }
}
