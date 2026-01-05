using System;
using UnityEngine;
using GameDB;
using System.Collections.Generic;
using System.Linq;

public abstract class EnterInGamePhaseEventArgBase : EventArgBase
{
    public InGamePhase PrevPhase;
    public InGamePhase NewPhase;
}

public class EnterPeacePhaseEventArg : EnterInGamePhaseEventArgBase
{
    public EnterPeacePhaseEventArg(InGamePhase prevPhase)
    {
        PrevPhase = prevPhase;
        NewPhase = InGamePhase.Peace;
    }
}

public class EnterBattlePhaseEventArg : EnterInGamePhaseEventArgBase
{
    public InGameBattleMode Mode;

    public EnterBattlePhaseEventArg(InGamePhase prevPhase, InGameBattleMode mode)
    {
        PrevPhase = prevPhase;
        NewPhase = InGamePhase.Battle;
        Mode = mode;
    }
}

public class InGameFSMEnterArgBase
{ }

public class PeaceStateEnterArg : InGameFSMEnterArgBase
{
}

public class BattleStateEnterArg : InGameFSMEnterArgBase
{
    public InGameBattleMode mode;
}

//public class SpawningEnemySettings
//{
//    public float spawnInterval;
//}

#region ====:: InGame 이벤트 Arg ::====

public class InGameEventArgBase { }

public class InGameFSMStateNotify : InGameEventArgBase
{
    public InGamePhase Phase;
}

public class StartRunBattleTimer : InGameEventArgBase
{
    public float StartBattleTimeAt;
}

public class PlayerCharacterRespawnEventArg : InGameEventArgBase
{
    public EntityBase Entity;
}

public class PlayerCharacterDiedEventArg : InGameEventArgBase
{
    public Vector3 DiedPosition;
}

public class EntityCreatedEventArg : InGameEventArgBase
{
    public EntityBase Entity;
}

public class EntitySpawnEventArg : InGameEventArgBase
{
    public EntityBase Entity;
}

public class EntityDiedEventArg : InGameEventArgBase
{
    public EntityTeamType Team;
    public ulong ID;
    public E_EntityType Type;
}

public class EntityRemovedEventArg : InGameEventArgBase
{
    public EntityTeamType Team;
    public ulong PastId;
    public E_EntityType Type;
}

public class EntityConstructedEventArg : InGameEventArgBase
{
    public EntityBase Entity;
}

public class EntityHugeImpactEventArg : InGameEventArgBase
{
    public Vector3 Position;
    public float Force;
}

#endregion

#region ====:: InGame CutScene ::====

public struct CutSceneArgs
{
    public InGamePhaseBaseState PhaseState;
    public InGamePhase PhaseType;
    public PlayerController PlayerController;
    public DefensePathSystem StrategySystem;
}

#endregion

abstract public class InGameStatusBase
{

}

public class PeacePhaseStatus : InGameStatusBase
{
}

abstract public class BattleStatusBase : InGameStatusBase
{
    public EntityTeamType WinnerTeam { get; private set; }

    public void SetWinner(EntityTeamType winner)
    {
        WinnerTeam = winner;
    }
    public virtual void Reset()
    {
        WinnerTeam = EntityTeamType.None;
    }

}

public class DefenseBattleStatus : BattleStatusBase
{
    public List<ulong> OrderedTargetIDs { get; private set; } = new List<ulong>();
    // public int CurrentTargetIndex { get; private set; }
    public bool HasCurrentTarget
    {
        get
        {
            return OrderedTargetIDs.Count > 0;
        }
    }

    public ulong CurrentTargetID
    {
        get
        {
            if (OrderedTargetIDs == null || OrderedTargetIDs.Count == 0)
                return 0;

            return OrderedTargetIDs[0];
        }
    }

    public bool IsTarget(ulong id)
    {
        return OrderedTargetIDs.Contains(id);
    }

    public void SetOrderedTargetIndexes(ulong[] indexes)
    {
        OrderedTargetIDs = indexes.ToList();
    }

    //public void SetCurrentTargetIndex(int idx)
    //{
    //    CurrentTargetIndex = idx;
    //}

    //public void SetNextTarget()
    //{
    //    if (CurrentTargetIndex == OrderedTargetIDs.Length - 1)
    //        CurrentTargetIndex = -1;
    //    else
    //        CurrentTargetIndex++;
    //}

    public override void Reset()
    {
        base.Reset();

        OrderedTargetIDs.Clear();
        //CurrentTargetIndex = -1;
    }

    public bool Remove(ulong iD)
    {
        return OrderedTargetIDs.Remove(iD);
    }
}
