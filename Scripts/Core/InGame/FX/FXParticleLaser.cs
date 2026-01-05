using System.Collections.Generic;
using UnityEngine;

public class FXParticleLaser : FXBase
{
    [Header("시작점에 위치"), SerializeField]
    private List<ParticleSystem> _startParts;

    [Header("레이저, Start -> End Position"), SerializeField]
    private List<ParticleSystem> _lazerParts;

    [Header("도착점에 위치"), SerializeField]
    private List<ParticleSystem> _endParts;

    ulong _targetFollowEntityId;
    Transform _targetToFollow;

    Vector3 _cachedStartPos;
    float? _cachedThickness;

    bool _autoReturn;
    float _autoReturnCheckWaitDuration;

    public override bool ActivateLateUpdate => true;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _autoReturn =
            _startParts.TrueForAll(t => t && t.main.loop == false)
            && _lazerParts.TrueForAll(t => t && t.main.loop == false)
            && _endParts.TrueForAll(t => t && t.main.loop == false);

        if (_autoReturn)
        {
            _autoReturnCheckWaitDuration = Mathf.Min(
                _startParts.Custom_GetShortestApproximateLifeTime(),
                _lazerParts.Custom_GetShortestApproximateLifeTime(),
                _endParts.Custom_GetShortestApproximateLifeTime());
        }
    }

    public override void Play(FXPlayData data)
    {
        base.Play(data);

        _cachedStartPos = data.startPos;
        _cachedThickness = data.thickness;

        if (data.targetValidId != 0 && data.target)
        {
            _targetFollowEntityId = data.targetValidId;
            _targetToFollow = data.target;
        }

        transform.position = data.endPos;
        transform.LookAt(data.startPos);

        foreach (var ps in _startParts)
        {
            if (!ps)
                continue;

            ps.transform.position = data.startPos;
            ps.Play();
        }

        foreach (var ps in _lazerParts)
        {
            if (!ps)
                continue;

            Vector3 targetPos = data.target ? data.target.position : data.endPos;
            ps.Custom_SetLength(data.startPos, targetPos, data.thickness);
            ps.Play();
        }

        foreach (var ps in _endParts)
        {
            if (!ps)
                continue;

            ps.transform.position = data.target ? data.target.position : data.endPos;
            ps.Play();
        }
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (_targetFollowEntityId != 0 && _targetToFollow)
        {
            if (EntityManager.Instance.IsEntityValid(_targetFollowEntityId))
            {
                Vector3 currentTargetPos = _targetToFollow.position;

                foreach (var ps in _lazerParts)
                {
                    if (ps) ps.Custom_SetLength(_cachedStartPos, currentTargetPos, _cachedThickness);
                }

                foreach (var ps in _endParts)
                {
                    if (ps) ps.transform.position = currentTargetPos;
                }
            }
            else
            {
                _targetToFollow = null;
                _targetFollowEntityId = 0;
            }
        }

        if (_autoReturn == false) return;

        if (Time.time < ActivatedAt + _autoReturnCheckWaitDuration)
            return;

        if (_startParts.IsPlaying() == false &&
            _lazerParts.IsPlaying() == false &&
            _endParts.IsPlaying() == false)
        {
            Return();
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();
        _targetFollowEntityId = 0;
        _targetToFollow = null;

        _cachedStartPos = Vector3.zero;
        _cachedThickness = null;

    }
}
