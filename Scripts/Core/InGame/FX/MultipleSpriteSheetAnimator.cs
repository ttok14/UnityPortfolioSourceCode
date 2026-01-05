using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MultipleSpriteSheetAnimator : FXBase
{
    [Serializable]
    public class Element
    {
        public SpriteRenderer _renderer;

        public string animationDataKey;

        [HideInInspector]
        public SpriteAnimationData dataRef;

        [HideInInspector]
        public float _changeSpriteInterval;

        [HideInInspector]
        public int _currentSpriteIdx;
        [HideInInspector]
        public float _changeSpriteAt;

        [HideInInspector]
        public bool _hasAppliedBillboard;

        [HideInInspector]
        public int _playedCount;

        [HideInInspector]
        public Sprite[] _sprites;

        [HideInInspector]
        public bool _exiting;

        [HideInInspector]
        public Vector3 _moveDirection;
        [HideInInspector]
        public Vector3 _oriPos;

        [HideInInspector]
        public float _moveSpeed;

        [HideInInspector]
        public Quaternion _oriRotation;

        [HideInInspector]
        public float _scaleVarying;
        [HideInInspector]
        public Vector3 _oriScale;
    }

    [SerializeField]
    private List<Element> _elements;

    bool _allLoaded;

    public override bool ActivateLateUpdate => true;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        for (int i = 0; i < _elements.Count; i++)
        {
            var ts = _elements[i]._renderer.transform;

            _elements[i]._oriPos = ts.localPosition;
            _elements[i]._oriRotation = ts.localRotation;
            _elements[i]._oriScale = ts.localScale;
        }

        Load().Forget();
    }

    async UniTaskVoid Load()
    {
        for (int i = 0; i < _elements.Count; i++)
        {
            if (_elements[i]._renderer == null)
            {
                TEMP_Logger.Err($"Please link SpriteRenderer for animation | {gameObject.name}");
                Return();
                return;
            }

            if (string.IsNullOrEmpty(_elements[i].animationDataKey))
            {
                TEMP_Logger.Err($"Element Sprite Animation Data is not valid | {gameObject.name}");
                Return();
                return;
            }

            _elements[i].dataRef = await AssetManager.Instance.LoadAsync<SpriteAnimationData>(_elements[i].animationDataKey);

            if (_elements[i].dataRef == null)
            {
                TEMP_Logger.Err($"Failed to load SpriteAnimationData : {_elements[i].animationDataKey}");
                return;
            }

            _elements[i]._sprites = new Sprite[_elements[i].dataRef._spriteCnt];

            for (int j = 0; j < _elements[i].dataRef._spriteCnt; j++)
            {
                string spriteKey = _elements[i].dataRef._spriteCnt > 1 ?
                    $"{_elements[i].dataRef._sourceSpriteSheetKey}[{_elements[i].dataRef._sourceSpriteSheetKey}_{j}]"
                    : _elements[i].dataRef._sourceSpriteSheetKey;

                var loadedSprite = await AssetManager.Instance.LoadAsync<Sprite>(spriteKey);

                if (loadedSprite == null)
                {
                    TEMP_Logger.Err($"Failed to load Sprite : {_elements[i].dataRef._sourceSpriteSheetKey}");
                    return;
                }

                _elements[i]._sprites[j] = loadedSprite;
            }
        }

        _allLoaded = true;
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        foreach (var e in _elements)
        {
            e._exiting = false;
            e._currentSpriteIdx = -1;
            e._changeSpriteInterval = 0;
            e._playedCount = 0;
            e._hasAppliedBillboard = false;
            e._changeSpriteAt = Time.time;

            if (e._renderer)
            {
                e._renderer.enabled = true;
                e._renderer.transform.SetLocalPositionAndRotation(e._oriPos, e._oriRotation);
                e._renderer.transform.localScale = e._oriScale;
            }
        }
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (_allLoaded == false)
        {
            return;
        }

        bool hasAlive = false;

        foreach (var e in _elements)
        {
            if (Time.time >= e._changeSpriteAt)
            {
                if (e._exiting)
                    continue;

                e._currentSpriteIdx++;
                if (e._currentSpriteIdx >= e.dataRef._spriteCnt)
                {
                    e._currentSpriteIdx = 0;
                }

                Refresh(e);
            }

            var renTs = e._renderer.transform;

            if (e._moveSpeed != 0)
                renTs.localPosition += e._moveDirection * e._moveSpeed * Time.deltaTime;

            var scale = renTs.localScale;
            if (e._scaleVarying != 0 && scale.x < e.dataRef._maxScale)
            {
                float newScale = Mathf.Min(scale.x + e._scaleVarying * Time.deltaTime, e.dataRef._maxScale);
                scale.Set(newScale, newScale, newScale);
                renTs.localScale = scale;
            }

            hasAlive = true;
        }

        if (hasAlive == false)
            Return();
    }

    void Refresh(Element elem)
    {
        // 한번 전체 플레이 마친 상태 
        if (elem._currentSpriteIdx == 0)
        {
            var renTs = elem._renderer.transform;

            if (elem.dataRef._playCount != -1 && elem._playedCount >= elem.dataRef._playCount)
            {
                elem._renderer.enabled = false;
                elem._exiting = true;
                return;
            }

            elem._playedCount++;

            elem._changeSpriteInterval = elem.dataRef._totalAnimationDuration.Next() / elem.dataRef._spriteCnt;

            if (elem.dataRef._rotationOption == SpriteAnimationData.RotationOption.AlwaysBillboard ||
                (elem.dataRef._rotationOption == SpriteAnimationData.RotationOption.InitialBillboard && elem._hasAppliedBillboard == false))
            {
                elem._hasAppliedBillboard = true;

                renTs.rotation = CameraManager.Instance.MainCam.transform.rotation;

                if (elem.dataRef._rotPerPlay.IsValid())
                    renTs.rotation *= Quaternion.AngleAxis(elem.dataRef._rotPerPlay.Next(), renTs.forward);
            }
            else
            {
                if (elem.dataRef._rotPerPlay.IsValid())
                {
                    renTs.rotation *= Quaternion.Euler(elem.dataRef._rotPerPlay.Next(), elem.dataRef._rotPerPlay.Next(), elem.dataRef._rotPerPlay.Next());
                }
            }

            if (elem.dataRef._movePerPlay.IsValid())
            {
                elem._moveSpeed = elem.dataRef._movePerPlay.Next();
                elem._moveDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), renTs.forward) * renTs.right;
            }
            else
            {
                elem._moveSpeed = 0f;
            }

            if (elem.dataRef._scalingPerPlay.IsValid())
            {
                renTs.localScale = elem.dataRef._startScale;
                elem._scaleVarying = elem.dataRef._scalingPerPlay.Next();
            }
            else
            {
                elem._scaleVarying = 0f;
            }
        }

        elem._changeSpriteAt += elem._changeSpriteInterval;
        elem._renderer.sprite = elem._sprites[elem._currentSpriteIdx];
    }
}
