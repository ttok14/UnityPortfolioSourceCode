using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class DefenseModeEnterCutScene : CutSceneBase
{
    public override async UniTask BeginCutScene(CutSceneArgs arg)
    {
        LightManager.Instance.ToNightTime();

        FX3DMarker targetMarker = null;
        var res = await PoolManager.Instance.RequestSpawnAsync<FX3DMarker>(ObjectPoolCategory.Fx, "WorldObjectMarker01");

        targetMarker = res.instance;
        res.instance.gameObject.SetActive(false);

        //var pos = MapManager.Instance.GetPoint("PlayerModeEnterPosition");

        // 연출 시작.

        // 적 스폰 지점 비춤 
        var enemySpawnPosition = EntityManager.Instance.GetNexus(EntityTeamType.Enemy).ApproxPosition; // SpawnManager.Instance.SpawnPoints[0].worldPosition;
        CameraManager.Instance.InGameController.SetManualStatePosition(enemySpawnPosition);
        await UniTask.WaitForSeconds(0.2f);

        AudioManager.Instance.Play("SFX_WarningPing");

        await UniTask.WaitForSeconds(2f);

        var enemyBattleStatus = InGameManager.Instance.EnemyCommander.BattleStatus;

        for (int i = 0; i < enemyBattleStatus.OrderedTargetIDs.Count; i++)
        {
            var targetEntity = EntityManager.Instance.GetEntity(enemyBattleStatus.OrderedTargetIDs[i]);
            var targetEntityPos = targetEntity.ApproxPosition;

            CameraManager.Instance.InGameController.SetManualStatePosition(targetEntityPos);

            targetMarker.SetPosition(targetEntity.ModelPart.TopPosition);
            targetMarker.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(0.6f);
        }

        await arg.PlayerController.Entity.CutSceneEnterPhase(InGamePhase.Battle);

        arg.PlayerController.Entity.SubMovePart.Stop();

        var initialPosition = MapManager.Instance.GetPoint("PlayerModeEnterPosition");
        arg.PlayerController.Entity.SubMovePart.MoveToPathFinding(initialPosition);

        while (arg.PlayerController.Entity.MovePart.IsMoving)
        {
            await UniTask.Yield();
        }

        // 각종 설정
        CameraManager.Instance.InGameController.FSM.ChangeState(CinemachineCameraType.FollowTarget, false, new object[]
        {
            arg.PlayerController.Entity.transform
        });

        targetMarker.Return();
    }
}
