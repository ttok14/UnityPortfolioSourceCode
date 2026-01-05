using UnityEngine;
using DG.Tweening;
using System;
using static TweenSequenceData.TweenStep;
using System.Collections;
using Cysharp.Threading.Tasks;

public class TweenSequenceRunner : MonoBehaviour
{
    [SerializeField]
    private bool _playOnAwake = false;
    public bool PlayOnAwake => _playOnAwake;

    //[SerializeField]
    //private TweenSequenceData _data;
    [Header("디폴트로 사용할 TweenSequenceData 키값. 인자없이 호출시 이 버퍼의 순서대로 반복하면서 호출"), SerializeField]
    private string[] _tweenSequenceDataBuffer;
    private int _currentSeqDataBufferIdx;

    [Header("시퀀스 플레이가 끝난 후 자동으로 대기 후 역실행 여부"), SerializeField]
    private float _playBackwardsInDelay = -1f;

    private Sequence _sequence;

    //--- 실제 타겟 Components (gameObject 고정) ---//
    CanvasGroup _canvasGroupCache;
    Renderer _rendererCache;

    //--- MPB용 캐시 ---//
    private MaterialPropertyBlock _mpb;

    public bool IsPlaying => _sequence != null && _sequence.IsPlaying();

    private RectTransform _rectTransformCache;

    private void Awake()
    {
        _rectTransformCache = transform as RectTransform;

        if (_playOnAwake)
        {
            //if (_data)
            if (_tweenSequenceDataBuffer != null)
            {
                var firstDataKey = Array.Find(_tweenSequenceDataBuffer, t => string.IsNullOrEmpty(t) == false);
                if (string.IsNullOrEmpty(firstDataKey) == false)
                {
                    InitialPlay(firstDataKey).Forget();
                }
            }
        }
    }

    async UniTaskVoid InitialPlay(string dataKey)
    {
        var data = await AssetManager.Instance.LoadAsync<TweenSequenceData>(dataKey);
        IncreaseSeqBufferIdx();
        await PlaySequenceAsync(data);
    }

    void IncreaseSeqBufferIdx()
    {
        _currentSeqDataBufferIdx++;
        if (_currentSeqDataBufferIdx >= _tweenSequenceDataBuffer.Length)
        {
            _currentSeqDataBufferIdx = 0;
        }
    }

    public async UniTaskVoid PlaySequenceAsyncCallBack(TweenSequenceData data = null, Action onCompleted = null)
    {
        await PlaySequenceAsync(data);
        onCompleted?.Invoke();
    }

    public async UniTask PlaySequenceAsync(TweenSequenceData data = null)
    {
        Release();

        if (data == null)
        {
            if (_tweenSequenceDataBuffer != null &&
                _currentSeqDataBufferIdx < _tweenSequenceDataBuffer.Length &&
                string.IsNullOrEmpty(_tweenSequenceDataBuffer[_currentSeqDataBufferIdx]) == false)
            {
                var loaded = await AssetManager.Instance.LoadAsync<TweenSequenceData>(_tweenSequenceDataBuffer[_currentSeqDataBufferIdx]);

                IncreaseSeqBufferIdx();
                data = loaded;
            }
        }

        if (data == null)
        {
            TEMP_Logger.Err($"Data is invalid");
            return;
        }

        _sequence = DOTween.Sequence()
            .SetAutoKill(data.AutoKill)
            .SetEase(data.SequenceEase)
            .SetLoops(data.Loops, data.LoopType);

        foreach (var step in data.Steps)
        {
            Tween tween = CreateTweenFromStep(step);
            switch (step.StepType)
            {
                case E_StepType.Append:
                    if (tween != null) _sequence.Append(tween);
                    break;
                case E_StepType.Join:
                    if (tween != null) _sequence.Join(tween);
                    break;
                case E_StepType.Interval:
                    _sequence.AppendInterval(step.Duration);
                    break;
                default:
                    break;
            }
        }

        data.OnStarted.Invoke();
        _sequence.Play();

        await _sequence.AsyncWaitForCompletion();

        if (_playBackwardsInDelay > 0f)
        {
            await UniTask.WaitForSeconds(_playBackwardsInDelay);
            _sequence.PlayBackwards();
            await _sequence.AsyncWaitForCompletion();
        }

        if (data.AutoKill)
        {
            Release(true);
        }

        data.OnFinished.Invoke();
    }

