using System.Collections.Generic;
using UnityEngine;
using GameDB;

public interface IInteractable
{
    bool OnInteract(IInteractor interactor, InteractionContext context);
}

public interface IEntityAffecter
{
    ulong ExecutorID { get; }
    uint Damage { get; }
    uint Heal { get; }
    Vector3 Position { get; }
    float PhysicalForce { get; }
}

public interface IInteractor
{
    GameObject GetGameObject();
}

public interface IResourceGenerator
{
    public bool IsEnabled { get; }

    public E_ResourceType ResourceType { get; }
    public uint DetailID { get; }
    public uint Amount { get; }

    public float GenerateInterval { get; }
    public float CurrentProgress { get; }


    public event EntityEventDelegates.OnResourceGenerated OnGenerated;
}

//public interface IItem
//{
//    uint ItemID();
//}

public abstract class InteractionContext { }

public class EntityInteractionUIArg : UIArgBase
{
    public EntityBase entity;

    public EntityInteractionUIArg(EntityBase entity)
    {
        this.entity = entity;
    }
}


public class PlayerInteractContext : InteractionContext
{
}

public class EntityEventDelegates
{
    public delegate void OnDataModified(EntityDataCategory category, EntityDataBase data);

    public delegate void OnHealed(ulong executorId, int amount, Vector3 effectPos);
    public delegate void OnDamaged(ulong executorId, int damaged, Vector3 effectPos, float effectForce);
    public delegate void OnHPChanged(int maxHP, int currentHP, int diff);
    public delegate void OnLevelUp(uint newLevel);
    public delegate void OnDied(ulong attackerId, Vector3 attackerPosition);

    public delegate void OnMovementBegin(EntityBase executor, bool pathsOrDirectional, Vector3? directionalMoveDir = null, List<Vector3> paths = null);
    public delegate void OnMoving(EntityBase executor, Vector3 position);
    public delegate void OnMovementEnd(EntityBase executor, bool reachedDest);

    public delegate void SkillTriggered(EntityBase executor);

    // TODO : 추후 스킬이나 공격 종류가 여러개가 되면 어떤 종류의 스킬이나 공격이 발동됐는지 전달해줘야겠지?
    // 또는 , OnSkillExecuteBegin 뭐 이런식으로 바뀔수도있겠고 스킬의 종류에 따라 또 콜백이 바뀌거나 할 수도 있겠고
    // 참고로 이게 호출된거는 , 해당 Entity 가 '공격' 을 할 수 있다는 의미도 있음 . 
    public delegate void OnAttackCommand(EntityBase executor);

    public delegate void OnAniamtionBegin(EntityBase executor, EntityAnimationStateID type);
    public delegate void OnAniamtionEnd(EntityBase executor, EntityAnimationStateID type);
    public delegate void OnAnimationIKSet(EntityBase executor, bool isLeftOrRight);

    //------------------------------------------------------------------------------------//

    public delegate void OnResourceGenerated(E_ResourceType type, uint id, uint amount);
}

public readonly struct EntitySetupContext
{
    public readonly SpawnerMode SpawnMode;

    public EntitySetupContext(SpawnerMode spawnMode)
    {
        SpawnMode = spawnMode;
    }
}
