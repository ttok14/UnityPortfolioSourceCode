using System.Collections.Generic;
using UnityEngine;

public class FXParticleSystem : FXBase
{
    public enum PositionPart
    {
        Start,
        End,
        GroundedStart,
        GroundedEnd
    }

    class Particle
    {
        public ParticleSystem ps;

        public float defaultConstantStartSize;

        public float defaultConstantStartSizeMin;
        public float defaultConstantStartSizeMax;

        public Vector3 defaultShapeScale;

        public bool IsDirty;

        public void Reset()
        {
            IsDirty = false;

            var sizeProp = ps.main.startSize;
            if (sizeProp.mode == ParticleSystemCurveMode.Constant)
            {
                sizeProp.constant = defaultConstantStartSize;
            }
            else if (sizeProp.mode == ParticleSystemCurveMode.TwoConstants)
            {
                sizeProp.constantMin = defaultConstantStartSizeMin;
                sizeProp.constantMax = defaultConstantStartSizeMax;
            }

            if (ps.shape.enabled)
            {
                var shape = ps.shape;
                shape.scale = defaultShapeScale;
            }
        }
    }

    private List<Particle> _particles = new List<Particle>();

    [SerializeField]
    private PositionPart _part;

    [SerializeField]
    private Vector3 _positionOffset;

    bool _autoReturn;
    float _autoReturnCheckWaitDuration;

    public override bool ActivateLateUpdate => true;

    //----------------------//
    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        var particles = GetComponentsInChildren<ParticleSystem>();
        if (particles.Length == 0)
        {
            TEMP_Logger.Err($"Particle do not exist | name : {gameObject.name}");
            PoolManager.Instance.Remove(this);
            return;
        }

        _autoReturn = true;

        foreach (var particle in particles)
        {
            var p = new Particle();
            p.ps = particle;

            if (particle.main.loop)
            {
                _autoReturn = false;

                var sizeProp = particle.main.startSize;

                if (sizeProp.mode == ParticleSystemCurveMode.Constant)
                {
                    p.defaultConstantStartSize = sizeProp.constant;
                }
                else if (sizeProp.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    p.defaultConstantStartSizeMin = sizeProp.constantMin;
                    p.defaultConstantStartSizeMax = sizeProp.constantMax;
                }
            }

            if (particle.shape.enabled)
            {
                p.defaultShapeScale = particle.shape.scale;
            }

            _particles.Add(p);

            p.ps.Play();
        }

        if (_autoReturn)
        {
            _autoReturnCheckWaitDuration = particles.Custom_GetShortestApproximateLifeTime();
        }
    }

    public override void Play(FXPlayData data)
    {
        base.Play(data);

        switch (_part)
        {
            case PositionPart.Start:
                transform.position = data.startPos + _positionOffset;
                break;
            case PositionPart.End:
                transform.position = data.endPos + _positionOffset;
                break;
            case PositionPart.GroundedStart:
                {
                    var pos = data.startPos;
                    pos.y = 0.1f;
                    transform.position = pos + _positionOffset;
                }
                break;
            case PositionPart.GroundedEnd:
                {
                    var pos = data.endPos;
                    pos.y = 0.1f;
                    transform.position = pos + _positionOffset;
                }
                break;
            default:
                TEMP_Logger.Err($"Not Implemented Type : {_part}");
                break;
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        foreach (var p in _particles)
        {
            if (p.IsDirty)
            {
                p.Reset();
            }
        }
    }

    public void SetSize(float size)
    {
        foreach (var p in _particles)
        {
            var sizeProp = p.ps.main.startSize;

            p.IsDirty = true;

            switch (sizeProp.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    {
                        sizeProp.constant = size;
                    }
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    {
                        float diff = p.defaultConstantStartSizeMax - p.defaultConstantStartSizeMin;

                        sizeProp.constantMin = size;
                        sizeProp.constantMax = size + diff;
                    }
                    break;
            }

            if (p.ps.shape.enabled)
            {
                var shape = p.ps.shape;
                shape.scale = new Vector3(size, size, size);
            }
        }
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (IsActivated == false || _autoReturn == false || Time.time < ActivatedAt + _autoReturnCheckWaitDuration)
            return;

        if (IsPlaying() == false)
        {
            Return();
        }
    }

    bool IsPlaying()
    {
        foreach (var p in _particles)
        {
            if (p.ps && p.ps.isPlaying)
            {
                return true;
            }
        }
        return false;
    }
}
