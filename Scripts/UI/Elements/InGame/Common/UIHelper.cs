using System;
using UnityEngine;
using UnityEngine.UI;

public static class UIHelper
{
    public static bool IsWorldPositionVisible(
        Camera worldCam,
        Vector3 worldPos,
        Vector2 visibleMargin,
        out Vector3 screenPos)
    {
        screenPos = worldCam.WorldToScreenPoint(worldPos);

        // Visible 체크
        bool isVisible = screenPos.z > 0 &&
            screenPos.x > -visibleMargin.x &&
            screenPos.x < Screen.width + visibleMargin.x &&
            screenPos.y > -visibleMargin.y &&
            screenPos.y < Screen.height + visibleMargin.y;

        return isVisible;
    }

    public static Vector2 GetAnchorPositionFromWorldPosition(
        Camera worldcam,
        Camera uiCam,
        Vector3 worldPos,
        RectTransform parentRectTransform)
    {
        var screenPos = worldcam.WorldToScreenPoint(worldPos);
        return GetAnchorPositionFromScreenPos(uiCam, screenPos, parentRectTransform);
    }

    public static Vector2 GetAnchorPositionFromScreenPos(
        Camera uiCam,
        Vector2 screenPos,
        RectTransform parentRectTransform)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRectTransform,
                    screenPos,
                    uiCam,
                    out var resultPos);

        return resultPos;
    }
}
