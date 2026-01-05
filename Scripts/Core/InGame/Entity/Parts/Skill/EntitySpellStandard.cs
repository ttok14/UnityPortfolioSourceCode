using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpellStandard : EntitySkillBase // , IDeliverySource
{
    // ImpactStrategyBase _impactStrategy;

    //#region ===:: Interface Impl ::===
    //public Vector3 StartPosition { get; private set; }
    //public Vector3 Position { get; private set; }
    //#endregion

    List<UIFloatingIcon.Arg> _skillIconArgs;

    public Func<EntityBase> TargetGetter { get; private set; }

    public override bool IsAvailable => base.IsAvailable && (IsTargetRequired == false || TargetGetter.Invoke());

    bool IsTargetRequired =>
        TableData != null &&
        (TableData.SpellStartPositionType == E_SpellPositionType.CurrentTarget ||
        TableData.SpellEndPositionType == E_SpellPositionType.CurrentTarget);

    public override void Trigger(EntitySkillTriggerContext context)
    {
        EntityTeamType targetTeam = EntityTeamType.None;

        if (string.IsNullOrEmpty(TableData.IconKey) == false && context.Executor)
        {
            var arg = _skillIconArgs[context.SlotIdx];
            arg.spriteKey = TableData.IconKey;
            arg.followTarget = context.Executor.ModelPart.GetSocket(EntityModelSocket.Head).transform;
            arg.uiOffsetPos = Constants.InGame.SkillExecutionIconOffset;

            UIManager.Instance.Show<UIFloatingIcon>(UITrigger.Default, arg);
        }

        switch (TableData.SkillType)
        {
            case E_SkillType.Attack:
                targetTeam = context.Executor.Team == EntityTeamType.Player ? EntityTeamType.Enemy : EntityTeamType.Player;
                break;
            default:
                TEMP_Logger.Err($"Not implmented SkillType : {TableData.SkillType}");
                break;
        }

        if (string.IsNullOrEmpty(TableData.ProjectileKey) == false)
        {
            var executorTs = context.Executor.SubMovePart.Mover.transform;

            var startPosition = CalculatePosition(
                TableData.SpellStartPositionType,
                context.Executor,
                executorTs,
                context.Target,
                TableData.SpellStartOffset,
                TableData.SpellStartOffsetRelative);

            var fixedPosition = CalculatePosition(
                TableData.SpellEndPositionType,
                context.Executor,
                executorTs,
                context.Target,
                TableData.SpellEndOffset,
                TableData.SpellEndOffsetRelative);

            ProjectileSystem.Fire(
                TableData.ProjectileKey,
                context.Executor,
                context.Target,
                startPosition,
                fixedPosition,
                Quaternion.LookRotation((fixedPosition - startPosition).normalized),
                context.Executor.Team,
                targetTeam,
                TableData.BaseDamage,
                0,
                string.Empty,
                TableData.EffectKeys).Forget();
        }
        // 일단 투사체가 아닌 경우 패스
        //else
        //{
        //    if (TableData.EffectKeys != null)
        //    {
        //        var weaponSocket = context.Executor.ModelPart.GetSocket(EntityModelSocket.MeleeWeaponEffect);
        //        if (weaponSocket)
        //        {
        //            var pos = weaponSocket.position;
        //            var rot = context.Executor.transform.rotation;
        //            for (int i = 0; i < TableData.EffectKeys.Length; i++)
        //            {
        //                FXSystem.PlayFX(TableData.EffectKeys[i], startPosition: pos, rotation: rot).Forget();
        //            }
        //        }
        //        else
        //        {
        //            TEMP_Logger.Err($"MeleeWeaponEffect is NULL ! | Name : {context.Executor.name} , Skill : {TableData.Name} , SkillID : {TableData.ID}");
        //        }
        //    }

        //    var targetTeamType = context.Target ? context.Target.Team : EntityTeamType.None;
        //    var deliveryCxt = DeliveryActionFactory.GetDeliveryContext(
        //        null,
        //        context.Executor ? context.Executor.ID : 0,
        //        context.Executor ? context.Executor.Team : EntityTeamType.None,
        //        targetTeamType,
        //        LayerUtils.GetLayerMask((targetTeamType, E_EntityType.Structure), (targetTeamType, E_EntityType.Character)),
        //        TableData.ImpactCollisionRangeType,
        //        TableData.ImpactCollisionRange,
        //        false,
        //        TableData.PreferMaxTargetCount,
        //        TableData.BaseDamage,
        //        0,
        //        true,
        //        TableData.ImpactSFXHitKeys,
        //        TableData.ImpactFXHitKeys,
        //        TableData.ImpactCollisionForce,
        //        E_DeliveryContextInheritType.None);

        //    deliveryCxt.SetPosition(context.Executor ? context.Executor.transform.position.FlatHeight() : Vector3.zero);

        //    StartPosition = spellPosition;
        //    Position = spellPosition;
        //    {
        //        ExecuteDeliveryImpact(context.Target, deliveryCxt);
        //    }
        //    deliveryCxt.DecreaseReferenceCount();
        //    StartPosition = Vector3.zero;
        //    Position = Vector3.zero;

        //    //// TODO : 근접 공격은 force 어떻게 할까?
        //    //context.Target.ApplyAffect((int)TableData.BaseDamage, 0, context.Executor.transform.position, 0);
        //}
    }

    //void ExecuteDeliveryImpact(EntityBase target, DeliveryContext cxt)
    //{
    //    _impactStrategy.Execute(this, target, cxt);
    //}

    Vector3 CalculatePosition(
        E_SpellPositionType type,
        EntityBase executor,
        Transform executorTs,
        EntityBase target,
        in Vector3 offset,
        bool isRelative)
    {
        switch (type)
        {
            case E_SpellPositionType.Self:
                {
                    if (isRelative)
                        return executorTs.position + (executorTs.rotation * offset);
                    else return executorTs.position + offset;
                }
            case E_SpellPositionType.CurrentTarget:
                {
                    var targetTs = target.transform;

                    if (isRelative)
                        return target.ApproxPosition + (targetTs.rotation * offset);
                    else return target.ApproxPosition + offset;
                }
            case E_SpellPositionType.Nexus:
                {
                    Vector3 nexusPosition = default;
                    Quaternion nexusRot = default;

                    switch (executor.Team)
                    {
                        case EntityTeamType.Player:
                            nexusPosition = InGameManager.Instance.PlayerCommander.GetNexusPosition();
                            nexusRot = InGameManager.Instance.PlayerCommander.GetNexusRotation();
                            break;
                        case EntityTeamType.Enemy:
                            nexusPosition = InGameManager.Instance.EnemyCommander.GetNexusPosition();
                            nexusRot = InGameManager.Instance.EnemyCommander.GetNexusRotation();
                            break;
                    }

                    if (isRelative)
                        return nexusPosition + (nexusRot * offset);
                    else return nexusPosition + offset;
                }
            default:
                TEMP_Logger.Err($"Not imlemented Type : {type}");
                break;
        }

        return executorTs.position;
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as EntitySpellSubInitData;

        TargetGetter = data.TargetGetter;

        // skill 처럼 즉발가능하게 만들려면 추후 작업 . 
        //_impactStrategy = ImpactSystemFactory.CreateStrategy(ImpactType.Standard);

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

        //_impactStrategy = null;
        //StartPosition = Vector3.zero;
        //Position = Vector3.zero;

        TargetGetter = null;

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
        InGameManager.Instance.CacheContainer.EntitySkillSubPool.ReturnSpell(this);
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
