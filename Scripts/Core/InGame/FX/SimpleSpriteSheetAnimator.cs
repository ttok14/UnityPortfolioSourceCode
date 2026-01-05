using System;
using UnityEngine;

public class SimpleSpriteSheetAnimator : FXBase
{
    [Serializable]
    public class Random
    {
        [Header("Min/Max 가 같으면 고정값됨")]
        public float Min;
        public float Max;
        public float Next()
        {
            if (Min == Max)
            {
                return Min;
            }

            return UnityEngine.Random.Range(Min, Max);
        }
    }

    public enum BillboardOption
    {
        None = 0,

        Always,
        First
    }

    // public override FXType Type => FXType.SpriteSheet;

    [SerializeField]
    public string _sourceSpriteSheetKey;
    [SerializeField]
    public int _spriteCnt;

    [Header("-1 은 무한 반복"), SerializeField]
    public int _playCount = 1;

    [SerializeField]
    public Random _totalAnimationDuration;

    [SerializeField]
    public Random _rotPerPlay;

    [SerializeField]
    public BillboardOption _billboardOption;

    //------------------------------------------//

    Sprite[] _sprites;

    SpriteRenderer _renderer;

    bool _allLoaded = false;

    float _changeSpriteInterval;

    int _currentSpriteIdx;
    float _changeSpriteAt;

    bool _hasAppliedBillboard;

    int _playedCount = 0;

    bool _exiting;

    public override bool ActivateLateUpdate => true;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (!_renderer)
            {
                TEMP_Logger.Err("Sprite Renderer Missing");
                return;
            }
        }

        if (string.IsNullOrEmpty(_sourceSpriteSheetKey))
        {
            TEMP_Logger.Err($"Sprite Sheet Key Name is Invalid | {gameObject.name}");
            return;
        }

        if (_sprites != null)
        {
            TEMP_Logger.Err($"Already Initialized or Ing");
            return;
        }

        _sprites = new Sprite[_spriteCnt];

        for (int i = 0; i < _spriteCnt; i++)
        {
            int idx = i;
            AssetManager.Instance.LoadAsyncCallBack<Sprite>($"{_sourceSpriteSheetKey}[{_sourceSpriteSheetKey}_{idx}]",
                onCompleted: (res) =>
                {
                    if (!res)
                    {
                        TEMP_Logger.Err($"Sprite Sheet Slice Sprite load Failed | SourceKey : {_sourceSpriteSheetKey} , Idx : {idx}");
                        return;
                    }

                    _sprites[idx] = res;

                    if (Array.TrueForAll(_sprites, s => s))
                    {
                        _allLoaded = true;
                    }
                }).Forget();
        }
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        _exiting = false;
        _currentSpriteIdx = -1;
        _changeSpriteInterval = 0;
        _playedCount = 0;
        _hasAppliedBillboard = false;
        _changeSpriteAt = Time.time;
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (_allLoaded == false)
            return;

        if (Time.time >= _changeSpriteAt)
        {
            // 이때 리턴해줘야 . 끝까지 다 보일듯.
            if (_exiting)
            {
                Return();
                return;
            }

            _currentSpriteIdx++;
            if (_currentSpriteIdx >= _spriteCnt)
                _currentSpriteIdx = 0;

            Refresh();
        }
    }

    void Refresh()
    {
        if (_currentSpriteIdx == 0)
        {
            if (_playCount != -1 && _playedCount >= _playCount)
            {
                _exiting = true;
                return;
            }

            _playedCount++;

            _changeSpriteInterval = _totalAnimationDuration.Next() / _spriteCnt;

            if (_billboardOption == BillboardOption.Always ||
                (_billboardOption == BillboardOption.First && _hasAppliedBillboard == false))
            {
                _hasAppliedBillboard = true;
                transform.rotation = CameraManager.Instance.MainCam.transform.rotation;
                transform.Rotate(Vector3.forward, _rotPerPlay.Next(), Space.Self);
            }
        }

        _changeSpriteAt += _changeSpriteInterval;
        _renderer.sprite = _sprites[_currentSpriteIdx];
    }
}
