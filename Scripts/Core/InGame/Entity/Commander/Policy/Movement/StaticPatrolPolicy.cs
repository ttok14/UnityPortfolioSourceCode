using UnityEngine;

public class StaticPatrolPolicy : MovePolicyBase
{
    public override MoveCommand GetCommand(EntityBase target)
    {
        // 적이 있어도 patrol, 없어도 patrol
        return new MoveCommand() { result = MoveCommandResult.Patrol };
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }
}
