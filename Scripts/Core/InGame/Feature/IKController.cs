using UnityEngine;

public class IKController : MonoBehaviour
{
    public float aimWeight = 1.0f;

    Animator animator;
    public Vector3? TargetPosition { get; set; }

    Vector3 _lastTargetPos;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator || TargetPosition.HasValue == false)
            return;

        // 일단 레이어 1 만 쓰자 하체는 이동하는 곳으로 고정
        if (layerIndex != 1)
            return;

        if (_lastTargetPos == TargetPosition.Value)
            return;

        _lastTargetPos = TargetPosition.Value;

        animator.SetLookAtWeight(aimWeight, 0.3f, 0.9f, 0.0f, 0.5f);

        Vector3 targetPos = TargetPosition.Value;
        animator.SetLookAtPosition(targetPos);
    }
}
