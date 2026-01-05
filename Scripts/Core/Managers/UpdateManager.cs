using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUpdatable
{
    void OnUpdate();

    //#if UNITY_EDITOR
    //    string DebugText();
    //#endif
}

public interface IUpdatableStandard : IUpdatable
{
    void OnLateUpdate();
    void OnFixedUpdate();
}

// TODO
//  - 여기서 확장해서 만약 Update 관련 순서나 그 외에 어떤.. 뭔가를 추가적으로
// 제어할만한게 있다면 그럴 가치가 있겠으나 일단은 Monobehaviour Update 루프 오버헤드만
// 제거하는 방향으로 . 추후 체크
//  - 근데 생각해보니까 굳이 IUpdatableStandad 뭐 이런식으로 나눠서 late 랑 fix 처리해야하나? 
// 그냥 하나의 함수뿐인데 필요한건 . IUpdatable 하나만 두고 그냥 등록하는 클래스의 입장에서는
// 아근데, 또 애매한게 그러면 , 함수명이 겹치니까 안되고 . 결과적으로 호출하는 함수 자체가 달라져야 하기는함
// 그럴려면 함수를 정말 별도로 하나 더 만들던지 (인터페이스가 늘어남) 아니면 제너릭으로 처리하던지.
// 뭐가 베스트일까 .

public class UpdateManager : SingletonBase<UpdateManager>
{
    class Container<T> where T : IUpdatable
    {
        // 실제 업데이트 객체
        public List<T> Updatables;
        // 루프중간에 변할수있는 active 체크용 
        public List<bool> ActiveStates;
        // Updatables 리스트에 다이렉트 접근위한 인덱스 저장용 
        public Dictionary<T, int> IndexMap;
        // 삭제할것들
        public HashSet<T> ToRemove;

        public Container(int updatableCapa, int toRemoveCapa)
        {
            Updatables = new List<T>(updatableCapa);
            ActiveStates = new List<bool>(updatableCapa);
            IndexMap = new Dictionary<T, int>(updatableCapa);
            ToRemove = new HashSet<T>(toRemoveCapa);
        }

        public bool Register(T updatable)
        {
            // 이미 내부에 대한 Updatable 이 존재함 
            if (IndexMap.TryGetValue(updatable, out int idx))
            {
                // 존재하는데, 만약 활성화 상태에서 또 Register 가 된거라면
                // 중복 Register 임 
                if (ActiveStates[idx])
                {
                    TEMP_Logger.Err($"Given Updatable is already registered (duplicate)");
                    return false;
                }
                // 존재는하는데, 전에 등록해제를 위해 UnRegister 가 불린 상황 ,
                // 이 경우에는 업데이트 루프를 돌다가 Inactivate 가 됐고
                // Remove 는 되기 전인 상황. 즉 중복 등록이 아니라
                // 비활성화된거 활성화만 해주면 끝.
                else
                {
                    ActiveStates[idx] = true;
                    ToRemove.Remove(updatable);
                    return true;
                }
            }

            Updatables.Add(updatable);
            ActiveStates.Add(true);
            IndexMap.Add(updatable, Updatables.Count - 1);

            return true;
        }

        public bool Unregister(T updatable)
        {
            if (IndexMap.TryGetValue(updatable, out int idx) == false)
            {
                TEMP_Logger.Err($"Given updatable does not exist");
                return false;
            }

            if (ActiveStates[idx] == false)
            {
                TEMP_Logger.Err($"Given Update is already unregistered (duplicate)");
                return false;
            }

            //#if UNITY_EDITOR
            //            UpdateManager.Debug_WriteHistory($"Inside Unregister | Updatable : {updatable} | Idx : {idx}");
            //#endif

            ActiveStates[idx] = false;

            return ToRemove.Add(updatable);
        }

        public void ProcessRemove()
        {
            foreach (var r in ToRemove)
            {
                int index = IndexMap[r];
                int lastIndex = Updatables.Count - 1;

                if (index != lastIndex)
                {
                    var last = Updatables[lastIndex];
                    bool lastActive = ActiveStates[lastIndex];

                    Updatables[index] = last;
                    ActiveStates[index] = lastActive;

                    IndexMap[last] = index;
                }

                Updatables.RemoveAt(lastIndex);
                ActiveStates.RemoveAt(lastIndex);
                IndexMap.Remove(r);
            }

            ToRemove.Clear();
        }

        public void Release()
        {
            Updatables?.Clear();
            ActiveStates?.Clear();
            IndexMap?.Clear();
            ToRemove?.Clear();

            Updatables = null;
            ActiveStates = null;
            IndexMap = null;
            ToRemove = null;
        }

        public bool Exist(T updatable)
        {
            return IndexMap.ContainsKey(updatable);
        }
    }

