using UnityEngine;
using GameDB;

public abstract class ImpactStrategyBase
{
    public abstract void Execute(
        IDeliverySource deliverSource,
        EntityBase target,
        DeliveryContext context);
}
