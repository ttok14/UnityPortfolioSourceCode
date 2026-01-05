using UnityEngine;

public class AreaDamageAction : DeliveryActionBase
{
    float _radius;
    float _damageRatio;

    public override void Initialize(ActionData data)
    {
        base.Initialize(data);

        _radius = data.Value01;
        _damageRatio = data.Value02;
    }

    public override void Execute(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        Play_SFX_FX(source, target, context);

        var cacheContainer = InGameManager.Instance.CacheContainer;
        var cols = cacheContainer.GetColliderCacheByCount((int)context.PreferMaxTargetCount);
        int count = Physics.OverlapSphereNonAlloc(source.Position, _radius, cols, context.TargetLayerMask);

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            var entity = cacheContainer.GetEntityFromCollider(cols[i]);

            if (EntityHelper.IsValid(entity) == false)
                continue;

            entity.ApplyAffect(context.ExecutorID, (int)(context.Damage * _damageRatio), 0, source.Position, context.PhysicalForce);
        }
    }
}
