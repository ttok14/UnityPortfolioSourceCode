using UnityEngine;
using GameDB;

public interface IMovementDataSource
{
    Transform Mover { get; }
    Vector3 StartPosition { get; }
    Vector3 DestOffsetPosition { get; }
    float MoveSpeed { get; }
    float RotationSpeed { get; }
    EntityBase Executor { get; }
    EntityBase Target { get; }
    Vector3 Destination { get; }
    E_AimType AimType { get; }
}
