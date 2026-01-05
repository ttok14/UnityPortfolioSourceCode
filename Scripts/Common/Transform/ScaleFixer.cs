using UnityEngine;

public class ScaleFixer : MonoBehaviour
{
    public Vector3 currentTargetWorldScale;

    private Vector3 lastLocalScale;
    private const float ScaleTolerance = 0.0001f;

    void Awake()
    {
        currentTargetWorldScale = transform.lossyScale;
        lastLocalScale = transform.localScale;
    }

    public void SetScale(Vector3 newWorldScale)
    {
        currentTargetWorldScale = newWorldScale;

        if (transform.parent == null)
        {
            transform.localScale = currentTargetWorldScale;
            lastLocalScale = transform.localScale;
            return;
        }

        Vector3 parentWorldScale = transform.parent.lossyScale;

        if (parentWorldScale.x == 0 || parentWorldScale.y == 0 || parentWorldScale.z == 0) return;

        Vector3 targetLocalScale = new Vector3(
            currentTargetWorldScale.x / parentWorldScale.x,
            currentTargetWorldScale.y / parentWorldScale.y,
            currentTargetWorldScale.z / parentWorldScale.z
        );

        transform.localScale = targetLocalScale;
        lastLocalScale = targetLocalScale;
    }

    void LateUpdate()
    {
        if (Vector3.SqrMagnitude(transform.localScale - lastLocalScale) > ScaleTolerance)
        {
            currentTargetWorldScale = transform.lossyScale;
            lastLocalScale = transform.localScale;
        }

        if (transform.parent == null)
        {
            transform.localScale = currentTargetWorldScale;
            lastLocalScale = transform.localScale;
            return;
        }

        Vector3 parentWorldScale = transform.parent.lossyScale;

        if (parentWorldScale.x == 0 || parentWorldScale.y == 0 || parentWorldScale.z == 0) return;

        Vector3 targetLocalScale = new Vector3(
            currentTargetWorldScale.x / parentWorldScale.x,
            currentTargetWorldScale.y / parentWorldScale.y,
            currentTargetWorldScale.z / parentWorldScale.z
        );

        transform.localScale = targetLocalScale;
        lastLocalScale = targetLocalScale;
    }
}
