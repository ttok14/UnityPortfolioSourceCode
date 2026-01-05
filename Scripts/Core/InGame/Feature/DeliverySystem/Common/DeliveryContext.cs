using System.Collections.Generic;
using UnityEngine;
using GameDB;

public class DeliveryContext : IInstancePoolElement, IEntityAffecter
{
    public ulong ExecutorID { get; set; }
    public EntityTeamType ExecutorTeam;
    // Target 과 TargetTeam 은 다를 수 있음 . 타겟은 아군으로 잡아 놓고
    // 충돌은 적이랑 할수도있어서 분리 
    public EntityTeamType TargetTeam;
    public int TargetLayerMask;

    public uint Damage { get; set; }
    public uint Heal { get; set; }
    public float PhysicalForce { get; set; }

    public Vector3 Position { get; set; }

    // TODO : 애를 재사용할 수있는 방법이 필요하다. 지금은 매번 새로 할당중
    //      1. 퍼져나가는 투사체같은 경우는 애를 마음대로 Clear 할 수가 없어서 .
    //      2. 그렇다고 별도의 세개에 파생 Context에 UnionWith 로 값만 카피한다?
    //          그럼 그 뒤로 애 공유가 아니라 각자 들고있음. 이 경우 중복 타깃팅 발생
    //          (이것도 기획 의도에 따라서 뭐 의도적으로 적용은 가능하기는 함)
    //          (즉 이걸 제어할 방법은 일단 대략 있는건데 지금은 디폴트로 일단
    //          중복이 없게끔 처리중 , 즉 Copy 하면서 애에 대한 참조를 그냥 놔버림)
    // public HashSet<ulong> VisitedEntityIDs;

    #region ====:: 상속 관련 데이터 ::====
    public SharedDeliveryHistory<ulong> DeliveryHistory;
    public E_DeliveryContextInheritType InheritType;
    #endregion

    // 바운스되면서 진행되는 경우에는 이 값으로 최대 카운트를 제한함
    // 이 값은 부모 오브젝트가 파생되는 오브젝트로 넘겨줄 수 있음
    // 그렇기 때문에 Source 가 있는 경우에는 값을 카피함 (임의로 0 설정 X)
    public uint ChainDepthMaxCount;

    public int ChainDepth;

    public E_CollisionRangeType CollisionType;
    public float CollisionRange;

    public bool AllowMultiHit;

    public uint PreferMaxTargetCount;

    public bool FXPerTargetOrDeliverySelf;

    public string[] SFXKeys;
    public string[] FXKeys;

    public uint ReferenceCount;

    public void Set(
        ulong executorId,
        EntityTeamType executorTeam,
        EntityTeamType targetTeam,
        int targetLayerMask,
        E_CollisionRangeType rangeType,
        float collisionRange,
        bool allowMultiHit,
        uint preferMaxTargetCount,
        uint damage,
        uint heal,
        float physicalForce,
        bool fxPerTargetOrDeliverySelf,
        string[] sfxKeys,
        string[] fxKeys,
        E_DeliveryContextInheritType inheritType,
        SharedDeliveryHistory<ulong> visitedIDs,
        int chainDepth)
    {
        ExecutorID = executorId;
        ExecutorTeam = executorTeam;
        TargetTeam = targetTeam;
        TargetLayerMask = targetLayerMask;
        CollisionType = rangeType;
        CollisionRange = collisionRange;
        PreferMaxTargetCount = preferMaxTargetCount;
        Damage = damage;
        Heal = heal;
        PhysicalForce = physicalForce;
        FXPerTargetOrDeliverySelf = fxPerTargetOrDeliverySelf;
        SFXKeys = sfxKeys;
        FXKeys = fxKeys;
        DeliveryHistory = visitedIDs;
        AllowMultiHit = allowMultiHit;
        InheritType = inheritType;
        ChainDepth = chainDepth;
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void IncreaseReferenceCount(uint addCount = 1)
    {
        ReferenceCount += addCount;
    }

    public void DecreaseReferenceCount(uint decreaseCount = 1)
    {
        // 이 케이스에는 Increase 가 누락된건가? 아무튼 일단 로그 출력
        if (ReferenceCount == 0)
        {
            TEMP_Logger.Err($"This DeliveryContext Ref Count is already zero. This is a bug.");
            return;
        }

        if (decreaseCount >= ReferenceCount)
            ReferenceCount = 0;
        else
            ReferenceCount -= decreaseCount;

        if (ReferenceCount == 0)
            ReturnToPool();
    }

    public void Reset()
    {
        ReferenceCount = 0;
        ChainDepthMaxCount = 0;
        Set(default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default);
        SetPosition(Vector3.zero);
    }

    #region ===:: Interface ::===
    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as DeliveryContextInitData;

        IncreaseReferenceCount();

        int chainDepth = data.Source != null ? data.Source.ChainDepth + 1 : 0;

        SharedDeliveryHistory<ulong> visitIDs = null;

        // 원본이 없는 케이스 
        if (data.Source == null)
        {
            ChainDepthMaxCount = 0;

            // 새로 할당받음
            visitIDs = DeliveryActionFactory.GetOrCreateVisitIDHashSet(E_DeliveryContextInheritType.None, null);
        }
        else
        {
            ChainDepthMaxCount = data.Source.ChainDepthMaxCount;

            // 파생된 현 투사체의 상속 정책에 따라 상속받음 참고.
            switch (data.InheritType)
            {
                case E_DeliveryContextInheritType.Share:
                    {
                        visitIDs = data.Source.DeliveryHistory;

                        // 그대로 물려받아 사용하는거기 때문에 참조 카운트 하나 수동으로 증가시킴
                        visitIDs.IncreaseReferenceCount();
                    }
                    break;
                case E_DeliveryContextInheritType.Copy:
                case E_DeliveryContextInheritType.Reset:
                    {
                        // 여기서는 Pool 에서 가져오는 과정에서 내부적으로 참조 카운트 자동으로 하나 올라감
                        visitIDs = DeliveryActionFactory.GetOrCreateVisitIDHashSet(data.InheritType, data.Source.DeliveryHistory);
                    }
                    break;
                default:
                    visitIDs = DeliveryActionFactory.GetOrCreateVisitIDHashSet(data.InheritType, data.Source.DeliveryHistory);
                    TEMP_Logger.Err($"Not implemented Type : {data.InheritType}");
                    break;
            }
        }

        Set(data.ExecutorID,
            data.ExecutorTeam,
            data.TargetTeam,
            data.TargetLayerMask,
            data.CollisionType,
            data.CollisionRange,
            data.AllowMultiHit,
            data.PreferMaxTargetCount,
            data.Damage,
            data.Heal,
            data.PhysicalForce,
            data.FXPerTargetOrDeliverySelf,
            data.SFXKeys,
            data.FXKeys,
            data.InheritType,
            visitIDs,
            chainDepth);
    }

    public void OnPoolInitialize()
    {

    }

    public void OnPoolReturned()
    {
        Reset();
    }

    public void ReturnToPool()
    {
        DeliveryHistory.DecreaseReferenceCount(1);
        DeliveryActionFactory.DataPool.Return(this);
    }
    #endregion
}
