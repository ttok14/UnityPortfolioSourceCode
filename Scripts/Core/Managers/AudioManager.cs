using System;
using UnityEngine;
using UnityEngine.Audio;
using GameDB;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class AudioManager : SingletonBase<AudioManager>
{
    [SerializeField]
    private AudioListenerController _listenerController;
    public Vector3 ListenerPosition => _listenerController?.transform.position ?? Vector3.zero;

    private AudioMixer _mixer;

    Transform _audioRoot;

    Dictionary<E_AudioType, AudioMixerGroup> _mixerGroups = new Dictionary<E_AudioType, AudioMixerGroup>();

    // Queue<AudioPlayer> _audioPlayers = new Queue<AudioPlayer>();

    public override void Initialize()
    {
        base.Initialize();

        DontDestroyOnLoad(_listenerController);
        _listenerController.Initialize();

        _audioRoot = new GameObject("AudioRoot").transform;
        _audioRoot.SetParent(transform);
        _audioRoot.position = Vector3.zero;

        AssetManager.Instance.LoadAsyncCallBack<AudioMixer>("MasterMixer", onCompleted: (res) =>
        {
            _mixer = res;

            foreach (E_AudioType type in Enum.GetValues(typeof(E_AudioType)))
            {
                if (type == E_AudioType.None)
                    continue;

                var groupPath = $"Master/{type}";
                var group = _mixer.FindMatchingGroups(groupPath);
                if (group == null)
                    TEMP_Logger.Err(@$"Make sure the AudioMixer has a group at : {groupPath}");
                else
                    _mixerGroups.Add(type, group[0]);
            }
        }).Forget();
    }

    public async UniTaskVoid Play(AudioPlayer currentPlayer, Vector3 playerPos, string key, AudioTrigger trigger, AudioSettings settings, Action onPlayed = null)
    {
        var data = DBAudio.Get(key);
        if (data == null)
        {
            TEMP_Logger.Err($"Given Audio Key does not exist : {key}");
            return;
        }

        var result = await AssetManager.Instance.LoadAsync<AudioClip>(key);

        currentPlayer.Play(result, data, playerPos, trigger, _mixerGroups[data.AudioType], settings);

        onPlayed?.Invoke();
    }

    public void Play(string key, Vector3 playerPos = default, AudioTrigger trigger = AudioTrigger.Default, float delay = 0f, AudioSettings? settings = null, Action<AudioPlayer> onPlayed = null)
    {
        PlayAsyncCallBack(DBAudio.Get(key), playerPos, trigger, delay, settings, onPlayed).Forget();
    }

    public void Play(uint id, Vector3 playerPos, AudioTrigger trigger, float delay = 0f, AudioSettings? settings = null, Action<AudioPlayer> onPlayed = null)
    {
        PlayAsyncCallBack(DBAudio.Get(id), playerPos, trigger, delay, settings, onPlayed).Forget();
    }

    async UniTaskVoid PlayAsyncCallBack(AudioTable data, Vector3 playerPos, AudioTrigger trigger, float delay = 0f, AudioSettings? settings = null, Action<AudioPlayer> onPlayed = null)
    {
        if (data == null)
        {
            TEMP_Logger.Err($"Given Audio TableData is Null");
            return;
        }

        if (data.Is3D)
        {
            playerPos.y = 0;

            if (DBAudio.Is3DAudioInRange(data, playerPos, _listenerController.transform.position) == false)
            {
                onPlayed?.Invoke(null);
                return;
            }
        }

        ObjectPoolCategory poolCategory;

        if (data.AudioType == E_AudioType.SFX)
            poolCategory = ObjectPoolCategory.Audio_Normal;
        else poolCategory = ObjectPoolCategory.Audio_Critical;

        var res = await PoolManager.Instance.RequestSpawnAsync<AudioPlayer>(poolCategory, "AudioPlayer", parent: _audioRoot);
        if (res.opRes != PoolOpResult.Successs)
            return;

        if (delay > 0f)
            await UniTask.WaitForSeconds(delay);

        await res.instance.PlayAsync(data, playerPos, trigger, _mixerGroups[data.AudioType], settings);

        onPlayed?.Invoke(res.instance);
    }
}
