using System.Collections.Generic;
using UnityEngine;

public class EntitySpawnerPartInitData : EntityPartInitDataBase
{
    public uint EntityID;
    public float Interval;
    public EntityTeamType TeamType;
    public Vector3 SpawnPosition;

    public EntitySpawnerPartInitData(EntityBase owner) : base(owner) { }
}

public enum SpawnerMode
{
    None = 0,

    Defensive,
    Aggresive
}

public class EntitySpawnerPart : EntityPartBase
{
    public uint SpawnEntityId { get; private set; }
    public float Interval { get; private set; }
    public float Progress => Mathf.Min(_elapsedTime / Interval, 1f);

    float _elapsedTime;

    Vector3 _spawnPosition;

    EntityTeamType _team;
    public SpawnerMode SpawnMode { get; set; }

    bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _elapsedTime = 0;
        }
    }

    public void Update()
    {
        if (IsEnabled == false)
            return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= Interval)
        {
            _elapsedTime -= Interval;

            RequestSpawn();
        }
    }

    void RequestSpawn()
    {
        switch (_team)
        {
            case EntityTeamType.Player:
                {
                    InGameManager.Instance.PlayerCommander.SpawnEntity(_spawnPosition, SpawnEntityId, SpawnMode);
                }
                break;
            case EntityTeamType.Enemy:
                {
                    InGameManager.Instance.EnemyCommander.SpawnEntity(_spawnPosition, SpawnEntityId);
                }
                break;
            default:
                TEMP_Logger.Err($"Not implemnted Team : {_team}");
                break;
        }
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as EntitySpawnerPartInitData;
        SpawnEntityId = data.EntityID;
        Interval = data.Interval;
        _spawnPosition = data.SpawnPosition;
        _team = data.TeamType;
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        IsEnabled = false;
        SpawnEntityId = 0;
        Interval = 0;
        _spawnPosition = default;
        _team = EntityTeamType.None;
        SpawnMode = SpawnerMode.None;
        _elapsedTime = 0;
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
