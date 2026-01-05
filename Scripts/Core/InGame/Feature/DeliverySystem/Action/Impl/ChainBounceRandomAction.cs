using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainBounceRandomAction : ChainBounceActionBase
{
    protected override int RefreshNextTargets(int bounceCount, DeliveryContext context)
    {
        var position = context.Position;
        var cacheContainer = InGameManager.Instance.CacheContainer;
        var cols = cacheContainer.GetColliderCacheByCount(bounceCount * 15);

        var count = Physics.OverlapSphereNonAlloc(position, Radius, cols, context.TargetLayerMask);
        if (count == 0)
            return 0;

        Candidates.Clear();

        for (int i = 0; i < count; i++)
        {
            var col = cols[i];
            var entity = cacheContainer.GetEntityFromCollider(col);

            if (entity == null || context.DeliveryHistory.VisitedIDs.Contains(entity.ID))
                continue;

            Candidates.Add(new Candidate()
            {
                entity = entity,
            });

            if (Candidates.Count >= bounceCount)
                break;
        }

        return Mathf.Min(Candidates.Count, bounceCount);
    }
}
