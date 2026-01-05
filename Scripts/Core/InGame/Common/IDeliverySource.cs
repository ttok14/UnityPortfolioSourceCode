using UnityEngine;
using GameDB;

public interface IDeliverySource
{
    Vector3 StartPosition { get; }
    Vector3 Position { get; }
    void ForceEnd();
    void OnDeliveryTrigger(E_UpdateLogicType updateLogic);
}
