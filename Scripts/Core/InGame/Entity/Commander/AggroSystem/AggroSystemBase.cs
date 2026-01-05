using UnityEngine;
using GameDB;

public abstract class AggroSystemBase
{
    public virtual void Initialize()
    {
    }

    public virtual void Release()
    {

    }

    public abstract EntityBase FindTarget(EntityBase asker);

    //public EntityBase FindTarget(EntityBase asker, int skillIdx = 0)
    //{
    //    if (_team == asker.Team)
    //    {
    //        TEMP_Logger.Err($"Target cannot be ");
    //        return null;
    //    }

    //    var statData = DBStat.Get(asker.TableData.StatTableID);

    //    // 충돌 LayerMask 를 최소화하기 위한 로직, 하지만 Weight 가 0 이라도
    //    // Policy 에서 충돌 체크가 필요한 경우라면 반드시 포함시켜주어야 할 것 .
    //    int targetLayer =
    //            (statData.AggroWeight_Structure > 0f ? _targetStructureLayerMask : 0) |
    //            (statData.AggroWeight_Character > 0f ? _targetCharacterLayerMask : 0);

    //    if (_policy == EntityAggroPolicy.Default)
    //    {
    //        var pos = asker.transform.position;
    //        pos.y = 0;

    //        int collisionCount = Physics.OverlapSphereNonAlloc(pos, asker.SkillPart.GetRange(skillIdx), _collisionResults, targetLayer);

    //        int characterHighestScore = (int)(statData.AggroWeight_Character * 100);
    //        int structureHighestScore = (int)(statData.AggroWeight_Structure * 100);

    //        for (int i = 0; i < collisionCount; i++)
    //        {
    //            var entity = _collisionResults[i].GetComponent<EntityBase>();
    //            if (!entity)
    //            {
    //                TEMP_Logger.Err($"NO Entity on collided object, this is Fatal Error Mustcheck | asker ID: {asker.ID}, Tid: {asker.EntityTID}");
    //                continue;
    //            }

    //            if (entity.Type == E_EntityType.Character && )
    //                {
    //        }
    //    }

    //    return
    //}
}
