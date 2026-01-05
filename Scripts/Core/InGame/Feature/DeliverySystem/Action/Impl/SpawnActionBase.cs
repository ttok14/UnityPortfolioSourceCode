using GameDB;
using System;
using UnityEngine;

public abstract class SpawnActionBase : DeliveryActionBase
{
    protected abstract bool ParseValues(
    IDeliverySource source,
    EntityBase currentTarget,
    DeliveryContext context,
    out Vector3 startPosition,
    out EntityBase newTarget,
    out Vector3 newTargetPos);

    public override void Initialize(ActionData data)
    {
        base.Initialize(data);
    }

    public override void Execute(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        Play_SFX_FX(source, target, context);

        bool isValid = ParseValues(
            source,
            target,
            context,
            out Vector3 startPosition,
            out EntityBase newTarget,
            out Vector3 newTargetPos);

        if (isValid == false)
        {
            return;
        }

        switch (Data.RefType)
        {
            case E_RefDataType.Projectile:
                SpawnProjectile(startPosition, newTarget, newTargetPos, source, context);
                break;
            case E_RefDataType.Zone:
                TEMP_Logger.Err($"Not implemented Zone");
                break;
            // 하이머딩거? 
            case E_RefDataType.Entity:
                TEMP_Logger.Err($"Not implemented Entity");
                break;
            default:
                TEMP_Logger.Err($"Not implemented Type : {Data.RefType}");
                break;
        }
    }

    private void SpawnProjectile(
        Vector3 newStartPosition,
        EntityBase newTarget,
        Vector3 newTargetPos,
        IDeliverySource source,
        DeliveryContext context)
    {
        EntityBase executor = context.ExecutorID != 0 ? EntityManager.Instance.GetEntity(context.ExecutorID) : null;
        var rotation = executor ? executor.transform.rotation :
            Quaternion.LookRotation((newTargetPos - newStartPosition).normalized);

        ProjectileSystem.Fire(
            Data.RefKey,
            executor,
            newTarget,
            newStartPosition,
            newTargetPos,
            rotation,
            context.ExecutorTeam,
            context.TargetTeam,
            context.Damage,
            context.Heal,
            sfxKey: null,
            fxKeys: null,
            inheritContext: context).Forget();
    }
}
