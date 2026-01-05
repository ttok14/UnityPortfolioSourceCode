using UnityEngine;

public interface IDeliveryAction
{
    void Execute(IDeliverySource source, EntityBase target, DeliveryContext context);
}
