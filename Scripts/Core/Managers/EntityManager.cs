using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;
using System.Collections;
using Cysharp.Threading.Tasks;

public class EntityManager : SingletonBase<EntityManager>
{
    //class TeamGroup
    //{
    //    public List<CharacterEntity> characters;
    //}

    // TODO : Pool 에서 재활용되는 애들 지금 여기에 재등록을 해주어야하는데 ?
    // 일단 이것도 안되고 있고, 그리고 카테고리별로 관리를 할까? 그게 더 나을듯함
    // 전체 내부 배열을 뒤집는거보다 자주 수정된다면 별도 카테고리로 나누는게 유리할듯
    private Dictionary<ulong, EntityBase> _allEntities;

    public Dictionary<ulong, EntityBase> AllEntities => _allEntities;
    private Dictionary<EntityTeamType, Dictionary<ulong, StructureEntity>> _structures;
    public Dictionary<ulong, StructureEntity> GetStructures(EntityTeamType team) => _structures[team];

    public EntityAnimationTriggerBuffer AnimationTriggerData { get; private set; }

    public int GetStructureCount(EntityTeamType team) => GetStructures(team).Count;
    public bool HasAliveStructure(EntityTeamType team, bool includeInvincible = true)
    {
        var structures = GetStructures(team);
        if (structures.Count == 0)
            return false;

        if (includeInvincible)
            return true;

        foreach (var s in structures)
        {
            if (s.Value.StatPart.StatData.StatTableData.IsInvincible == false)
                return true;
        }

        return false;
    }

    public EntityBase GetNexus(EntityTeamType team)
    {
        var structures = GetStructures(team);
        foreach (StructureEntity s in structures.Values)
        {
            if (s.StructureData.StructureType == E_StructureType.Nexus)
                return s;
        }
        return null;
    }

    public List<StructureEntity> GetStructures(EntityTeamType team, int count, Predicate<StructureEntity> selector)
    {
        var result = new List<StructureEntity>();
        int foundCnt = 0;

        foreach (var structure in GetStructures(team))
        {
            if (selector(structure.Value))
            {
                result.Add(structure.Value);
                foundCnt++;

                if (count > 0 && foundCnt == count)
                    break;
            }
        }

        return result;
    }

    private Dictionary<EntityTeamType, Dictionary<ulong, CharacterEntity>> _charactersByTeam;
    private Dictionary<Transform, CharacterEntity> _charactersByTransform;
    public Dictionary<ulong, CharacterEntity> GetCharacters(EntityTeamType team) => _charactersByTeam[team];
    public int GetCharacterCount(EntityTeamType team) => GetCharacters(team).Count;
    public CharacterEntity GetCharacterByTransform(Transform ts)
    {
        if (_charactersByTransform.TryGetValue(ts, out var res))
            return res;
        return null;
    }

    private EntityAICommanderBase _aiCommander;
    public EntityAICommanderBase AICommander => _aiCommander;

    public CurveDataContainer CurveData { get; private set; }

    // private Dictionary<E_EntityType, Dictionary<ulong, EntityBase>> _entitiesByType;

    // 원래 매니저에서 EntityDataBase 를 관리하려 했는데 너무 오버엔지니어링임.
    // 무엇보다 애를 만드려면 매니저를 거쳐야 한다는 암묵적인 룰이 지켜지기 힘듬
    // 이로인해 일단 필요해지기 전까지는 보류한다.
    // (원래는 어떤 엔티티던 그 entity 의 id 만 알면 중앙 시스템에서 그 entity 의 데이터를
    // 참조할 수 있는 유연함을 주기 위함이었음)
    // private Dictionary<ulong, EntityDataBase> _entityData;

    private Transform _entityRoot;
    private Transform[] _entityRootsByType;

    private EntityCreatedEventArg _entityCreatedEventArg = new EntityCreatedEventArg();

    private List<ulong> _removeCache = new List<ulong>(64);

    public EntityBase GetEntity(ulong id)
    {
        if (_allEntities.TryGetValue(id, out var entity) == false)
        {
            return null;
        }

        return entity;
    }

    // 자주쓰면안됨, 성능 포기함수
    public EntityBase FindEntity(Predicate<EntityBase> predicate)
    {
        foreach (var entity in AllEntities)
        {
            if (predicate.Invoke(entity.Value))
                return entity.Value;
        }
        return null;
    }

    public bool HasEntity(EntityTeamType team, Predicate<EntityBase> predicate)
    {
        foreach (var structure in GetStructures(team))
        {
            if (predicate.Invoke(structure.Value))
                return true;
        }
        return false;
    }

    public bool IsEntityValid(ulong id)
    {
        if (id == 0)
            return false;

        var entity = GetEntity(id);
        return EntityHelper.IsValid(entity);
    }

    private void Update()
    {
        if (_aiCommander != null)
        {
            _aiCommander.Update();
        }
    }

