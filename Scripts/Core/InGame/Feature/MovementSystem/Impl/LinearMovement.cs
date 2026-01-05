using UnityEngine;

public class LinearMovementInitData : MovementInitDataBase
{
}

public class LinearMovement : MovementStrategyBase
{
    Vector3 _direction;

    public override void OnPoolInitialize()
    {
    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        // 그냥 충분히 큰값이면 .
        if (RotationSpeed >= 1000f)
            Mover.rotation = Quaternion.LookRotation(_direction);
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        _direction = Vector3.zero;
    }

    public override void ReturnToPool()
    {
        MovementFactory.Return(this);
    }

    protected override void OnTargetPosUpdated(Vector3 targetPos)
    {
        _direction = (base.TargetPos - Mover.position).normalized;
    }

    public override MovementStrategyResult UpdateMovement()
    {
        base.UpdateMovement();

        var movement = _direction * MoveSpeed * Time.deltaTime;
        Mover.position += movement;

        if (RotationSpeed > 0f && _direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(_direction);

            Mover.rotation = Quaternion.RotateTowards(
                Mover.rotation,
                targetRot,
                RotationSpeed * Time.deltaTime);
        }

        bool arrived = Vector3.Dot((TargetPos - Mover.position).normalized, _direction) < 0;

        if (arrived)
        {
            Mover.position = TargetPos;
        }

        return arrived ? MovementStrategyResult.Finished : MovementStrategyResult.Moving;
    }
}
