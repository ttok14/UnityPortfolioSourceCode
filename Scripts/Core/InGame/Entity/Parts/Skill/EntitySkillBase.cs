using UnityEngine;
using GameDB;

public abstract class EntitySkillBase : IInstancePoolElement
{
    public uint TableID;
    public SkillTable TableData { get; private set; }
    public float LastCastAt { get; private set; }
    public float CooltimeLeft => LastCastAt + TableData.CooldownTime - Time.time;
    public float CooltimeProgress => 1f - (CooltimeLeft / TableData.CooldownTime);

    public int SkillIdx { get; private set; }

    public virtual bool IsAvailable => CooltimeLeft <= 0;

    public void StartCasting() => LastCastAt = Time.time;
    public abstract void Trigger(EntitySkillTriggerContext context);

    public uint PoolableInstanceValidID;

    public virtual void OnPoolInitialize() { }

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as EntitySkillSubInitData;

        TableID = data.TableID;
        TableData = DBSkill.Get(data.TableID);
        SkillIdx = data.Index;
        LastCastAt = 0;

        PoolableInstanceValidID++;
    }

    public virtual void OnPoolReturned()
    {
        TableID = 0;
        TableData = null;
        LastCastAt = 0;

        // Return 됐을때도 외부에서 변별가능하게 ID 변경
        PoolableInstanceValidID++;
    }

    public abstract void ReturnToPool();
}
