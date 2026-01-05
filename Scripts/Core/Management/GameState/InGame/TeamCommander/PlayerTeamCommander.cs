using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class PlayerTeamCommander
{
    private PlayerController _player;
    public PlayerController Player => _player;

    //private TeamStrategy _strategy;

    public DefenseBattleStatus BattleStatus { get; private set; }

    Action<EntityBase> _onEntitySpawned;

    uint _nexusOriginalId;
    Vector3 _nexusOriginalPosition;
    Quaternion _nexusOriRot;
    int _nexusOriginalEulerY;

    FX3DMarker _target3DMarker;

    float _lastTimeCoinAcquired;

    public void Initialize()
    {
        // TODO : 플레이어 캐릭터는 Entity 로 분류해야 하고
        // 이 엔티티의 Part 중 Move 같은거에는 유저의 input 으로 작동하는
        // Part 를 넣어주면 될듯함 
        _player = GameObject.FindAnyObjectByType<PlayerController>();
        //var playerPos = CameraManager.Instance.InGameController.CurrentCameraTarget.position;
        //playerPos.y = 0;
        _player.Initialize();

        // 수동으로 일단 넥서스 위치 저장
        var nexus = EntityManager.Instance.GetNexus(EntityTeamType.Player);
        _nexusOriginalId = nexus.TableData.ID;
        _nexusOriginalPosition = nexus.transform.position.FlatHeight();
        _nexusOriginalEulerY = (int)nexus.transform.eulerAngles.y;
        _nexusOriRot = nexus.transform.rotation;

        PoolManager.Instance.RequestSpawnAsyncCallBack<FX3DMarker>(ObjectPoolCategory.Fx, "WorldObjectMarker01", onCompleted: (res, opRes) =>
        {
            _target3DMarker = res;
            res.gameObject.SetActive(false);
        }).Forget();

        // _strategy = new TeamStrategy();
        BattleStatus = new DefenseBattleStatus();

        InitBattleStatus();

        _onEntitySpawned = OnSpawned;

        InGameManager.Instance.EventListener += OnInGameEvent;
    }

    public void Release()
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;

        if (_player)
        {
            _player.Release();
            _player = null;
        }

        BattleStatus = null;
        _onEntitySpawned = null;
        _nexusOriginalId = 0;
        _nexusOriginalPosition = default;
        _nexusOriRot = Quaternion.identity;
        _nexusOriginalEulerY = 0;
        _lastTimeCoinAcquired = 0;

        if (_target3DMarker)
        {
            _target3DMarker.Return();
            _target3DMarker = null;
        }
    }

    public void OnPeaceStart()
    {
        InitBattleStatus();
        _target3DMarker.gameObject.SetActive(false);
    }

    public void OnBattleStart()
    {
        RefreshTarget3DMarker();
    }

    void InitBattleStatus()
    {
        var enemyEntities = EntityManager.Instance.GetStructures(EntityTeamType.Enemy, 0, (t) => t.StatPart.StatData.StatTableData.IsInvincible == false).ToList();
        if (enemyEntities.Count == 0)
        {
            // Enemy Rebuild 가 안된건가? 
            TEMP_Logger.Err($"No Sturcutre left anymore ?");
            return;
        }

        enemyEntities.Sort((lhs, rhs) =>
        {
            if (lhs.StructureData.StructureType == GameDB.E_StructureType.Nexus)
                return 1;
            return -1;
        });

        BattleStatus.SetOrderedTargetIndexes(enemyEntities.Select(t => t.ID).ToArray());
        //BattleStatus.SetCurrentTargetIndex(0);
    }

    public async UniTask<EntityBase> RebuildNexus()
    {
        return await EntityManager.Instance.CreateEntity(new EntityObjectData(_nexusOriginalPosition, _nexusOriginalEulerY, _nexusOriginalId, EntityTeamType.Player));
    }

    public Vector3 GetNexusPosition()
    {
        return _nexusOriginalPosition;
    }

    public void ObtainItem(uint detailTableID)
    {
        if (Player.IsAlive == false)
            return;

        var data = DBItem.Get(detailTableID);

        Player.Entity.AcquireItem(data.DetailID, 1);

        // 사운드 플레이 처리
        // 최소한 이정도 시간은 지나야 다시 플레이
        // (사운드 겹침 방지/최적화)
        if (Time.time > _lastTimeCoinAcquired + 0.2f)
        {
            if (data.ItemType == GameDB.E_ItemType.Currency)
                AudioManager.Instance.Play("SFX_CoinJingle");
            else TEMP_Logger.Err($"Not implemented Type : {data.ItemType}");
        }

        _lastTimeCoinAcquired = Time.time;
    }

    public Quaternion GetNexusRotation()
    {
        return _nexusOriRot;
    }

    void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.End)
        {
            var arg = argBase as InGameFSMStateNotify;
            if (arg.Phase == InGamePhase.Battle)
                EntityManager.Instance.RemoveCharacterEntities(EntityTeamType.Player);
        }
        else if (evt == InGameEvent.PlayerCharacterDied)
        {
            var arg = argBase as PlayerCharacterDiedEventArg;
            CameraManager.Instance.InGameController.FSM.ChangeState(CinemachineCameraType.Free, false, new object[] { arg.DiedPosition });
        }
    }

    public void SpawnEntity(Vector3 position, uint spawnEntityId, SpawnerMode spawnMode)
    {
        // TODO : 인구수 제한 둬야할듯 나중에

        EntityManager.Instance.CreateEntityCallBack(new EntityObjectData(position, 0, spawnEntityId, EntityTeamType.Player), new EntitySetupContext(spawnMode), onCompleted: _onEntitySpawned).Forget();
    }

    public void OnTargetChanged()
    {
        //if (BattleStatus.HasNextTarget == false)
        //{
        //    TEMP_Logger.Err($"No Target Exist");
        //    return;
        //}

        // BattleStatus.SetNextTarget();
        RefreshTarget3DMarker();
    }

    private void OnSpawned(EntityBase entity)
    {
        InGameManager.Instance.PublishEvent(
            InGameEvent.EntitySpawned,
            new EntitySpawnEventArg()
            {
                Entity = entity
            });
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
