using System;
using UnityEngine;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
using GameDB;

public class AudioPlayer : PoolableObjectBase, IUpdatable
{
    [SerializeField]
    private AudioSource Source;
    public string ClipKey;

    bool _returnOnStopPlay;

    AudioTrigger _triggeredBy;

    public bool IsPlaying => Source.isPlaying;

    public bool AutoStopEnabled { get; set; }

    public float TimePassedSinceStopped { get; private set; }

    float _defaultMinDistance;
    float _defaultMaxDistance;

    void SetDefault()
    {
        _triggeredBy = AudioTrigger.Default;
        AutoStopEnabled = true;
        _returnOnStopPlay = false;
        Source.loop = false;
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _defaultMinDistance = Source.minDistance;
        _defaultMaxDistance = Source.maxDistance;
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);
        SetDefault();

        UpdateManager.Instance.RegisterSingle(this);
    }

    public override void OnInactivated()
    {
        base.OnInactivated();
        SetDefault();

        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingle(this);
    }


    // private void Update()
    void IUpdatable.OnUpdate()
    {
        if (Source.isPlaying)
        {
            TimePassedSinceStopped += Time.deltaTime;
        }
        else TimePassedSinceStopped = 0;

        if (AutoStopEnabled)
        {
            if (_returnOnStopPlay && Source.isPlaying == false)
            {
                Return();
            }
        }
    }

    public void Play()
    {
        Source.Play();
    }

    public void Play(AudioClip clip, AudioTable data, Vector3 playerPos, AudioTrigger trigger, AudioMixerGroup mixerGroup, AudioSettings? settings = null)
    {
        if (data == null)
        {
            TEMP_Logger.Err($"Given AudioTable is Null");
            return;
        }

        ClipKey = AssetManager.Instance.GetKeyByObject(clip);

        Source.clip = clip;
        Source.outputAudioMixerGroup = mixerGroup;

        _triggeredBy = trigger;

        if (data.Is3D)
        {
            transform.position = playerPos;
            Source.spatialBlend = 1f;
            Source.spread = 360;

            DBAudio.GetMinMaxDistance(data.DistanceType, out float minDistance, out float maxDistance);

            Source.minDistance = minDistance;
            Source.maxDistance = maxDistance;
        }
        else
        {
            Source.spatialBlend = 0f;
        }

        Source.volume = data.Volume;
        Source.pitch = 1f - (data.RandomPitchRange * 0.5f) + UnityEngine.Random.Range(0f, data.RandomPitchRange);
        Source.loop = data.AudioType == GameDB.E_AudioType.BGM || data.AudioType == GameDB.E_AudioType.SFX_Structure;
        AutoStopEnabled = Source.loop == false;

        Source.Play();
        _returnOnStopPlay = true;

        if (trigger == AudioTrigger.EntityInteraction)
        {
            EventManager.Instance.Register(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnUserInteractionEnded);
        }
    }

    public async UniTask PlayAsync(AudioTable data, Vector3 playerPos, AudioTrigger trigger, AudioMixerGroup mixerGroup, AudioSettings? settings)
    {
        Source.Stop();

        var clip = await AssetManager.Instance.LoadAsync<AudioClip>(data.ResourceKey);

        if (clip == null)
        {
            TEMP_Logger.Err($"Failed to load Audio | Key: {data.ResourceKey} ");
            Return();
            return;
        }

        Play(clip, data, playerPos, trigger, mixerGroup, settings);
    }

    public void Stop(bool release = false)
    {
        _triggeredBy = AudioTrigger.Default;

        if (Source.isPlaying)
        {
            Source.Stop();
            _returnOnStopPlay = true;
        }

        if (release && IsActivated)
        {
            Return();
        }
    }

    private void OnUserInteractionEnded(EventContext cxt)
    {
        Stop();
        EventManager.Instance.Unregister(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED, OnUserInteractionEnded);
    }
}
