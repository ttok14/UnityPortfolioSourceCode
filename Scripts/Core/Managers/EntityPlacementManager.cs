using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class EntityPlacementManager : SingletonBase<EntityPlacementManager>
{
    EntityPlacementGhost _ghostController;

    EntityPlacementMode _mode;
    Plane _terrainPlane;

    Vector2 _lastDraggingScreenPosition;
    const float SqrGhostPositionUpdateThreshold = 3f * 3f;

    EntityPlacementNavigation _navigationHud;

    // Coroutine _routineCo;
    CancellationTokenSource _routineCancellationTokenSrc;

    public override void Initialize()
    {
        base.Initialize();
        _terrainPlane = new Plane(Vector3.up, Vector3.zero);
    }

    public void StartPlacement(uint modelId)
    {
        StopRoutine();

        InGameManager.Instance.EventListener += OnInGameEvent;
        EventManager.Instance.Register(GLOBAL_EVENT.USER_INPUT, OnUserInput, GLOBAL_EVENT_PRIORITY.Medium);

        _routineCancellationTokenSrc = new CancellationTokenSource();
        PlacementRoutineAsync(modelId, _routineCancellationTokenSrc.Token).Forget();
        // _routineCo = CoroutineRunner.Instance.RunCoroutine(PlacementRoutineCo(modelId));
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.Enter)
        {
            StopRoutine();
        }
    }

    //public void CancelPlacement()
    //{
    //    StopRoutine();
    //}

    async UniTaskVoid PlacementRoutineAsync(uint modelId, CancellationToken token)
    {
        if (_mode != EntityPlacementMode.None)
        {
            // TODO: 뭔가 정리해줘야할게있나?
            // TODO : UNItask 로 바꾸면서 여기서 대기탈까 다 탈출할때까지? 이런 케이스는 어케 처리하는게 정석이누?
        }

        if (_navigationHud != null && _navigationHud.IsEnabled)
        {
            _navigationHud.Hide();
        }

        var viewRay = CameraManager.Instance.MainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 position;

        if (_terrainPlane.Raycast(viewRay, out var enter))
            position = viewRay.GetPoint(enter);
        else
        {
            // 여기걸릴일은 사실상 없긴함.
            DoClean();
            return;
        }

        var isCancelled = await _ghostController.ShowGhostCo(modelId, position, token).SuppressCancellationThrow();
        if (isCancelled)
            return;

        var navigation = await UIManager.Instance.ShowAsync<EntityPlacementNavigation>(UITrigger.Default, arg: new EntityPlacementNavigation.Arg()
        {
            entityTid = modelId,
            initialCanPlace = _ghostController.CanPlace,
            initialWorldPos = position,
            rotateHandler = DoRotateGhost,
            onResultReceived = OnNavigationResultReceived
        });

        if (token.IsCancellationRequested)
            return;

        _navigationHud = navigation;

        SetMode(EntityPlacementMode.ControlGhost);

        UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_DynamicInformative, "드래그 하세요");

        while (_mode != EntityPlacementMode.None)
        {
            bool isCanelled = await UniTask.Yield(token).SuppressCancellationThrow();
            if (isCancelled)
                return;
        }

        DoClean();
    }

    void StopRoutine()
    {
        if (_routineCancellationTokenSrc != null)
        {
            DoClean();
            _routineCancellationTokenSrc.Cancel();
            _routineCancellationTokenSrc.Dispose();
            _routineCancellationTokenSrc = null;
        }

        //if (_routineCo != null)
        //{
        //    DoClean();
        //    CoroutineRunner.Instance.Stop(_routineCo);
        //    _routineCo = null;
        //}
    }

    void DoClean()
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;
        EventManager.Instance.Unregister(GLOBAL_EVENT.USER_INPUT, OnUserInput, GLOBAL_EVENT_PRIORITY.Medium);

        _mode = EntityPlacementMode.None;
        UIToastSystem.Hide(UIToastSystem.ToastType.Center_DynamicInformative);
        _lastDraggingScreenPosition = Vector3.zero;
        if (_navigationHud && _navigationHud.IsEnabled)
        {
            _navigationHud.Hide();
        }
        _navigationHud = null;
        _ghostController.Hide();
    }

    private void OnNavigationResultReceived(EntityPlacementNavigationResult res)
    {
        if (res == EntityPlacementNavigationResult.Confirm)
        {
            var costData = DBPurchaseCost.GetByEntityID(_ghostController.ModelID);
            Me.SpendCurrency(costData.CostCurrencyType, (int)costData.CostPrice);

            var pos = _ghostController.ModelPosition;
            var scale = _ghostController.VolumeRadius * 2.5f;

            EntityManager.Instance.CreateEntityCallBack(new EntityObjectData(
                _ghostController.ModelPosition,
                _ghostController.ModelEulerY,
                _ghostController.ModelID,
                EntityTeamType.Player), onCompleted: (res) =>
                {
                    FXSystem.PlayFX("FX_Trans", startPosition: pos, scale: scale);

                    InGameManager.Instance.PublishEvent(InGameEvent.EntityConstructed, new EntityConstructedEventArg()
                    {
                        Entity = res
                    });
                }).Forget();

            AudioManager.Instance.Play("SFX_ConstructionBuild");
        }
        else if (res == EntityPlacementNavigationResult.Cancel)
        {
        }

        StopRoutine();
    }

    void SetMode(EntityPlacementMode newMode)
    {
        _mode = newMode;

        _lastDraggingScreenPosition = Vector3.zero;
    }

    private void DoRotateGhost()
    {
        _ghostController.DoUpdate_Rotate(90);

        if (_navigationHud)
            _navigationHud.CanPlace = _ghostController.CanPlace;
    }

    public override void Release()
    {
        base.Release();

        if (_ghostController != null)
        {
            _ghostController.Release();
            _ghostController = null;
        }
    }

    public async UniTask PrepareGame()
    {
        _ghostController = new EntityPlacementGhost();
        await _ghostController.Initialize();
    }

    private void OnUserInput(EventContext cxt)
    {
        cxt.Use();

        if (_mode != EntityPlacementMode.ControlGhost)
            return;

        var arg = cxt.Arg as InputEventBaseArg;

        if (/* 이거하면 클릭하는 순간 모델 이동돼서 네비 버튼 클릭안댐. workaround 필요
             * arg.InputType == UserInputType.FirstPressDown || */ arg.InputType == UserInputType.Dragging)
        {
            // 자체쓰로틀링 최적화 처리
            if (arg.InputType == UserInputType.Dragging &&
                Vector2.SqrMagnitude(_lastDraggingScreenPosition - arg.ScreenPosition) < SqrGhostPositionUpdateThreshold)
                return;

            if (_navigationHud.gameObject.activeSelf)
                _navigationHud.gameObject.SetActive(false);

            _lastDraggingScreenPosition = arg.ScreenPosition;

            var ray = CameraManager.Instance.MainCam.ScreenPointToRay(arg.ScreenPosition);

            if (_terrainPlane.Raycast(ray, out var enter))
            {
                var newPos = ray.GetPoint(enter);

                _ghostController.DoUpdate_NewPos(newPos);
                _navigationHud.CanPlace = _ghostController.CanPlace;

                // cxt.Use();
            }
        }
        else if (arg.InputType == UserInputType.PressUp)
        {
            if (_navigationHud.gameObject.activeSelf == false)
                _navigationHud.gameObject.SetActive(true);

            _navigationHud.SetEntityPosition(_ghostController.ModelPosition);
        }
    }
}
