using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;
using Cysharp.Threading.Tasks;

public class DefenseStructureEntity : StructureEntity
{
    EntityAIPartInitData _aiPartInitData;
    EntityMovePartInitData _movePartInitData;
    EntitySkillPartInitData _skillPartInitData;

    FXRangeIndicator _rangeIndicator;
    static readonly Color _rangeFarColor = new Color(1f, 0.2f, 0.2f, 0.3f);
    static readonly Color _rangeClosestColor = new Color(1f, 0.2f, 0.2f, 0.7f);

    float _skillRange;
    float _sqrSkillRange;
    bool _prevInRange;

    const int UpdateRangeThresholdFrame = 3;

    protected override void OnBeforeEnterNewPhase(EventContext cxt)
    {
        base.OnBeforeEnterNewPhase(cxt);

        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;
        switch (arg.NewPhase)
        {
            case InGamePhase.Peace:
                if (_rangeIndicator)
                {
                    _rangeIndicator.Return();
                    _rangeIndicator = null;
                }
                break;
            case InGamePhase.Battle:
                if (Team == EntityTeamType.Enemy)
                {
                    if (_rangeIndicator)
                    {
                        _rangeIndicator.Return();
                        _rangeIndicator = null;
                    }

                    _skillRange = SkillPart.GetRange(0);
                    _sqrSkillRange = _skillRange * _skillRange;

                    FXSystem.PlayFX_RangeIndicator(
                        _rangeFarColor,
                        _skillRange,
                        0f,
                        transform.position + new Vector3(0, 0.15f, 0),
                        onCompleted: (res) =>
                        {
                            if (EntityHelper.IsValid(this) == false)
                            {
                                res.Return();
                                return;
                            }

                            _rangeIndicator = res;
                        });
                }

                break;
        }
    }

    protected override void OnUpdateImpl()
    {
        base.OnUpdateImpl();

        if (Time.frameCount % UpdateRangeThresholdFrame != 0)
        {
            if (Team == EntityTeamType.Enemy && _rangeIndicator)
            {
                var playerCharacter = InGameManager.Instance.PlayerCommander.Player.Entity;
                if (EntityHelper.IsValid(playerCharacter))
                {
                    bool isInRange = _sqrSkillRange > Vector3.SqrMagnitude(playerCharacter.ApproxPosition.FlatHeight() - ApproxPosition.FlatHeight());

                    if (isInRange != _prevInRange)
                    {
                        _prevInRange = isInRange;
                        _rangeIndicator.SetColor(isInRange ? _rangeClosestColor : _rangeFarColor);
                    }
                }
                else
                {
                    _rangeIndicator.SetColor(_rangeFarColor);
                }
            }
        }
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _aiPartInitData = new EntityAIPartInitData(this);
        _movePartInitData = new EntityMovePartInitData(this);
        _skillPartInitData = new EntitySkillPartInitData(this);
    }

    protected override EntityAIPart CreateAIPart()
    {
        return InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityAIPart>(_aiPartInitData);
    }

    protected override EntitySkillPart CreateSkillPart()
    {
        var structureData = DBStructure.Get(TableData.DetailTableID);
        if (structureData.SkillSet == null || structureData.SkillSet.Length == 0)
            return null;

        var skillList = new List<EntitySkillBase>();
        for (int i = 0; i < structureData.SkillSet.Length; i++)
        {
            var skillData = DBSkill.Get(structureData.SkillSet[i]);
            if (skillData == null)
            {
                TEMP_Logger.Err($"Failed to get DefenseStructureEntity Skill Data | StructureID : {EntityTID} , SkillID : {structureData.SkillSet[i]}");
                return null;
            }

            // 지금은 건물은 스펠 지원 X 일단 이건 데이터 잘못넣은거
            if (skillData.SkillCategory != E_SkillCategoryType.Standard)
            {
                TEMP_Logger.Err($"DefenseSturcture only supports for Standard Skills | Current : {skillData.SkillCategory}");
                continue;
            }

            var skill = InGameManager.Instance.CacheContainer.EntitySkillSubPool.GetOrCreateSkill(StructureData.SkillSet[i], skillList.Count);
            skillList.Add(skill);
        }

        _skillPartInitData.SkillSet = skillList;
        var part = InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntitySkillPart>(_skillPartInitData);
        return part;
    }

    protected override EntityMovePartBase CreateMovePart()
    {
        var pivot = ModelPart.GetSocket(EntityModelSocket.Pivot);
        if (pivot == null)
            return null;

        _movePartInitData.Mover = pivot;

        return InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityPatrolMovePart>(_movePartInitData);
    }


    public override void OnInactivated()
    {
        base.OnInactivated();

        _skillRange = 0f;
        _sqrSkillRange = 0f;
        _prevInRange = false;

        if (_rangeIndicator)
        {
            _rangeIndicator.Return();
            _rangeIndicator = null;
        }
    }
}
