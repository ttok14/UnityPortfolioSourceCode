using System;
using UnityEngine;

/// <summary>
/// 상태 머신의 상태가 기본적으로 상속 받아야하는 클래스.
/// virtual 선언된 함수들중 내용이 있는 함수는 상속받아 사용하는 클래스 단에서 호출 해줘야 함.
/// </summary>
/// <typeparam name="STATE">자신의 State</typeparam>
/// <typeparam name="PARENT">상태를 가지는 객채</typeparam>
public class BaseState<STATE, PARENT, ENTER_PARAM> : MonoBehaviour
{
    ///// <summary>
    ///// 인스펙터 상에 비주얼 적으로 표시해주기 위한 플래그.
    ///// </summary>
    //[SerializeField]
    //bool ActiveFlag = false;

    public event Action EnterEventHandle;
    public event Action ExitEventHandler;

    /// <summary>
    /// 상태를 가지는 클래스.
    /// </summary>
    public PARENT Parent { get; private set; }

    public STATE State { get; private set; }

    /// <summary>
    /// Override하여 사용시 내부에서 [가장먼저] base로 접근하여 호출 되어야 함.
    /// </summary>
    public void Awake()
    {
        enabled = false;
    }

    protected virtual void OnEnable() { }

    /// <summary>
    /// 상태가 추가되는 타임에 부모를 설정해줌.
    /// Override하여 사용시 내부에서 [가장먼저] base로 접근하여 호출 되어야 함.
    /// </summary>
    /// <param name="_parent"></param>
    public virtual void OnInitialize(PARENT _parent, STATE state)
    {
        Parent = _parent;
        State = state;
    }

    public virtual void ManualLateUpdate() { }

    /// <summary>
    /// 상태 진입시점.
    /// Override하여 사용시 내부에서 [가장먼저] base로 접근하여 호출 되어야 함.
    /// </summary>
    //public virtual void OnEnter(System.Action callback, params object[] args)
    //{
    //    EnterEventHandle?.Invoke();
    //    enabled = true;

    //    if (null != callback)
    //        callback();
    //}

    public virtual void OnEnter(System.Action callback, ENTER_PARAM[] args = null)
    {
        EnterEventHandle?.Invoke();
        enabled = true;

        if (null != callback)
            callback();
    }

    /// <summary>
    /// 상태 종료 시점.
    /// Override하여 사용시 내부에서 [마지막]으로 base로 접근하여 호출 되어야 함.
    /// </summary>
    public virtual void OnExit(System.Action callback)
    {
        ExitEventHandler?.Invoke();
        enabled = false;

        if (null != callback)
            callback();
    }

    /// <summary>
    /// 상태 파괴전 호출.
    /// Override하여 사용시 내부에서 [마지막]으로 base로 접근하여 호출 되어야 함.
    /// </summary>
    public virtual void OnRelease()
    {
        //enabled = ActiveFlag = false;
        enabled = false;
        Destroy(this);
    }

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
    /// <summary> Unity IMGUI용 </summary>
    public virtual void Dev_OnGUI() { }
#endif
}
