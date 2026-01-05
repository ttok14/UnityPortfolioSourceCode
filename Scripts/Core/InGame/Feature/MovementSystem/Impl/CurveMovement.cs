using DG.Tweening;
using UnityEngine;

public class CurveMovementInitData : MovementInitDataBase
{
    public AnimationCurve Curve;
    public float CurveHeight;
    public Ease EaseType;

    public void SetCurveData(AnimationCurve curve, float curveHeight, Ease easeType)
    {
        Curve = curve;
        CurveHeight = curveHeight;
        EaseType = easeType;
    }
}

public class CurveMovement : MovementStrategyBase
{
    AnimationCurve _curve;
    float _height;
    Ease _easeType;

    float _movedDistance;

    float _totalDistance;

    public override void OnPoolInitialize()
    {

    }

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as CurveMovementInitData;

        _curve = data.Curve;
        _height = data.CurveHeight;
        _easeType = data.EaseType;

        // _startPos = Mover.position;

        _totalDistance = Vector3.Distance(TargetPos, Mover.position);
        _movedDistance = 0f;

        UpdateMovement();
    }

    public override void OnPoolReturned()
    {
        _curve = null;
        _height = 0f;
        _easeType = default;
        _totalDistance = 0f;
        _movedDistance = 0f;
    }

    public override void ReturnToPool()
    {
        MovementFactory.Return(this);
    }

    public override MovementStrategyResult UpdateMovement()
    {
        base.UpdateMovement();

        _movedDistance += MoveSpeed * Time.deltaTime;

        float t = _movedDistance / _totalDistance;

        if (t >= 1f)
        {
            Mover.position = TargetPos;
            return MovementStrategyResult.Finished;
        }

        Vector3 nextPos = CurveHelper.MoveEaseWithCurve(_curve, t, StartPos, TargetPos, _easeType, _height);

        if (RotationSpeed > 0)
        {
            var targetRot = Quaternion.LookRotation((nextPos - Mover.position).normalized);

            Mover.rotation = Quaternion.RotateTowards(Mover.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }

        Mover.position = nextPos;

        return MovementStrategyResult.Moving;
    }
}
