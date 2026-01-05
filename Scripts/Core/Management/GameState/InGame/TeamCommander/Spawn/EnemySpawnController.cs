using UnityEngine;
using GameDB;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public enum SpawnStrategyType
{
    None = 0,

    Distribute,
    Random,
    Broadcast,
    Concentrate,
}

public class EnemySpawnController
{
    public int SpawningEntityCount { get; private set; }

    private List<SpawnerStructureEntity> _spawners;
    private List<Vector3> _spawnPositions;
    public IReadOnlyList<Vector3> SpawnPositions => _spawnPositions;

    bool _paused;

    CancellationTokenSource _cancellationTokenSource;

    public void Initialize()
    {
        _spawners = new List<SpawnerStructureEntity>();
        _spawnPositions = new List<Vector3>();
    }

    public void Release()
    {
        _paused = false;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;

        EntityManager.Instance.RemoveCharacterEntities(EntityTeamType.Enemy);

        SpawningEntityCount = 0;

        _spawners.Clear();
        _spawnPositions.Clear();
    }

    public void Pause()
    {
        _paused = true;
    }

    public Vector3 GetCenterPosition()
    {
        Vector3 pos = Vector3.zero;
        foreach (var p in _spawnPositions)
        {
            pos += p;
        }
        pos.y = 0;
        return pos / _spawnPositions.Count;
    }

    public void RegisterSpawner(SpawnerStructureEntity spawner)
    {
        if (_spawners.Contains(spawner))
        {
            TEMP_Logger.Err($"Duplicate Register : {spawner.ID}");
            return;
        }

        _spawners.Add(spawner);
        _spawnPositions.Add(spawner.ModelPart.GetSocket(EntityModelSocket.ActionPoint).position);
    }

    //public void UnregisterSpawner(SpawnerStructureEntity spawner)
    //{
    //    int idx = _spawners.FindIndex((t) => t == spawner);
    //    if (idx == -1)
    //    {
    //        TEMP_Logger.Err($"Invalid idx Spawner : {spawner.ID}");
    //        return;
    //    }

    //    _spawners.RemoveAt(idx);
    //    _spawnPositions.RemoveAt(idx);
    //}

    public void PrepareBattle()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _paused = false;
    }

    public void SpawnEnemyWave(RuntimeSpawnCmd cmdData)
    {
        //if (cmdData.Count == 1)
        //{
        //    SpawnEntity(EntityTeamType.Enemy, cmdData.SpawnEntityID, GetSpawnPosition((int)cmdData.SpawnPointID), 0);
        //}
        //else
        //{

        SpawnEnemyEntityCountAsync(
            cmdData.EntityID,
            cmdData.Count,
            cmdData.Strategy,
            cmdData.Interval).Forget();
    }

    void SpawnEntity(EntityTeamType team, uint entityId, Vector3 position, int eulerY)
    {
        EntityManager.Instance.CreateEntityCallBack(
            new EntityObjectData(position, eulerY, entityId, team),
            default,
            (res) =>
             {
                 //_spawnedEntity.Add(res.ID);

                 InGameManager.Instance.PublishEvent(
                      InGameEvent.EntitySpawned,
                      new EntitySpawnEventArg()
                      {
                          Entity = res,
                      });
             }).Forget();
    }

    Vector3 GetRandomSpawnPoint()
    {
        if (_spawnPositions.Count == 0)
            return default;
        return _spawnPositions[UnityEngine.Random.Range(0, _spawnPositions.Count)];
    }

    async UniTaskVoid SpawnEnemyEntityCountAsync(uint entityId, int count, SpawnStrategyType strategy, float interval)
    {
        int distributeIdx = 0;
        int concentrateIdx = UnityEngine.Random.Range(0, _spawnPositions.Count);

        // 한 사이클에 몇 마리 생성인가 ? 
        int spawnCntPerCycle = strategy == SpawnStrategyType.Broadcast ? _spawnPositions.Count : 1;

        SpawningEntityCount += count * spawnCntPerCycle;

        for (int i = 0; i < count; i++)
        {
            if (_paused)
                continue;

            switch (strategy)
            {
                case SpawnStrategyType.Distribute:
                    {
                        SpawnEntity(EntityTeamType.Enemy, entityId, _spawnPositions[distributeIdx], 0);
                        distributeIdx++;

                        if (distributeIdx >= _spawnPositions.Count)
                            distributeIdx = 0;
                    }
                    break;
                case SpawnStrategyType.Random:
                    {
                        SpawnEntity(EntityTeamType.Enemy, entityId, GetRandomSpawnPoint(), 0);
                    }
                    break;
                case SpawnStrategyType.Broadcast:
                    {
                        foreach (var pos in _spawnPositions)
                        {
                            SpawnEntity(EntityTeamType.Enemy, entityId, pos, 0);
                        }
                    }
                    break;
                case SpawnStrategyType.Concentrate:
                    {
                        SpawnEntity(EntityTeamType.Enemy, entityId, _spawnPositions[concentrateIdx], 0);
                    }
                    break;
                default:
                    break;
            }

            SpawningEntityCount = Mathf.Max(SpawningEntityCount - spawnCntPerCycle, 0);

            try
            {
                await UniTask.WaitForSeconds(interval, cancellationToken: _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                int remained = (count - i) * spawnCntPerCycle;
                if (SpawningEntityCount >= remained)
                    SpawningEntityCount -= remained;
                else SpawningEntityCount = 0;

                return;
            }
        }
    }
}
