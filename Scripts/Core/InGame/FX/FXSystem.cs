using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameDB;

public static class FXSystem
{
    // 반드시 위치매개변수 지정으로 사용할것 왜냐면
    // 파라미터 변경이 잦을수있음 . 
    public static async UniTask<FXBase> PlayFXCallBack(
        string key,
        Vector3? startPosition = null,
        Vector3? endPosition = null,
        Quaternion? rotation = null,
        float? scale = null,
        float? thickness = null,
        ulong? followTargetId = null,
        Transform followTarget = null,
        Transform parent = null,
        Action<FXBase> onCompleted = null)
    {
        var res = await PoolManager.Instance.RequestSpawnAsync<FXBase>(
            ObjectPoolCategory.Fx,
            key,
            startPosition.HasValue ? startPosition.Value : default,
            // endPosition.HasValue ? endPosition : startPosition,
            rotation: rotation,
            parent: parent);

        // LimitCount 막힐수있음 에러판정 X 
        if (res.opRes != PoolOpResult.Successs)
        {
            onCompleted?.Invoke(null);
            return null;
        }

        if (scale.HasValue)
        {
            if (res.instance is FXParticleSystem ps)
            {
                ps.SetSize(scale.Value);
            }
            else
            {
                res.instance.transform.localScale = new Vector3(scale.Value, scale.Value, scale.Value);
            }
        }

        Vector3 start = default;
        Vector3 end;

        if (startPosition.HasValue)
            start = startPosition.Value;

        if (endPosition.HasValue)
            end = endPosition.Value;
        else end = start;


        res.instance.Play(new FXPlayData(
            startPos: start,
            endPos: end,
            thickness: thickness,
            targetValidId: followTargetId.HasValue ? followTargetId.Value : 0,
            target: followTarget));

        onCompleted?.Invoke(res.instance);

        return res.instance;
    }

    public static async UniTask<T> PlayFXAsync<T>(
        string key,
        Vector3? startPosition = null,
        Vector3? endPosition = null,
        Quaternion? rotation = null,
        float? scale = null,
        float? thickness = null,
        ulong? followTargetId = null,
        Transform followTarget = null,
        Transform parent = null)
        where T : FXBase
    {
        return await PlayFXCallBack(
            key: key,
            startPosition: startPosition,
            endPosition: endPosition,
            rotation: rotation,
            scale: scale,
            thickness: thickness,
            followTargetId: followTargetId,
            followTarget: followTarget,
            parent: parent) as T;
    }

    public static void PlayFX(
        string key,
        Vector3? startPosition = null,
        Vector3? endPosition = null,
        Quaternion? rotation = null,
        float? scale = null,
        float? thickness = null,
        ulong? followTargetId = null,
        Transform followTarget = null,
        Transform parent = null)
    {
        PlayFXCallBack(
            key: key,
            startPosition: startPosition,
            endPosition: endPosition,
            rotation: rotation,
            scale: scale,
            thickness: thickness,
            followTargetId: followTargetId,
            followTarget: followTarget,
            parent: parent).Forget();
    }

    public static void PlayFX_RangeIndicator(
        /* Range 를 다양한 에셋으로 보여줘야한다면 필요할수잇는데, 일단 주석 처리 string key,*/
        Color color,
        float range,
        float duration,
        Vector3? position, Action<FXRangeIndicator> onCompleted = null)
    {
        PlayFXCallBack("FXRangeIndicator", startPosition: position, rotation: Quaternion.Euler(90, 0, 0),
            onCompleted: (fx) =>
            {
                if (fx == null)
                {
                    onCompleted?.Invoke(null);
                    return;
                }

                  (fx as FXRangeIndicator).SetInfo(color, range, duration);

                onCompleted?.Invoke(fx as FXRangeIndicator);
            }).Forget();
    }

    public static void PlayCommonHitFXByEntity(Vector3 position, E_EntityType damagedEntityType)
    {
        switch (damagedEntityType)
        {
            case E_EntityType.Structure:
                PlayFXCallBack("SpriteFX_HitSpark01").Forget();
                break;
            case E_EntityType.Character:
            case E_EntityType.Animal:
                PlayFXCallBack("SpriteFX_BloodSplatter", startPosition: position).Forget();
                break;
            default:
                TEMP_Logger.Err($"Not implemented : {damagedEntityType}");
                break;
        }
    }
}
