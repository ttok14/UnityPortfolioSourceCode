using UnityEngine;
using GameDB;
using System.Collections;
using Cysharp.Threading.Tasks;

public abstract class EntityAICommanderBase
{
    protected virtual void OnInGameEventReceived(InGameEvent evt, InGameEventArgBase arg) { }

    public virtual async UniTask InitializeAsync()
    {
        InGameManager.Instance.EventListener += OnInGameEventReceived;
    }

    public virtual void Release()
    {
        InGameManager.Instance.EventListener -= OnInGameEventReceived;
    }

    public virtual void Update() { }

    // 요청받은 Entity 에 맞는 AIBehaviour 생성 후 반환
    // 즉 Entity 의 AI동작이 여기서 구현됨
    public abstract EntityAIBehaviour CreateBehaviour(EntityBase entity, in EntitySetupContext context);
}
