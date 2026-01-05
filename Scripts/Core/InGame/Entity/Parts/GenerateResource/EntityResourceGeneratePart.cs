using System;
using UnityEngine;
using GameDB;

public class EntityResourceGeneratorInitData : EntityPartInitDataBase
{
    public E_ResourceType ResourceType;
    public uint DetailResourceId;
    public uint Amount;
    public float Interval;

    public EntityResourceGeneratorInitData(EntityBase owner) : base(owner) { }

    public void Set(E_ResourceType resourceType, uint detailResourceId, uint amount, float interval)
    {
        ResourceType = resourceType;
        DetailResourceId = detailResourceId;
        Amount = amount;
        Interval = interval;
    }
}

public class EntityResourceGeneratePart : EntityPartBase
{
    float _prevGeneratedTimeAt;

    bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;

            if (value)
            {
                _prevGeneratedTimeAt = Time.time;
            }
            else
            {
                _prevGeneratedTimeAt = 0;
            }
        }
    }

    // public float NextGenRemainedTime => IsEnabled ? _nextGenerateTimeAt - Time.time : 0f;
    public float Progress => IsEnabled ? (Time.time - _prevGeneratedTimeAt) / Interval : 0f;

    public E_ResourceType ResourceType { get; private set; }
    // ResourceType에 따라 참조할 테이블의 ID 임 주의.
    public uint DetailResourceId { get; private set; }
    public float Interval { get; private set; }
    public uint Amount { get; private set; }

    public event EntityEventDelegates.OnResourceGenerated OnGeneratedListener;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var idata = initData as EntityResourceGeneratorInitData;

        ResourceType = idata.ResourceType;
        DetailResourceId = idata.DetailResourceId;
        Amount = idata.Amount;
        Interval = idata.Interval;
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        ResourceType = E_ResourceType.None;
        DetailResourceId = 0;
        Amount = 0;
        Interval = 0;

        OnGeneratedListener = null;
    }

    public void Update()
    {
        if (IsEnabled == false)
            return;

        if (Progress >= 1.0f)
        {
            _prevGeneratedTimeAt = Time.time;

            DoGenerate();
        }
    }

    void DoGenerate()
    {
        switch (ResourceType)
        {
            case E_ResourceType.Currency:
                {
                    Me.AcquireCurrency(DBCurrency.GetCurrencyType(DetailResourceId), (int)Amount);
                }
                break;
            case E_ResourceType.Entity:
                {
                    // TODO : 엔티티를 생성하는 거는 , 일단 필요한게
                    //      1. 어디에 생성할것인가.
                    //          인데 이게 지금은 바로 구현하기 애매하기에 일단 킵
                    //          일단 지금 생각으로는, 보통은 Structure 가 사용하는 기능이기때문에
                    //          가능하다면 NeighborOccupyPosition 들을 가져와서 , 여기서 현재 Entity의
                    //          앞 방향쪽 Neighbor 중 하나를 가져와서 여기를 position 으로 지정하고
                    //          rotation 값도 Structure 가 바라보는 방향으로 설정하면 될거같기는 함. ??
                    //EntityManager.Instance.CreateEntity(
                    //    new EntityObjectData(worldPosition, eulerRotY, _detailResourceId),
                    //    _owner.Team);
                    TEMP_Logger.Deb($"Not imlemented : CreateEntity(Resource)");
                }
                break;
            default:
                TEMP_Logger.Err($"Not implemented ResourceType : {ResourceType}");
                return;
        }

        OnGeneratedListener?.Invoke(ResourceType, DetailResourceId, Amount);
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
