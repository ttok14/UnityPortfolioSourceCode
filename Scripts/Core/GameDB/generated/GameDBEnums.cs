//*** Auto Generation Code ***

namespace GameDB
{
	public enum E_ItemType
	{
		None = 0, /* 없음 */
		Currency = 1, /* 재화 아이템 */
	}

	public enum E_AudioType
	{
		None = 0, /* 없음 */
		BGM = 1, /* 배경음악 */
		SFX = 2, /* 효과음 (풀링 상태에 따라 안들릴 수 있음) */
		SFX_Critical = 3, /* 효과음 (풀링 상태에 무관하게 들림) */
		SFX_Structure = 4, /* 건물 줌 효과음 */
		UI = 5, /* UI 효과음 */
	}

	public enum E_Audio3D_DistanceType
	{
		None = 0, /* 없음 */
		Small = 1, /* 주로 로컬 사운드 (발소리, 평타 등등) */
		Medium = 2, /* 주로 표준 사운드 (스킬 사용음,일반적인 폭발 등) */
		Large = 3, /* 거의 글로벌한 사운드 */
	}

	public enum E_LoaderType
	{
		None = 0, /* 없음 */
		Resources = 1, /* Resources.Load 로 로드 */
		Addressables = 2, /* Addressables 로 로드 */
	}

	public enum E_AssetType
	{
		None = 0, /* 없음 */
		Prefab = 1, /* 프리팹 에셋 */
		Other = 2, /* 다른 일반 에셋 */
	}

	public enum E_EntityType
	{
		None = 0, /* 없음 */
		Environment = 1, /* 환경 (산,꽃,돌 등) */
		Structure = 2, /* 상호작용이 있는 건축물 (건물 등) */
		StaticStructure = 3, /* 상호작용 없는 정적 건축물 등 */
		Weapon = 4, /* 무기 */
		Food = 5, /* 음식 */
		Prop = 6, /* 자잘한 물건 */
		Character = 7, /* 캐릭터 */
		Animal = 8, /* 동물 */
		Item = 9, /* 아이템 */
	}

	public enum E_MapObjectBoundaryType
	{
		None = 0, /* 없음 */
		SimpleRectangle = 1, /* NxN 형태로 점유하는 형태 */
	}

	[System.Flags]
	public enum E_MapObjectPlacementRuleFlags
	{
		None = 0, /* 없음 */
		Free = 1, /* 배치에 제약이 없음 */
	}

	public enum E_ResourceType
	{
		None = 0, /* 없음 */
		Currency = 1, /* 재화 */
		Entity = 2, /* 엔티티 (e.g. 유닛) */
	}

	[System.Flags]
	public enum E_TileStatusFlags
	{
		None = 0, /* 없음 */
		Walkable_Ground = 1, /* 타일의 지상 Walkable 상태 */
		Walkable_Air = 2, /* 타일위에 공중 Walkable 여부 */
		Jumpable_Ground = 4, /* 점프가 가능한 Jumpable 여부 */
		Standard_Terrain = 8, /* 기본 터레인 상태 */
	}

	[System.Flags]
	public enum E_EntityFlags
	{
		None = 0, /* 없음 */
		GroundedEntity = 1, /* 지상 오브젝트 */
		FlyingEntity = 2, /* 날아다니는 오브젝트 */
		Jumpable = 4, /* 점프될 수 있는 오브젝트 */
		Requires_Walkable_Ground = 8, /* 배치을 위해선 지상 경로가 Walkable 이어야한다 */
		Require_Walkable_Air = 16, /* 배치을 위해선 하늘 경로가 Walkable 이어야한다 */
		Require_Jumpable = 32, /* 배치을 위해선 지상 경로가 Jumpable 이어야한다 */
		Block_Ground_Movement = 64, /* 배치한 타일의 ground walkable 을 non walkable 로 만든다 */
		Block_Air_Movement = 128, /* 배치한 타일의 air walkable 을 non walkable 로 만든다 */
	}

	public enum E_StructureType
	{
		None = 0, /* 없음 */
		Nexus = 1, /* 넥서스 */
		Residential = 2, /* 주민들이 쉬거나 자는 공간 */
		Defense = 3, /* 디펜스 타워 (적 유닛 공격) */
		Spawner = 4, /* 유닛 생성 타워 */
		Storage = 5, /* 재화 저장소 */
		ResourceGenerator = 6, /* 재화 생성소 */
		Abandoned = 7, /* 버려진 공간 */
	}

	public enum E_CurrencyType
	{
		None = 0, /* 없음 */
		Gold = 1, /* 황금 재화 */
		Wood = 2, /* 목재 재화 */
		Food = 3, /* 음식 재화 */
	}

	public enum E_SkillType
	{
		None = 0, /* 없음 */
		Attack = 1, /* 공격 스킬 */
	}

