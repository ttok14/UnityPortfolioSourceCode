//*** Auto Generation Code ***

using System;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

namespace GameDB
{
	public class GameDBContainer
	{
		public Dictionary<uint, AnimationTable> AnimationTable_data = new Dictionary<uint, AnimationTable>();
		public Dictionary<string, AssetMetaTable> AssetMetaTable_data = new Dictionary<string, AssetMetaTable>();
		public Dictionary<uint, AudioTable> AudioTable_data = new Dictionary<uint, AudioTable>();
		public Dictionary<uint, CharacterTable> CharacterTable_data = new Dictionary<uint, CharacterTable>();
		public Dictionary<uint, CurrencyTable> CurrencyTable_data = new Dictionary<uint, CurrencyTable>();
		public Dictionary<uint, EntityTable> EntityTable_data = new Dictionary<uint, EntityTable>();
		public Dictionary<uint, ItemTable> ItemTable_data = new Dictionary<uint, ItemTable>();
		public Dictionary<uint, KillStreakTable> KillStreakTable_data = new Dictionary<uint, KillStreakTable>();
		public Dictionary<uint, PetTable> PetTable_data = new Dictionary<uint, PetTable>();
		public Dictionary<uint, ProjectileTable> ProjectileTable_data = new Dictionary<uint, ProjectileTable>();
		public Dictionary<uint, PurchaseCostTable> PurchaseCostTable_data = new Dictionary<uint, PurchaseCostTable>();
		public Dictionary<uint, ResourceTable> ResourceTable_data = new Dictionary<uint, ResourceTable>();
		public Dictionary<uint, SkillTable> SkillTable_data = new Dictionary<uint, SkillTable>();
		public Dictionary<uint, StatTable> StatTable_data = new Dictionary<uint, StatTable>();
		public Dictionary<uint, StructureTable> StructureTable_data = new Dictionary<uint, StructureTable>();
		public Dictionary<uint, WaveSequenceTable> WaveSequenceTable_data = new Dictionary<uint, WaveSequenceTable>();
		public Dictionary<uint, WaveTable> WaveTable_data = new Dictionary<uint, WaveTable>();
	}

