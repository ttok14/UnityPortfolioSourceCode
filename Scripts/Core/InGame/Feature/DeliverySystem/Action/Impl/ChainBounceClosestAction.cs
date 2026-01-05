using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainBounceClosestAction : ChainBounceActionBase
{
    class CandidateComparer : IComparer<Candidate>
    {
        public int Compare(Candidate x, Candidate y)
        {
            return x.sqrDist.CompareTo(y.sqrDist);
        }
    }

    static readonly CandidateComparer _candidateComparer = new CandidateComparer();

    protected override int RefreshNextTargets(int bounceCount, DeliveryContext context)
    {
        var position = context.Position;
        var cacheContainer = InGameManager.Instance.CacheContainer;
        var cols = cacheContainer.GetColliderCacheByCount(30);

        var count = Physics.OverlapSphereNonAlloc(position, Radius, cols, context.TargetLayerMask);
        if (count == 0)
            return 0;

        // 코드가 더러워지지만, 한마리때는 애초에 소팅을 안해도 되는
        // 성능상 큰 이득이 있기 때문에 분기 처리.
        if (bounceCount == 1)
        {
            float sqrMinDist = float.MaxValue;
            EntityBase target = null;

            for (int i = 0; i < count; i++)
            {
                var col = cols[i];

                var sqrDist = Vector3.SqrMagnitude(position - col.transform.position);
                if (sqrDist >= sqrMinDist)
                    continue;

                var entity = cacheContainer.GetEntityFromCollider(col);

                if (context.DeliveryHistory.VisitedIDs.Contains(entity.ID))
                    continue;

                sqrMinDist = sqrDist;
                target = entity;
            }

            if (target)
            {
                // 이 방식이 성능상 이득이 거의 없다면 그냥 Clear() 해버리는게 나을수도 ..
                Candidates.Clear();
                Candidates.Add(new Candidate()
                {
                    entity = target,
                    sqrDist = sqrMinDist
                });

                return 1;
            }

            return 0;
        }
        else
        {
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
                    sqrDist = Vector3.SqrMagnitude(position - col.transform.position)
                });
            }

            if (Candidates.Count > 1)
                Candidates.Sort(_candidateComparer);

            return Mathf.Min(Candidates.Count, bounceCount);
        }
    }
}
