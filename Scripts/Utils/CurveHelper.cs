using UnityEngine;
using DG.Tweening;

public class CurveHelper
{
    public static Vector3 MoveEaseWithCurve(
        AnimationCurve curve,
        float time,
        Vector3 from,
        Vector3 to,
        Ease ease,
        float heightMultiplier)
    {
        if (curve.length == 0)
            return from;

        time = Mathf.Min(time, 1f);
        float easedT = (float)DG.Tweening.Core.Easing.EaseManager.Evaluate(
            ease,
            DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(ease),
            time,
            1f,
            1f,
            0f);

        var xzFrom = from;
        var xzTo = to;

        Vector3 nextPos = Vector3.Lerp(xzFrom, xzTo, easedT);

        nextPos.y += curve.Evaluate(easedT) * heightMultiplier; // ((1 - easedT) * from.y) + curve.Evaluate(easedT) * heightMultiplier;

        return nextPos;
    }
}
