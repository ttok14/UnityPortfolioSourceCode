using UnityEngine;

public class PenetrateAction : DeliveryActionBase
{
    public int _penetrateLimit;

    public override void Initialize(ActionData data)
    {
        base.Initialize(data);

        _penetrateLimit = data.Value01.GetApproximateInt();
    }

    public override void Execute(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        Play_SFX_FX(source, target, context);

        // 0 은 무한 관통
        if (_penetrateLimit > 0 && context.DeliveryHistory.ImpactedIDs.Count >= _penetrateLimit)
        {
            source.ForceEnd();
        }
    }

    //public override void OnPoolActivated(IInstancePoolInitData initData)
    //{
    //    base.OnPoolActivated(initData);

    //    _penetrateLimit = _data.Destroy
    //}

    //public override void OnPoolReturned()
    //{
    //    _penetrateLimit = 0;
    //}

    //public override void ReturnToPool()
    //{
    //    DeliveryActionFactory.ActionPool.Return(this);
    //}
}