    public async UniTask PrepareGame(List<EntityObjectData> dataList)
    {
        EventManager.Instance.Register(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnPhaseChanged);

        AnimationTriggerData = new EntityAnimationTriggerBuffer();

        AssetManager.Instance.LoadAsyncCallBack<CurveDataContainer>("CurveDataContainer", (res) =>
        {
            CurveData = res;
        }).Forget();

        _entityRoot = new GameObject("EntityRoot").transform;

        var entityTypes = (E_EntityType[])System.Enum.GetValues(typeof(E_EntityType));
        _entityRootsByType = new Transform[entityTypes.Length];
        for (int i = 0; i < entityTypes.Length; i++)
        {
            _entityRootsByType[i] = new GameObject(entityTypes[i].ToString()).transform;
            _entityRootsByType[i].SetParent(_entityRoot);
        }

        _allEntities = new Dictionary<ulong, EntityBase>(dataList.Count);

        _structures = new Dictionary<EntityTeamType, Dictionary<ulong, StructureEntity>>();
        _structures.Add(EntityTeamType.Player, new Dictionary<ulong, StructureEntity>());
        _structures.Add(EntityTeamType.Enemy, new Dictionary<ulong, StructureEntity>());
        _structures.Add(EntityTeamType.Neutral, new Dictionary<ulong, StructureEntity>());

        _charactersByTeam = new Dictionary<EntityTeamType, Dictionary<ulong, CharacterEntity>>();
        _charactersByTeam.Add(EntityTeamType.Player, new Dictionary<ulong, CharacterEntity>());
        _charactersByTeam.Add(EntityTeamType.Enemy, new Dictionary<ulong, CharacterEntity>());
        _charactersByTeam.Add(EntityTeamType.Neutral, new Dictionary<ulong, CharacterEntity>());

        _charactersByTransform = new Dictionary<Transform, CharacterEntity>(64);

        // _entityData = new Dictionary<ulong, EntityDataBase>(dataList.Count);

        bool finished = false;

        foreach (var data in dataList)
        {
            var tableData = DBEntity.Get(data.entityId);
            if (tableData == null)
            {
                TEMP_Logger.Err($"Failed to get Entity Table Data | ID : {data.entityId}");
                continue;
            }

            CreateEntityCallBack(data, default, onCompleted: (created) =>
            {
                if (_allEntities.Count == dataList.Count)
                {
                    finished = true;
                }
            }).Forget();
        }

        await UniTask.WaitUntil(() => finished);

        // MeshCombiner 테스트 결과, 모바일 환경에서는 이미 지금 GPU 바운드 상태임.
        // 여기서 MeshCombine 으로 드로우콜/셋패스 줄여야 크게 의미가 없는 테스트 결과가 나와서
        // 일단은 주석치되, 추후 전체적인 최적화 작업 후에 한번 더 테스트해볼 만한 가치가 있는 툴이라
        // 일단 코드만 킵
        //var combiner = _entityRoot.gameObject.AddComponent<MeshCombiner>();

        //combiner.combineFilterPredicate = (go) => go.GetComponent<Collider>() == false;

        //combiner.CreateMultiMaterialMesh = true;
        //combiner.DeactivateCombinedChildren = true;
        //combiner.CombineMeshes(false);

        TEMP_Logger.Deb($"[EntityManager] PrepareGame Done");
    }

    void ShowHud(EntityBase entity)
    {
        if (entity.Type != E_EntityType.Structure)
        {
            return;
        }

        UIManager.Instance.Show<UIEntityCommonHud>(UITrigger.Default, new UIEntityCommonHud.Arg(
            entity.ID,
            entity.transform,
            new Vector2(0, 200)));
    }

    //public void ShowEntityHud()
    //{
    //    foreach (var structure in _structures[EntityTeamType.Player])
    //    {
    //        UIManager.Instance.Show<UIEntityCommonHud>(UITrigger.Default, new UIEntityCommonHud.Arg(structure.Key, structure.Value.transform, new Vector2(0, 200)));
    //    }

    //    foreach (var structure in _structures[EntityTeamType.Enemy])
    //    {
    //        UIManager.Instance.Show<UIEntityCommonHud>(UITrigger.Default, new UIEntityCommonHud.Arg(structure.Key, structure.Value.transform, new Vector2(0, 200)));
    //    }
    //}

    public void SetAllEntityAIPartActivation(bool isAcitvated)
    {
        foreach (var entity in _allEntities.Values)
        {
            if (entity.AIPart != null)
            {
                entity.AIPart.SetActivated(isAcitvated);
            }
        }
    }

    public void RegisterAICommander(EntityAICommanderBase commander, in EntitySetupContext context = default)
    {
        _aiCommander = commander;
        foreach (var entity in _allEntities)
        {
            ApplyAIBehaviour(entity.Value, context);
        }
    }

    void ApplyAIBehaviour(EntityBase entity, in EntitySetupContext context)
    {
        if (entity != null && entity.AIPart != null && _aiCommander != null)
        {
            var behaviour = _aiCommander.CreateBehaviour(entity, context);
            entity.AIPart.SwitchBehaviour(behaviour);
        }
    }

