using GameDB;
using System;
using UnityEngine;

public class SkyFallSpawnAction : SpawnActionBase
{
    protected override bool ParseValues(
        IDeliverySource source,
        EntityBase currentTarget,
        DeliveryContext context,
        out Vector3 startPosition,
        out EntityBase newTarget,
        out Vector3 newTargetPos)
    {
        // Value01 : 랜덤 스폰 범위 수치
        var randomCircle = UnityEngine.Random.insideUnitCircle * Data.Value01;

        // 시작 위치 설정 - xz 평면상 랜덤위치
        startPosition = source.Position + new Vector3(randomCircle.x, 0, randomCircle.y);

        newTarget = currentTarget;

        newTargetPos = startPosition;
        newTargetPos.y = 0;

        // 도착 위치 설정 - Value02 값 따라 정해진 축 방향으로 Value03 만큼 오프셋 띄어서 도착 위치를 설정
        int destAxisOption = Data.Value02.GetApproximateInt();
        switch (destAxisOption)
        {
            // X 축 + 
            case 0:
                newTargetPos.x += Data.Value03;
                break;
            // X 축 -
            case 1:
                newTargetPos.x -= Data.Value03;
                break;
            // Z 축 +
            case 2:
                newTargetPos.z += Data.Value03;
                break;
            // Z 축 -
            case 3:
                newTargetPos.z -= Data.Value03;
                break;
        }

        return true;
    }
}
