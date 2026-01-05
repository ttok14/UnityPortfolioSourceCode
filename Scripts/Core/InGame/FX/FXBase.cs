using UnityEngine;

public struct FXPlayData
{
    public Vector3 startPos;
    public Vector3 endPos;
    public float? thickness;
    public ulong targetValidId;
    public Transform target;

    public FXPlayData(Vector3 startPos, Vector3 endPos, float? thickness, ulong targetValidId, Transform target)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.thickness = thickness;
        this.targetValidId = targetValidId;
        this.target = target;
    }
}

public abstract class FXBase : PoolableObjectBase, IUpdatable
{
    // public abstract FXType Type { get; }

    // 업데이트 함수 사용할지에 대한 여부
    // (업데이트 루틴은 별도 매니저로 실행)
    public abstract bool ActivateLateUpdate { get; }

    protected virtual void OnUpdated() { }

    public virtual void Play(FXPlayData data)
    {
        transform.position = data.startPos;

        if (ActivateLateUpdate)
            UpdateManager.Instance.RegisterSingleLateUpdatable(this);
    }

    void IUpdatable.OnUpdate()
    {
        OnUpdated();
    }

    public override void OnInactivated()
    {
        if (ActivateLateUpdate && UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingleLateUpdatable(this);

        base.OnInactivated();
    }
}
