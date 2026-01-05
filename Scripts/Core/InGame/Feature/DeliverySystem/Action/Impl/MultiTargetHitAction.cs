using UnityEngine;

public class MultiTargetHitAction : DeliveryActionBase
{
    float _radius;
    int _targetCount;

    public override void Initialize(ActionData data)
    {
        base.Initialize(data);

        _radius = data.Value01;
        _targetCount = data.Value02.GetApproximateInt();
    }

    public override void Execute(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        var cacheContainer = InGameManager.Instance.CacheContainer;
        var cols = cacheContainer.GetColliderCacheByCount((int)context.PreferMaxTargetCount);
        int count = Physics.OverlapSphereNonAlloc(source.Position, _radius, cols, context.TargetLayerMask);

        if (count == 0)
            return;

        int processedCount = 0;
        var executorID = context.ExecutorID;

        for (int i = 0; i < count; i++)
        {
            if (processedCount >= _targetCount)
                break;

            var entity = cacheContainer.GetEntityFromCollider(cols[i]);

            if (EntityHelper.IsValid(entity) == false)
                continue;

            PlayFX(source, entity, context, false, true);

            entity.ApplyAffect(executorID, (int)context.Damage, 0, source.Position, context.PhysicalForce);
            processedCount++;
        }

        if (processedCount > 0)
            PlaySFX(source);
    }
}
