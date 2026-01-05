using UnityEngine;
using GameDB;

public enum MovementStrategyResult
{
    Moving,
    Arrived,
    Finished
}

public abstract class MovementStrategyBase : IInstancePoolElement
{
    protected Transform Mover;
    protected Vector3 StartPos;
    private EntityBase Target;
    private Vector3 FixedDest;
    private Vector3 DestOffset;
    protected float MoveSpeed;
    protected float RotationSpeed;
    protected E_AimType AimType;

    ulong _validTargetId;

    protected Vector3 TargetPos { get; private set; }

    public abstract void ReturnToPool();

    protected virtual void OnTargetPosUpdated(Vector3 targetPos) { }

    public virtual void OnPoolActivated(IInstancePoolInitData initData)
    {
        var data = initData as MovementInitDataBase;

        Mover = data.Mover;
        SetTarget(data.Target);
        StartPos = data.StartPosition;
        FixedDest = data.FixedDest;
        DestOffset = data.DestOffset;
        MoveSpeed = data.MoveSpeed;
        RotationSpeed = data.RotationSpeed;
        AimType = data.AimType;

        Vector3 srcTargetPos;
        if (Target)
            srcTargetPos = Target.ApproxPosition + data.DestOffset;
        else
            srcTargetPos = data.FixedDest + data.DestOffset;

        TargetPos = MovementHelper.ApplyAimedPosition(AimType, srcTargetPos, StartPos, Target);

        OnTargetPosUpdated(TargetPos);
    }

    public abstract void OnPoolInitialize();
    public virtual void OnPoolReturned()
    {
        Mover = null;
        Target = null;
        StartPos = default;
        FixedDest = default;
        MoveSpeed = 0;
        RotationSpeed = 0;
        TargetPos = default;
        _validTargetId = 0;
        AimType = E_AimType.None;
    }

    protected void UpdateTargetPos()
    {
        if (_validTargetId > 0)
        {
            if (EntityHelper.IsValid(Target, _validTargetId))
            {
                TargetPos = Target.ApproxPosition + DestOffset;
            }
            else
            {
                SetTarget(null);
            }

            // TODO : TargetPos 업데이트 루프를 좀 최적해야할거같음 .
            // AimedPosition 적용까지 . 
            TargetPos = MovementHelper.ApplyAimedPosition(AimType, TargetPos, StartPos, Target);
            OnTargetPosUpdated(TargetPos);
        }
    }

    void SetTarget(EntityBase target)
    {
        if (EntityHelper.IsValid(target))
        {
            Target = target;
            _validTargetId = target.ID;
        }
        else
        {
            Target = null;
            _validTargetId = 0;
        }
    }

    public virtual MovementStrategyResult UpdateMovement()
    {
        UpdateTargetPos();
        return MovementStrategyResult.Moving;
    }
}
