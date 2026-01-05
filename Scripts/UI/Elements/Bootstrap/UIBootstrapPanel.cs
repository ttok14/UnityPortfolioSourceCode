using DG.Tweening;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

[UIAttribute("UIBootstrapPanel")]
public class UIBootstrapPanel : UIBase
{
    public override void OnShow(UITrigger trigger, UIArgBase arg = null)
    {
        base.OnShow(trigger, arg);
        transform.localPosition = default;
    }

    public override async UniTask Exit()
    {
        var tween = RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), Constants.UI_EnterExit_Duration)
            .SetRelative(true)
            .SetEase(Ease.InOutSine);

        await tween.AsyncWaitForCompletion();
    }
}
