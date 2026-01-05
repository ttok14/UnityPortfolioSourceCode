using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

[UIAttribute("UILoadingPanel")]
public class UILoadingPanel : UIBase
{
    public class Arg : UIArgBase
    {
        public Func<float> progressGetter;
        public Func<string> statusGetter;

        public Arg(Func<float> progressGetter, Func<string> statusGetter)
        {
            this.progressGetter = progressGetter;
            this.statusGetter = statusGetter;
        }
    }

    [SerializeField]
    private Image loadingImg;
    [SerializeField]
    private int _loadingSpriteCount;
    [SerializeField]
    private Slider _loadingSlider;
    [SerializeField]
    private TextMeshProUGUI _loadingProgressTxt;
    [SerializeField]
    private TextMeshProUGUI _loadingStatusTxt;

    private Arg _arg;

    [SerializeField]
    private Image _loadingImg;
    private Sprite[] _aniSprites;

    Coroutine _staticAniCoroutine;
    Coroutine _progressBarCoroutine;

    bool _spriteLoadEnd;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        if (_spriteLoadEnd == false)
        {
            _loadingImg.gameObject.SetActive(false);
            _aniSprites = new Sprite[_loadingSpriteCount];
            for (int i = 0; i < _loadingSpriteCount; i++)
            {
                int idx = i;
                AssetManager.Instance.LoadAsyncCallBack<Sprite>($"UI/Sprites/Loading_Circle_{i + 1}", (sprite) =>
                {
                    _aniSprites[idx] = sprite;

                    if (idx == _loadingSpriteCount - 1)
                    {
                        _spriteLoadEnd = true;
                    }
                }).Forget();
            }
        }
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);

        _arg = arg as Arg;
        if (_arg == null || _arg.progressGetter == null || _arg.statusGetter == null)
        {
            TEMP_Logger.Err("UILoadingPanel requires a valid Arg with getters.");
        }

        RectTf.localPosition = new Vector3(RectTf.rect.width, 0, 0);

        _staticAniCoroutine = CoroutineRunner.Instance.RunCoroutine(RunStaticAnimation());
        _progressBarCoroutine = CoroutineRunner.Instance.RunCoroutine(RunProgressBar());

        //RectTf.localPosition += new Vector3(RectTf.rect.width, 0, 0);
        //RectTf.DOAnchorPos(-new Vector2(RectTf.rect.width, 0), 0.7f)
        //    .SetRelative(true)
        //    .SetEase(Ease.InOutSine);
    }

    public override void OnHide(UIArgBase arg)
    {
        if (_staticAniCoroutine != null)
        {
            CoroutineRunner.Instance.Stop(_staticAniCoroutine);
            _staticAniCoroutine = null;
        }

        if (_progressBarCoroutine != null)
        {
            CoroutineRunner.Instance.Stop(_progressBarCoroutine);
            _progressBarCoroutine = null;
        }

        base.OnHide(arg);
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

    private IEnumerator RunProgressBar()
    {
        while (true)
        {
            var currentProgress = _arg.progressGetter();
            if (currentProgress >= 1f)
            {
                break;
            }

            _loadingSlider.value = currentProgress;
            _loadingProgressTxt.text = $"{(int)(currentProgress * 100f)}%";
            _loadingStatusTxt.text = $"{_arg.statusGetter()}";

            yield return null;
        }

        _loadingSlider.value = 1f;
        _loadingProgressTxt.text = "100%";
        _loadingStatusTxt.text = _arg.statusGetter();
    }

    private IEnumerator RunStaticAnimation()
    {
        int idx = 0;

        while (true)
        {
            if (_spriteLoadEnd == false)
                yield return null;

            yield return new WaitForSeconds(0.15f);

            loadingImg.sprite = _aniSprites[idx];

            if (_loadingImg.gameObject.activeSelf == false)
                _loadingImg.gameObject.SetActive(true);

            idx++;
            if (idx >= _loadingSpriteCount)
            {
                idx = 0;
            }
        }
    }
}
