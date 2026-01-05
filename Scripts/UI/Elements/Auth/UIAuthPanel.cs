using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

[UIAttribute("UIAuthPanel")]
public class UIAuthPanel : UIBase
{
    [SerializeField]
    private RectTransform _title;

    public override void OnShow(UITrigger trigger, UIArgBase arg = null)
    {
        base.OnShow(trigger, arg);

        _title.DOAnchorPosY(-50, 1)
            .SetRelative(true)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        RectTf.localPosition = new Vector3(RectTf.rect.width, 0, 0);
    }

    public void OnGoLobbyBtnClicked()
    {
        EventManager.Instance.Publish(GLOBAL_EVENT.REQUEST_GO_LOBBY_START_ROUTINE);
    }

    public override async UniTask Enter()
    {
        var tween = RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), Constants.UI_EnterExit_Duration)
            .SetRelative(true)
            .SetEase(Ease.InOutSine);

        await tween.AsyncWaitForCompletion();
    }

    public override async UniTask Exit()
    {
        var tween = RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), Constants.UI_EnterExit_Duration)
            .SetRelative(true)
            .SetEase(Ease.InOutSine);
        await tween.AsyncWaitForCompletion();
    }
}
