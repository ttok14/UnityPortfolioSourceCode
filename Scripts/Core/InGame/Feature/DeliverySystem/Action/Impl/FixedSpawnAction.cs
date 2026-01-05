using GameDB;
using System;
using UnityEngine;

public class FixedSpawnAction : SpawnActionBase
{
    protected override bool ParseValues(IDeliverySource source, EntityBase currentTarget, DeliveryContext context, out Vector3 startPosition, out EntityBase newTarget, out Vector3 newTargetPos)
    {
        startPosition = source.Position;
        newTarget = currentTarget;
        newTargetPos = currentTarget?.ApproxPosition ?? Vector3.zero;

        // Int 값에 따라 설정 로직 분기

        // 기존 값 최대한 유지 
        if (Data.Value01 == 0)
        {
            startPosition = context.Position;

            if (currentTarget)
            {
                newTarget = currentTarget;
                newTargetPos = currentTarget.ApproxPosition;
            }
            else
            {
                newTarget = null;
                newTargetPos = context.Position;
            }
        }
        // Executor 로 설정 
        else if (Data.Value01 == 1)
        {
            startPosition = context.Position;

            if (EntityManager.Instance.IsEntityValid(context.ExecutorID))
            {
                newTarget = EntityManager.Instance.GetEntity(context.ExecutorID);
                newTargetPos = newTarget.ApproxPosition;
            }
            else
            {
                // Executor 를 target 으로 설정해야 하는데 그 사이에
                // 죽거나 하면 현 Action 은 소멸되어야 함
                // (e.g 롤 아리 스킬중 볼을 날렷다가 그 사이에 아리가 죽으면
                // 볼은 사라지는 처리)
                return false;
            }
        }
        // 현 위치로 설정
        else if (Data.Value01 == 2)
        {
            startPosition = source.Position;
            newTarget = null;
            newTargetPos = source.Position;
        }
        else
        {
            TEMP_Logger.Err($"Not Implemented SpawnAction Value01 : {Data.Value01}");
            return false;
        }

        return true;
    }
}
