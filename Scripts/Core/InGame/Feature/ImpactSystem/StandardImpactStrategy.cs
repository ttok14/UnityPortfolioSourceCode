using UnityEngine;
using GameDB;
using System;

public class StandardImpactStrategy : ImpactStrategyBase
{
    public override void Execute(
        IDeliverySource deliverSource,
        EntityBase target,
        DeliveryContext context)
    {
        if (context.CollisionType == E_CollisionRangeType.Single)
        {
            bool isTargetValid = EntityHelper.IsValid(target);

            if (isTargetValid)
            {
                bool hitBefore = context.DeliveryHistory.ImpactedIDs.Contains(target.ModelPart.UniqueID);

                if (hitBefore)
                {
                    if (context.AllowMultiHit == false)
                        return;
                }
                else
                {
                    context.DeliveryHistory.ImpactedIDs.Add(target.ModelPart.UniqueID);
                }

                if (context.DeliveryHistory.VisitedIDs.Contains(target.ModelPart.UniqueID) == false)
                    context.DeliveryHistory.VisitedIDs.Add(target.ModelPart.UniqueID);

                // ApplyEffect 에서 죽으면 내부 구성요소들이 Null 이 될수있으므로
                // 내부 구성요소에 의존성이 있는건 이전에 처리
                var center = target.ModelPart.GetSocket(EntityModelSocket.Center);
                if (center)
                    FXSystem.PlayCommonHitFXByEntity(center.position, target.Type);
                else TEMP_Logger.Err($"No Center Socket please add : {target.name}");

                if (context.FXPerTargetOrDeliverySelf && context.FXKeys != null)
                {
                    foreach (var key in context.FXKeys)
                    {
                        if (string.IsNullOrEmpty(key) == false)
                        {
                            FXSystem.PlayFX(key, startPosition: deliverSource.StartPosition, endPosition: target.ApproxPosition);
                        }
                    }
                }

                target.ApplyAffect(context);
            }
        }
        else if (context.CollisionType == E_CollisionRangeType.RangeArea)
        {
            var cols = InGameManager.Instance.CacheContainer.GetColliderCacheByCount((int)context.PreferMaxTargetCount);
            int collisionCount = Physics.OverlapSphereNonAlloc(context.Position, context.CollisionRange, cols, context.TargetLayerMask);
            for (int i = 0; i < collisionCount; i++)
            {
                var entity = InGameManager.Instance.CacheContainer.GetEntityFromCollider(cols[i]);

                // 여기서 충돌이 된거는 애초에 valid 하니까 충돌이 됐을거임.
                // 근데 이게 깨지면 다시 추가해야할듯.
                //if (EntityHelper.IsValid(entity) == false)
                //    continue;

                bool hitBefore = context.DeliveryHistory.ImpactedIDs.Contains(entity.ModelPart.UniqueID);

                if (hitBefore)
                {
                    if (context.AllowMultiHit == false)
                        continue;
                }
                else
                {
                    context.DeliveryHistory.ImpactedIDs.Add(entity.ModelPart.UniqueID);
                }

                if (context.DeliveryHistory.VisitedIDs.Contains(entity.ModelPart.UniqueID) == false)
                    context.DeliveryHistory.VisitedIDs.Add(entity.ModelPart.UniqueID);

                var center = entity.ModelPart.GetSocket(EntityModelSocket.Center);
                if (center)
                    FXSystem.PlayCommonHitFXByEntity(center.position, entity.Type);
                else TEMP_Logger.Err($"No Center Socket please add : {entity.name}");

                var entityPos = entity.ApproxPosition;

                if (context.FXPerTargetOrDeliverySelf && context.FXKeys != null)
                {
                    foreach (var key in context.FXKeys)
                    {
                        if (string.IsNullOrEmpty(key) == false)
                        {
                            FXSystem.PlayFX(key, startPosition: deliverSource.StartPosition, endPosition: entityPos);
                        }
                    }
                }

                float force = CalculateCollisionForce(context.Position, entityPos, context.CollisionRange, context.PhysicalForce);
                entity.ApplyAffect(context.ExecutorID, (int)context.Damage, (int)context.Heal, context.Position, force);
            }

            if (context.CollisionRange >= Constants.InGame.HugeImapctThreshold)
            {
                InGameManager.Instance.PublishEvent(
                    InGameEvent.HugeImpact,
                    new EntityHugeImpactEventArg()
                    {
                        Position = context.Position,
                        Force = context.PhysicalForce
                    });
            }
        }

        if (context.SFXKeys != null)
            PlayHitAudio(context.Position, context.SFXKeys);

        if (context.FXPerTargetOrDeliverySelf == false && context.FXKeys != null)
        {
            foreach (var key in context.FXKeys)
            {
                if (string.IsNullOrEmpty(key) == false)
                {
                    FXSystem.PlayFX(key, startPosition: deliverSource.StartPosition, endPosition: context.Position);
                }
            }
        }
    }

    private void PlayHitAudio(Vector3 position, string[] keys)
    {
        if (keys == null || keys.Length == 0)
            return;

        AudioManager.Instance.Play(
            keys[UnityEngine.Random.Range(0, keys.Length)],
            position,
            AudioTrigger.Default);
    }

    float CalculateCollisionForce(Vector3 myPosition, Vector3 targetPos, float? applyRangeRadius, float force)
    {
        if (force == 0)
            return 0;

        // 만약 범위 충돌이 아니라면 (타겟은 하나)
        // Force 그대로 적용
        if (applyRangeRadius.HasValue == false)
            return force;

        // 범위 충돌이라면 , 그 범위에 가까운 유닛에
        // 더 강한 Force 를 적용시켜야 함, 그래야 시각적으로 자연스러움.
        myPosition.y = targetPos.y;
        float sqrDist = Vector3.SqrMagnitude(targetPos - myPosition);
        float sqrEffectRange = applyRangeRadius.Value * applyRangeRadius.Value;

        // 이거는 일단 영향권안에 들어왔으면 기본으로 적용시켜주어야 할 적당선
        // 수치 적용 ㄱㄱ 이건 그냥 '적당히'.. 왜냐면 이게없으면 범위에 걸쳐서 영향받으면
        // force 가 0 인데 이건 좀 어색해보임
        float baseForce = force * 0.5f;
        return baseForce + Mathf.Lerp(0f, force, 1f - (sqrDist / sqrEffectRange));
    }
}
