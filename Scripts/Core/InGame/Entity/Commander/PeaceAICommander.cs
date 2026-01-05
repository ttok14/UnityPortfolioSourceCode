using UnityEngine;

public class PeaceAICommander : EntityAICommanderBase
{
    StandardAggroSystem _standardAggroSystem = new StandardAggroSystem(1, 1f, 1f, true);

    AggroTargetingPolicyInitData _aggroTargetingPolicyInitData = new AggroTargetingPolicyInitData();
    EntityAIBehaviourInitData _behaviourInitData = new EntityAIBehaviourInitData();

    MovePolicyInitDataBase _movePolicyInitData = new MovePolicyInitDataBase();

    public override EntityAIBehaviour CreateBehaviour(EntityBase entity, in EntitySetupContext context)
    {
        if (entity.Type == GameDB.E_EntityType.Structure)
        {
            var structure = entity as StructureEntity;
            MovePolicyBase movePolicy = null;

            if (entity.MovePart != null)
            {
                if (entity.MovePart is EntityPatrolMovePart)
                {
                    _movePolicyInitData.Set(entity);
                    movePolicy = InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<StaticPatrolPolicy>(_movePolicyInitData);
                }
                else TEMP_Logger.Err($"Not Implemented MovePart To MovePolicy : {entity.MovePart}");
            }

            if (structure.StructureData.StructureType == GameDB.E_StructureType.Defense)
            {
                _aggroTargetingPolicyInitData.Set(_standardAggroSystem);

                _behaviourInitData.Set(
                    entity,
                    InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<AggroTargetingPolicy>(_aggroTargetingPolicyInitData)
                    , movePolicy, null);
                return InGameManager.Instance.CacheContainer.EntityAIPool.GetOrCreate<EntityAIBehaviour>(_behaviourInitData);
            }
        }

        return null;
    }

    protected override void OnInGameEventReceived(InGameEvent evt, InGameEventArgBase arg)
    {

    }
}
