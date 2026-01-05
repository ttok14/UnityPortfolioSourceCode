using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class DefenseModeExitCutScene : CutSceneBase
{
    public override async UniTask BeginCutScene(CutSceneArgs arg)
    {
        var winnerTeam = InGameManager.Instance.PlayerCommander.BattleStatus.WinnerTeam;

        if (winnerTeam == EntityTeamType.Player)
        {
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_Joyful, "승리!");
            AudioManager.Instance.Play("SFX_LoLVictory");
        }
        else if (winnerTeam == EntityTeamType.Enemy)
        {
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_Despair, "패배!");
            AudioManager.Instance.Play("SFX_LoLDefeat");
        }

        await UniTask.WaitForSeconds(5f);

        LightManager.Instance.ToDayTime();

        if (arg.PlayerController.IsAlive)
            await arg.PlayerController.Entity.CutSceneEnterPhase(InGamePhase.Peace);
    }
}
