
/// <summary>
/// 씬 종류들
///     * 실제 BuildSetting 에 등록되는 Scene 의 이름과 일치해야 한다! *
/// </summary>
public enum SCENES
{
    None = -1,

    BootStrap = 10,
    Loading = 20,
    Auth = 40,
    Lobby = 50,
    InGame = 60,
}

public enum GameStateEvent
{
    Next,
}

/// 수정에 주의 
/// <see cref="GameStateMetaData"/> 에서 에디터에서 사용중
public enum GameState
{
    BootStrap = 10,
    TransitionState = 20,
    Loading = 30,
    Auth = 40,
    Lobby = 50,
    InGame = 60
}

public enum GLOBAL_EVENT
{
    None = -1,

    GAME_STATE_CHANGED,

    NEW_SCENE_LOAD_ENTER,
    NEW_SCENE_LOADED,

    SCENE_UNLOAD_ENTER,
    SCENE_UNLOADED,

    USER_INPUT,

    MAIN_CAMERA_REGISTERED,

    REQUEST_GO_LOBBY_START_ROUTINE,

    TABLE_PATCH_COMPLETED,

    MAP_DATA_PATCH_COMPLETED,

    BEFORE_ENTER_INGAME_NEW_PHASE,
    ENTER_INGAME_NEW_PHASE,

    // Interaction 은 고유하니까 중복은 없겟지..??
    PLAYER_INTERACTION_ENDED,
}

public enum GLOBAL_EVENT_PRIORITY
{
    Low = 0,
    Medium,
    High
}

public enum PoolOpResult
{
    None = 0,

    Successs,

    Fail_Default,
    Fail_LoadAsset,
    Fail_NoExistInPool,
    Fail_ReachedPoolLimit,
    Fail_ReachedInstanceLimit,
}

public enum ObjectPoolCategory
{
    None = -1,

    Default,
    UI,
    Model,
    Entity,
    Fx,
    Audio_Critical,
    Audio_Normal,
    Projectile
}

//[System.Flags]
//public enum PoolableFlags
//{
//    None = 0,

//    // 로드 출처
//    FromResources = 1 << 0,
//    FromAddressables = 1 << 1,
//}

public enum ResourceGroupLabel
{
    None = -1,

    BuiltIn,
    External,
}

[System.Flags]
public enum PatchStateFlags
{
    None = 0,

    AssetDownloadCompleted,
    TableDownloadCompleted
}

public enum PatchUnitType
{
    Addressables,
    Table,
    MapData,
}

/// <summary>
/// UI 의 레이어 정의 (캔버스 별 할당)
/// </summary>
public enum UILayer
{
    World2D,
    Panel,
    TutorialGuide,
    Popup,
    System,
    Toast,
    Screen
}

/// ui 출력을 누가 트리거 하였는가 
public enum UITrigger
{
    Default = 0,
    EntityInteraction
}

public enum CameraType
{
    None = -1,
    MainCamera = 0,
    UICamera = 10,
}

public enum CameraControlTransitionEvent
{

}

public enum CinemachineCameraType
{
    None = 0,

    Free,
    Focused,
    FollowTarget,
    Manual,
}

[System.Flags]
public enum StateSwitchRequirement
{
    None = 0,

    UI = 1 << 0,
    Asset = 1 << 1
}

public enum LoadingProcessResult
{
    None = 0,
    Failed,
    Cancelled,
    Success,
}

public enum EntityTeamType
{
    None = 0,

    Player,
    Enemy,
    Neutral
}

public enum EntityPartType
{
    None = 0,

    Move,
    Animation,
    Stat,
    AI,
}

public enum EntityModelSocket
{
    None = 0,

    PeaceModeActivationGroup = 3,
    BattleModeActivationGroup = 4,

    Center = 5,
    Head = 10,
    LeftHand = 20,
    RightHand = 30,

    // Pet
    Saddle = 110,

