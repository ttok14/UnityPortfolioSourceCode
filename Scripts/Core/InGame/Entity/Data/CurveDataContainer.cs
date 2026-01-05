using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CurveDataContainer", menuName = "Jayce/CurveDataContainer")]
public class CurveDataContainer : ScriptableObject
{
    [Serializable]
    public class Data
    {
        public AnimationCurve curve;

        [HideInInspector]
        float _lastMaxTime = -1f;

        public float LastMaxTime
        {
            get
            {
                if (_lastMaxTime == -1f)
                {
                    _lastMaxTime = curve.keys[curve.length - 1].time;
                }
                return _lastMaxTime;
            }
        }
    }

    [SerializeField]
    public Data StandardProjectileMovementCurveData;

    [SerializeField]
    public Data CharacterDieHeightCurveData;

    [SerializeField]
    public Data ObtainItemCurveData;
}
