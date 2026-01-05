using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameDB;

public class EntityMovePartInitData : EntityPartInitDataBase
{
    public Transform Mover;

    public EntityMovePartInitData(EntityBase owner) : base(owner) { }
    public EntityMovePartInitData(EntityBase owner, Transform mover) : base(owner)
    {
        Mover = mover;
    }
}

public readonly struct MoveContext
{
    public readonly E_EntityType DestEntityType;

    public MoveContext(E_EntityType destEntityType)
    {
        DestEntityType = destEntityType;
    }
}

public abstract class EntityMovePartBase : EntityPartBase
{
    protected Rigidbody _rigidbody;

    public Transform Mover { get; private set; }

    //protected EntityMovePartBase(Transform mover)
    //{
    //    Mover = mover;
    //}

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var idata = initData as EntityMovePartInitData;
        Mover = idata.Mover;
    }

    public override void OnPoolReturned()
    {
        if (IsMoving)
            Stop();

        _rigidbody = null;
        Mover = null;

        base.OnPoolReturned();
    }

    public virtual void OnDie(Vector3 attackerPos)
    {
        Stop();
    }

    public abstract bool IsMoving { get; }

    public virtual void MoveToPathFinding(Vector3 destination) { }
    public virtual void MoveAlongPaths(PathListPoolable paths, MoveContext? context) { }
    public virtual void MoveDirectional(Vector3 direction, float normalizedAmount) { }
    public virtual void MoveToDestDirectional(Vector3 destPos, float normalizedAmount) { }

    public virtual void RotateToDirection(Vector3 direction)
    {
        Mover.rotation.SetLookRotation(direction);
    }

    public virtual void Stop() { }

    public virtual void DoFixedUpdate() { }

    public void SetRigidBody(Rigidbody rb)
    {
        _rigidbody = rb;
    }

    protected void SetPos(Vector3 newPos)
    {
        Mover.position = newPos;
        Owner.DoUpdatePositionInfo(newPos);
    }

    protected void Move(Vector3 amount)
    {
        Vector3 newPos = Mover.position + amount;
        Mover.position = newPos;
        Owner.DoUpdatePositionInfo(newPos);
    }

    protected void RigidBodyMove(Vector3 newPos)
    {
        _rigidbody.MovePosition(newPos);
        Owner.DoUpdatePositionInfo(newPos);
    }

    protected void SetForward(Vector3 forward)
    {
        if (forward == default)
            return;

        Mover.forward = forward;
    }

    protected virtual bool RotateToward(float amount, Vector3 dirToTarget)
    {
        float signedAngle = Vector3.SignedAngle(Mover.forward, dirToTarget, Vector3.up);

        if (amount >= Mathf.Abs(signedAngle))
        {
            SetForward(dirToTarget);
            return true;
        }

        RotateEuler(0, amount * Mathf.Sign(signedAngle), 0);
        return false;
    }

    protected void RotateEuler(float x, float y, float z)
    {
        Mover.Rotate(x, y, z);
    }

    protected void RotateEuler(Vector3 euler)
    {
        Mover.Rotate(euler);
    }
}
