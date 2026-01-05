using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(nameof(UITransCommonProgressElement))]
public class UITransCommonProgressElement : UICommonProgressElement
{
    [SerializeField]
    CanvasGroup _canvasGroup;

    public void SetAlpha(float alpha)
    {
        _canvasGroup.alpha = alpha;
    }
}
