using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CutSceneManager : SingletonBase<CutSceneManager>
{
    public bool IsCutSceneShowing { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

    }

    public override void Release()
    {
        base.Release();

    }

    public async UniTask BeginCutScene(InGameCutSceneType type)
    {
        IsCutSceneShowing = true;
        InputManager.Instance.BlockEventCount++;
        InGameManager.Instance.PublishEvent(InGameEvent.BeginCutScene);

        CutSceneBase cutScene = null;

        switch (type)
        {
            case InGameCutSceneType.None:
                break;
            case InGameCutSceneType.EnterDefenseMode:
                cutScene = new DefenseModeEnterCutScene();
                break;
            case InGameCutSceneType.ExitDefenseMode:
                cutScene = new DefenseModeExitCutScene();
                break;
            default:
                break;
        }

        if (cutScene != null)
        {
            var commonArg = new CutSceneArgs()
            {
                PhaseState = InGameManager.Instance.CurrentPhase,
                PhaseType = InGameManager.Instance.CurrentPhaseType,
                PlayerController = InGameManager.Instance.PlayerCommander.Player,
                StrategySystem = InGameManager.Instance.DefensePathSystem
            };

            await cutScene.BeginCutScene(commonArg);
        }

        InGameManager.Instance.PublishEvent(InGameEvent.EndCutScene);
        InputManager.Instance.BlockEventCount--;
        IsCutSceneShowing = false;
    }
}
