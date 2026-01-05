using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class BattleModeBase
{
    protected BattlePhase _owner;
    //protected BattleStatusBase _status;

    public abstract BattleStatusBase GetBattleStatus(EntityTeamType team);

    public abstract InGameBattleMode Mode { get; }

    protected EntityTeamType _winner;
    protected void SetWinnerTeam(EntityTeamType team)
    {
        _winner = team;

        GetBattleStatus(EntityTeamType.Player).SetWinner(team);
        GetBattleStatus(EntityTeamType.Enemy).SetWinner(team);
    }
    public EntityTeamType GetWinner() => _winner;

    public virtual async UniTask EnterAsync(BattlePhase owner)
    {
        _owner = owner;
        // _status = CreateBattleStatus();

        InGameManager.Instance.EventListener += OnEventReceived;
        return;
    }

    protected abstract BattleStatusBase CreateBattleStatus();

    protected abstract void OnEventReceived(InGameEvent evt, InGameEventArgBase arg);

    public virtual void Update() { }
    public abstract bool CheckFinished();
    public abstract UniTaskVoid FinishCutSceneRoutine(EntityTeamType winnerTeam, Action onCompleted);
    public abstract void ForceFinish();

    public virtual void Release()
    {
        InGameManager.Instance.EventListener -= OnEventReceived;
    }

    #region ====:: BattleStatus ::====
    //public void SetBattleStatus_CommonTargetObjective(EntityBase entity)
    //{
    //    if (entity.Team == EntityTeamType.Player)
    //        _status.player.commonTargetObjective = entity;
    //    else if (entity.Team == EntityTeamType.Enemy)
    //        _status.enemy.commonTargetObjective = entity;
    //}

    //protected void ReportBattleStatusChanged(InGameStatusModifyFlag modification)
    //{
    //    InGameManager.Instance.PublishEvent(InGameEvent.BattleStatusChanged, new InGameStatusChangeArg()
    //    {
    //        phase = InGamePhase.Battle,
    //        battleMode = Mode,
    //        status = get,
    //        modification = modification
    //    });
    //}

    #endregion
}
