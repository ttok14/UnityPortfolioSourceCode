using UnityEngine;

public class TimeLimitInitData : IInstancePoolInitData
{
    public float LimitTime;
}

public class TimeLimitStrategy : IDeliveryUpdateStrategy
{
    float _limitTIme;
    float _elapsedTime;
    bool _isTriggered;

    public void OnUpdate(IDeliverySource source, DeliveryContext conext)
    {
        if (_isTriggered)
            return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _limitTIme)
        {
            _isTriggered = true;

            source.OnDeliveryTrigger(GameDB.E_UpdateLogicType.Timer);
        }
    }

    public void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as TimeLimitInitData;

        _limitTIme = data.LimitTime;
        _elapsedTime = 0;
        _isTriggered = false;
    }

    public void OnPoolInitialize() { }

    public void OnPoolReturned()
    {
        _limitTIme = 0;
        _elapsedTime = 0;
        _isTriggered = false;
    }

    public void ReturnToPool()
    {
        DeliveryUpdateFactory.Return(this);
    }
}
