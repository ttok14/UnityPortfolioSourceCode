using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public class StandardAggroSystem : AggroSystemBase
{
    struct History
    {
        public readonly EntityBase LastFoundTarget;
        public readonly float NextScanTimeAt;

        public History(EntityBase lastFoundTarget, float nextScanTimeAt)
        {
            LastFoundTarget = lastFoundTarget;
            NextScanTimeAt = nextScanTimeAt;
        }
    }

    // const float DefaultRecaulcateWaitDuration = 0.7f;
    // const float WithValidTargetRecaulcateWaitDuration = 1.4f;

    float _recalculateWaitDuration { get; set; } = 0.7f;
    float _withValidTargetRecalculateWaitDuration { get; set; } = 1.4f;

    Dictionary<EntityTeamType, int> _targetLayerMasksByTeam = new Dictionary<EntityTeamType, int>();
    int _playerTargetLayerMask;
    int _enemyTargetLayerMask;

    int _preferTargetCount;

    Dictionary<ulong, History> _askerRequestHistory = new Dictionary<ulong, History>(64);

    // History Cache 가 있지만
    // Cache 된 애가 현재 공격 사정거리 범위를 넘어가면
    // 멍하니 타겟을 바라보기만 하는 상황이 생김.
    // 몬스터같은 유닛은 크게 상관없을지 몰라도
    // 유저 캐릭터가 그러면 버그임 . 그렇기 때문에
    // 이 케이스에 바로 Cache 를 Invalidate 하고 다시 서치를 할 수 있도록
    // 플래스를 별도로 추가 
    bool _invalidateOnTargetMissing;

    public StandardAggroSystem(
        int preferTargetCount,
        float recalculationWaitDuration,
        float withValidTargetRecalculateWaitDuration,
        bool invalidateOnTargetMissing,
        params E_EntityType[] targetEntityTypes)
    {
        _preferTargetCount = preferTargetCount;

        foreach (var targetEntityType in targetEntityTypes)
        {
            _playerTargetLayerMask |= LayerUtils.GetLayerMask((EntityTeamType.Enemy, targetEntityType));
            _enemyTargetLayerMask |= LayerUtils.GetLayerMask((EntityTeamType.Player, targetEntityType));
        }

        _targetLayerMasksByTeam.Add(EntityTeamType.Player, _playerTargetLayerMask);
        _targetLayerMasksByTeam.Add(EntityTeamType.Enemy, _enemyTargetLayerMask);

        _recalculateWaitDuration = recalculationWaitDuration;
        _withValidTargetRecalculateWaitDuration = withValidTargetRecalculateWaitDuration;
        _invalidateOnTargetMissing = invalidateOnTargetMissing;
    }

    public override void Initialize()
    {
        InGameManager.Instance.EventListener += OnInGameEvent;
    }

    public override void Release()
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;
        _askerRequestHistory.Clear();
    }

    public override EntityBase FindTarget(EntityBase asker)
    {
        if (_askerRequestHistory.TryGetValue(asker.ID, out var history))
        {
            // 대기 시간동안은 캐싱된 마지막 타겟 바로 리턴
            if (Time.time < history.NextScanTimeAt &&
                EntityHelper.IsValid(history.LastFoundTarget) &&
                // 만약 Target Missing 때 Invalidate 옵션이 켜져있다면
                // 스킬 파트를 참조해서 현재 범위에 '사용가능한' 스킬의 거리안ㅇ
                // 타겟이 들어오는지 체크 , 만약 들어오지 않는다면 캐시무효
                (_invalidateOnTargetMissing && asker.SkillPart.CheckIfTargetIsInRange(history.LastFoundTarget)))
            {
                return history.LastFoundTarget;
            }
        }

        var statData = DBStat.Get(asker.TableData.StatTableID);
        var cacheContainer = InGameManager.Instance.CacheContainer;
        Collider[] cols = cacheContainer.GetColliderCacheByCount(_preferTargetCount);
        var askerPosition = asker.ApproxPosition;

        int count = Physics.OverlapSphereNonAlloc(askerPosition, statData.ScanRange, cols, _targetLayerMasksByTeam[asker.Team]);

        // 찾아봤는데 타겟이 없는 상황 
        if (count == 0)
        {
            _askerRequestHistory[asker.ID] = new History(null, Time.time + _recalculateWaitDuration);
            return null;
        }

        float sqrMinDist = float.MaxValue;
        EntityBase closestEntity = null;

        for (int i = 0; i < count; i++)
        {
            var collider = cols[i];
            var entity = cacheContainer.GetEntityFromCollider(collider);

            if (EntityHelper.IsValid(entity) == false)
                continue;

            // 기존에 타겟팅 되고 있던애가 똑같이 검출되면 일관성있게 타겟팅
            //      => 가장 가까운애가 일단 더 자연스러울듯 . 
            //if (lastTarget != null && lastTarget == entity)
            //{
            //    _askerRequestHistory[asker.ID] = (lastTarget, Time.time);
            //    return lastTarget;
            //}

            float sqrDist = Vector3.SqrMagnitude(entity.ApproxPosition - askerPosition);
            if (sqrDist < sqrMinDist)
            {
                sqrMinDist = sqrDist;
                closestEntity = entity;
            }
        }

        _askerRequestHistory[asker.ID] = new History(closestEntity, Time.time + _withValidTargetRecalculateWaitDuration);

        return closestEntity;
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.EntityDied)
        {
            var arg = argBase as EntityDiedEventArg;
            _askerRequestHistory.Remove(arg.ID);
        }
    }
}

