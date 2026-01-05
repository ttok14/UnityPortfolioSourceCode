using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class DefenseMode : BattleModeBase, IPathProvider, IObjectiveProvider
{
    //public class TargetingEntity
    //{
    //    public ulong TargetEntityID;
    //    public Vector3 closestDestPos;
    //    public List<PathCaching> Path;

    //    public TargetingEntity(ulong targetEntityID)
    //    {
    //        TargetEntityID = targetEntityID;
    //        Path = new List<PathCaching>();
    //    }

    //    public List<Vector3> TryGetPath(Vector3 pos)
    //    {
    //        foreach (var path in Path)
    //        {
    //            var res = path.Get(pos, 1);
    //            if (res != null)
    //                return res;
    //        }
    //        return null;
    //    }
    //}

    // new DefenseBattleStatus _status;
    public override InGameBattleMode Mode => InGameBattleMode.Defense;

    ulong _playerEntityId;

    //public EntityBase[] OrderedTargets => _status.OrderedTargetIDs.Select(t => EntityManager.Instance.GetEntity(t)).ToArray();
    //Dictionary<Vector2Int, List<List<Vector3>>> _spawnPointsToTargets;
    //public Dictionary<Vector3, List<List<Vector3>>> SpawnPointToTargetPaths { get; private set; }

    // key : targetID 
    // public Dictionary<ulong, TargetingEntity> ToTargetPathCache;

    // public ulong CurrentTargetID => _strategySystem.CurrentTargetID;
    public override BattleStatusBase GetBattleStatus(EntityTeamType team)
    {
        if (team == EntityTeamType.Player)
            return InGameManager.Instance.PlayerCommander.BattleStatus;
        else if (team == EntityTeamType.Enemy)
            return InGameManager.Instance.EnemyCommander.BattleStatus;
        else
        {
            TEMP_Logger.Err($"Not handled Team type : {team}");
            return null;
        }
    }

    public ulong GetCurrentTargetID(EntityTeamType team)
    {
        var bs = GetBattleStatus(team);
        if (bs == null)
            return 0;
        return (bs as DefenseBattleStatus).CurrentTargetID;
    }

    // public EntityBase CurrentTargetEntity => EntityManager.Instance.GetEntity(CurrentTargetID);
    public EntityBase GetTargetEntity(EntityTeamType team)
    {
        ulong id = GetCurrentTargetID(team);
        if (id == 0)
            return null;
        return EntityManager.Instance.GetEntity(id);
    }

    // List<PathLineRenderController> _pathController;

    //public List<Vector3> GetSpawnPointToTarget(Vector2Int spawnPoint, int targetIdx) => _spawnPointsToTargets[spawnPoint][targetIdx];

    float _finishCheckTimeAt;

    bool _gameFinished;

    protected override BattleStatusBase CreateBattleStatus()
    {
        return new DefenseBattleStatus();
    }

    public override async UniTask EnterAsync(BattlePhase owner)
    {
        await base.EnterAsync(owner);

        _playerEntityId = InGameManager.Instance.PlayerCommander.Player.Entity.ID;

        //_status = base.Status as DefenseBattleStatus;
        //_status.OrderedTargetIDs = new ulong[2];
        //_status.CurrentTargetIndex = 0;

        //_spawnPointsToTargets = new Dictionary<Vector2Int, List<List<Vector3>>>();
        //ToTargetPathCache = new Dictionary<ulong, TargetingEntity>();
        //_pathController = new List<PathLineRenderController>();

        var randomStructures = EntityManager.Instance.GetStructures(EntityTeamType.Player).ToList();
        randomStructures.Shuffle();

        // TODO : 임시로 일단 아무 건물이나 타게팅. 추후 기준을 잡아서 공격 타겟 잡을것
        //int targetCount = Math.Clamp(randomStructures.Count, 0, _status.OrderedTargetIDs.Length);
        //for (int i = 0; i < targetCount; i++)
        //{
        //    _status.OrderedTargetIDs[i] = randomStructures[i].Key;
        //}

        // await SetSpawnPointsToTargetPaths();

        //foreach (var point in SpawnManager.Instance.SpawnPoints)
        //{
        //    var resIndicator = await PoolManager.Instance.RequestSpawnAsync<PathLineRenderController>(
        //         ObjectPoolCategory.Default,
        //         "EnemyPathIndicator");

        //    resIndicator.instance.gameObject.SetActive(false);
        //    _pathController.Add(resIndicator.instance);
        //}

        // 컷씬 전에 미리 경로 보여줌
        // RefreshPathIndicator();

        await CutSceneManager.Instance.BeginCutScene(InGameCutSceneType.EnterDefenseMode);

        _finishCheckTimeAt = Time.time;
    }

    //void RefreshPathIndicator()
    //{
    //    for (int i = 0; i < _pathController.Count; i++)
    //    {
    //        int idx = i;
    //        GetPathCacheToTarget(SpawnManager.Instance.SpawnPoints[idx].worldPosition, (path) =>
    //        {
    //            // _pathController[idx].gameObject.SetActive(true);
    //            _pathController[idx].ChangeSettings(path.ToArray());
    //        });
    //    }
    //}

    public override void Release()
    {
        base.Release();

        _gameFinished = false;
    }

    public override bool CheckFinished()
    {
        return _gameFinished;
    }

    public override async UniTaskVoid FinishCutSceneRoutine(EntityTeamType winnerTeam, Action onCutsceneFinished)
    {
        await CutSceneManager.Instance.BeginCutScene(InGameCutSceneType.ExitDefenseMode);

        onCutsceneFinished();
    }

    protected override void OnEventReceived(InGameEvent evt, InGameEventArgBase arg)
    {
        if (evt == InGameEvent.EntitySpawned)
        {
            var entity = (arg as EntitySpawnEventArg).Entity;
            if (entity.AIPart != null)
            {
                entity.AIPart.SetActivated(true);
            }
        }
        else if (evt == InGameEvent.EntityDied)
        {
            var earg = arg as EntityDiedEventArg;

            var opponentTeam = earg.Team == EntityTeamType.Player ? EntityTeamType.Enemy : EntityTeamType.Player;

            // player Entity 가 죽었으면 enemy 팀으로 넣어줘서 player 의 타겟 id 를 가져오고
            // 반대도 같은 룰 적용
            // var currentTargetId = GetCurrentTargetID(opponentTeam);

            //bool finished = false;

            bool isPlayerDead = _playerEntityId == earg.ID;

            // 플레이어 사망시 적의 승리
            if (isPlayerDead)
            {
                SetWinnerTeam(EntityTeamType.Enemy);
                _gameFinished = true;
                return;
            }

            DefenseBattleStatus oppoBattleStatus = null;

            if (earg.Team == EntityTeamType.Player)
            {
                oppoBattleStatus = InGameManager.Instance.EnemyCommander.BattleStatus;
            }
            else if (earg.Team == EntityTeamType.Enemy)
            {
                oppoBattleStatus = InGameManager.Instance.PlayerCommander.BattleStatus;
            }

            ulong prevTargetId = oppoBattleStatus.CurrentTargetID;
            oppoBattleStatus.Remove(earg.ID);

            bool targetChanged = prevTargetId != oppoBattleStatus.CurrentTargetID;
            if (targetChanged)
            {
                switch (earg.Team)
                {
                    case EntityTeamType.Player:
                        InGameManager.Instance.EnemyCommander.OnTargetChanged();
                        AudioManager.Instance.Play("SFX_LoLTurretDestroyed01");
                        break;
                    case EntityTeamType.Enemy:
                        InGameManager.Instance.PlayerCommander.OnTargetChanged();
                        AudioManager.Instance.Play("SFX_LoLTurretDestroy01");
                        break;
                }
            }

            if (oppoBattleStatus.HasCurrentTarget == false)
            {
                SetWinnerTeam(opponentTeam);
                _gameFinished = true;
            }

            //// 죽은애가 타겟이 아니면
            //if (currentTargetId != earg.ID)
            //{
            //    if (earg.Type == GameDB.E_EntityType.Character)
            //    {
            //        var character = EntityManager.Instance.GetEntity(earg.ID) as CharacterEntity;
            //        // 플레이어 사망시 적의 승리
            //        if (character.CharacterType == GameDB.E_CharacterType.Actor)
            //        {
            //            SetWinnerTeam(EntityTeamType.Enemy);
            //            _gameFinished = true;
            //        }
            //    }

            //    return;
            //}

            //bool finished = false;

            //if (earg.Team == EntityTeamType.Player)
            //{
            //    InGameManager.Instance.EnemyCommander.BattleStatus.Remove(earg.ID);
            //    if (InGameManager.Instance.EnemyCommander.BattleStatus.HasNextTarget)
            //        InGameManager.Instance.EnemyCommander.SetNextTarget();
            //    else finished = true;

            //    AudioManager.Instance.Play("SFX_MonsterCrowdShout", delay: 1f);
            //}
            //else if (earg.Team == EntityTeamType.Enemy)
            //{
            //    if (InGameManager.Instance.PlayerCommander.BattleStatus.HasNextTarget)
            //        InGameManager.Instance.PlayerCommander.SetNextTarget();
            //    else finished = true;
            //}

            //if (finished)
            //{
            //    // 게임 끄읕.
            //    SetWinnerTeam(opponentTeam);
            //    _gameFinished = true;
            //}
        }
    }

    public override void ForceFinish()
    {
        SetWinnerTeam(EntityTeamType.Enemy);
        _gameFinished = true;
    }

    #region ====:: Interface Impl ::====

    public void FetchPath(EntityBase mover, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        var id = GetCurrentTargetID(mover.Team);
        InGameManager.Instance.DefensePathSystem.GetPathToEntityAsyncCallBack(mover.ApproxPosition, id, mover.TableData.EntityFlags, new PathBuffer.Modifier(true), ctk, onFetched).Forget();
    }

    public void FetchPath(EntityBase mover, ulong targetEntityID, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        InGameManager.Instance.DefensePathSystem.GetPathToEntityAsyncCallBack(mover.ApproxPosition, targetEntityID, mover.TableData.EntityFlags, new PathBuffer.Modifier(true), ctk, onFetched).Forget();
    }

    public void FetchPath(EntityBase mover, Vector3 destination, in PathBuffer.Modifier modifier, CancellationToken ctk, Action<PathListPoolable> onFetched)
    {
        InGameManager.Instance.DefensePathSystem.GetPathAsyncCallBack(mover.ApproxPosition, destination, mover.TableData.EntityFlags, modifier, ctk, onFetched).Forget();
    }

    #endregion
}
