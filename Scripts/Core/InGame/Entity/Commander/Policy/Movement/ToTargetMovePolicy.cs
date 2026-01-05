using GameDB;
using System.Threading;

public class ToTargetMovePolicy : MovePolicyBase
{
    // EntityBase _lastTarget;

    //PathListPoolable _cachedPath;

    //bool _isRequestingPath;

    //uint _pathVersion;

    // CancellationTokenSource _ctkSrc;

    public override MoveCommand GetCommand(EntityBase target)
    {
        if (target == null)
        {
            return MoveCommand.Stop;
        }

        return new MoveCommand()
        {
            result = target.Type == E_EntityType.Structure ? MoveCommandResult.Path : MoveCommandResult.Directional,
            // 강제로 ground 시킴
            destination = target.ApproxPosition.FlatHeight(),
            destEntityType = target.Type
        };
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
    }
}