    Container<IUpdatableStandard> _unityStandard;
    Container<IUpdatable> _singleUpdatable;
    Container<IUpdatable> _singleLateUpdatable;

    public bool ExistStandard(IUpdatableStandard updatable) => _unityStandard.Exist(updatable);
    public bool ExistSingle(IUpdatable updatable) => _singleUpdatable.Exist(updatable);
    public bool ExistSingleLateUpdatable(IUpdatable updatable) => _singleLateUpdatable.Exist(updatable);

    //#if UNITY_EDITOR
    //    public static System.Text.StringBuilder DebugHistory { get; private set; } = new System.Text.StringBuilder(16 * 10000);

    //    public static void Debug_WriteHistory(string text)
    //    {
    //        DebugHistory.AppendLine($"{Time.frameCount} | {text}");
    //    }

    //#endif
    public override void Initialize()
    {
        base.Initialize();

        _unityStandard = new Container<IUpdatableStandard>(512, 16);
        _singleUpdatable = new Container<IUpdatable>(128, 16);
        _singleLateUpdatable = new Container<IUpdatable>(32, 8);
    }

    public override void Release()
    {
        if (_unityStandard != null)
        {
            _unityStandard.Release();
            _unityStandard = null;
        }

        if (_singleUpdatable != null)
        {
            _singleUpdatable.Release();
            _singleUpdatable = null;
        }

        if (_singleLateUpdatable != null)
        {
            _singleLateUpdatable.Release();
            _singleLateUpdatable = null;
        }

        base.Release();
    }

    public bool RegisterStandard(IUpdatableStandard updatable)
    {
        return _unityStandard.Register(updatable);

        //#if UNITY_EDITOR
        //        Debug_WriteHistory($"Register({success}) : {updatable.GetHashCode()} | Debug : {updatable}");
        //#endif
    }

    public bool UnregisterStandard(IUpdatableStandard updatable)
    {
        return _unityStandard.Unregister(updatable);

        //#if UNITY_EDITOR
        //        Debug_WriteHistory($"Unregister({success}) : {updatable.GetHashCode()} | Debug : {updatable}");
        //#endif
    }

    public bool RegisterSingle(IUpdatable updatable)
    {
        return _singleUpdatable.Register(updatable);
    }

    public bool UnregisterSingle(IUpdatable updatable)
    {
        return _singleUpdatable.Unregister(updatable);
    }

    public bool RegisterSingleLateUpdatable(IUpdatable updatable)
    {
        return _singleLateUpdatable.Register(updatable);
    }

    public bool UnregisterSingleLateUpdatable(IUpdatable updatable)
    {
        return _singleLateUpdatable.Unregister(updatable);
    }

    private void Update()
    {
        int count = _unityStandard.Updatables.Count;
        var activeList = _unityStandard.ActiveStates;
        var updatableList = _unityStandard.Updatables;

        for (int i = 0; i < count; i++)
        {
            if (activeList[i])
                updatableList[i].OnUpdate();
        }

        if (_unityStandard.ToRemove.Count > 0)
            _unityStandard.ProcessRemove();

        count = _singleUpdatable.Updatables.Count;
        activeList = _singleUpdatable.ActiveStates;
        var singleUpdatableList = _singleUpdatable.Updatables;

        for (int i = 0; i < count; i++)
        {
            if (activeList[i])
                singleUpdatableList[i].OnUpdate();
        }

        if (_singleUpdatable.ToRemove.Count > 0)
            _singleUpdatable.ProcessRemove();
    }

    private void LateUpdate()
    {
        int count = _unityStandard.Updatables.Count;
        var activeList = _unityStandard.ActiveStates;
        var updatableList = _unityStandard.Updatables;

        for (int i = 0; i < count; i++)
        {
            if (activeList[i])
                updatableList[i].OnLateUpdate();
        }

        if (_unityStandard.ToRemove.Count > 0)
            _unityStandard.ProcessRemove();

        count = _singleLateUpdatable.Updatables.Count;
        activeList = _singleLateUpdatable.ActiveStates;
        var lateUpdatableList = _singleLateUpdatable.Updatables;

        for (int i = 0; i < count; i++)
        {
            if (activeList[i])
                lateUpdatableList[i].OnUpdate();
        }

        if (_singleLateUpdatable.ToRemove.Count > 0)
            _singleLateUpdatable.ProcessRemove();
    }

    private void FixedUpdate()
    {
        int count = _unityStandard.Updatables.Count;
        var activeList = _unityStandard.ActiveStates;
        var updatableList = _unityStandard.Updatables;

        for (int i = 0; i < count; i++)
        {
            if (activeList[i])
                updatableList[i].OnFixedUpdate();
        }

        if (_unityStandard.ToRemove.Count > 0)
            _unityStandard.ProcessRemove();
    }
}
