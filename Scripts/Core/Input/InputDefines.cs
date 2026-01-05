using System;
using UnityEngine;

public enum UserInputType
{
    None = 0,
    FirstPressDown,
    SecondPressDown,
    PressUp,
    DragBegin,
    Dragging,
    DragEnd,
    MouseScroll,
    PinchingBegin,
    Pinching,
    PinchingEnd,
}

public abstract class InputEventBaseArg : EventArgBase
{
    public abstract UserInputType InputType { get; }
    public virtual Vector2 ScreenPosition { get; private set; }
    protected InputEventBaseArg(Vector2 screenPosition)
    {
        ScreenPosition = screenPosition;
    }
}

public class InputPrimaryPressDownEventData : InputEventBaseArg
{
    public Lazy<Vector3> WorldPickingPosition;

    public override UserInputType InputType => UserInputType.FirstPressDown;

    public InputPrimaryPressDownEventData(Vector2 screenPosition, Lazy<Vector3> worldPickingPosition) : base(screenPosition)
    {
        WorldPickingPosition = worldPickingPosition;
    }
}

public class InputPrimaryPressUpEventData : InputEventBaseArg
{
    public bool OtherModeAlreadyTriggered;

    public override UserInputType InputType => UserInputType.PressUp;

    public InputPrimaryPressUpEventData(Vector2 screenPosition, bool otherModeAlreadyTriggered) : base(screenPosition)
    {
        OtherModeAlreadyTriggered = otherModeAlreadyTriggered;
    }
}

public class InputDragBeginEventData : InputEventBaseArg
{
    public override UserInputType InputType => UserInputType.DragBegin;

    public InputDragBeginEventData(Vector2 screenPosition) : base(screenPosition)
    {
    }
}

public class InputDraggingEventData : InputEventBaseArg
{
    public Vector2 Delta;

    public override UserInputType InputType => UserInputType.Dragging;

    public InputDraggingEventData(Vector3 screenPosition, Vector2 delta) : base(screenPosition)
    {
        Delta = delta;
    }
}

public class InputDragEndEventData : InputEventBaseArg
{
    public override UserInputType InputType => UserInputType.DragEnd;

    public InputDragEndEventData(Vector2 screenPosition) : base(screenPosition)
    {
    }
}


public class InputMouseScrollEventData : InputEventBaseArg
{
    public float Delta;

    public override UserInputType InputType => UserInputType.MouseScroll;

    public InputMouseScrollEventData(Vector2 screenPosition, float delta) : base(screenPosition)
    {
        Delta = delta;
    }
}

//public class InputPinchingBeginEventData : InputEventBaseArg
//{
//    public Vector2 PrimaryScreenPosition;
//    public Vector2 SecondaryScreenPosition;
//    public Vector2 Distance;

//    public override UserInputType inputType => UserInputType.Pinching;
//}

public class InputPinchingEventData : InputEventBaseArg
{
    public float Difference;

    public InputPinchingEventData(Vector2 screenPosition, float difference) : base(screenPosition)
    {
        Difference = difference;
    }

    public override UserInputType InputType => UserInputType.Pinching;
}

//public class InputPinchingEndEventData : InputEventBaseArg
//{
//    // 필요한게 잇을까?

//    public override UserInputType inputType => UserInputType.PinchingEnd;
//}
