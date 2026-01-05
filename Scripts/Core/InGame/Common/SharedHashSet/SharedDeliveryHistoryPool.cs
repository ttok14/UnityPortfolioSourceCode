
using System;

public class SharedDeliveryHistoryInitData<T> : IInstancePoolInitData { }

//public class SharedHashSetPool<HashSetInstanceType, InitDataType> where InitDataType : SharedHashSetInitData<HashSetInstanceType>
public class SharedDeliveryHistoryPool<HashSetInstanceType>
{
    InstancePool<SharedDeliveryHistory<HashSetInstanceType>> _pool;

    //public InitDataType InitDataCache;

    // 람다 캐싱 (매번 할당 방지)
    Action<SharedDeliveryHistory<HashSetInstanceType>> _returnHandler;

    public SharedDeliveryHistoryPool() // InitDataType initDataCache)
    {
        _returnHandler = Return;
        _pool = new InstancePool<SharedDeliveryHistory<HashSetInstanceType>>(() => new SharedDeliveryHistory<HashSetInstanceType>(_returnHandler));
        // InitDataCache = initDataCache;
    }

    public SharedDeliveryHistory<HashSetInstanceType> GetOrCreate(IInstancePoolInitData initData)
    {
        return _pool.GetOrCreate(initData);
    }

    public void Return(SharedDeliveryHistory<HashSetInstanceType> element)
    {
        _pool.Return(element);
    }
}
