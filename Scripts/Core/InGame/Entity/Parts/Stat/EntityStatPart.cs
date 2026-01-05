using System;
using UnityEngine;
using GameDB;
using Cysharp.Threading.Tasks;

public class EntityStatPart : EntityPartBase
{
    public EntityStatData StatData { get; private set; }

    EntityEventDelegates.OnHealed _onHealed;
    EntityEventDelegates.OnDamaged _onDamaged;
    EntityEventDelegates.OnLevelUp _onLevelUp;

    public override void OnPoolInitialize()
    {
        base.OnPoolInitialize();

        _onHealed = OnHealed;
        _onDamaged = OnDamaged;
        _onLevelUp = OnLevelUp;
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        Owner.HealedListener += _onHealed;
        Owner.DamagedListener += _onDamaged;
        Owner.LevelUpListener += _onLevelUp;

        StatData = Owner.GetData<EntityStatData>();
    }

    public override void OnPoolReturned()
    {
        StatData = null;

        base.OnPoolReturned();
    }

    private void OnLevelUp(uint newLevel)
    {
        StatData.SetLevel(newLevel);
    }

    private void OnHealed(ulong executorId, int amount, Vector3 effectPos)
    {
        StatData.SetCurrentHP(StatData.CurrentHP + (uint)amount);

        if (amount != 0)
            Owner.HpChangedListener?.Invoke((int)StatData.MaxHp, (int)StatData.CurrentHP, amount);
    }

    private void OnDamaged(ulong executorId, int damaged, Vector3 effectPos, float effectForce)
    {
        // 이미 사망
        if (StatData.CurrentHP == 0)
            return;

        uint newHp = (uint)Math.Max((int)StatData.CurrentHP - damaged, 0);

        StatData.SetCurrentHP(newHp);

        if (damaged != 0)
            Owner.HpChangedListener?.Invoke((int)StatData.MaxHp, (int)StatData.CurrentHP, -damaged);

        if (newHp == 0)
        {
            InGameManager.Instance.PublishEvent(InGameEvent.EntityDied,
                new EntityDiedEventArg()
                {
                    ID = Owner.ID,
                    Team = Owner.Team,
                    Type = Owner.TableData.EntityType
                });

            Owner.DiedListener?.Invoke(executorId, effectPos);
            Owner.OnDie(executorId, effectPos, effectForce).Forget();
        }
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
