using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyTeamCommander
{
    // 일단은 이정도로 타깃 개수 설정하자
    const int TargetCount = 2;

    public EnemySpawnController SpawnController { get; private set; }

    //private TeamStrategy _strategy;

    private List<PathVisualizer> _pathVisualizer;

    public DefenseBattleStatus BattleStatus { get; private set; }

    CancellationTokenSource _ctkSrc;

    FX3DMarker _target3DMarker;

    List<EntityObjectData> _fixedEntitySet;
    //uint _nexusOriginalId;
    Vector3 _nexusOriginalPosition;
    Quaternion _nexusOriRot;
    //int _nexusOriginalEulerY;

    uint _waveId = 0;

    public void Initialize()
    {
        SpawnController = new EnemySpawnController();
        SpawnController.Initialize();

        _fixedEntitySet = new List<EntityObjectData>();

        foreach (var structure in EntityManager.Instance.GetStructures(EntityTeamType.Enemy))
        {
            if (structure.Value.StructureData.StructureType == GameDB.E_StructureType.Nexus)
            {
                _nexusOriginalPosition = structure.Value.transform.position.FlatHeight();
                _nexusOriRot = structure.Value.transform.rotation;
            }

            _fixedEntitySet.Add(new EntityObjectData(structure.Value.transform.position.FlatHeight(), (int)structure.Value.transform.eulerAngles.y, structure.Value.EntityTID, EntityTeamType.Enemy));
        }

        BattleStatus = new DefenseBattleStatus();

        // _strategy = new TeamStrategy();

        _pathVisualizer = new List<PathVisualizer>();

        InGameManager.Instance.EventListener += OnInGameEvent;

        PoolManager.Instance.RequestSpawnAsyncCallBack<FX3DMarker>(ObjectPoolCategory.Fx, "WorldObjectMarker01", onCompleted: (res, opRes) =>
        {
            _target3DMarker = res;
            res.gameObject.SetActive(false);
        }).Forget();

        AssignToken();

        // EventManager.Instance.Register(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnNewPhaseEnter);
    }

    public Vector3 GetNexusPosition()
    {
        return _nexusOriginalPosition;
    }

    internal Quaternion GetNexusRotation()
    {
        return _nexusOriRot;
    }

    public void SpawnEntity(Vector3 position, uint spawnEntityId)
    {
        // TODO : 인구수 제한 둬야할듯 나중에

        EntityManager.Instance.CreateEntity(new EntityObjectData(position, 0, spawnEntityId, EntityTeamType.Enemy)).Forget();
    }

    public async UniTask RebuildStructures()
    {
        var structures = EntityManager.Instance.GetStructures(EntityTeamType.Enemy).Values.ToList();

        foreach (var preset in _fixedEntitySet)
        {
            var tilePos = MapUtils.WorldPosToTilePos(preset.worldPosition);
            if (structures.Exists(t => MapUtils.WorldPosToTilePos(t.transform.position) == tilePos) == false)
            {
                await EntityManager.Instance.CreateEntity(preset);
            }
        }
    }

    public void Release()
    {
        // EventManager.Instance.Unregister(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnNewPhaseEnter);

        InGameManager.Instance.EventListener -= OnInGameEvent;

        if (SpawnController != null)
        {
            SpawnController.Release();
            SpawnController = null;
        }

        if (_fixedEntitySet != null)
        {
            _fixedEntitySet.Clear();
            _fixedEntitySet = null;
        }

        if (_pathVisualizer != null)
        {
            for (int i = 0; i < _pathVisualizer.Count; i++)
            {
                _pathVisualizer[i].Release();
            }
            _pathVisualizer.Clear();
            _pathVisualizer = null;
        }

        if (_target3DMarker)
        {
            _target3DMarker.Return();
            _target3DMarker = null;
        }

        CancelToken();

        _nexusOriginalPosition = default;
        _nexusOriRot = Quaternion.identity;
        BattleStatus = null;
    }

    void CancelToken()
    {
        if (_ctkSrc != null)
        {
            _ctkSrc.Cancel();
            _ctkSrc.Dispose();
            _ctkSrc = null;
        }
    }

    void AssignToken()
    {
        CancelToken();
        _ctkSrc = new CancellationTokenSource();
    }

    public void OnPeaceStart()
    {
        // Spawner 등록 
        var spawners = EntityManager.Instance.GetStructures(EntityTeamType.Enemy, 0,
              (t) =>
              {
                  bool isSpawner = t.Type == GameDB.E_EntityType.Structure && (t as StructureEntity).StructureData.StructureType == GameDB.E_StructureType.Spawner;
                  return isSpawner;
              });

        foreach (var spawner in spawners)
        {
            SpawnController.RegisterSpawner(spawner as SpawnerStructureEntity);
        }

        InitBattleStatus(TargetCount);
        RefreshPathIndicators(_ctkSrc.Token).Forget();

        _target3DMarker.gameObject.SetActive(false);
    }

    void InitBattleStatus(int targetCount)
    {
        targetCount = Mathf.Min(targetCount, EntityManager.Instance.GetStructureCount(EntityTeamType.Player));
        if (targetCount == 0)
        {
            TEMP_Logger.Err($"No Sturcutre left anymore ?");
            return;
        }

        var targetEntities = new List<StructureEntity>();
        Vector3 centerSpawnPos = SpawnController.GetCenterPosition();

        targetEntities = EntityManager.Instance.GetStructures(EntityTeamType.Player).Values.ToList();

        targetEntities.Sort((lhs, rhs) =>
        {
            return Vector3.Distance(lhs.transform.position, centerSpawnPos).CompareTo(Vector3.Distance(rhs.transform.position, centerSpawnPos));
        });

        if (targetEntities.Count > Constants.InGame.TargetCount)
        {
            targetEntities.Resize(Constants.InGame.TargetCount);
        }

        BattleStatus.SetOrderedTargetIndexes(targetEntities.Select(t => t.ID).ToArray());
        // BattleStatus.SetCurrentTargetIndex(0);

        // await RefreshIndicators();
    }

    async UniTaskVoid RefreshPathIndicators(CancellationToken ctk)
    {
        var spawnPositions = SpawnController.SpawnPositions;
        int requiredCount = spawnPositions.Count;

        if (requiredCount > _pathVisualizer.Count)
        {
            for (int i = 0; i < requiredCount; i++)
            {
                _pathVisualizer.Add(new PathVisualizer());
            }
        }
        else if (requiredCount < _pathVisualizer.Count)
        {
            int removeCnt = _pathVisualizer.Count - requiredCount;
            for (int i = 0; i < removeCnt; i++)
            {
                _pathVisualizer[i].Hide();
            }

            _pathVisualizer.RemoveRange(0, removeCnt);
        }

        ulong targetId = BattleStatus.CurrentTargetID;

        if (targetId != 0)
        {
            for (int i = 0; i < _pathVisualizer.Count; i++)
            {
                var path = await InGameManager.Instance.DefensePathSystem.GetPathToEntityAsync(
                    spawnPositions[i],
                    targetId,
                    EntityManager.Instance.GetEntity(targetId).TableData.EntityFlags,
                    new PathBuffer.Modifier(false),
                    _ctkSrc.Token);

                _pathVisualizer[i].Show(path.Instance.ToArray());

                // 다 썼으니 풀에 반납해야함
                path.ReturnToPool();
            }
        }
    }

    public void OnBattleStart()
    {
        SpawnController.PrepareBattle();

        StartSpawn();

        RefreshTarget3DMarker();
    }

    void StartSpawn()
    {
        if (_waveId == 0)
            _waveId = DBWave.GetFirstWaveID();
        else _waveId = DBWave.GetNextWave(_waveId);

        WaveManager.Instance.StartWave(_waveId);
    }

    public void OnBattleEnd()
    {
        SpawnController.Release();
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.EntityConstructed)
        {
            var arg = argBase as EntityConstructedEventArg;
            if (arg.Entity.Type == GameDB.E_EntityType.Structure)
            {
                OnConstructed(arg.Entity);
            }
        }
        else if (evt == InGameEvent.BattleEnding)
        {
            SpawnController.Pause();
        }
    }

    void OnConstructed(EntityBase entity)
    {
        AssignToken();

        //var occupationData = entity.GetData(EntityDataCategory.Occupation) as EntityOccupationData;
        //if (occupationData == null)
        //    return;

        //bool invalidated = InGameManager.Instance.DefensePathSystem.RemoveOverlappedPaths(
        //   occupationData.GroundWalkableNeighborPositionList);

        //if (_ctkSrc.IsCancellationRequested)
        //    return;

        ulong prevFirstTarget = BattleStatus.CurrentTargetID;

        InitBattleStatus(TargetCount);

        if (prevFirstTarget != BattleStatus.CurrentTargetID)
            RefreshPathIndicators(_ctkSrc.Token).Forget();

        //Refresh(new RefreshSettings()
        //{
        //    type = RefreshType.InvalidatePath,
        //    invalidateSettings = new InvalidateSettings() { createdEntityID = arg.entity.ID }
        //});
    }

    // Wave 시스템도 더 이상 작업중이지 않고
    // 스폰 대기중인 애도 없고
    // 살아있는 적 캐릭터 & 건물도 없다면 배틀끝으로 처리 
    //public bool IsFinished()
    //{
    //    return WaveManager.Instance.IsProcessing == false &&
    //        SpawnController.SpawningEntityCount == 0 &&
    //        EntityManager.Instance.GetCharacterCount(EntityTeamType.Enemy) == 0 &&
    //        EntityManager.Instance.HasAliveStructure(EntityTeamType.Enemy) == false;
    //}

    public void OnTargetChanged()
    {
        //if (BattleStatus.HasNextTarget == false)
        //{
        //    TEMP_Logger.Err($"No Target Exist");
        //    return;
        //}

        //BattleStatus.SetNextTarget();

        //if (BattleStatus.HasNextTarget == false)
        //    return;
        AssignToken();
        RefreshPathIndicators(_ctkSrc.Token).Forget();

        // TODO : path 갱신 등
        RefreshTarget3DMarker();
    }

    void RefreshTarget3DMarker()
    {
        var currentTarget = EntityManager.Instance.GetEntity(BattleStatus.CurrentTargetID);

        if (currentTarget)
        {
            var markerPos = currentTarget.ModelPart.TopPosition;
            _target3DMarker.SetPosition(markerPos);
            _target3DMarker.gameObject.SetActive(true);
        }
        else
        {
            _target3DMarker.gameObject.SetActive(false);
        }
    }
}
