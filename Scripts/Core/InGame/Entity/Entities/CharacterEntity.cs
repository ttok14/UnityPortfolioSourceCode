using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class CharacterEntity : EntityBase
{
    public E_CharacterType CharacterType { get; private set; }
    public CharacterTable CharacterData { get; private set; }

    public bool IsRidingPet => _pet != null;
    EntityBase _pet;
    public EntityBase Pet => _pet;

    bool _createdHud;
    public void HudRemoved() => _createdHud = false;

    static EntityAIPartInitData _aiPartInitData;
    static EntityMovePartInitData _defaultMovePartInitData;
    static EntitySkillPartInitData _skillPartInitData;
    static EntitySpellPartInitData _spellPartInitData;
    static EntityStatisticInitData _statisticInitData;

    float _lastFootstepSfxAt;

    FXBase _trailFx;

    public override Vector3 RealForward
    {
        get
        {
            if (IsRidingPet)
                return _pet.transform.forward;
            else
                return base.RealForward;
        }
    }

    public override EntityMovePartBase SubMovePart => IsRidingPet ? _pet.MovePart : MovePart;

    // 전투모드에서 플레이어 캐릭터의 directionalMove 때 충돌체에 막히는 체크하기 위함
    // CharacterController _characterController;

    public override void OnInitializeFinished()
    {
        base.OnInitializeFinished();

        CharacterData = DBCharacter.Get(TableData.DetailTableID);
        CharacterType = CharacterData.CharacterType;

        if (CharacterType == E_CharacterType.Actor)
        {
            _statisticInitData.SetBaseInitData(this, ID, EntityDataCategory.Statistic, EntityTID);
            AddDataBase(EntityDataCategory.Statistic, InGameManager.Instance.CacheContainer.EntityDataPool.GetOrCreate<EntityStatisticData>(_statisticInitData));
        }

        HpChangedListener += OnHpChanged;

        if (string.IsNullOrEmpty(CharacterData.FootStepAudioKey) == false)
            MovementProcessingListener += OnMoved;

        if (string.IsNullOrEmpty(CharacterData.MoveTrailFXKey) == false)
        {
            var trailSocket = ModelPart.GetSocket(EntityModelSocket.Trail);
            if (trailSocket)
            {
                FXSystem.PlayFXCallBack(CharacterData.MoveTrailFXKey,
                    parent: trailSocket,
                    onCompleted: (fx) =>
                    {
                        if (EntityHelper.IsValid(this))
                        {
                            _trailFx = fx;
                            fx.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            fx.Return();
                        }
                    }).Forget();
            }
            else
            {
                TEMP_Logger.Err($"Dust Trail Exist but Socket does not exist | Name : {TableData.Name} , {gameObject.name}");
            }
        }
    }

    void OnMoved(EntityBase executor, Vector3 position)
    {
        if (string.IsNullOrEmpty(CharacterData.FootStepAudioKey) == false)
        {
            if (_lastFootstepSfxAt + 0.6f < Time.time)
            {
                _lastFootstepSfxAt = Time.time;
                AudioManager.Instance.Play(CharacterData.FootStepAudioKey, position, AudioTrigger.Default);
            }
        }
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _aiPartInitData = new EntityAIPartInitData(this);
        _defaultMovePartInitData = new EntityMovePartInitData(this, this.transform);
        _skillPartInitData = new EntitySkillPartInitData(this);
        _spellPartInitData = new EntitySpellPartInitData(this);
        _statisticInitData = new EntityStatisticInitData();
    }

    private void OnHpChanged(int maxHP, int currentHP, int diff)
    {
        if (_createdHud == false && currentHP > 0)
        {
            if (InGameManager.Instance.CurrentPhaseType == InGamePhase.Battle)
            {
                bool show = EntityHelper.IsValid(this) && currentHP > 0 && currentHP != maxHP;

                if (show)
                {
                    _createdHud = true;

                    UIManager.Instance.ShowCallBack<UICharacterHud>(
                        UITrigger.Default,
                        new UICharacterHud.Arg(ID, CharacterData.CharacterType != E_CharacterType.Actor, transform, new Vector2(0, 200)),
                        (res) =>
                        {
                            if (IsAlive == false)
                            {
                                res.Return();
                            }
                        }).Forget();
                }
            }
        }
    }

    public async UniTask CutSceneEnterPhase(InGamePhase phase)
    {
        bool done = false;

        if (phase == InGamePhase.Peace)
        {
            FXSystem.PlayFXCallBack("FX_Trans", startPosition: transform.position,
                onCompleted: (res) =>
                {
                    if (CharacterType == E_CharacterType.Actor)
                    {
                        GetData<EntityStatisticData>().ResetKillCount();
                        ModelPart.SetEquipmentActive(EntityEquipmentType.Weapon, false);

                        LeavePet();
                    }

                    AnimationPart.ResetParameters();

                    done = true;
                }).Forget();
        }
        else if (phase == InGamePhase.Battle)
        {
            FXSystem.PlayFXCallBack("FX_Trans", startPosition: transform.position,
                onCompleted: (res) =>
                {
                    if (CharacterType == E_CharacterType.Actor)
                    {
                        ModelPart.SetEquipmentActive(EntityEquipmentType.Weapon, true);

                        // 호랭이 태우기~ 일단 하드코딩
                        RidePet(359, () =>
                           {
                               done = true;
                           });
                    }
                }).Forget();
        }

        await UniTask.WaitUntil(() => done);
    }

    protected override void OnBeforeEnterNewPhase(EventContext cxt)
    {
        base.OnBeforeEnterNewPhase(cxt);

        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;
        if (arg.NewPhase == InGamePhase.Peace)
        {
            if (Team == EntityTeamType.Enemy)
            {
                // FXSystem.PlayFX("SpriteFX_DustShattered", () => transform.position);
            }
        }
        else if (arg.NewPhase == InGamePhase.Battle)
        {
            SkillPart.Reset();
        }
    }

    public override void OnInactivated()
    {
        if (IsRidingPet)
            LeavePet();

        if (_trailFx)
        {
            _trailFx.Return();
            _trailFx = null;
        }

        _createdHud = false;
        HpChangedListener -= OnHpChanged;
        MovementProcessingListener -= OnMoved;

        base.OnInactivated();
    }

    protected override EntityAIPart CreateAIPart()
    {
        _aiPartInitData.Owner = this;
        return InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityAIPart>(_aiPartInitData);
    }

    protected async override UniTask PlayDieCutScene(Vector3 forcePosition, float force, CancellationToken ctk)
    {
        if (CharacterData.DropItemID != 0)
        {
            EntityManager.Instance.CreateEntity(new EntityObjectData(
                ApproxPosition,
                0,
                CharacterData.DropItemID,
                _team == EntityTeamType.Player ? EntityTeamType.Enemy : EntityTeamType.Player)).Forget();
        }

        // 사이즈에 따라서 Force 를 조정해주어야함.
        // 덩치작은 고블린보다는 덩치큰 골렘이 더 적은 힘으로 날아가야 자연스러움
        force = EntityHelper.ApplySizeToForce(force, CharacterData.CharacterSize);

        // 충격이 발생한 곳을 바라본 상태에서 날아가게 lookat 함 
        forcePosition.y = ApproxPosition.y;
        transform.LookAt(forcePosition);
        AnimationPart.SetParameter(EntityAnimationParameterType.Die);

        // 죽는 연출에선 movePart 말고 그냥 직접 조작하자
        Vector3 direction = transform.forward * -1;

        Vector3 startPos = ApproxPosition;
        Vector3 destPos = ApproxPosition + direction * force;

        // Force 도 미세하기 랜덤성 적용해서
        // 같은 Force 라도 미세한 변동성줌 
        force = UnityEngine.Random.Range(force, force + 1f);

        var curve = EntityManager.Instance.CurveData.CharacterDieHeightCurveData.curve;

        // Duration 도 미세하게 랜덤성 적용 ㄱ
        // Force 가 강할수록 조금 이동 시간을 늘림
        float duration = UnityEngine.Random.Range(0.6f, 0.8f) + Mathf.Lerp(0f, 1f, force / 6f);
        float t = 0f;
        Ease easeType = Ease.OutExpo;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            var newPos = CurveHelper.MoveEaseWithCurve(curve, t, startPos, destPos, easeType, force);

            SetPosition(newPos);

            await UniTask.Yield();
            if (ctk.IsCancellationRequested)
                return;
        }

        SetPosition(destPos);

        try
        {
            await UniTask.WaitForSeconds(2f, cancellationToken: ctk);
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    public async override UniTask OnDie(ulong attackerId, Vector3 attackerPosition, float force)
    {
        var attacker = EntityManager.Instance.GetEntity(attackerId);
        if (attacker)
        {
            // 사망했으니 공격자의 statistic 에 접근해 KillCount 올림
            var statisticData = attacker.GetData(EntityDataCategory.Statistic) as EntityStatisticData;
            if (statisticData != null)
                statisticData.IncreaseKillCount(1);
        }

        if (IsRidingPet)
            LeavePet();

        ModelPart.SetEquipmentActive(EntityEquipmentType.Weapon, false);

        await base.OnDie(attackerId, attackerPosition, force);
    }

    //protected override IEnumerator PlayDieCutSceneCo(Vector3 forcePosition, float force, Action onCompleted)
    //{
    //    var characterData = DBCharacter.Get(TableData.DetailTableID);

    //    if (characterData.DropItemID != 0)
    //    {
    //        EntityManager.Instance.CreateEntity(new EntityObjectData(transform.position, 0, characterData.DropItemID),
    //            _team == EntityTeamType.Player ? EntityTeamType.Enemy : EntityTeamType.Player);
    //    }

    //    forcePosition.y = transform.position.y;
    //    transform.LookAt(forcePosition);
    //    AnimationPart.SetParameter(EntityAnimationParameterType.Die);

    //    // 죽는 연출에선 movePart 말고 그냥 직접 조작하자
    //    Vector3 direction = transform.forward * -1;

    //    Vector3 startPos = transform.position;
    //    Vector3 destPos = transform.position + direction * force;
    //    var curve = EntityManager.Instance.CurveData.CharacterDieHeightCurveData.curve;
    //    float duration = 0.6f;
    //    float t = 0f;
    //    Ease easeType = Ease.OutQuad;

    //    while (t < 1f)
    //    {
    //        t += Time.deltaTime / duration;
    //        transform.position = CurveHelper.MoveEaseWithCurve(curve, t, startPos, destPos, easeType, force);
    //        yield return null;
    //    }

    //    transform.position = destPos;

    //    yield return CoroutineRunner.Instance.WaitForSeconds(2.5f);

    //    onCompleted.Invoke();
    //}

    //private void OnSkillIKValueChanged(EntityBase executor, bool isLeftOrRight)
    //{
    //    if (isLeftOrRight)
    //    {
    //        ModelPart.SwitchEquipmentSocket(EntityEquipmentType.Weapon, EntityModelSocket.LeftHand);
    //    }
    //    else
    //    {
    //        ModelPart.SwitchEquipmentSocket(EntityEquipmentType.Weapon, EntityModelSocket.RightHand);
    //    }
    //}

    protected override EntityMovePartBase CreateMovePart()
    {
        _defaultMovePartInitData.Owner = this;
        _defaultMovePartInitData.Mover = transform;

        return InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntityStandardMovePart>(_defaultMovePartInitData);
    }

    protected override EntitySkillPart CreateSkillPart()
    {
        var characterData = DBCharacter.Get(TableData.DetailTableID);

        uint[] skillSetIds;

        // TODO : 추후에 수정해야겟쥬?
        if (characterData.CharacterType == E_CharacterType.Actor)
            skillSetIds = Me.GetSkills();
        else
            skillSetIds = characterData.SkillSet;

        if (skillSetIds == null || skillSetIds.Length == 0)
            return null;

        List<EntitySkillBase> skillList = null;
        for (int i = 0; i < skillSetIds.Length; i++)
        {
            var skillData = DBSkill.Get(skillSetIds[i]);
            if (skillData == null)
            {
                TEMP_Logger.Err($"Failed to get Character Skill Data | CharacterEntity ID : {EntityTID} , SkillID : {skillSetIds[i]}");
                return null;
            }

            if (skillData.SkillCategory != E_SkillCategoryType.Standard)
                continue;

            if (skillList == null)
                skillList = new List<EntitySkillBase>();

            var skill = InGameManager.Instance.CacheContainer.EntitySkillSubPool.GetOrCreateSkill(skillSetIds[i], skillList.Count);

            skillList.Add(skill);
        }

        if (skillList == null)
            return null;

        _skillPartInitData.Owner = this;
        _skillPartInitData.SkillSet = skillList;

        var part = InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntitySkillPart>(_skillPartInitData);
        part.UseAnimationIK = characterData.UseIK;
        return part;
    }

    protected override EntitySpellPart CreateSpellPart()
    {
        var characterData = DBCharacter.Get(TableData.DetailTableID);

        uint[] spellSetIds;

        // TODO : 추후에 수정해야겟쥬?
        if (characterData.CharacterType == E_CharacterType.Actor)
            spellSetIds = Me.GetSpells();
        else
            spellSetIds = characterData.SkillSet;

        if (spellSetIds == null || spellSetIds.Length == 0)
            return null;

        List<EntitySkillBase> skillList = null;

        for (int i = 0; i < spellSetIds.Length; i++)
        {
            var spellId = spellSetIds[i];
            var skillData = DBSkill.Get(spellId);
            if (skillData == null)
            {
                TEMP_Logger.Err($"Failed to get Character Skill Data | CharacterEntity ID : {EntityTID} , SkillID : {spellId}");
                return null;
            }

            if (skillData.SkillCategory != E_SkillCategoryType.Spell)
                continue;

            if (skillList == null)
                skillList = new List<EntitySkillBase>();

            var spell = InGameManager.Instance.CacheContainer.EntitySkillSubPool.GetOrCreateSkillSpell(
                spellId,
                skillList.Count,
                AIPart.TargetGetter);

            skillList.Add(spell);
        }

        if (skillList == null)
            return null;

        _spellPartInitData.Owner = this;
        _spellPartInitData.SpellSet = skillList;

        return InGameManager.Instance.CacheContainer.EntityPartsPool.GetOrCreate<EntitySpellPart>(_spellPartInitData);
    }

    public void RidePet(uint animalEntityID, Action onCompleted = null)
    {
        var petData = DBEntity.Get(animalEntityID);
        if (petData == null || DBAnimal.IsRidable(petData.DetailTableID) == false)
        {
            TEMP_Logger.Err($"This is not ridable pet");
            onCompleted?.Invoke();
            return;
        }

        EntityManager.Instance.CreateEntityCallBack(new EntityObjectData(ApproxPosition, (int)transform.eulerAngles.y, animalEntityID, Team), default, (res) =>
         {
             _pet = res;

             AnimationPart.SetParameter(EntityAnimationParameterType.RidingPet, true);

             var saddleSocket = res.ModelPart.GetSocket(EntityModelSocket.Saddle);
             SetPosition(saddleSocket.position);
             transform.SetParent(saddleSocket, true);

             onCompleted?.Invoke();
         }).Forget();
    }

    public void LeavePet()
    {
        if (_pet == null)
            return;

        AnimationPart.SetParameter(EntityAnimationParameterType.RidingPet, false);

        transform.SetParent(null, true);
        transform.rotation = Quaternion.identity;
        SetPosition(new Vector3(ApproxPosition.x, 0, ApproxPosition.z));

        // Pet 의 하위에 들어가있는 Character 가 Pet 이 Remove 되는 순간 Active 가 꺼지기 때문에
        // Character 의 AnimatorStateBehaviour 관련 Disable 되는 이슈 발생. 이 문제 방지로
        // 순서를 지켜주어야함. 
        EntityManager.Instance.RemoveEntity(_pet);
        _pet = null;

        MovePart.Stop();
    }
}
