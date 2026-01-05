
//public class DeliverActionInitData : IInstancePoolInitData
//{
//    public ActionDataBase data;
//}

using UnityEngine;

public abstract class DeliveryActionBase //  : IInstancePoolElement
{
    protected ActionData Data { get; private set; }

    public virtual void Initialize(ActionData data)
    {
        Data = data;
    }

    public abstract void Execute(IDeliverySource source, EntityBase target, DeliveryContext context);

    protected void PlaySFX(IDeliverySource source)
    {
        if (Data == null)
            return;

        if (Data.SFXKeys != null)
        {
            foreach (var key in Data.SFXKeys)
            {
                AudioManager.Instance.Play(key, source.Position);
            }
        }
    }

    protected void PlayFX(IDeliverySource source, EntityBase target, DeliveryContext context, bool doFollowTarget = true, bool enablePlayHitFx = false)
    {
        if (Data.FXKeys != null)
        {
            foreach (var key in Data.FXKeys)
            {
                if (string.IsNullOrEmpty(key) == false)
                {
                    Vector3 endPos;
                    Transform targetTs = null;

                    if (target)
                    {
                        // 기본적으로는 센터에 FX 출력
                        targetTs = target.ModelPart.GetSocket(EntityModelSocket.Center);

                        if (targetTs == null)
                            targetTs = target.transform;
                    }

                    if (targetTs)
                        endPos = targetTs.position;
                    else
                        endPos = source.Position;

                    FXSystem.PlayFX(key,
                        startPosition: source.Position,
                        endPosition: endPos,
                        followTargetId: (doFollowTarget && targetTs) ? target.ID : 0,
                        followTarget: (doFollowTarget && targetTs) ? targetTs : null);

                    // 데미지가 존재할때만 Hit
                    if (context.Damage > 0 && enablePlayHitFx && target)
                        FXSystem.PlayCommonHitFXByEntity(endPos, target.Type);
                }
            }
        }
    }

    protected void Play_SFX_FX(IDeliverySource source, EntityBase target, DeliveryContext context)
    {
        if (Data == null)
            return;

        PlaySFX(source);

        PlayFX(source, target, context, true, true);

        //public virtual void OnPoolActivated(IInstancePoolInitData initData)
        //{
        //    _data = (initData as DeliverActionInitData).data;
        //}

        //public virtual void OnPoolInitialize() { }
        //public abstract void OnPoolReturned();
        //public abstract void ReturnToPool();
    }
}
