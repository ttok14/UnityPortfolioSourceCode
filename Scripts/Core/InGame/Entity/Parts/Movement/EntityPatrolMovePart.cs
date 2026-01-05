using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class EntityPatrolMovePart : EntityMovePartBase
{
    public enum Mode
    {
        None = 0,

        Patrolling,
        LookAt
    }

    Mode _mode;

    Vector3 _patrolDirFrom;
    Vector3 _patrolDirTo;

    int _patrolSign;

    Vector3 _lookDirection;

    EntityStatData _stat;

    public override bool IsMoving => _mode != Mode.None;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        _stat = Owner.GetData(EntityDataCategory.Stat) as EntityStatData;
    }

    public void StartPatrol(Vector3 patrolDirFrom, Vector3 patrolDirTo, float oneWayDuration)
    {
        _mode = Mode.Patrolling;
        _patrolDirFrom = patrolDirFrom;
        _patrolDirTo = patrolDirTo;

        float angle = Vector3.Angle(patrolDirFrom, patrolDirTo);
        _stat.SetCurrentRotationSpeed(angle / oneWayDuration, false);

        _patrolSign = 1;
    }

    public override void RotateToDirection(Vector3 direction)
    {
        _mode = Mode.LookAt;
        _lookDirection = direction;

        _stat.SetCurrentRotationSpeed(300f, false);
    }

    public override void Stop()
    {
        _mode = Mode.None;

        _patrolDirFrom = Vector3.zero;
        _patrolDirTo = Vector3.zero;

        _patrolSign = 0;

        _lookDirection = Vector3.zero;

        _stat.SetCurrentMoveSpeed(0, false);
        _stat.SetCurrentRotationSpeed(0, false);
    }

    public override void DoFixedUpdate()
    {
        base.DoFixedUpdate();

        if (_mode == Mode.Patrolling)
        {
            bool arrived = RotateToward(_stat.CurrentRotationSpeed * Time.fixedDeltaTime, _patrolSign == 1 ? _patrolDirTo : _patrolDirFrom);
            if (arrived)
                _patrolSign *= -1;
        }
        else if (_mode == Mode.LookAt)
        {
            if (RotateToward(_stat.CurrentRotationSpeed * Time.fixedDeltaTime, _lookDirection))
            {
                Stop();
            }
        }
    }

    protected override bool RotateToward(float amount, Vector3 dirToTarget)
    {
        if (Vector3.Angle(Mover.forward, dirToTarget) <= amount)
        {
            SetForward(dirToTarget);
            return true;
        }

        Mover.rotation = Quaternion.RotateTowards(Mover.rotation, Quaternion.LookRotation(dirToTarget), amount);

        return false;
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
