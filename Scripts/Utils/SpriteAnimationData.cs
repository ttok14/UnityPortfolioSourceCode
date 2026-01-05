using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SpriteAnimationData ", menuName = "Jayce/SpriteAnimationData")]
public class SpriteAnimationData : ScriptableObject
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

        public bool IsValid()
        {
            return Min != 0 || Max != 0;
        }
    }

    public enum RotationOption
    {
        None = 0,

        AlwaysBillboard,
        InitialBillboard,

    }

    public string _sourceSpriteSheetKey;

    public int _spriteCnt;

    [Header("-1 은 무한 반복")]
    public int _playCount = 1;

    public Random _totalAnimationDuration;

    public Random _rotPerPlay;

    public Random _movePerPlay;

    // scaling 을 사용할때만 유효
    public Vector3 _startScale = new Vector3(1, 1, 1);
    public Random _scalingPerPlay;
    public float _maxScale;

    public RotationOption _rotationOption;
}
