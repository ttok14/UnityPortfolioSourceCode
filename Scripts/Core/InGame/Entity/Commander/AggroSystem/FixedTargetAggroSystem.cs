using UnityEngine;
using GameDB;

// 전용 디펜스 모드 어그로 시스템 
public class FixedTargetAggroSystem : AggroSystemBase
{
    public DefenseMode _mode;

    public FixedTargetAggroSystem(DefenseMode mode)
    {
        _mode = mode;
    }

    public override EntityBase FindTarget(EntityBase asker)
    {
        var targetEntity = _mode.GetTargetEntity(asker.Team);
        if (targetEntity == null)
            return null;

        if (asker.SkillPart == null || asker.SkillPart.SkillCount == 0)
            return null;

        var statData = asker.GetData(EntityDataCategory.Stat) as EntityStatData;
        var sqrDist = Vector3.SqrMagnitude(targetEntity.ApproxPosition - asker.ApproxPosition);

        if (sqrDist <= statData.ScanRange * statData.ScanRange)
            return targetEntity;

        return null;

        //var skillIdx = asker.SkillPart.GetBestAvailableSkillIdx(_mode.CurrentTargetEntity);
        //if (skillRange == 0)
        //    return null;

        //var distToTarget = Vector3.Distance(_mode.CurrentTargetEntity.transform.position, asker.transform.position);
        //if (distToTarget < skillRange + _mode.CurrentTargetEntity.ModelPart.VolumeRadius)
        //{
        //    return _mode.CurrentTargetEntity;
        //}

        return null;
    }
}
