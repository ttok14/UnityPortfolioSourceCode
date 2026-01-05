using System;
using System.Collections.Generic;
using GameDB;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class ProjectileSystem
{
    static Dictionary<uint, DeliveryActionBase> _processActionCache = new Dictionary<uint, DeliveryActionBase>();
    static Dictionary<uint, DeliveryActionBase> _endActionCache = new Dictionary<uint, DeliveryActionBase>();

    public static void Initialize()
    {
        _processActionCache.Clear();
        _endActionCache.Clear();

        foreach (var data in GameDBManager.Instance.Container.ProjectileTable_data)
        {
            var processActionData = new ActionData()
            {
                Type = data.Value.ProcessActionType,
                RefType = data.Value.ProcessRefType,
                RefID = data.Value.ProcessRefID,
                RefKey = data.Value.ProcessRefKey,
                Value01 = data.Value.ProcessValue01,
                Value02 = data.Value.ProcessValue02,
                Value03 = data.Value.ProcessValue03,
                SFXKeys = data.Value.ProcessSFXKeys,
                FXKeys = data.Value.ProcessFXKeys,
            };

            var endActionData = new ActionData()
            {
                Type = data.Value.EndActionType,
                RefType = data.Value.EndRefType,
                RefID = data.Value.EndRefID,
                RefKey = data.Value.EndRefKey,
                Value01 = data.Value.EndValue01,
                Value02 = data.Value.EndValue02,
                Value03 = data.Value.EndValue03,
                SFXKeys = data.Value.EndSFXKeys,
                FXKeys = data.Value.EndFXKeys,
            };

            if (data.Value.ProcessActionType != E_ActionType.None)
                _processActionCache.Add(data.Key, DeliveryActionFactory.ToAction(processActionData));

            if (data.Value.EndActionType != E_ActionType.None)
                _endActionCache.Add(data.Key, DeliveryActionFactory.ToAction(endActionData));
        }
    }

    public static async UniTaskVoid Fire(
        string key,
        EntityBase executor,
        EntityBase target,
        Vector3 startPosition,
        Vector3? preferredTargetPosition,
        Quaternion startRot,
        EntityTeamType projectileTeam,
        EntityTeamType targetTeam,
        uint damage,
        uint heal,
        string sfxKey,
        string[] fxKeys,
        DeliveryContext inheritContext = null,
        Action<ProjectileController> onCompleted = null)
    {
        // 중요. 만약 DeliveryContext 를 상속해서 전달하는 경우라면
        // 다음에 이어받을 투사체가 이어서 사용해야 하기 때문에
        // 참조 카운트 하나 증가시켜야함
        // + 또한 시점은 , 비동기로 생성되서 실행되는 투사체기 때문에
        // 여기서 미리 하나 올려야, 기존에 투사체가 사라질때
        // 참조 카운트가 하나 깎여서 반환되는 버그가 없을것임.
        if (inheritContext != null)
            inheritContext.IncreaseReferenceCount(1);

        ulong executorId = EntityHelper.IsValid(executor) ? executor.ID : 0;
        ulong targetId = EntityHelper.IsValid(target) ? target.ID : 0;

        var res = await PoolManager.Instance.RequestSpawnAsync<ProjectileController>(
            ObjectPoolCategory.Projectile,
            key,
            null);

        if (res.opRes != PoolOpResult.Successs)
        {
            // 실패 했을때 위에서 올려줬던거 하나 깍아야함
            if (inheritContext != null)
                inheritContext.DecreaseReferenceCount(1);

            onCompleted?.Invoke(null);
            return;
        }

        // 비동기 방어코드
        if (executorId != 0)
        {
            if (EntityHelper.IsValid(executor, executorId) == false)
            {
                executorId = 0;
                executor = null;
            }
        }

        // 비동기 방어코드
        if (targetId != 0)
        {
            if (EntityHelper.IsValid(target, targetId) == false)
            {
                targetId = 0;
                target = null;
            }
        }

        res.instance.gameObject.layer = LayerUtils.GetLayer_String((projectileTeam, "Projectile"));

        if (string.IsNullOrEmpty(sfxKey) == false)
            AudioManager.Instance.Play(sfxKey, startPosition);

        if (fxKeys != null)
        {
            for (int i = 0; i < fxKeys.Length; i++)
            {
                FXSystem.PlayFX(fxKeys[i],
                    startPosition: startPosition,
                    rotation: startRot);
            }
        }

        Vector3 fixedPoint;

        if (preferredTargetPosition.HasValue)
            fixedPoint = preferredTargetPosition.Value;
        else if (target)
            fixedPoint = target.ApproxPosition;
        else fixedPoint = default;

        res.instance.Fire(
            executor,
            startPosition,
            damage,
            heal,
            projectileTeam,
            targetTeam,
            target,
            fixedPoint,
            inheritContext: inheritContext);

        onCompleted?.Invoke(res.instance);
    }

    public static DeliveryActionBase GetProcessAction(uint projectileId)
    {
        if (_processActionCache.TryGetValue(projectileId, out var result))
            return result;
        return null;
    }

    public static DeliveryActionBase GetEndAction(uint projectileId)
    {
        if (_endActionCache.TryGetValue(projectileId, out var result))
            return result;
        return null;
    }
}