    void OnPhaseChanged(EventContext cxt)
    {
        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;

    }

    // 아무래도 여기저기서 Entity 를 삭제할수있데 열어두는것보다
    // Manager 의 단일 API 로만 통일해놓는게 나을것같아서 삭제
    //public void OnEntityInactivated(EntityBase entity)
    //{
    //    bool removed = _allEntities.Remove(entity.ID);
    //    if (removed == false)
    //    {
    //        TEMP_Logger.Err($"Entity ID : {entity.ID} was not exist in Dictionary | {entity.gameObject.name}");
    //    }
    //    else
    //    {
    //        if (entity.Type == E_EntityType.Character)
    //        {
    //            _characters[entity.Team].Remove(entity.ID);
    //        }
    //        else if (entity.Type == E_EntityType.Structure)
    //        {
    //            _structures[entity.Team].Remove(entity.ID);
    //        }
    //    }
    //}

    public async UniTaskVoid CreateEntityCallBack(EntityObjectData data, EntitySetupContext context = default, Action<EntityBase> onCompleted = null)
    {
      //  try
        {
            var res = await CreateEntity(data, context);
            onCompleted?.Invoke(res);
        }
      //  catch (Exception exp)
        {
       //     TEMP_Logger.Err(exp.Message);
        }
    }

    public async UniTask<EntityBase> CreateEntity(EntityObjectData data, EntitySetupContext context = default)
    {
        var tableData = DBEntity.Get(data.entityId);
        if (tableData == null)
        {
            TEMP_Logger.Err($"Failed to get Entity Table Data | ID : {data.entityId}");
            return null;
        }

        var res = await EntityFactory.CreateEntity(data, _entityRootsByType[(int)tableData.EntityType]);

        if (res.entity == null)
            return null;

        ulong id = res.entity.ID;

        _allEntities.Add(id, res.entity);

        if (tableData.EntityType == E_EntityType.Character)
        {
            var character = res.entity as CharacterEntity;

            _charactersByTransform[res.entity.transform] = character;
            _charactersByTeam[res.entity.Team].Add(id, character);
        }
        else if (tableData.EntityType == E_EntityType.Structure)
        {
            _structures[res.entity.Team].Add(id, res.entity as StructureEntity);
        }

        ApplyAIBehaviour(res.entity, context);

        MapManager.Instance.ApplyEntityToGrid(true, res.entity);

        ShowHud(res.entity);

        _entityCreatedEventArg.Entity = res.entity;
        InGameManager.Instance.PublishEvent(InGameEvent.EntityCreated, _entityCreatedEventArg);

        return res.entity;
    }

    public void RemoveEntity(ulong id)
    {
        if (_allEntities.TryGetValue(id, out var entity) == false)
            return;

        RemoveEntity(entity);
    }

    public void RemoveEntity(EntityBase entity)
    {
        if (entity == null || _allEntities.ContainsKey(entity.ID) == false)
            return;

        if (entity.Type == E_EntityType.Character)
        {
            _charactersByTeam[entity.Team].Remove(entity.ID);
        }
        else if (entity.Type == E_EntityType.Structure)
        {
            _structures[entity.Team].Remove(entity.ID);

            // !! MapManager 에 사라진 엔티티를 전달해서
            // 실시간으로 타일 상태에 반영되게함 !!
            MapManager.Instance.ApplyEntityToGrid(false, entity);
        }

        AllEntities.Remove(entity.ID);

        InGameManager.Instance.PublishEvent(InGameEvent.EntityRemoved, new EntityRemovedEventArg()
        {
            Team = entity.Team,
            Type = entity.Type,
            PastId = entity.ID
        });

        entity.Return();
    }

    public void RemoveCharacterEntities(EntityTeamType team)
    {
        var characters = GetCharacters(team);

        _removeCache.Clear();

        foreach (var c in characters)
        {
            // actor 는 플레이어 캐릭터니까 제외시킴
            if (c.Value.CharacterType != E_CharacterType.Actor)
                _removeCache.Add(c.Value.ID);
        }

        for (int i = 0; i < _removeCache.Count; i++)
        {
            RemoveEntity(characters[_removeCache[i]]);
        }

        // 함수의 역할은 모든 해당 팀의 모든 캐릭 엔티티 삭제인데
        // Remove 중간에 뭔가 생긴거. 이건 의도되지않앗을 확률이 높죠
        if (GetCharacterCount(team) > 0)
        {
            TEMP_Logger.Wrn($"Another Character possibly created during Removing ?");
        }
    }

    public override void Release()
    {
        base.Release();

        _allEntities.Clear();
        _allEntities = null;
        _aiCommander.Release();
        //_entityData.Clear();
        //_entityData = null;

        EventManager.Instance.Unregister(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnPhaseChanged);

        Destroy(_entityRoot.gameObject);
        _entityRoot = null;
    }
}
