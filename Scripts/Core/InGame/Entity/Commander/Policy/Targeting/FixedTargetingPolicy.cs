
//public class FixedTargetingPolicyInitData : IInstancePoolInitData
//{
//    public IObjectiveProvider TargetProvider;

//    public void Set(IObjectiveProvider targetProvider)
//    {
//        TargetProvider = targetProvider;
//    }
//}

//public class FixedTargetingPolicy : ITargetSelectionPolicy
//{
//    IObjectiveProvider _targetProvider;

//    public FixedTargetingPolicy(IObjectiveProvider targetProvider)
//    {
//        _targetProvider = targetProvider;
//    }

//    public EntityBase FindTarget(EntityBase asker)
//    {
//        return _targetProvider.GetTargetEntity(asker.Team);
//    }

//    public void OnPoolActivated(IInstancePoolInitData initData)
//    {
//        var data = initData as FixedTargetingPolicyInitData;
//        _targetProvider = data.TargetProvider;
//    }

//    public void OnPoolInitialize()
//    {
//    }

//    public void OnPoolReturned()
//    {
//        _targetProvider = null;
//    }

//    public void ReturnToPool()
//    {
//        InGameManager.Instance.CacheContainer.EntityAIPool.Return(this);
//    }
//}
