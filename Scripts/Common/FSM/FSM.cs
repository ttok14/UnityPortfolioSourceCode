using System;
using System.Collections.Generic;
using UnityEngine;

public class FSM<EVENT, STATE, ENTER_PARAM, PARENT>
    where EVENT : struct, System.Enum
    where STATE : struct, System.Enum
{
    protected PARENT Parent;
    public STATE Current_State { get; protected set; }
    protected bool isEntering;
    protected Dictionary<STATE, Dictionary<EVENT, STATE>> TransitionMap;
    protected Dictionary<STATE, BaseState<STATE, PARENT, ENTER_PARAM>> StateMap;
    public BaseState<STATE, PARENT, ENTER_PARAM> Current => StateMap[Current_State];

    #region ====:: 람다 최적화 ::====
    Action _setIsEnteringFalseAction;
    Action _releaseInternal;
    SetCurStateAndOnEnter_LambdaCache _setCurStateAndOnEnter = new SetCurStateAndOnEnter_LambdaCache();

    class SetCurStateAndOnEnter_LambdaCache
    {
        public STATE newState;
        public ENTER_PARAM[] args;

        public Action<STATE, ENTER_PARAM[]> action;
        public Action callAction;

        public SetCurStateAndOnEnter_LambdaCache()
        {
            callAction = Call;
        }

        public void Call()
        {
            action.Invoke(newState, args);
        }
    }
    #endregion

    public FSM(PARENT parent)
    {
        // NUll 이면 별도로 initialize 를 호출하는 방식으로 
        if (parent == null)
            return;

        Initialize(parent);
    }

    public void Initialize(PARENT newParent = default)
    {
        if (newParent != null)
            Parent = newParent;

        if (TransitionMap == null)
            TransitionMap = new Dictionary<STATE, Dictionary<EVENT, STATE>>();
        if (StateMap == null)
            StateMap = new Dictionary<STATE, BaseState<STATE, PARENT, ENTER_PARAM>>(32);
        if (_setIsEnteringFalseAction == null)
            _setIsEnteringFalseAction = SetIsEnteringFalse;

        if (_setCurStateAndOnEnter.action == null)
        {
            _setCurStateAndOnEnter.action = (state, args) =>
            {
                Current_State = state;
                StateMap[Current_State].OnEnter(_setIsEnteringFalseAction, args);
            };
        }
        if (_releaseInternal == null)
            _releaseInternal = ReleaseInternal;

        OnInit();
    }

    protected virtual void OnInit() { }

    /// <summary> </summary>
    public virtual void Release()
    {
        if (null != StateMap[Current_State])
            StateMap[Current_State].OnExit(_releaseInternal);
    }

    public void CancelInvokeALL()
    {
        foreach (var ibs in StateMap)
        {
            if (null == ibs.Value)
                continue;

            ibs.Value.CancelInvoke();
        }
    }

    // TODO[GC] : 24B (25-11-29)
    protected void ReleaseInternal()
    {
        foreach (var ibs in StateMap)
        {
            if (null == ibs.Value)
                continue;

            ibs.Value.OnRelease();
        }
        StateMap.Clear();
        // StateMap = null;
        TransitionMap.Clear();
        // TransitionMap = null;
        EnableFlag = false;
        isEntering = false;
        Current_State = default;
        Parent = default;
    }

    public bool IsEnable { get { return EnableFlag; } }
    bool EnableFlag = false;
    public void Enable(STATE state)
    {
        if (EnableFlag)
        {
            ChangeState(state);
        }
        else
        {
            Current_State = state;
            Enable(true);
        }
    }

    private void Enable(bool flag)
    {
        if (EnableFlag == flag)
            return;
        else EnableFlag = flag;

        if (EnableFlag)
        {
            if (StateMap.ContainsKey(Current_State))
                StateMap[Current_State].OnEnter(null);
        }

        else StateMap[Current_State].OnExit(null);
    }

    public void AddState(STATE _state, BaseState<STATE, PARENT, ENTER_PARAM> _stateinterface)
    {
        if (null == _stateinterface)
        {
            TEMP_Logger.Err($"AddState() | State is null : {_state}");
            return;
        }

        StateMap.Add(_state, _stateinterface);
        _stateinterface.OnInitialize(Parent, _state);
    }

    public bool RemoveState(STATE _state)
    {
        if (StateMap.TryGetValue(_state, out var _stateComp))
        {
            UnityEngine.Object.DestroyImmediate(_stateComp);
        }

        return StateMap.Remove(_state);
    }

    //이벤트 값을 얻어옴 ( 지금 상태에서 타겟 상태로 가기위한 이벤트가 있는지 확인 후 있으면 그 이벤트를 줌 )
    public EVENT GetEvent(STATE _BaseState, STATE _TargetState)
    {
        if (!TransitionMap.ContainsKey(_BaseState))
            return default(EVENT);

        foreach (var kv in TransitionMap[_BaseState])
        {
            if (TransitionMap[_BaseState][kv.Key].Equals(_TargetState))
            {
                return kv.Key;
            }
        }

        return default(EVENT);
    }

    void SetIsEnteringFalse()
    {
        isEntering = false;
    }

    public void RegistEvent(STATE _state, EVENT _event, STATE _targetstate)
    {
        try
        {
            if (!TransitionMap.ContainsKey(_state))
                TransitionMap.Add(_state, new Dictionary<EVENT, STATE>());
            TransitionMap[_state].Add(_event, _targetstate);
        }
        catch (System.Exception e)
        {
            TEMP_Logger.Err($"FiniteStateMap Add Error : {_event}\nException : {e}");
            Debug.Break();
        }
    }

    public virtual bool ChangeState(EVENT _event, ENTER_PARAM[] args)
    {
        if (isEntering)
        {
            TEMP_Logger.Wrn($"Current : {Current_State}, Target : {_event} State Map Error.");
            TEMP_Logger.Wrn($"current state is already entering!");
            return false;
        }

        if (TransitionMap[Current_State].ContainsKey(_event))
        {
            //현재 상태와 동일 할경우 
            if (StateMap[Current_State].Equals(TransitionMap[Current_State][_event]))
                return false;

            isEntering = true;
            StateMap[Current_State].OnExit(delegate ()
            {
                Current_State = TransitionMap[Current_State][_event];
                StateMap[Current_State].OnEnter(_setIsEnteringFalseAction, args);
            });
            return true;
        }
        else
        {
            TEMP_Logger.Wrn($"Current : {Current_State}, Target : {_event} State Map Error.");
            return false;
        }
    }

    public virtual bool ChangeState(STATE _state, bool _bForce = false, ENTER_PARAM[] args = null)
    {
        if (isEntering)
        {
            TEMP_Logger.Wrn($"Current : {Current_State}");
            TEMP_Logger.Wrn($"current state is already entering!");
            return false;
        }

        //현재 상태와 동일 할경우 
        if (false == _bForce && Current_State.Equals(_state))
            return false;

        isEntering = true;

        if (StateMap[Current_State] != null)
        {
            _setCurStateAndOnEnter.newState = _state;
            _setCurStateAndOnEnter.args = args;

            StateMap[Current_State].OnExit(_setCurStateAndOnEnter.callAction);
            //delegate ()
            //{
            //    Current_State = _state;
            //    StateMap[Current_State].OnEnter(_setIsEnteringFalseAction, args);
            //});
        }
        else
        {
            Current_State = _state;
            if (StateMap[Current_State] != null)
            {
                StateMap[Current_State].OnEnter(_setIsEnteringFalseAction, args);
            }
        }

        return true;
    }

    public bool CheckEvent(EVENT _event)
    {
        if (TransitionMap.ContainsKey(Current_State) && TransitionMap[Current_State].ContainsKey(_event))
        {
            if (StateMap[Current_State].Equals(TransitionMap[Current_State][_event]))
            {

                return false;
            }
            else
            {

                return true;
            }
        }

        return false;
    }

    public bool HasState(STATE state)
    {
        return StateMap.ContainsKey(state);
    }
}
