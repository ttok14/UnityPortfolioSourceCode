using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class InputManager : SingletonBase<InputManager>
{
    InputActions _inputActions;

    private bool _isPrimaryPressed;
    private bool _isSecondaryPressed;
    private bool _isDragging;
    private bool _isPinching;

    private Vector2 _dragStartPosition;
    private float _dragThreshold = 2f;
    private float _totalDragDistance = 0f;

    private float _previousPinchDistance = 0f;
    public Vector2 ScreenPosition => _inputActions.Player.ScreenPosition.ReadValue<Vector2>();

    bool _isPointerOverUI;

    private int _blockEventCount;
    public int BlockEventCount
    {
        get => _blockEventCount;
        set
        {
            int prev = _blockEventCount;
            _blockEventCount = Mathf.Max(value, 0);

            if (prev > 0 && _blockEventCount == 0)
            {
                ResetAllStates();
                _inputActions.Enable();
                EnhancedTouchSupport.Enable();
            }
            else if (prev == 0 && _blockEventCount > 0)
            {
                _inputActions.Disable();
                EnhancedTouchSupport.Disable();
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _inputActions = new InputActions();

        _inputActions.Player.PrimaryContact.started += OnPrimaryPressed;
        _inputActions.Player.PrimaryContact.canceled += OnPrimaryReleased;
        _inputActions.Player.SecondaryContact.started += OnSecondaryPressed;
        _inputActions.Player.SecondaryContact.canceled += OnSecondaryReleased;
        _inputActions.Player.Delta.performed += OnDeltaPerformed;
        _inputActions.Player.Scroll.performed += OnScrollPerformed;

        _blockEventCount = 0;
    }

    private void OnPrimaryPressed(InputAction.CallbackContext context)
    {
        _isPointerOverUI = IsCurrentPointerOverUI(ScreenPosition);
        if (_isPointerOverUI)
            return;

        _isPrimaryPressed = true;
        _dragStartPosition = ScreenPosition;
        _totalDragDistance = 0f;

        EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
            new InputPrimaryPressDownEventData(
                ScreenPosition,
                new Lazy<Vector3>(() => MapUtils.ScreenPosToWorldPos(
                    CameraManager.Instance.MainCam, ScreenPosition).Value)));
    }

    private void OnPrimaryReleased(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
        {
            _isPointerOverUI = false;
            return;
        }

        bool wasDraggingOrPinching = _isDragging || _isPinching;

        EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
            new InputPrimaryPressUpEventData(ScreenPosition, wasDraggingOrPinching));

        _isPrimaryPressed = false;

        if (_isDragging)
        {
            EndDrag();
        }

        if (_isPinching && _isSecondaryPressed == false)
        {
            EndPinch();
        }
    }

    private void OnSecondaryPressed(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        _isSecondaryPressed = true;

        // 양쪽 다 눌리면 핀치 시작
        if (_isPrimaryPressed)
        {
            StartPinch();
        }
    }

    private void OnSecondaryReleased(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        _isSecondaryPressed = false;

        if (_isPinching)
        {
            EndPinch();
        }
    }

    private void OnDeltaPerformed(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        Vector2 delta = context.ReadValue<Vector2>();
        float deltaMagnitude = delta.magnitude;

        // Threshold 미만이면 무시
        if (deltaMagnitude < _dragThreshold)
        {
            return;
        }

        // 핀치 중이면 핀치 처리
        if (_isPinching)
        {
            ProcessPinch(deltaMagnitude);
            return;
        }

        // Primary만 눌린 상태면 드래그 처리
        if (_isPrimaryPressed && _isSecondaryPressed == false)
        {
            ProcessDrag(delta, deltaMagnitude);
        }
    }

    private void ProcessDrag(Vector2 delta, float deltaMagnitude)
    {
        if (_isPointerOverUI)
            return;

        _totalDragDistance += deltaMagnitude;

        // 드래그 시작
        if (_isDragging == false)
        {
            _isDragging = true;
            EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
                new InputDragBeginEventData(ScreenPosition));
        }
        // 드래그 중
        else
        {
            EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
                new InputDraggingEventData(ScreenPosition, delta));
        }
    }

    private void EndDrag()
    {
        if (_isPointerOverUI)
            return;

        if (_isDragging == false)
            return;

        EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
            new InputDragEndEventData(ScreenPosition));

        _isDragging = false;
        _totalDragDistance = 0f;
    }

    private void StartPinch()
    {
        _isPinching = true;
        _isDragging = false; // 드래그 중이었다면 취소

        var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (touches.Count >= 2)
        {
            _previousPinchDistance = Vector2.Distance(
                touches[0].screenPosition,
                touches[1].screenPosition);
        }
    }

    private void ProcessPinch(float movementMagnitude)
    {
        if (_isPinching == false) return;

        var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (touches.Count < 2) return;

        float currentDistance = Vector2.Distance(
            touches[0].screenPosition,
            touches[1].screenPosition);

        float distanceDelta = currentDistance - _previousPinchDistance;
        int sign = distanceDelta > 0 ? 1 : -1;

        EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
            new InputPinchingEventData(ScreenPosition, movementMagnitude * sign));

        _previousPinchDistance = currentDistance;
    }

    private void EndPinch()
    {
        if (_isPinching == false)
            return;

        _isPinching = false;
        _previousPinchDistance = 0f;
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        if (_isDragging || _isPinching) return;

        float scrollDelta = context.ReadValue<Vector2>().y;
        EventManager.Instance.Publish(GLOBAL_EVENT.USER_INPUT,
            new InputMouseScrollEventData(ScreenPosition, scrollDelta));
    }

    public static bool IsCurrentPointerOverUI(Vector2 screenPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    private void ResetAllStates()
    {
        _isPrimaryPressed = false;
        _isSecondaryPressed = false;
        _isDragging = false;
        _isPinching = false;
        _dragStartPosition = Vector2.zero;
        _totalDragDistance = 0f;
        _previousPinchDistance = 0f;
    }

    public override void Release()
    {
        base.Release();

        _inputActions.Player.PrimaryContact.started -= OnPrimaryPressed;
        _inputActions.Player.PrimaryContact.canceled -= OnPrimaryReleased;
        _inputActions.Player.SecondaryContact.started -= OnSecondaryPressed;
        _inputActions.Player.SecondaryContact.canceled -= OnSecondaryReleased;
        _inputActions.Player.Delta.performed -= OnDeltaPerformed;
        _inputActions.Player.Scroll.performed -= OnScrollPerformed;

        _inputActions.Disable();
        EnhancedTouchSupport.Disable();
    }
}
