using System;
using UnityEngine;
using DG.Tweening;
using GameDB;
using System.Collections.Generic;

public class ItemEntity : EntityBase
{
    enum State
    {
        None = 0,

        Appear,
        Waiting,
        Flying
    }

    const float Duration = 1f;
    const Ease EaseType = Ease.OutQuad;
    const float RotateSpeed = 300f;

    State _state;

    float _startFlyAt;
    float _time;

    Vector3 _startPos;

    Transform _attractor;

    Action<InGameEvent, InGameEventArgBase> _onInGameEvent;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        _onInGameEvent = OnInGameEvent;
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        if (InGameManager.Instance.PlayerCommander.Player.IsAlive == false)
        {
            EntityManager.Instance.RemoveEntity(this);
            return;
        }

        _attractor = InGameManager.Instance.PlayerCommander.Player.Entity.ModelPart.GetSocket(EntityModelSocket.Head);

        transform.rotation = Quaternion.identity;

        _time = 0f;
        _startFlyAt = Time.time + 0.6f;
        _state = State.Appear;

        InGameManager.Instance.EventListener += _onInGameEvent;
    }

    private void OnInGameEvent(InGameEvent evt, InGameEventArgBase argBase)
    {
        if (evt == InGameEvent.BattleEnding)
        {
            EntityManager.Instance.RemoveEntity(this);
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        InGameManager.Instance.EventListener -= _onInGameEvent;
        _attractor = null;
        _state = State.None;
        _time = 0f;
        _startFlyAt = 0f;
    }

    protected override void OnUpdateImpl()
    {
        if (IsAlive == false)
            return;

        base.OnUpdateImpl();

        if (_state == State.Appear)
        {
            if (ModelPart)
            {
                ModelPart.RunTweenRunner();
                _state = State.Waiting;
            }
        }
        else if (_state == State.Waiting)
        {
            if (Time.time >= _startFlyAt)
            {
                _startPos = ApproxPosition;
                _state = State.Flying;
            }
        }
        else if (_state == State.Flying)
        {
            if (!_attractor || _attractor.gameObject.activeInHierarchy == false)
            {
                EntityManager.Instance.RemoveEntity(this);
                return;
            }

            Vector3 destPos = _attractor.position;

            var curve = EntityManager.Instance.CurveData.ObtainItemCurveData.curve;

            _time += Time.deltaTime / Duration;

            if (_time < 1f)
            {
                SetPosition(CurveHelper.MoveEaseWithCurve(curve, _time, _startPos, destPos, EaseType, 5f));
                transform.Rotate(new Vector3(RotateSpeed * Time.deltaTime, 0, 0), Space.Self);
            }
            else
            {
                // 플레이어만 Item Obtain 하는 시스템이니까 일단은 이렇게 하는데 ,
                // 나중에 Item 을 Obtain 하는 주체가 다양해지면 수정이 필요하겠지?
                InGameManager.Instance.PlayerCommander.ObtainItem(TableData.DetailTableID);

                SetPosition(destPos);
                _state = State.None;
                EntityManager.Instance.RemoveEntity(this);
                return;
            }
        }
        else
        {
            EntityManager.Instance.RemoveEntity(this);
        }
    }
}