	public enum E_SkillTriggerType
	{
		None = 0, /* 없음 */
		Animation = 1, /* 애니메이션 이벤트에 의한 트리거 */
		CastingTime = 2, /* 캐스팅 후 일정 시간 경과후 트리거 */
	}

	public enum E_CharacterType
	{
		None = 0, /* 없음 */
		Actor = 1, /* 플레이어 캐릭터 */
		NPC = 2, /* 주민 캐릭터 등 */
		Monster = 3, /* 적 몬스터 */
	}

	public enum E_ProjectileTargetingType
	{
		None = 0, /* 없음 */
		FixedPoint = 1, /* 고정 위치를 향해 이동 */
		Directional = 2, /* 방향을 향해 이동 */
		TrackingTarget = 3, /* 특정 타겟을 향해 이동 */
	}

	public enum E_ProjectileMovementType
	{
		None = 0, /* 없음 */
		Linear = 1, /* 직선 이동 */
		Curve = 2, /* 포물선 이동 */
	}

	public enum E_ProjectileCollisionActivationType
	{
		None = 0, /* 없음 */
		SpecificTargetOnHit = 1, /* 특정 타겟에 충돌하면 */
		AnyTargetOnHit = 2, /* 아무 타겟이나 충돌하면 */
		OnArriveDest = 3, /* 도착지에 도달하는 순간 */
	}

	public enum E_CollisionRangeType
	{
		None = 0, /* 없음 */
		Single = 1, /* 하나의 타겟에 효과 발동 */
		RangeArea = 2, /* 반지름안에 포함되는 모든 타겟들에게 효과 발동 */
	}

	public enum E_WaveCommandType
	{
		None = 0, /* 없음 */
		Wait = 1, /* 순수 대기 */
		Spawn = 2, /* 유닛 생성 */
		Camera = 3, /* 카메라 연출 */
		Notification = 4, /* Toast 알림 띄우기 */
		Sound = 5, /* 사운드 재생 */
		FX = 6, /* 이펙트 생성 */
		WaitForClear = 10, /* 모든 적이 사망할때까지 대기 */
	}

	public enum E_SizeType
	{
		None = 0, /* 없음 */
		Small = 1, /* 작음 */
		Medium = 2, /* 중간 */
		Large = 3, /* 거대 */
	}

	public enum E_UpdateLogicType
	{
		None = 0, /* 없음 */
		Timer = 1, /* 정해둔 고정 타이머 */
		Interval = 2, /* 일정 주기 */
	}

	public enum E_ActionType
	{
		None = 0, /* 없음 */
		Penetrate = 1, /* 관통 처리 */
		FixedSpawn = 2, /* 제한된 방법으로 새로운 투사체 생성 */
		ChainBounceClosestSpawn = 3, /* 가장 가까운, 새로운 타겟으로 향하는 새로운 투사체 생성 */
		ChainBounceRandomSpawn = 4, /* 랜덤으로 새로운 타겟에게 향하는 새로운 투사체 생성 */
		AreaDamage = 5, /* 즉시 범위 공격 */
		MultiTargetHit = 6, /* 다중 타게팅 공격 */
		SkyFallSpawn = 7, /* 하늘에서 땅으로 낙하 및 폭격하는 형태의 투사체 생성 */
	}

	public enum E_RefDataType
	{
		None = 0, /* 없음 */
		Projectile = 1, /* 투사체 참조 */
		Zone = 2, /* 장판 참조 */
		Entity = 3, /* Entity 참조 */
	}

	public enum E_AimType
	{
		None = 0, /* 없음 */
		FixHeight = 1, /* 출발 위치의 Y 값 고정시킨 위치에 조준 */
		Ground = 2, /* 타겟의 Y 값을 0 으로 맞춘 후 조준 */
		TransformPosition = 3, /* 타겟의 Transform position 에 조준 */
		Head = 4, /* 타겟의 Head 소켓에 조준 */
		Center = 5, /* 타겟의 Center 소켓에 조준 */
	}

	public enum E_DeliveryContextInheritType
	{
		None = 0, /* 없음 */
		Share = 1, /* 공유 (Reference Count 시스템으로 동작 - 중복 타게팅 방지) */
		Copy = 2, /* 복사 (새로 할당 후 내용 복제 - 일부 중복 타게팅 허용) */
		Reset = 3, /* 리셋 (이전 데이터는 완전히 삭제, 중복 타게팅 완전 허용 ) */
	}

	public enum E_SkillCategoryType
	{
		None = 0, /* 없음 */
		Standard = 1, /* 스탠다드 스킬 (자동 시전) */
		Spell = 2, /* 스펠 스킬 (직접 시전) */
	}

	public enum E_SpellPositionType
	{
		None = 0, /* 없음 */
		Self = 1, /* 내 엔티티의 위치 */
		CurrentTarget = 2, /* 현재 타겟의 위치 */
		Nexus = 3, /* 넥서스의 위치 */
	}


}
