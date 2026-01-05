//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using Cysharp.Threading.Tasks;
//using System.Threading;

//public class SpawnManager : SingletonBase<SpawnManager>
//{
//    public class SpawnPoint
//    {
//        public int id;
//        public Vector2Int tilePosition;
//        public Vector3 worldPosition;
//        public FXBase fx;

//        public SpawnPoint(int id, Vector2Int tilePosition, FXBase fx)
//        {
//            this.id = id;
//            this.tilePosition = tilePosition;
//            worldPosition = MapUtils.TilePosToWorldPos(tilePosition);
//            this.fx = fx;
//            // objectDataCache = new EntityObjectData(worldPosition, 0, (uint)UnityEngine.Random.Range(362, 371));
//        }
//    }

//    List<SpawnPoint> _spawnPoints;
//    Dictionary<int, SpawnPoint> _spawnPointsDic;
//    public List<SpawnPoint> SpawnPoints => _spawnPoints;
//    public Vector3 CenterSpawnPoint
//    {
//        get
//        {
//            var sum = Vector3.zero;

//            foreach (var p in _spawnPoints)
//            {
//                sum += p.worldPosition;
//            }

//            sum.y = 0;
//            return sum / _spawnPoints.Count;
//        }
//    }

//    HashSet<ulong> _spawnedEntity = new HashSet<ulong>(100);

//    // SpawningEnemySettings _settings;

//    CancellationTokenSource _cancellationTokenSource;

//    public uint SpawningEntityCount { get; private set; }

//    public override void Initialize()
//    {
//        base.Initialize();
//    }

//    public override void Release()
//    {
//        Clean();
//        base.Release();
//    }

//    public async UniTask PrepareGame(MapData mapData)
//    {
//        InGameManager.Instance.EventListener += OnEntityRemoved;

//        if (mapData.enemySpawnPositions != null)
//        {
//            _spawnPoints = new List<SpawnPoint>(mapData.enemySpawnPositions.Count);
//            _spawnPointsDic = new Dictionary<int, SpawnPoint>();

//            int pointId = 1;
//            foreach (var pos in mapData.enemySpawnPositions)
//            {
//                var p = pos;
//                var worldPos = MapUtils.TilePosToWorldPos(p);
//                FXSystem.PlayFX("EnemySpawnPoint",
//                    startPosition: worldPos,
//                    rotation: Quaternion.identity,
//                    onCompleted: (fx) =>
//                    {
//                        var spawnPoint = new SpawnPoint(pointId, p, fx);
//                        _spawnPoints.Add(spawnPoint);
//                        _spawnPointsDic.Add(pointId, spawnPoint);
//                        pointId++;
//                    }).Forget();
//            }

//            await UniTask.WaitUntil(() => _spawnPoints.Count == mapData.enemySpawnPositions.Count);
//        }
//    }

//    public void StartBattle()
//    {
//        _cancellationTokenSource = new CancellationTokenSource();
//    }

//    public Vector3 GetSpawnPosition(int id)
//    {
//        if (_spawnPointsDic.TryGetValue(id, out var point))
//            return point.worldPosition;
//        return _spawnPoints[0].worldPosition;
//    }

//    public void SpawnEnemyWave(WaveSpawnCmdData cmdData)
//    {
//        if (cmdData.Count == 1)
//        {
//            SpawnEntity(EntityTeamType.Enemy, cmdData.SpawnEntityID, GetSpawnPosition((int)cmdData.SpawnPointID), 0);
//        }
//        else
//        {
//            SpawnEnemyEntityCountAsync(
//                cmdData.SpawnEntityID,
//                (int)cmdData.Count,
//                GetSpawnPosition((int)cmdData.SpawnPointID),
//                cmdData.Interval).Forget();
//        }
//    }

//    void SpawnEntity(EntityTeamType team, uint entityId, Vector3 position, int eulerY)
//    {
//        EntityManager.Instance.CreateEntity(
//            new EntityObjectData(position, eulerY, entityId, team), null,
//            (res) =>
//            {
//                _spawnedEntity.Add(res.ID);

//                InGameManager.Instance.PublishEvent(
//                    InGameEvent.EntitySpawned,
//                    new EntitySpawnEventArg()
//                    {
//                        entity = res,
//                    });
//            });
//    }

//    async UniTaskVoid SpawnEnemyEntityCountAsync(uint entityId, int count, Vector3 position, float interval)
//    {
//        SpawningEntityCount += (uint)count;

//        for (int i = 0; i < count; i++)
//        {
//            SpawnEntity(EntityTeamType.Enemy, entityId, position, 0);

//            if (SpawningEntityCount > 0)
//                SpawningEntityCount--;

//            try
//            {
//                await UniTask.WaitForSeconds(interval, cancellationToken: _cancellationTokenSource.Token);
//            }
//            catch (OperationCanceledException)
//            {
//                int remained = count - i;
//                if (SpawningEntityCount >= remained)
//                    SpawningEntityCount -= (uint)remained;
//                else SpawningEntityCount = 0;

//                return;
//            }
//        }
//    }

//    public void Clean()
//    {
//        SpawningEntityCount = 0;

//        if (_cancellationTokenSource != null)
//        {
//            _cancellationTokenSource.Cancel();
//            _cancellationTokenSource.Dispose();
//            _cancellationTokenSource = null;
//        }

//        InGameManager.Instance.EventListener -= OnEntityRemoved;

//        foreach (var entity in _spawnedEntity)
//        {
//            EntityManager.Instance.RemoveEntity(entity);
//        }
//        _spawnedEntity.Clear();
//    }

//    private void OnEntityRemoved(InGameEvent evt, InGameEventArgBase arg)
//    {
//        if (evt == InGameEvent.EntityRemoved)
//            _spawnedEntity.Remove((arg as EntityRemovedEventArg).pastId);
//    }
//}
