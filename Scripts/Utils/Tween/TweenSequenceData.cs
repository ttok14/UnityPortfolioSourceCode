using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SequenceData", menuName = "Jayce/SequenceData")]
public class TweenSequenceData : ScriptableObject
{
    [Serializable]
    public class TweenStep
    {
        public enum E_StepType { Append, Join, Interval }

        [SerializeField]
        private E_StepType _stepType = E_StepType.Append;
        public E_StepType StepType => _stepType;

        public enum E_FromTo { From, To }

        [SerializeField]
        private float _duration = 1f;
        public float Duration => _duration;

        [SerializeField]
        private Ease _easeType = Ease.Linear;
        public Ease EaseType => _easeType;

        [SerializeField]
        private float _delay = 0f;
        public float Delay => _delay;

        [SerializeField]
        private bool _isRelative = false;
        public bool IsRelative => _isRelative;

        public enum E_TweenTargetType { Move, Rotate, Scale, UIAlpha, Color, Custom }

        [SerializeField]
        private E_TweenTargetType _targetType = E_TweenTargetType.Move;
        public E_TweenTargetType TargetType => _targetType;

        [SerializeField]
        private E_FromTo _fromOrTo = E_FromTo.From;
        public E_FromTo FromOrTo => _fromOrTo;

        [SerializeField]
        private Vector3 _toValue = Vector3.zero;
        public Vector3 ToValue => _toValue;

        [SerializeField]
        private Color _toColor = Color.white;
        public Color ToColor => _toColor;

        // 색상 값에 접근해야 할때 쉐이더에서 색상 프로퍼티 이름. (Runner에서 MPB 쓰고잇어서)
        // 참고로 _Color , _BaseColor 는 내부적으로 접근 시도하기때문에 이 두개는 제외 
        [SerializeField]
        private string _fallBackColorPropertyName;
        public string FallBackColorPropertyName => _fallBackColorPropertyName;
    }

    [SerializeField]
    private List<TweenStep> _steps = new List<TweenStep>();
    public IReadOnlyList<TweenStep> Steps => _steps;

    [SerializeField]
    private bool _autoKill = true;
    public bool AutoKill => _autoKill;

    [SerializeField]
    private Ease _sequenceEase = Ease.Linear;
    public Ease SequenceEase => _sequenceEase;

    [SerializeField]
    private int _loops = 1;
    public int Loops => _loops;

    [SerializeField]
    private LoopType _loopType = LoopType.Restart;
    public LoopType LoopType => _loopType;

    [SerializeField]
    private UnityEngine.Events.UnityEvent _onStarted;
    public UnityEngine.Events.UnityEvent OnStarted => _onStarted;

    [SerializeField]
    private UnityEngine.Events.UnityEvent _onFinished;
    public UnityEngine.Events.UnityEvent OnFinished => _onFinished;

    public void PlaySound(int id)
    {
        AudioManager.Instance.Play((uint)id, Vector3.zero, AudioTrigger.Default);
    }

    public void PlaySound(string key)
    {
        AudioManager.Instance.Play(key, Vector3.zero, AudioTrigger.Default);
    }
}
