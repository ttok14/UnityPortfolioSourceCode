using System;
using UnityEngine;
using GameDB;
using System.Collections.Generic;

public class EntitySpellPartInitData : EntityPartInitDataBase
{
    public List<EntitySkillBase> SpellSet;

    public EntitySpellPartInitData(EntityBase owner) : base(owner) { }
}

public class EntitySpellPart : EntityPartBase
{
    protected List<EntitySkillBase> _spellSet;
    public int SpellCount => _spellSet.Count;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as EntitySpellPartInitData;

        _spellSet = data.SpellSet;
    }

    public override void OnPoolReturned()
    {
        if (_spellSet != null)
        {
            for (int i = 0; i < _spellSet.Count; i++)
            {
                _spellSet[i].ReturnToPool();
            }

            _spellSet = null;
        }

        base.OnPoolReturned();
    }

    public EntitySkillBase GetSpell(int idx)
    {
        if (_spellSet == null || idx >= _spellSet.Count)
        {
            TEMP_Logger.Err($"Spell Idx OutofRange : {idx} | Entity ID : {Owner.EntityTID}");
            return null;
        }

        return _spellSet[idx];
    }

    public bool RequestUse(EntitySkillTriggerContext context)
    {
        if (_spellSet == null || context.SlotIdx >= _spellSet.Count)
        {
            TEMP_Logger.Err($"Invalid slot Index | isSpellNull ? : {_spellSet == null} , Spell Count : {_spellSet?.Count ?? 0}");
            return false;
        }

        var spell = _spellSet[context.SlotIdx];

        if (spell.IsAvailable == false)
            return false;

        spell.StartCasting();

        spell.Trigger(context);

        PlayAudio(spell.TableData);

        return true;
    }

    void PlayAudio(SkillTable tableData)
    {
        if (tableData == null)
            return;

        var audioKeys = tableData.TriggerAudioKey;
        if (audioKeys == null || audioKeys.Length == 0)
            return;

        if (tableData.AudioRandomPick)
        {
            AudioManager.Instance.Play(
                tableData.TriggerAudioKey[UnityEngine.Random.Range(0, audioKeys.Length)],
                Owner.ApproxPosition,
                AudioTrigger.Default);
        }
        else
        {
            foreach (var key in tableData.TriggerAudioKey)
            {
                AudioManager.Instance.Play(
                    key,
                    Owner.ApproxPosition,
                    AudioTrigger.Default);
            }
        }
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
