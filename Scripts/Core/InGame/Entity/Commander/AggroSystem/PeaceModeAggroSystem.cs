using UnityEngine;
using GameDB;

// 적 전용 디펜스 모드 어그로 시스템 
public class PeaceModeAggroSystem : AggroSystemBase
{
    public override EntityBase FindTarget(EntityBase asker)
    {
        return null;
    }
}
