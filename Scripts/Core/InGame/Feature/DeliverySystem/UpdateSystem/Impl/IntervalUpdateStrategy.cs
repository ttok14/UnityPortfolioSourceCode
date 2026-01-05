using UnityEngine;

public class IntervalUpdateInitData : IInstancePoolInitData
{
    public float Interval;
}

public class IntervalUpdateStrategy : IDeliveryUpdateStrategy
{
    float _interval;
    float _elapsedTime;

    public void OnUpdate(IDeliverySource source, DeliveryContext conext)
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _interval)
        {
            _elapsedTime -= _interval;

            source.OnDeliveryTrigger(GameDB.E_UpdateLogicType.Interval);
        }
    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as IntervalUpdateInitData;

        _interval = data.Interval;
        _elapsedTime = 0;
    }

    public void OnPoolInitialize() { }

    public void OnPoolReturned()
    {
        _interval = 0;
        _elapsedTime = 0;
    }

    public void ReturnToPool()
    {
        DeliveryUpdateFactory.Return(this);
    }
}
