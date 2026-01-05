
public interface IDeliveryUpdateStrategy : IInstancePoolElement
{
    void OnUpdate(IDeliverySource source, DeliveryContext conext);
}
