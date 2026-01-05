using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;
using Cysharp.Threading.Tasks;

public class SpawnerStructureEntity : StructureEntity
{
    EntitySpawnerPartInitData _spawnerInitData;

    FXBase _spawnPointFx;
    public EntitySpawnerPart SpawnerPart { get; private set; }

    public SpawnerMode SpawnerMode { get; private set; }
    public bool IsSpawnerEnabled => SpawnerPart != null && SpawnerPart.IsEnabled;

    public event Action<SpawnerMode> SpawnerModeChangedListener;

    public override async UniTask<(EntityBase, IEnumerable<EntityDataBase>)> Initialize(
        E_EntityType entityType,
        IEnumerable<EntityDataBase> entityDatabase,
        EntityObjectData objectData)
    {
        var res = await base.Initialize(entityType, entityDatabase, objectData);

        InGameManager.Instance.EventListener += OnInGameEvent;

        SetSpawnerMode(SpawnerMode.Defensive);

        return res;
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.Enter || evt == InGameEvent.Start)
        {
            var arg = argBase as InGameFSMStateNotify;
            if (arg.Phase == InGamePhase.Battle)
            {
                var actionPointSocket = ModelPart.GetSocket(EntityModelSocket.ActionPoint);
                if (actionPointSocket == null)
                {
                    TEMP_Logger.Err($"Spawner Must have a ActionPoint Socket for Spawn Position | Name : {name} , StructureId : {StructureData.ID}");
                    return;
                }

                if (evt == InGameEvent.Enter)
                {
                    // 이건걍 하드코딩해도댈듯
                    string fxKey = _team == EntityTeamType.Player ? "FX_AreaGreen" : "FX_AreaRed";
                    var fxPos = actionPointSocket.position;
                    fxPos.y = 0.2f;
                    FXSystem.PlayFXCallBack(fxKey, startPosition: fxPos, onCompleted: (res) =>
                    {
                        _spawnPointFx = res;
                    }).Forget();
                }
                else if (evt == InGameEvent.Start)
                {
                    if (SpawnerPart != null)
                        SpawnerPart.IsEnabled = true;
                }
            }
        }
        else if (evt == InGameEvent.BattleEnding)
        {
            if (SpawnerPart != null)
                SpawnerPart.IsEnabled = false;

            ReleaseSpawnPointFx();
        }
    }

    protected override void OnUpdateImpl()
    {
        base.OnUpdateImpl();

        SpawnerPart?.Update();
    }

    public override void OnInitializeFinished()
    {
        base.OnInitializeFinished();

        if (StructureData.StructureType == E_StructureType.Spawner && StructureData.EnableSpawning)
        {
            if (_spawnerInitData == null)
                _spawnerInitData = new EntitySpawnerPartInitData(this);
            else _spawnerInitData.Owner = this;

            _spawnerInitData.EntityID = StructureData.SpawnEntityIDOnCombat;
            _spawnerInitData.Interval = StructureData.SpawnIntervalSeconds;
            _spawnerInitData.TeamType = Team;
            _spawnerInitData.SpawnPosition = ModelPart.GetSocket(EntityModelSocket.ActionPoint).position;

            SpawnerPart = InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntitySpawnerPart>(_spawnerInitData);
        }
    }
    public override UniTask OnDie(ulong attackerId, Vector3 attackerPos, float force)
    {
        ReleaseSpawner();

        return base.OnDie(attackerId, attackerPos, force);
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        InGameManager.Instance.EventListener -= OnInGameEvent;

        ReleaseSpawner();
    }

    void ReleaseSpawner()
    {
        SpawnerModeChangedListener = null;

        if (SpawnerPart != null)
        {
            SpawnerPart.ReturnToPool();
            SpawnerPart = null;
        }

        ReleaseSpawnPointFx();

    }

    public void SetSpawnerMode(SpawnerMode mode)
    {
        SpawnerMode = mode;
        if (SpawnerPart != null)
            SpawnerPart.SpawnMode = mode;
        SpawnerModeChangedListener?.Invoke(mode);
    }

    private void ReleaseSpawnPointFx()
    {
        if (_spawnPointFx)
        {
            _spawnPointFx.Return();
            _spawnPointFx = null;
        }
    }
}
