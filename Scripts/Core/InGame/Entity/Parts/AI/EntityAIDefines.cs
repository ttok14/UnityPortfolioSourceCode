
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using GameDB;

public interface ICombatBlackboard
{
    EntityBase CurrentTarget { get; }
    // PathListPoolable CurrentPath { get; }
    Vector3 CurrentDestination { get; }
    MoveCommandResult CurrentMoveType { get; }

    // 움직이는 Entity 가 아니라, 상황에 따라 캐릭터를 향해 움직이는중인지
    // 건물을 향해 움직이는 중인지 등 문맥 파악을 위한 값
    E_EntityType CurrentMoveDestEntityType { get; }

    //uint PathVersion { get; }
}

public class EntityAIFSMArgBase
{
}

public interface IPathProvider
{
    void FetchPath(EntityBase mover, CancellationToken ctk, Action<PathListPoolable> onFetched);
    void FetchPath(EntityBase mover, ulong targetEntityID, CancellationToken ctk, Action<PathListPoolable> onFetched);
    void FetchPath(EntityBase mover, Vector3 destination, in PathBuffer.Modifier modifier, CancellationToken ctk, Action<PathListPoolable> onFetched);
}

public interface IObjectiveProvider
{
    EntityBase GetTargetEntity(EntityTeamType team);
}

#region ====:: Move Policy ::====

public enum MoveCommandResult
{
    Stop = 0,
    Path,
    Directional,
    Patrol
}

public struct MoveCommand
{
    public MoveCommandResult result;
    // public PathListPoolable path;
    public Vector3 destination;
    // public Vector3 direction;
    public E_EntityType destEntityType;

    public static MoveCommand Stop => new MoveCommand() { result = MoveCommandResult.Stop };
}

public interface IMovePolicy : IInstancePoolElement
{
    MoveCommand GetCommand(EntityBase target);
}

#endregion

#region ====:: Targeting Policy ::====

public interface ITargetSelectionPolicy : IInstancePoolElement
{
    EntityBase FindTarget(EntityBase asker);
}

#endregion
