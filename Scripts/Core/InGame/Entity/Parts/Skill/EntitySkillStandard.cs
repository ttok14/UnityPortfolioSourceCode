using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySkillStandard : EntitySkillBase, IDeliverySource
{
    ImpactStrategyBase _impactStrategy;

    #region ===:: Interface Impl ::===
    public Vector3 StartPosition { get; private set; }
    public Vector3 Position { get; private set; }
    #endregion

    List<UIFloatingIcon.Arg> _skillIconArgs;

    public override void Trigger(EntitySkillTriggerContext context)
    {
        Vector3 skillPosition = context.Executor.ModelPart.GetEquimentTransform(context.SkillEquipment).position;
        EntityTeamType targetTeam = EntityTeamType.None;

        // 기본 스킬 쓸때마다 아이콘 뜨면 투머치니까 [0] 은 막자 
        if (context.SlotIdx != 0)
        {
            if (string.IsNullOrEmpty(TableData.IconKey) == false && context.Executor)
            {
                var arg = _skillIconArgs[context.SlotIdx];
                arg.spriteKey = TableData.IconKey;
                var head = context.Executor.ModelPart.GetSocket(EntityModelSocket.Head);
                if (head)
                {
                    arg.followTarget = head;
                }
                else
                {
                    TEMP_Logger.Err($"Head is Null (this is required) | ExecutorName : {context.Executor.name} | ID : {context.Executor.ID} | TID : {context.Executor.EntityTID}");
                    arg.followTarget = context.Executor.transform;
                }

                arg.uiOffsetPos = Constants.InGame.SkillExecutionIconOffset;

                UIManager.Instance.Show<UIFloatingIcon>(UITrigger.Default, arg);
            }
        }

        if (TableData.SkillType == GameDB.E_SkillType.Attack)
            targetTeam = context.Executor.Team == EntityTeamType.Player ? EntityTeamType.Enemy : EntityTeamType.Player;
        else TEMP_Logger.Err($"Not implmented SkillType : {TableData.SkillType}");

        if (string.IsNullOrEmpty(TableData.ProjectileKey) == false)
        {
            // Sfx 는 지금 SkillPart 에서 플레이중
            // string sfxKey = string.Empty;

            //if (TableData.TriggerAudioKey != null && TableData.TriggerAudioKey.Length > 0)
            //    sfxKey = TableData.TriggerAudioKey[UnityEngine.Random.Range(0, TableData.TriggerAudioKey.Length)];

            ProjectileSystem.Fire(
                TableData.ProjectileKey,
                context.Executor,
                context.Target,
                skillPosition,
                null,
                context.Executor.transform.rotation,
                context.Executor.Team,
                targetTeam,
                TableData.BaseDamage,
                0,
                string.Empty,
                TableData.EffectKeys).Forget();
        }
        else
        {
            if (TableData.EffectKeys != null)
            {
                var weaponSocket = context.Executor.ModelPart.GetSocket(EntityModelSocket.MeleeWeaponEffect);
                if (weaponSocket)
                {
                    var pos = weaponSocket.position;
                    var rot = context.Executor.transform.rotation;
                    for (int i = 0; i < TableData.EffectKeys.Length; i++)
                    {
                        FXSystem.PlayFX(TableData.EffectKeys[i], startPosition: pos, rotation: rot);
                    }
                }
                else
                {
                    TEMP_Logger.Err($"MeleeWeaponEffect is NULL ! | Name : {context.Executor.name} , Skill : {TableData.Name} , SkillID : {TableData.ID}");
                }
            }

            var targetTeamType = context.Target ? context.Target.Team : EntityTeamType.None;
            var deliveryCxt = DeliveryActionFactory.GetDeliveryContext(
                null,
                context.Executor ? context.Executor.ID : 0,
                context.Executor ? context.Executor.Team : EntityTeamType.None,
                targetTeamType,
                LayerUtils.GetLayerMask((targetTeamType, E_EntityType.Structure), (targetTeamType, E_EntityType.Character)),
                TableData.ImpactCollisionRangeType,
                TableData.ImpactCollisionRange,
                false,
                TableData.PreferMaxTargetCount,
                TableData.BaseDamage,
                0,
                true,
                TableData.ImpactSFXHitKeys,
                TableData.ImpactFXHitKeys,
                TableData.ImpactCollisionForce,
                E_DeliveryContextInheritType.None);

            deliveryCxt.SetPosition(context.Executor ? context.Executor.ApproxPosition.FlatHeight() : Vector3.zero);

            StartPosition = skillPosition;
            Position = skillPosition;
            {
                ExecuteDeliveryImpact(context.Target, deliveryCxt);
            }
            deliveryCxt.DecreaseReferenceCount();
            StartPosition = Vector3.zero;
            Position = Vector3.zero;

            //// TODO : 근접 공격은 force 어떻게 할까?
            //context.Target.ApplyAffect((int)TableData.BaseDamage, 0, context.Executor.transform.position, 0);
        }
    }

    void ExecuteDeliveryImpact(EntityBase target, DeliveryContext cxt)
    {
        _impactStrategy.Execute(this, target, cxt);
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);
        _impactStrategy = ImpactSystemFactory.CreateStrategy(ImpactType.Standard);

        if (_skillIconArgs == null)
        {
            _skillIconArgs = new List<UIFloatingIcon.Arg>()
            {
                new UIFloatingIcon.Arg(null,null, Vector2.zero),
                new UIFloatingIcon.Arg(null,null, Vector2.zero),
                new UIFloatingIcon.Arg(null,null, Vector2.zero)
            };
        }
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        _impactStrategy = null;
        StartPosition = Vector3.zero;
        Position = Vector3.zero;

        for (int i = 0; i < _skillIconArgs.Count; i++)
        {
            var arg = _skillIconArgs[i];
            arg.followTarget = null;
            arg.spriteKey = null;
            arg.uiOffsetPos = Vector2.zero;
        }
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntitySkillSubPool.ReturnSkill(this);
    }

    public void ForceEnd()
    {
        throw new NotImplementedException();
    }

    public void OnDeliveryTrigger(E_UpdateLogicType updateLogic)
    {
        throw new NotImplementedException();
    }
}