    private Tween CreateTweenFromStep(TweenSequenceData.TweenStep step)
    {
        Tween t = null;

        switch (step.TargetType)
        {
            case E_TweenTargetType.Move:
                {
                    if (_rectTransformCache)
                    {
                        t = _rectTransformCache.DOAnchorPos(step.ToValue, step.Duration);
                    }
                    else
                    {
                        t = transform.DOMove(step.ToValue, step.Duration);
                    }
                }
                break;
            case E_TweenTargetType.Rotate:
                t = gameObject.transform.DORotate(step.ToValue, step.Duration);
                break;
            case E_TweenTargetType.Scale:
                t = gameObject.transform.DOScale(step.ToValue, step.Duration);
                break;
            case E_TweenTargetType.UIAlpha:
                if (_canvasGroupCache == null)
                    _canvasGroupCache = gameObject.GetComponent<CanvasGroup>();

                if (_canvasGroupCache)
                    t = _canvasGroupCache.DOFade(step.ToColor.a, step.Duration);
                else
                    TEMP_Logger.Err($"Failed to get CanvasGroup for AlphaTween !");
                break;

            case E_TweenTargetType.Color:
                if (_rendererCache == null)
                    _rendererCache = gameObject.GetComponent<Renderer>();

                if (_rendererCache == null)
                {
                    TEMP_Logger.Err("$Failed to get Renderer for RendererColorTween on " + gameObject.name + "!");
                    break;
                }

                int propertyID = -1;

                // MPB 캐시 (없으면 생성)
                if (_mpb == null)
                    _mpb = new MaterialPropertyBlock();

                // 렌더러의 현재 MPB 로드
                _rendererCache.GetPropertyBlock(_mpb);

                // 1순위: _Color (빌트인 기본값) 시도
                int idColor = Shader.PropertyToID("_Color");
                if (_rendererCache.sharedMaterial.HasProperty(idColor))
                {
                    propertyID = idColor;
                }
                // 2순위: _BaseColor
                else
                {
                    int idBaseColor = Shader.PropertyToID("_BaseColor");
                    if (_rendererCache.sharedMaterial.HasProperty(idBaseColor))
                    {
                        propertyID = idBaseColor;
                    }
                    // 3순위: 기본값 모두 실패 -> 사용자 지정값 확인 (step.ColorPropertyName 필드가 있다고 가정)
                    else if (!string.IsNullOrEmpty(step.FallBackColorPropertyName))
                    {
                        propertyID = Shader.PropertyToID(step.FallBackColorPropertyName);
                    }
                }

                // 4순위: 최종 ID 확인 및 에러 처리
                if (propertyID == -1)
                {
                    TEMP_Logger.Err(
                        $"[Color Tween Fail] Renderer on {gameObject.name} does not have standard ('_Color' or '_BaseColor') color property. " +
                        $"Please set the exact shader property name in TweenStep.ColorPropertyName in the Inspector."
                    );
                    break; // 트윈 생성 중단
                }

                Color currentColor = _mpb.HasColor(propertyID)
                    ? _mpb.GetColor(propertyID)
                    : _rendererCache.sharedMaterial.GetColor(propertyID);

                t = DOTween.To(
                    // Getter: 트윈 시작 시 현재 Color 값을 MPB 또는 Material에서 읽어옴
                    () => _mpb.HasColor(propertyID) ? _mpb.GetColor(propertyID) : _rendererCache.sharedMaterial.GetColor(propertyID),

                    // Setter: 트윈 진행 중 새로운 Color 값이 들어오면 MPB에 설정하고 렌더러에 적용
                    (newColor) =>
                    {
                        _mpb.SetColor(propertyID, newColor);
                        _rendererCache.SetPropertyBlock(_mpb);
                    },
                    step.ToColor,
                    step.Duration
                );
                break;

            case E_TweenTargetType.Custom:
                TEMP_Logger.Err($"NOT IMPLEMENTED {step.StepType}");
                break;
            default:
                TEMP_Logger.Err($"NOT IMPLEMENTED {step.StepType}");
                break;
        }

        if (t != null)
        {
            t.SetEase(step.EaseType)
                .SetDelay(step.Delay);

            if (step.FromOrTo == E_FromTo.From)
            {
                ((Tweener)t).From(step.IsRelative);
            }
            else if (step.FromOrTo == E_FromTo.To)
            {
                t.SetRelative(step.IsRelative);
            }
        }

        return t;
    }

    private void OnDestroy()
    {
        Release();
    }

    public void Release(bool complete = false)
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill(complete);
        }

        _sequence = null;
    }
}
