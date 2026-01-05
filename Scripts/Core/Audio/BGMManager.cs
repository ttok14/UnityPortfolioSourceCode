using System.Collections.Generic;
using UnityEngine;

public class BGMManager : SingletonBase<BGMManager>
{
    private Dictionary<GameState, string> _defaultBgmsByMainState = new Dictionary<GameState, string>()
    {
        { GameState.Auth, "BGM_Default" },
        { GameState.Lobby, "BGM_Default" },
        // { GameState.InGame, "BGM_InGame01" }
    };

    //private Dictionary<GameState, Dictionary<InGamePhase, string>> _ingameBgmsByMainState = new Dictionary<GameState, Dictionary<InGamePhase, string>>()
    //{
    //    {
    //        GameState.InGame,
    //        new Dictionary<InGamePhase, string>()
    //        {
    //            { InGamePhase.Peace, "BGM_InGame01" },
    //            { InGamePhase.Battle, "BGM_InGameBattle" }
    //        }
    //    }
    //};

    private AudioPlayer _currentPlayer;
    private AudioSettings _settings = new AudioSettings(loop: true, enableAutoStop: false);

    public override void Initialize()
    {
        EventManager.Instance.Register(GLOBAL_EVENT.GAME_STATE_CHANGED, OnGameStateChanged);
    }

    public void Play(string key)
    {
        Play(key, AudioTrigger.Default, 0, false);
    }

    private void OnGameStateChanged(EventContext context)
    {
        var stateChangeArg = context.Arg as GameStateChangeEventArg;

        if (_defaultBgmsByMainState.TryGetValue(stateChangeArg.To, out var key))
        {
            Play(key, AudioTrigger.Default, 0, false);
        }
    }

    void Play(string key, AudioTrigger trigger, float delay = 0f, bool forceRewind = false)
    {
        bool alreadyPlaying = _currentPlayer ? _currentPlayer.ClipKey == key : false;
        if (forceRewind == false && alreadyPlaying)
        {
            return;
        }

        if (_currentPlayer)
        {
            _currentPlayer.Stop();

            AudioManager.Instance.Play(_currentPlayer, Vector3.zero, key, trigger, _settings).Forget();
        }
        else
        {
            AudioManager.Instance.Play(key, Vector3.zero, trigger, 0, _settings, (player) =>
              {
                  _currentPlayer = player;
              });
        }
    }
}
