using GameDB;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ChainBounceActionBase : DeliveryActionBase
{
    protected struct Candidate
    {
        public EntityBase entity;
        public float sqrDist;
    }

    protected static List<Candidate> Candidates { get; private set; } = new List<Candidate>(32);

    protected int BounceCount { get; private set; }
    protected float Radius { get; private set; }
    protected int MaxChainCount { get; private set; }

    protected abstract int RefreshNextTargets(int bounceCount, DeliveryContext context);

    public override void Initialize(ActionData data)
    {
        base.Initialize(data);

        BounceCount = Data.Value01.GetApproximateInt();
        Radius = Data.Value02;
        MaxChainCount = data.Value03.GetApproximateInt();
    }

    public override void Execute(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        // 상속받은게 없다면 내거를 일단 넣어줌
        if (context.ChainDepthMaxCount == 0)
            context.ChainDepthMaxCount = (uint)MaxChainCount;

        // 현재 최초 투사체로부터 Bounce 된 Depth 가
        // Max 에 도달하면 멈춤
        if (context.ChainDepth >= context.ChainDepthMaxCount)
            return;

        // Debug.LogError("Current ChainDepth : " + context.ChainDepth + " , Value3 : " + Data.Value03);

        //if (context.BounceTargetMaxCount > 0)
        //{
        //    int remainedCnt = Mathf.Max(0, (int)context.BounceTargetMaxCount - context.DeliveryHistory.VisitedIDs.Count);
        //    availableBounceCount = Mathf.Min(BounceCount, remainedCnt);
        //}
        //else
        //{
        //    availableBounceCount = BounceCount;
        //}

        PlaySFX(source);

        if (EntityHelper.IsValid(target) && context.DeliveryHistory.VisitedIDs.Contains(target.ID) == false)
        {
            context.DeliveryHistory.VisitedIDs.Add(target.ID);
        }

        var executor = context.ExecutorID != 0 ? EntityManager.Instance.GetEntity(context.ExecutorID) : null;

        int targetCount = RefreshNextTargets(BounceCount, context);

        for (int i = 0; i < targetCount; i++)
        {
            var targetEntity = Candidates[i].entity;

            // 미리 Add 해야 다른데에서 똑같은 놈을 타깃팅 하지않음 .
            // 근데 추후 이거는 중복 가능하게 하려면 선택적으로 해야하는 로직이긴함. 참고.
            context.DeliveryHistory.VisitedIDs.Add(targetEntity.ID);

            PlayFX(source, targetEntity, context, false, true);

            var srcPos = source.Position;

            ProjectileSystem.Fire(
                Data.RefKey,
                executor,
                targetEntity,
                srcPos,
                null,
                Quaternion.LookRotation((targetEntity.ApproxPosition - srcPos).normalized),
                context.ExecutorTeam,
                context.TargetTeam,
                context.Damage,
                context.Heal,
                null,
                null,
                context).Forget();

            // 진짜 그럴일 거의 없을거 같기는 한데 ,
            // 위에 Fire 에서 생긴애가 현 프레임에 바로 ChainBounceAction 이 실행돼서
            // static 변수인 _candidates 을 가지고 다시 서치를 하게 된다면
            // 레이스컨디션임. 만약 이 상황이 오면 최소한 알수라도 있게 로그라고 박아두자 .
            //      -> 버그아닐 수 있음 다시한번 체크 
            //if (targetCount != Candidates.Count)
            //{
            //    UnityEngine.Debug.LogError($"TargetCount and Candidates Count became different | Source : {source}, RefKey : {Data.RefKey}");
            //}
        }
    }

}