	[MessagePackObject]
	public class AnimationTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public string ControllerName;
		[Key(2)]
		public string StateName;
		[Key(3)]
		public float TriggerAt;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, AnimationTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, AnimationTable> dicTables = new Dictionary<uint, AnimationTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<AnimationTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class AssetMetaTable
	{
		[Key(0)]
		public string Key;
		[Key(1)]
		public E_AssetType AssetType;
		[Key(2)]
		public E_LoaderType LoaderType;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<string, AssetMetaTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<string, AssetMetaTable> dicTables = new Dictionary<string, AssetMetaTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<AssetMetaTable>(ref reader);
				dicTables.Add(table.Key, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class AudioTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_AudioType AudioType;
		[Key(2)]
		public string ResourceKey;
		[Key(3)]
		public bool Is3D;
		[Key(4)]
		public E_Audio3D_DistanceType DistanceType;
		[Key(5)]
		public float Volume;
		[Key(6)]
		public float RandomPitchRange;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, AudioTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, AudioTable> dicTables = new Dictionary<uint, AudioTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<AudioTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class CharacterTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_CharacterType CharacterType;
		[Key(2)]
		public E_SizeType CharacterSize;
		[Key(3)]
		public uint[] SkillSet;
		[Key(4)]
		public float ShadowScale;
		[Key(5)]
		public string MoveTrailFXKey;
		[Key(6)]
		public bool UseIK;
		[Key(7)]
		public uint DropItemID;
		[Key(8)]
		public string FootStepAudioKey;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, CharacterTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, CharacterTable> dicTables = new Dictionary<uint, CharacterTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<CharacterTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class CurrencyTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_CurrencyType Type;
		[Key(2)]
		public string Name;
		[Key(3)]
		public string Description;
		[Key(4)]
		public string SpriteKey;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, CurrencyTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, CurrencyTable> dicTables = new Dictionary<uint, CurrencyTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<CurrencyTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class EntityTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public string Name;
		[Key(2)]
		public string ResourceKey;
		[Key(3)]
		public E_EntityType EntityType;
		[Key(4)]
		public uint DetailTableID;
		[Key(5)]
		public uint StatTableID;
		[Key(6)]
		public string IconKey;
		[Key(7)]
		public Vector2Int[] OccupyOffsets;
		[Key(8)]
		public E_EntityFlags EntityFlags;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, EntityTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, EntityTable> dicTables = new Dictionary<uint, EntityTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<EntityTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class ItemTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_ItemType ItemType;
		[Key(2)]
		public string Name;
		[Key(3)]
		public string ResourceKey;
		[Key(4)]
		public bool Is3D;
		[Key(5)]
		public uint DetailID;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, ItemTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, ItemTable> dicTables = new Dictionary<uint, ItemTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<ItemTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class KillStreakTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public uint KillCount;
		[Key(2)]
		public float DisplayDuration;
		[Key(3)]
		public string NotificationText;
		[Key(4)]
		public string[] AudioKeys;
		[Key(5)]
		public string ColorHex;
		[Key(6)]
		public float ScalePunch;
		[Key(7)]
		public bool DoImpulse;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, KillStreakTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, KillStreakTable> dicTables = new Dictionary<uint, KillStreakTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<KillStreakTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class PetTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public bool IsRidable;
		[Key(2)]
		public string MoveTrailFXKey;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, PetTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, PetTable> dicTables = new Dictionary<uint, PetTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<PetTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class ProjectileTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public string ResourceKey;
		[Key(2)]
		public float LifeTime;
		[Key(3)]
		public E_ProjectileTargetingType TargetingType;
		[Key(4)]
		public E_AimType AimType;
		[Key(5)]
		public bool EnableShowTargetingIndicator;
		[Key(6)]
		public E_DeliveryContextInheritType InheritType;
		[Key(7)]
		public E_ProjectileMovementType MovementType;
		[Key(8)]
		public float MaxDistance;
		[Key(9)]
		public E_ProjectileCollisionActivationType CollisionActivationType;
		[Key(10)]
		public E_CollisionRangeType CollisionRangeType;
		[Key(11)]
		public float CollisionAreaRange;
		[Key(12)]
		public float CollisionForce;
		[Key(13)]
		public uint PreferMaxTargetCount;
		[Key(14)]
		public float MoveSpeed;
		[Key(15)]
		public float StatReductionMinRatio;
		[Key(16)]
		public float StatReductionRatioPerHit;
		[Key(17)]
		public bool AllowMultiHit;
		[Key(18)]
		public string[] HitSFXKeys;
		[Key(19)]
		public bool AudioRandomPick;
		[Key(20)]
		public string[] HitFXKeys;
		[Key(21)]
		public bool HitDestroy;
		[Key(22)]
		public E_UpdateLogicType UpdateLogicType;
		[Key(23)]
		public float UpdateLogicValue;
		[Key(24)]
		public E_ActionType ProcessActionType;
		[Key(25)]
		public E_RefDataType ProcessRefType;
		[Key(26)]
		public uint ProcessRefID;
		[Key(27)]
		public string ProcessRefKey;
		[Key(28)]
		public float ProcessValue01;
		[Key(29)]
		public float ProcessValue02;
		[Key(30)]
		public float ProcessValue03;
		[Key(31)]
		public bool ProcessDestroy;
		[Key(32)]
		public string[] ProcessSFXKeys;
		[Key(33)]
		public string[] ProcessFXKeys;
		[Key(34)]
		public E_ActionType EndActionType;
		[Key(35)]
		public E_RefDataType EndRefType;
		[Key(36)]
		public uint EndRefID;
		[Key(37)]
		public string EndRefKey;
		[Key(38)]
		public float EndValue01;
		[Key(39)]
		public float EndValue02;
		[Key(40)]
		public float EndValue03;
		[Key(41)]
		public string[] EndSFXKeys;
		[Key(42)]
		public string[] EndFXKeys;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, ProjectileTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, ProjectileTable> dicTables = new Dictionary<uint, ProjectileTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<ProjectileTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class PurchaseCostTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public uint EntityID;
		[Key(2)]
		public E_CurrencyType CostCurrencyType;
		[Key(3)]
		public uint CostPrice;
		[Key(4)]
		public uint AcquireCount;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, PurchaseCostTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, PurchaseCostTable> dicTables = new Dictionary<uint, PurchaseCostTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<PurchaseCostTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class ResourceTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_ResourceType ResourceType;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, ResourceTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, ResourceTable> dicTables = new Dictionary<uint, ResourceTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<ResourceTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class SkillTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public string Name;
		[Key(2)]
		public string IconKey;
		[Key(3)]
		public E_SkillCategoryType SkillCategory;
		[Key(4)]
		public E_SkillType SkillType;
		[Key(5)]
		public string Description;
		[Key(6)]
		public E_SkillTriggerType TriggerType;
		[Key(7)]
		public float CastingTime;
		[Key(8)]
		public string[] TriggerAudioKey;
		[Key(9)]
		public bool AudioRandomPick;
		[Key(10)]
		public string[] EffectKeys;
		[Key(11)]
		public string ProjectileKey;
		[Key(12)]
		public uint ProjectileCount;
		[Key(13)]
		public float CooldownTime;
		[Key(14)]
		public uint Cost;
		[Key(15)]
		public uint BaseDamage;
		[Key(16)]
		public float Range;
		[Key(17)]
		public bool LookAtTarget;
		[Key(18)]
		public E_CollisionRangeType ImpactCollisionRangeType;
		[Key(19)]
		public float ImpactCollisionRange;
		[Key(20)]
		public uint PreferMaxTargetCount;
		[Key(21)]
		public float ImpactCollisionForce;
		[Key(22)]
		public string[] ImpactSFXHitKeys;
		[Key(23)]
		public string[] ImpactFXHitKeys;
		[Key(24)]
		public E_SpellPositionType SpellStartPositionType;
		[Key(25)]
		public Vector3 SpellStartOffset;
		[Key(26)]
		public bool SpellStartOffsetRelative;
		[Key(27)]
		public E_SpellPositionType SpellEndPositionType;
		[Key(28)]
		public Vector3 SpellEndOffset;
		[Key(29)]
		public bool SpellEndOffsetRelative;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, SkillTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, SkillTable> dicTables = new Dictionary<uint, SkillTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<SkillTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class StatTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public string Description;
		[Key(2)]
		public uint Grade;
		[Key(3)]
		public uint BaseHP;
		[Key(4)]
		public bool IsInvincible;
		[Key(5)]
		public uint BaseAttackPower;
		[Key(6)]
		public float AttackSpeed;
		[Key(7)]
		public float AttackSpeedGrowthPerLevel;
		[Key(8)]
		public uint HPGrowthPerLevel;
		[Key(9)]
		public uint AttackGrowthPerLevel;
		[Key(10)]
		public float MoveSpeed;
		[Key(11)]
		public float RotateSpeed;
		[Key(12)]
		public float ScanRange;
		[Key(13)]
		public float AggroWeight_Structure;
		[Key(14)]
		public float AggroWeight_Character;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, StatTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, StatTable> dicTables = new Dictionary<uint, StatTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<StatTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class StructureTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public E_StructureType StructureType;
		[Key(2)]
		public string Description;
		[Key(3)]
		public string SoundKey;
		[Key(4)]
		public string DestroyEffectKey;
		[Key(5)]
		public uint[] SkillSet;
		[Key(6)]
		public E_CurrencyType UpgradeCostCurrencyType;
		[Key(7)]
		public uint UpgradeCost;
		[Key(8)]
		public uint MaxLevel;
		[Key(9)]
		public uint MaxResidential;
		[Key(10)]
		public E_ResourceType GenResourceType;
		[Key(11)]
		public uint GenResourceID;
		[Key(12)]
		public uint GenResourceBaseAmount;
		[Key(13)]
		public uint GenCurrencyGrowthPerLevel;
		[Key(14)]
		public float GenResourceInterval;
		[Key(15)]
		public bool EnableSpawning;
		[Key(16)]
		public uint SpawnEntityIDOnCombat;
		[Key(17)]
		public float SpawnIntervalSeconds;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, StructureTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, StructureTable> dicTables = new Dictionary<uint, StructureTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<StructureTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class WaveSequenceTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public uint WaveID;
		[Key(2)]
		public uint Order;
		[Key(3)]
		public E_WaveCommandType CmdType;
		[Key(4)]
		public float ResumeDelay;
		[Key(5)]
		public uint Chance;
		[Key(6)]
		public uint IntValue01;
		[Key(7)]
		public uint IntValue02;
		[Key(8)]
		public uint IntValue03;
		[Key(9)]
		public float FloatValue01;
		[Key(10)]
		public float FloatValue02;
		[Key(11)]
		public string StringValue01;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, WaveSequenceTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, WaveSequenceTable> dicTables = new Dictionary<uint, WaveSequenceTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<WaveSequenceTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

	[MessagePackObject]
	public class WaveTable
	{
		[Key(0)]
		public uint ID;
		[Key(1)]
		public uint StageID;
		[Key(2)]
		public string Description;
		[Key(3)]
		public float NextWaveDelay;
		[Key(4)]
		public E_CurrencyType RewardCurrencyType;
		[Key(5)]
		public uint RewardAmount;

		[UnityEngine.Scripting.Preserve]
		public static Dictionary<uint, WaveTable> Deserialize(ref byte[] _readBytes)
		{
			Dictionary<uint, WaveTable> dicTables = new Dictionary<uint, WaveTable>();
			MessagePackReader reader = new MessagePackReader(new System.ReadOnlyMemory<byte>(_readBytes));
			int tableCount = MessagePackSerializer.Deserialize<int>(ref reader);
			for (int i = 0; i < tableCount; i++)
			{
				var table = MessagePackSerializer.Deserialize<WaveTable>(ref reader);
				dicTables.Add(table.ID, table);
			}
			return dicTables;
		}
	}

}
