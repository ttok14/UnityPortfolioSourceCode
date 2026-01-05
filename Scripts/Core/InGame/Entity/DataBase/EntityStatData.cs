using UnityEngine;
using GameDB;

public class EntityStatData : EntityDataBase
{
    public StatTable StatTableData => DBStat.Get(TableData.StatTableID);

    public uint Level { get; private set; }

    public float TableMoveSpeed { get; private set; }
    public float TableRotationSpeed { get; private set; }

    public uint MaxHp { get; private set; }
    public uint CurrentHP { get; private set; }
    public float CurrentHPNormalized => (float)CurrentHP / MaxHp;
    public float CurrentAttackPower { get; private set; }

    public float CurrentAttackSpeed { get; private set; }

    public float CurrentMoveSpeed { get; private set; }
    public float CurrentRotationSpeed { get; private set; }

    public float ScanRange { get; private set; }

    //-------//
    float _lastCurrentMoveSpeed;
    float _lastAttackSpeed;
    float _lastRotationSpeed;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = StatTableData;

        ScanRange = data.ScanRange;

        TableMoveSpeed = data.MoveSpeed;
        TableRotationSpeed = data.RotateSpeed;

        SetMaxHP(data.BaseHP, false);
        SetCurrentHP(data.BaseHP, false);
        SetCurrentAttackPower(data.BaseAttackPower, false);
        SetCurrentAttackSpeed(data.AttackSpeed, false);

        SetCurrentMoveSpeed(data.MoveSpeed, false);
        SetCurrentRotationSpeed(data.RotateSpeed, false);

        SetLevel(1);
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        Level = 0;

        TableMoveSpeed = 0;
        TableRotationSpeed = 0;
        MaxHp = 0;
        CurrentHP = 0;
        CurrentAttackPower = 0;
        CurrentAttackSpeed = 0;
        CurrentMoveSpeed = 0;
        CurrentRotationSpeed = 0;
        ScanRange = 0;

        _lastCurrentMoveSpeed = 0f;
        _lastAttackSpeed = 0f;
        _lastRotationSpeed = 0f;
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityDataPool.Return(this);
    }

    public void SetLevel(uint level)
    {
        Level = level;
        UpdateStat();
        CurrentHP = MaxHp;

        _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
    }

    public void UpdateStat()
    {
        DBStat.GetFinalStatAtLevel(
            TableData.StatTableID,
            Level,
            out uint attackPower,
            out float attackSpeed,
            out uint maxHp
            );
        var statTable = DBStat.Get(TableData.StatTableID);

        SetCurrentAttackPower(attackPower, notifyEvent: false);
        SetCurrentAttackSpeed(attackSpeed, notifyEvent: false);

        SetMaxHP(maxHp, notifyEvent: false);

        SetCurrentMoveSpeed(statTable.MoveSpeed, notifyEvent: false);

        // !! 마지막에 항상 notify event 할것 !! 
        SetCurrentRotationSpeed(statTable.RotateSpeed, notifyEvent: true);
    }

    public void SetCurrentAttackSpeed(float attackSpeed, bool notifyEvent = true)
    {
        CurrentAttackSpeed = attackSpeed;

        if (notifyEvent && _lastAttackSpeed != attackSpeed)
        {
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
        }
        _lastAttackSpeed = attackSpeed;
    }

    public void SetCurrentMoveSpeed(float moveSpeed, bool notifyEvent = true)
    {
        CurrentMoveSpeed = moveSpeed;

        if (notifyEvent && _lastCurrentMoveSpeed != moveSpeed)
        {
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
        }

        _lastCurrentMoveSpeed = moveSpeed;
    }

    public void SetCurrentRotationSpeed(float rotationSpeed, bool notifyEvent = true)
    {
        CurrentRotationSpeed = rotationSpeed;

        if (notifyEvent && _lastRotationSpeed != rotationSpeed)
        {
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
        }

        _lastRotationSpeed = rotationSpeed;
    }

    public void SetCurrentAttackPower(uint attackPower, bool notifyEvent = true)
    {
        CurrentAttackPower = attackPower;

        if (notifyEvent)
        {
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
        }
    }

    public void SetCurrentHP(uint hp, bool notifyEvent = true)
    {
        CurrentHP = (uint)Mathf.Min(hp, MaxHp);

        if (notifyEvent)
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
    }

    public void SetMaxHP(uint hp, bool notifyEvent = true)
    {
        MaxHp = hp;

        if (notifyEvent)
            _owner.DataModifiedListener?.Invoke(EntityDataCategory.Stat, this);
    }
}
