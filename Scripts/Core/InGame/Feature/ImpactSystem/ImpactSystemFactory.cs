using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;

// TODO : 이거는 projectile 로 뺴야하나? 아니면 projectile table 값 조회해서
// factory 가 적절히 만들어줘야하나?
public enum ImpactType
{
    Standard,
}

public static class ImpactSystemFactory
{
    private static Dictionary<ImpactType, ImpactStrategyBase> _impactExecutors = new Dictionary<ImpactType, ImpactStrategyBase>()
    {
        [ImpactType.Standard] = new StandardImpactStrategy()
    };

    public static ImpactStrategyBase CreateStrategy(ImpactType type)
    {
        if (_impactExecutors.TryGetValue(type, out var res))
            return res;

        return null;
    }
}
