using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[UIAttribute("UILobbyPanel")]
public class UILobbyPanel : UIBase
{
    [SerializeField]
    List<GameObject> activatelistOnEnter;

    public override void OnShow(UITrigger trigger, UIArgBase arg = null)
    {
        base.OnShow(trigger, arg);
        RectTf.localPosition = new Vector3(RectTf.rect.width, 0, 0);
        activatelistOnEnter.ForEach(t => t.SetActive(false));
    }

    public override async UniTask Enter()
    {
        var tween = RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), Constants.UI_EnterExit_Duration)
            .SetRelative(true)
            .SetEase(Ease.InOutSine);

        await tween.AsyncWaitForCompletion();

        activatelistOnEnter.ForEach(t => t.SetActive(true));

        Debug.Log("로비 연출 끝 !");
    }

    public void OnClickGoInGame()
    {
        CoroutineRunner.Instance.RunCoroutine(GoInGame());
    }

    IEnumerator GoInGame()
    {
        yield return GameManager.Instance.FSM.TransitionController.TransitionStateWithLoading(GameState.InGame, new LoadSimulationProcessor());
    }

    public override async UniTask Exit()
    {
        var tween = RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), Constants.UI_EnterExit_Duration)
            .SetRelative(true)
            .SetEase(Ease.InOutSine);
        await tween.AsyncWaitForCompletion();
    }

    //public override async Task Enter()
    //{
    //    await _rectTransform.DOAnchorPos(-new Vector2(_rectTransform.rect.width, 0), 0.7f)
    //                .SetRelative(true)
    //                .SetEase(Ease.InOutSine)
    //                .AsyncWaitForCompletion();
    //}

    //public override async Task Exit()
    //{
    //    await _rectTransform.DOAnchorPos(-new Vector2(_rectTransform.rect.width, 0), 0.7f)
    //                .SetRelative(true)
    //                .SetEase(Ease.InOutSine)
    //                .AsyncWaitForCompletion();
    //}
}
