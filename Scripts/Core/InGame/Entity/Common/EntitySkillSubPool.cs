
using System;

public class EntitySkillSubInitData : IInstancePoolInitData
{
    public uint TableID;
    public int Index;
}

public class EntitySpellSubInitData : EntitySkillSubInitData
{
    public Func<EntityBase> TargetGetter;
}

public class EntitySkillSubPool
{
    InstancePool<EntitySkillStandard> _skillPool = new InstancePool<EntitySkillStandard>(() => new EntitySkillStandard());
    InstancePool<EntitySpellStandard> _spellPool = new InstancePool<EntitySpellStandard>(() => new EntitySpellStandard());

    EntitySkillSubInitData _skillInitData = new EntitySkillSubInitData();
    EntitySpellSubInitData _spellInitData = new EntitySpellSubInitData();

    public EntitySkillStandard GetOrCreateSkill(uint tableId, int idx)
    {
        _skillInitData.TableID = tableId;
        _skillInitData.Index = idx;
        return _skillPool.GetOrCreate(_skillInitData);
    }

    public EntitySpellStandard GetOrCreateSkillSpell(uint tableId, int idx, Func<EntityBase> targetGetter)
    {
        _spellInitData.TableID = tableId;
        _spellInitData.Index = idx;
        _spellInitData.TargetGetter = targetGetter;
        return _spellPool.GetOrCreate(_spellInitData);
    }

    public void ReturnSkill<T>(T element) where T : EntitySkillStandard
    {
        _skillPool.Return(element);
    }

    public void ReturnSpell<T>(T element) where T : EntitySpellStandard
    {
        _spellPool.Return(element);
    }
}
