using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using GameDB;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour, IInteractor
{
    CharacterEntity _entity;
    public CharacterEntity Entity => _entity;
    public bool IsAlive => EntityHelper.IsValid(_entity);

    [SerializeField]
    CinemachineImpulseSource _impulseSource;

    private PathLineRenderController _pathIndicater;

    float _nextAutoBubbleAt;

    RaycastHit[] _raycastHits = new RaycastHit[5];
    int _pickingLayer;

    float _lastUserInputProcessedTime;

    public void Initialize()
    {
        transform.position = default;
        transform.rotation = Quaternion.identity;

        _pickingLayer = LayerUtils.GetLayerMask((EntityTeamType.Player, E_EntityType.Structure));

        PoolManager.Instance.RequestSpawnAsyncCallBack<PathLineRenderController>(ObjectPoolCategory.Default, "PathIndicator", onCompleted: (res, opRes) =>
        {
            res.gameObject.SetActive(false);
            _pathIndicater = res;
        }).Forget();

        EventManager.Instance.Register(GLOBAL_EVENT.USER_INPUT, OnUserInput);
        EventManager.Instance.Register(GLOBAL_EVENT.BEFORE_ENTER_INGAME_NEW_PHASE, OnEnterNewPhase);
        InGameManager.Instance.EventListener += OnInGameEvent;
    }

    public void Release()
    {
        InGameManager.Instance.EventListener -= OnInGameEvent;
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.Start)
        {
            if (PlayerInteractionManager.Instance.CurrentInteraction == PlayerInteractionType.WithEntity)
            {
                PlayerInteractionManager.Instance.EndInteraction();
                _lastUserInputProcessedTime = Time.time;
            }
        }
        else if (evt == InGameEvent.HugeImpact)
        {
            if (IsAlive)
            {
                var arg = argBase as EntityHugeImpactEventArg;
                var sqrDist = Vector3.SqrMagnitude(arg.Position.FlatHeight() - AudioManager.Instance.ListenerPosition.FlatHeight());

                if (sqrDist <= Constants.InGame.SqrImpactImpulsePercetibleRange)
                {
                    _impulseSource.GenerateImpulse(UnityEngine.Random.insideUnitSphere * arg.Force);
                }
            }
        }
        else if (evt == InGameEvent.RequestCameraImpulse)
        {
            if (IsAlive)
            {
                _impulseSource.GenerateImpulse(UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(2f, 3f));
            }
        }
    }

    int _respawnCount;
    public int CreateCharacterEntity()
    {
        var nexus = EntityManager.Instance.GetNexus(EntityTeamType.Player);
        if (nexus == null)
        {
            TEMP_Logger.Err($"Create Nexus First!");
            return _respawnCount;
        }

        var data = new EntityObjectData(nexus.ModelPart.GetSocket(EntityModelSocket.ActionPoint).position, 0, 352, EntityTeamType.Player);
        GenerateCharacter(data);
        return ++_respawnCount;
    }

    private void OnEnterNewPhase(EventContext cxt)
    {
        if (_entity == null)
            return;

        _entity.MovePart.Stop();
        _entity.SubMovePart.Stop();

        var arg = cxt.Arg as EnterInGamePhaseEventArgBase;

        if (arg.NewPhase == InGamePhase.Peace)
        {
            UIManager.Instance.Hide<UIJoystickFrame>();
        }
        else if (arg.NewPhase == InGamePhase.Battle)
        {
            UIManager.Instance.ShowCallBack<UIJoystickFrame>(arg: new UIJoystickFrame.Arg()
            {
                onTouched = OnJoystickTouched,
                onReleased = OnJoystickReleased
            }).Forget();
        }
    }

    private void OnJoystickTouched(Vector2 direction, float amountNormalized)
    {
        if (EntityHelper.IsValid(_entity))
            _entity.SubMovePart.MoveDirectional(new Vector3(direction.x, 0, direction.y), amountNormalized);
    }

    private void OnJoystickReleased()
    {
        if (EntityHelper.IsValid(_entity))
            _entity.SubMovePart.Stop();
    }

    private void GenerateCharacter(EntityObjectData objectData)
    {
        TEMP_Logger.Deb($"GenerateCharacter | id : {objectData.entityId} , position : {objectData.worldPosition} , team : {objectData.teamType}");

        EntityManager.Instance.CreateEntityCallBack(
            objectData,
            default,
            (resEntity) =>
            {
                _entity = resEntity as CharacterEntity;

                resEntity.MovementBeginListener += OnCharacterMoveBegin;
                resEntity.MovementEndListener += OnCharacterMoveEnd;

                UpdateNextAutoBubbleTime();

                resEntity.DamagedListener += OnDamaged;
                resEntity.DiedListener += OnDie;

                InGameManager.Instance.PublishEvent(InGameEvent.PlayerCharacterSpawned, new PlayerCharacterRespawnEventArg() { Entity = resEntity });

                UIManager.Instance.ShowCallBack<UIKillStreak>().Forget();

                TEMP_Logger.Deb($"Player Respawned ! | id : {resEntity.ID} | activeSelf : {resEntity.gameObject.activeSelf} , activeINHier : {resEntity.gameObject.activeInHierarchy}");
            }).Forget();
    }

    private void OnDamaged(ulong executorId, int damaged, Vector3 effectPos, float effectForce)
    {
        float impulseForce = MathF.Min(effectForce - 3f, 1f);

        if (impulseForce > 0f)
            _impulseSource.GenerateImpulse(UnityEngine.Random.insideUnitSphere * effectForce);
    }

    void OnDie(ulong attackerId, Vector3 attackerPosition)
    {
        UIManager.Instance.Hide<UIKillStreak>();
        UIManager.Instance.Hide<UIJoystickFrame>();

        var pos = _entity.ApproxPosition.FlatHeight();
        _entity = null;
        InGameManager.Instance.PublishEvent(InGameEvent.PlayerCharacterDied, new PlayerCharacterDiedEventArg() { DiedPosition = pos });
    }

    private void OnCharacterMoveBegin(EntityBase executor, bool pathsOrDirectional, Vector3? directionalMoveDir = null, List<Vector3> paths = null)
    {
        if (pathsOrDirectional && _pathIndicater)
        {
            _pathIndicater.gameObject.SetActive(true);
            _pathIndicater.ChangeSettings(paths.ToArray());
        }
    }

    private void OnCharacterMoveEnd(EntityBase executor, bool reachedDest)
    {
        if (_pathIndicater)
        {
            _pathIndicater.gameObject.SetActive(false);
        }
    }

    string[] _peaceAutoBubbleTextBuffer = new string[]
    {
        "내거를 노리는 놈들이 너무 많아",
        "마을 사람들 일 열심히 하고 있나?",
        "고기먹고싶다"
    };

    string[] _combatBubbleTextBuffer = new string[]
    {
        "다 때려 부숴라!!",
        "이번 전투를 반드시 이겨야 한다 !!",
        "악!!!"
    };

    EntityBase test_enemy;

    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            EntityManager.Instance.CreateEntityCallBack(new EntityObjectData(_entity.transform.position.FlatHeight(), 0, 353, EntityTeamType.Enemy),
                default,
                (res) =>
                 {
                     test_enemy = res;
                 }).Forget();
        }

        //if (Keyboard.current.eKey.wasPressedThisFrame)
        //{
        //    var ids = new uint[] { 360, 144, 145, 147, 163 };
        //    uint id = ids[UnityEngine.Random.Range(0, ids.Length)];
        //    EntityPlacementManager.Instance.StartPlacement(id);
        //}

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            //#if UNITY_EDITOR
            //            System.IO.File.WriteAllText(@"D:\Projects\JayceDefense\Log01.md", EntityBase.UpdatableDebugHistory.ToString());
            //            System.IO.File.WriteAllText(@"D:\Projects\JayceDefense\Log02.md", UpdateManager.DebugHistory.ToString());
            //#endif

            //var comp = FindAnyObjectByType<CinemachineImpulseSource>();
            //// 여러 개의 랜덤 방향으로 작은 임펄스들을 발생
            //for (int i = 0; i < 3; i++)
            //{
            //    Vector3 randomDir = new Vector3(
            //        UnityEngine.Random.Range(-1f, 1f),
            //        UnityEngine.Random.Range(-1f, 1f),
            //        0f
            //    ).normalized;
            //    comp.GenerateImpulse(UnityEngine.Random.insideUnitSphere * 0.15f);
            //        return; 
            //    comp.GenerateImpulseWithVelocity(
            //        randomDir * UnityEngine.Random.Range(10, 20)
            //    );
            //}

            // Debug.LogError(MapManager.Instance.CanPlace(Entity.transform.position, Entity.TableData.EntityFlags));
        }


        // TODO : 테스트용
        // TODO : 기본 공격은 일단 AI 파트에서 자동으로 근처 몬스터 타겟팅해서 쏘는 시스템을 만들어줘야함
        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{
        //    _entity.SkillPart.RequestUseSkill(new EntitySkillTriggerContext()
        //    {
        //        Executor = _entity,
        //        Target = test_enemy,
        //        SlotIdx = 0,
        //        SkillEquipment = EntityEquipmentType.Weapon
        //    });
        //if (_entity.SkillPart.IsCasting == false)
        //    _entity.SkillPart.RequestUseSkill(new EntitySkillTriggerContext();
        //}

        //if (Keyboard.current.zKey.wasPressedThisFrame)
        //{
        //    _entity.SkillPart.RequestUseSkill(new EntitySkillTriggerContext()
        //    {
        //        Executor = _entity,
        //        // Target = test_enemy,
        //        //FixedPoint = _entity.transform.position + new Vector3(10, 0, 0),
        //        SlotIdx = 1,
        //        // StartSocket = _entity.ModelPart.GetEquimentTransform(EntityEquipmentType.OneHandedWeapon)
        //    });
        //    //if (_entity.SkillPart.IsCasting == false)
        //    //    _entity.SkillPart.RequestUseSkill(new EntitySkillTriggerContext();
        //}

        if (!_entity)
            return;

        if (Time.time >= _nextAutoBubbleAt)
        {
            UpdateNextAutoBubbleTime();

            string text = InGameManager.Instance.CurrentPhaseType == InGamePhase.Peace ?
                _peaceAutoBubbleTextBuffer[UnityEngine.Random.Range(0, _peaceAutoBubbleTextBuffer.Length)] :
                _combatBubbleTextBuffer[UnityEngine.Random.Range(0, _combatBubbleTextBuffer.Length)];

            UIManager.Instance.ShowCallBack<UIBubbleFrame>(
                UITrigger.Default,
                new UIBubbleFrame.Arg(
                    text,
                    new Vector3(0, 250),
                    _entity.transform)).Forget();
        }
    }

    private void OnUserInput(EventContext cxt)
    {
        if (Time.time - _lastUserInputProcessedTime < 0.85f)
            return;

        if (_entity == null)
            return;

        var inputBaseArg = cxt.Arg as InputEventBaseArg;
        if (inputBaseArg.InputType == UserInputType.PressUp)
        {
            var inputArg = inputBaseArg as InputPrimaryPressUpEventData;

            if (inputArg.OtherModeAlreadyTriggered)
                return;

            if (PlayerInteractionManager.Instance.CurrentInteraction == PlayerInteractionType.WithEntity)
            {
                bool isPointerOnUI = InputManager.IsCurrentPointerOverUI(inputBaseArg.ScreenPosition); // EventSystem.current.IsPointerOverGameObject();

                if (isPointerOnUI == false)
                {
                    PlayerInteractionManager.Instance.EndInteraction();
                    _lastUserInputProcessedTime = Time.time;
                    return;
                }
            }
            else
            {
                if (InGameManager.Instance.CurrentPhaseType == InGamePhase.Peace)
                {
                    if (DoInteractionRaycast(inputArg.ScreenPosition))
                    {
                        _lastUserInputProcessedTime = Time.time;
                        cxt.Use();
                        return;
                    }

                    if (DoPathMove(inputArg.ScreenPosition))
                    {
                        _lastUserInputProcessedTime = Time.time;
                        cxt.Use();
                        return;
                    }
                }
            }
        }
    }

    bool DoPathMove(Vector2 screenPosition)
    {
        var cam = CameraManager.Instance.MainCam;
        var pointOnTerrain = MapUtils.ScreenPosToWorldPos(cam, screenPosition);
        if (pointOnTerrain.HasValue)
        {
            //if (_moveCo != null)
            //{
            //    CoroutineRunner.Instance.StopCoroutine(_moveCo);
            //}

            //_moveCo = CoroutineRunner.Instance.RunCoroutine(Move(pointOnTerrain.Value));

            _entity.MovePart.MoveToPathFinding(pointOnTerrain.Value);

            return true;
        }

        return false;
    }

    bool DoInteractionRaycast(Vector2 screenPosition)
    {
        if (Physics.RaycastNonAlloc(CameraManager.Instance.MainCam.ScreenPointToRay(screenPosition),
                _raycastHits,
                200,
                // 실제 Interactable 이 가능한 단위의 Entity 를 먼저
                // Layer 로 나누면 조금이라도 연산이 줄지 않을까 싶은데.
                // 일단 Structure 만 . 하지만 EntityBase 가 Interactable 을 상속받아
                // 유연학 구현된 만큼 , 이부분은 수정이 추후 불가피할듯 
                _pickingLayer) > 0)
        {
            var interactable = _raycastHits[0].collider.GetComponentInParent<IInteractable>();
            if (interactable.OnInteract(this, new PlayerInteractContext()))
            {
                return true;
            }
        }
        return false;
    }

    void UpdateNextAutoBubbleTime()
    {
        if (_entity == null)
            return;

        _nextAutoBubbleAt = Time.time + UnityEngine.Random.Range(Constants.UI.NextAutoBubbleRandomMin, Constants.UI.NextAutoBubbleRandomMax);
    }

    private void OnDestroy()
    {
        if (PoolManager.HasInstance)
        {
            if (_pathIndicater)
            {
                PoolManager.Instance.Return(_pathIndicater);
                _pathIndicater = null;
            }
        }
    }

    //    IEnumerator Move(Vector3 to)
    //    {
    //        _resultPaths.Clear();

    //        // TODO : TEST TEST 수정할것 . 근데 objectFlag 강튼거,
    //        // 캐릭은 상태가 바뀔수있을까?
    //        yield return PathFindingManager.Instance.PathFinder.FindPath(
    //            // MapUtils.WorldPosToTilePos(transform.position),
    //            transform.position,
    //            to,
    //            // MapUtils.WorldPosToTilePos(to),
    //            _resultPaths,
    //            GameDB.E_EntityFlags.GroundedEntity | GameDB.E_EntityFlags.Requires_Walkable_Ground | GameDB.E_EntityFlags.Require_PlacementCondition);

    //        if (_resultPaths.Count == 0)
    //        {
    //            yield break;
    //        }

    //#if UNITY_EDITOR
    //        gizmos_paths = _resultPaths.ToList();
    //#endif

    //        if (_pathIndicater)
    //        {
    //            _pathIndicater.gameObject.SetActive(true);
    //            _pathIndicater.ChangeSettings(_resultPaths.ToArray());
    //        }

    //        // TODO : 킹샷처럼 initial rotation 처리는 처음에 하는게 좋겠지
    //        // 이거를 별도 상태머신으로 빼는게 좋을까? 그러겠쟈?
    //        // 더 나아가서 추상화 설계를 해보자 . 이동에 관한 .
    //        int curDestIdx = 0;
    //        bool rotateEnabled = true;

    //        // [0] 에는 항상 Start 위치넣어주니 그냥 빼자
    //        _resultPaths.RemoveAt(0);

    //        while (curDestIdx < _resultPaths.Count)
    //        {
    //            // var destWorldPos = MapUtils.TilePosToWorldPos(result[curDestIdx].Position);
    //            var dirToTarget = _resultPaths[curDestIdx] - transform.position;
    //            dirToTarget.y = 0;
    //            dirToTarget.Normalize();

    //            // 1. 지금 방식은 매번 이동 시작전 회전이 끝난 후에 이동을 시작하기때문에
    //            // 뚝뚝 끊김. 그렇다고 forward 로 이동을 시키게되면 지금 이동 로직의 종료
    //            // 조건으로서는 회전이 덜된 상태에서 dest에 도착하게되면 dest 에 충분히
    //            // 가깝게 가지못해 종료되지 않고 뻉뻉이 도는 현상이 발현됨
    //            // 그래서, 추후 이 뚝 끊기는 느낌과 뺑뻉이를 동시에 해결하려면
    //            // 이동 경로를 한번 베지어나 spline 방식으로 곡선 포인트를 찍어줘야할거같음. 그러면 이때는
    //            // 시작할때만 rotation 하고 , 중간에 이동해서 멈추는 포인트에서는 그냥 forward 로만가도되지않을까?
    //            // 2. 중간 이동중 회전은 조금더 속도를 빠르게 해줄까?
    //            // 3. 아니면 이동 종료 조건을 개선할까. 추후작업할것 

    //            if (rotateEnabled)
    //            {
    //                float rotationAmount = _rotationSpeed * Time.deltaTime;
    //                float signedAngle = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);

    //                if (rotationAmount >= MathF.Abs(signedAngle))
    //                {
    //                    transform.forward = dirToTarget;
    //                    rotateEnabled = false;
    //                }
    //                else
    //                {
    //                    transform.Rotate(0, rotationAmount * MathF.Sign(signedAngle), 0);
    //                }
    //            }

    //            bool translate = rotateEnabled == false;

    //            if (translate)
    //            {
    //                Vector3 movement = transform.forward * _moveSpeed * Time.deltaTime;
    //                if (movement.sqrMagnitude < (_resultPaths[curDestIdx] - transform.position).sqrMagnitude)
    //                {
    //                    transform.position += movement;
    //                }
    //                else
    //                {
    //                    transform.position = _resultPaths[curDestIdx];
    //                    curDestIdx++;
    //                    rotateEnabled = true;
    //                }
    //            }
    //            yield return null;
    //            // RotateCo();
    //        }

    //        if (_pathIndicater)
    //        {
    //            _pathIndicater.gameObject.SetActive(false);
    //        }
    //        _moveCo = null;
    //    }

    // IInteractable 인터페이스 구현
    public GameObject GetGameObject()
    {
        return gameObject;
    }

#if UNITY_EDITOR
    List<Vector3> gizmos_paths = new List<Vector3>();

    private void OnDrawGizmos()
    {
        for (int i = 0; i < gizmos_paths.Count; i++)
        {
            var pos = gizmos_paths[i];
            Gizmos.DrawLine(pos, pos + new Vector3(0, 2, 0));
        }
    }
#endif

    //IEnumerator RotateCo()
    //{

    //}
}
