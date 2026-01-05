using UnityEngine;

public class CircleShadow : PoolableObjectBase, IUpdatable
{
    const int UpdateFrameThreshold = 2;

    Transform _followTarget;

    float _baseScaleScalar = 1f;

    Vector3 _lastUpdatedPosition;
    Vector3 _lastUpdatedLightPosition;
    Vector3 _lastUpdatedLightRot;

    static float FloorHeight = 0.02f;
    static float MaxScale = 2;
    static float HeightForLeastScale = 4;

    public bool _useDynamicHeight;
    public bool UseDynamicHeight
    {
        get => _useDynamicHeight;
        set
        {
            _useDynamicHeight = value;
        }
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);
        _followTarget = transform.parent;

        UpdateManager.Instance.RegisterSingle(this);
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingle(this);
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
    }

    public void SetScaleScalar(float scalar)
    {
        _baseScaleScalar = scalar;
        transform.localScale = new Vector3(scalar, scalar, 1);
    }

    void IUpdatable.OnUpdate()
    {
        if (!_followTarget)
        {
            base.Return();
            return;
        }

        if (Time.frameCount % UpdateFrameThreshold != 0)
            return;

        var targetPos = _followTarget.position;

        if (UseDynamicHeight == false || targetPos.y < FloorHeight)
        {
            transform.localPosition = new Vector3(0, FloorHeight, 0);
            return;
        }

        var manager = LightManager.Instance;
        var lightPos = manager.ShadowLightApproxPosition;
        var lightEuler = manager.ShadowLightApproxEulerAngles;

        Vector3 pos = transform.position;

        bool doUpdate =
            _lastUpdatedPosition != pos ||
            _lastUpdatedLightPosition != lightPos ||
            _lastUpdatedLightRot != lightEuler;

        if (doUpdate == false)
            return;

        var lightForward = manager.ShadowLightApproxForward;

        _lastUpdatedPosition = pos;
        _lastUpdatedLightPosition = lightPos;
        _lastUpdatedLightRot = lightEuler;

        float rad = Vector3.Angle(Vector3.down, lightForward) * Mathf.Deg2Rad;
        float dist = Mathf.Abs(Mathf.Cos(rad)) * targetPos.y / Mathf.Sin(rad);
        var destPos = (new Vector3(lightForward.x, 0, lightForward.z).normalized * dist);
        destPos = destPos + targetPos;
        transform.position = new Vector3(destPos.x, FloorHeight, destPos.z);
        float scale = Mathf.Lerp(MaxScale, _baseScaleScalar, targetPos.y / HeightForLeastScale);
        transform.localScale = new Vector3(scale, scale, 1);
    }
}