    // ModelPart 루트가 아닌 중간 Transform 을 조작해야하거나
    // 하는 상황 (e.g 회전 포 ?) 에 pivot 으로 소켓 지정후 조작 가능
    Pivot = 120,

    // 근접 공격인 경우 공격시 이펙트 위치 등을 잡기 위해 직접 소켓 설정해야함
    MeleeWeaponEffect = 130,

    // 건물같은 경우 , 일정 체력 이하가되면 위급한 상태 연출 위치용
    LowHealthEffect = 140,

    // 이동 경로에 따라 무언가를 표현해주기 위한 위치용 (e.g TrailDust)
    Trail = 150,

    // 해당 Model 에서 정의해야하는 추상화된 '액션' 을 위한 위치
    ActionPoint = 160,
}

public enum EntityEquipmentType
{
    None = 0,

    Weapon = 10,
}

public enum EntityDataCategory
{
    None = 0,

    Stat,
    Occupation,
    Statistic,
}

public enum EntityEvent
{
    None = 0,

    AttackBegin,
    Attacking,
    AttackEnd,
}

//public enum EntitySocket
//{
//    None = 0,


//}

public enum EntityAnimationParameterType
{
    None = 0,

    MoveSpeedRate = 10,
    AttackSpeedRate = 20,

    Skill01 = 40,
    Skill02 = 50,
    Skill03 = 60,

    // 0 : 모델기준 왼쪽, 1 : 모델기준 오른쪽 (for ik)
    // SkillTarget_Direction = 70,

    Lumbering = 80,

    RidingPet = 90,

    Die = 100,
}

/// <summary>
/// 애니메이터 스테이트 식별용.
/// <see cref="EntityAnimationStateBeh"/> 에서 인스펙터 필드로 노출시켜
/// 설정 중이기 때문에 웬만하면 10 단위로 끊어서 관리하고.
/// 이미 할당된 정수값은 변경하는 일이 없도록 관리.
/// </summary>
public enum EntityAnimationStateID
{
    None = 0,

    Skill01 = 30,
    Skill02 = 40,
    Skill03 = 50,

    Die = 60,
}

public enum AudioTrigger
{
    Default = 0,

    UI,

    EntityInteraction
}

public enum FXType
{
    None = 0,

    SpriteSheet = 10,
}

public enum EntityAIStateEvent
{
    None = 0,

    MoveEnd_NotReachedDestination,
    TargetInRange,

    Cannot_Move,
    Cannot_Attack,

    TargetLost,
    TargetChangedDuringAttack,
}

public enum EntityAIState
{
    None = 0,

    Idle,
    WaitingTarget,
    PatrolTarget,
    ChaseTarget,
    AttackTarget,
}

//[System.Flags]
// 플래그는 일단 보류. 그냥 하나의 명확한 상태로 관리해보자. 
public enum EntityAggroPolicy
{
    Default = 0,



    // Ignore_Character = 0x1,
    // Ignore_Structure = 0x1 << 1,

}

public enum InGameEvent
{
    None = 0,

    Enter,
    Start,
    End,

    BattleEnding,

    BeginCutScene,
    EndCutScene,

    PlayerCharacterSpawned,
    PlayerCharacterDied,

    // BattleStatusChanged,

    StartRunBattleTimer,

    WaveStart,
    WaveEnd,

    EntityCreated,
    // spawn 은 spawn 시스템에 의해 스폰된 경우 (e.g 적 몬스터)
    EntitySpawned,
    EntityDied,
    EntityRemoved,

    EntityConstructed,

    HugeImpact,

    RequestCameraImpulse,
}

//[System.Flags]
//public enum InGameStatusModifyFlag
//{
//    None = 0,

//    Defense_CurrentTargetChanged = 0x1,
//}

public enum LightType
{
    None = 0,

    ColorTintLight,
    ShadowLight,
}

public enum EntityPlacementMode
{
    None = 0,

    // ShowGhost,
    ControlGhost,
}
