using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIWorldFollowBase : UIBase, IUpdatable
{
    public class WorldFollowArg : UIArgBase
    {
        public Transform followTarget;
        public Vector2 uiOffsetPos;

        public WorldFollowArg() { }
        public WorldFollowArg(Transform followTarget, Vector2 uiOffsetPos)
        {
            this.followTarget = followTarget;
            this.uiOffsetPos = uiOffsetPos;
        }
    }

    protected Transform _followTarget;

    [SerializeField]
    RectTransform _followerRoot;

    [Header("월드 팔로워 컬링 설정, 이 CanvasGroup 은 타겟의 Visible 여부에 따라 " +
        "Alpha 값을 0 또는 1 로 강제 설정하기 때문에 이 게임오브젝트을" +
        "트윈 오브젝트로 쓸때 알파 요소는 사용하면 안됨 !!!")]
    [SerializeField]
    CanvasGroup _followerRootCanvasGroup;

    [SerializeField]
    Vector2 _visibleScreenMargin = new Vector2(50, 50);

    RectTransform _followerRootParent;

    Vector2 _uiOffset;

    Vector3 _lastCameraPos;
    float _lastCamFov;
    Vector3 _lastFollowTargetPosition;
    Vector2 _lastAnchoredPositionOffset;

    bool _prevShouldShow;
    float _alphaFadeStartAt;

    // 성능을 위해..
    Camera _worldCamCache;
    Camera _uiCamCache;

    protected virtual bool ShouldShow => true;
    protected virtual Vector2 AnchoredPositionOffset => default;

    protected virtual void OnHudUpdated() { }
    protected virtual void OnPersistentUpdate() { }
    protected virtual void OnBecomeTransparent() { }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        var uiArg = arg as WorldFollowArg;
        if (uiArg == null)
        {
            TEMP_Logger.Err($"Wrong argument : {arg}");
            Hide();
            return;
        }

        // 근데, 이 사이에 만약 Pooling 돼서 새로운 Entity 로 재생성되는 미친 이벤트는
        // 없겟지? 일단 이정도 처리면 비동기로인한 예외처리 되지않을까 싶음.. 일단ㅇㅋ
        if (!uiArg.followTarget || uiArg.followTarget.gameObject.activeSelf == false)
        {
            Hide();
            return;
        }

        _worldCamCache = CameraManager.Instance.MainCam;
        _uiCamCache = CameraManager.Instance.GetUICamera(SortPolicy.layer);

        _followerRootParent = _followerRoot.parent as RectTransform;

        // 컬링용 canvasGroup 세팅 , 근데 만약
        // 설정된 tweenCavnasGroup 이 follower 과 같다면 굳이
        // add 할 필요가 없고 이걸 그대로 쓰면 되는 로직
        // (canvasGroup 의 alpha 가 0 이면 유니티에서 애초에 gpu 연산에 포함도 안하는
        // 이득때문에 SetActive(false) 이나 Position 을 max 로 보내거나 등 보다 더 나을듯 참고)
        // * 근데 , 해당 오브젝트에 tween 중 alpha 가 들어가면 경합이 발생할수도 있을거같긴는 한데.. *
        if (!_followerRootCanvasGroup)
        {
            if (_tweenCanvasGroup && (_tweenCanvasGroup.gameObject == _followerRoot.gameObject))
            {
                _followerRootCanvasGroup = _tweenCanvasGroup;
            }
            else
            {
                _followerRootCanvasGroup = _followerRoot.gameObject.GetComponent<CanvasGroup>();
            }
        }

        // 초기에는 일단 안보이게 하고 Visible/Position 등 체크해서 실제로 보이는 시점에 On
        if (_followerRootCanvasGroup)
        {
            _followerRootCanvasGroup.alpha = 0;
        }

        _followTarget = uiArg.followTarget;
        _uiOffset = uiArg.uiOffsetPos;

        DoUpdate();

        UpdateManager.Instance.RegisterSingleLateUpdatable(this);
    }

    public override void OnHide(UIArgBase arg)
    {
        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingleLateUpdatable(this);

        base.OnHide(arg);

        _worldCamCache = null;
        _uiCamCache = null;
        _followTarget = null;
        _uiOffset = default;
        _lastCameraPos = default;
        _lastCamFov = 0;
        _lastFollowTargetPosition = default;
        _lastAnchoredPositionOffset = default;
    }

    // protected virtual void LateUpdate()
    // 실제 호출은 Late Update
    void IUpdatable.OnUpdate()
    {
        if (IsEnabled == false)
        {
            return;
        }

        if (!_followTarget || _followTarget.gameObject.activeInHierarchy == false)
        {
            Hide();
        }
        else
        {
            if (DoUpdate())
            {
                OnHudUpdated();
            }
            else
            {
                OnBecomeTransparent();
            }
        }

        OnPersistentUpdate();
    }

    bool DoUpdate()
    {
        bool shouldShow = ShouldShow;
        bool prevShouldShow = _prevShouldShow;
        _prevShouldShow = shouldShow;

        if (_followerRootCanvasGroup)
        {
            if (shouldShow == false && _followerRootCanvasGroup.alpha <= 0)
            {
                return false;
            }

            float preferAlpha = _followerRootCanvasGroup.alpha;

            if (shouldShow)
            {
                preferAlpha = 1f;
            }
            else
            {
                if (prevShouldShow)
                    _alphaFadeStartAt = Time.time + 1;

                bool isFading = Time.time >= _alphaFadeStartAt;
                if (isFading)
                    preferAlpha = _followerRootCanvasGroup.alpha - 1 * Time.deltaTime;
            }

            if (preferAlpha > 0f)
            {
                if (IsVisible(out var screenPos))
                {
                    UpdatePosition(screenPos);
                }
                else
                {
                    preferAlpha = 0f;
                }
            }

            _followerRootCanvasGroup.alpha = preferAlpha;

            return preferAlpha > 0;
        }
        else
        {
            if (IsVisible(out var screenPos))
            {
                UpdatePosition(screenPos);
                return true;
            }
            return false;
        }
    }

    protected bool IsVisible(out Vector3 screenPos)
    {
        return UIHelper.IsWorldPositionVisible(
            _worldCamCache,
            _followTarget.position,
            _visibleScreenMargin,
            out screenPos);
    }

    void UpdatePosition(Vector2 screenPosition)
    {
        var camPos = _worldCamCache.transform.position;
        var targetPos = _followTarget.position;
        float fov = _worldCamCache.fieldOfView;

        if (_lastCameraPos == camPos &&
            _lastCamFov == fov &&
            _lastFollowTargetPosition == targetPos &&
            _lastAnchoredPositionOffset == AnchoredPositionOffset)
        {
            return;
        }

        _lastCameraPos = camPos;
        _lastCamFov = fov;
        _lastFollowTargetPosition = targetPos;
        _lastAnchoredPositionOffset = AnchoredPositionOffset;

        var anchoredPos = UIHelper.GetAnchorPositionFromScreenPos(
            _uiCamCache,
            screenPosition,
            _followerRootParent);

        _followerRoot.anchoredPosition = anchoredPos + _uiOffset + AnchoredPositionOffset;
    }
}
