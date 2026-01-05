using System;
using System.Collections.Generic;

#if DEVELOPMENT
// debugging 용
using System.Text;
#endif

public class EventManager : SingletonBase<EventManager>
{
    class EventGroup
    {
        public List<Action<EventContext>> evts = new List<Action<EventContext>>();
        public bool Invoke(EventContext cxt)
        {
            // TODO : 혹시나 publishing Count 로 Publishing 중간에
            // register/unregister 실행을 막았는데도 , 계속 문제가 생긴다면
            // 매번 호출마다 ToArray() 하는 방법도 고려해볼 수있으나. 아직은 이슈없음.
            // 하지만 안전성을 위해서 for 문이나 ToArray 로 하는것도 나중에 고려가능 
            // foreach (var e in evts.ToArray())
            foreach (var e in evts)
            {
                e.Invoke(cxt);
                if (cxt.IsUsed)
                {
                    return true;
                }
            }
            return false;
        }
    }

    private Dictionary<GLOBAL_EVENT, Dictionary<GLOBAL_EVENT_PRIORITY, EventGroup>> _listeners = new Dictionary<GLOBAL_EVENT, Dictionary<GLOBAL_EVENT_PRIORITY, EventGroup>>();
    // private Dictionary<GLOBAL_EVENT, List<Action<EventContext>>> _listeners = new Dictionary<GLOBAL_EVENT, List<Action<EventContext>>>();

    // Publish 가 nested 하게 호출이 되는 경우가 있을수 있기 떄문에 중간에
    // collection 변경이 발생할 수 있는 구간이 있을 수있어서
    // count 변수로 원천차단 (싱글쓰레드 한정 안전함)
    int _eventPublishingCount = 0;

    Queue<(GLOBAL_EVENT evt, GLOBAL_EVENT_PRIORITY priority, Action<EventContext> listener)> _pendingRegisters = new Queue<(GLOBAL_EVENT evt, GLOBAL_EVENT_PRIORITY priority, Action<EventContext> listener)>();
    Queue<(GLOBAL_EVENT evt, GLOBAL_EVENT_PRIORITY priority, Action<EventContext> listener)> _pendingUnregisters = new Queue<(GLOBAL_EVENT evt, GLOBAL_EVENT_PRIORITY priority, Action<EventContext> listener)>();

    public override void Initialize()
    {
        base.Initialize();

        var events = Enum.GetValues(typeof(GLOBAL_EVENT));

        foreach (GLOBAL_EVENT type in events)
        {
            _listeners.Add(type, new Dictionary<GLOBAL_EVENT_PRIORITY, EventGroup>(3));

            _listeners[type].Add(GLOBAL_EVENT_PRIORITY.Low, new EventGroup());
            _listeners[type].Add(GLOBAL_EVENT_PRIORITY.Medium, new EventGroup());
            _listeners[type].Add(GLOBAL_EVENT_PRIORITY.High, new EventGroup());
        }
    }

    public void Register(GLOBAL_EVENT evt, Action<EventContext> listener, GLOBAL_EVENT_PRIORITY priority = GLOBAL_EVENT_PRIORITY.Low)
    {
        if (_eventPublishingCount > 0)
        {
            _pendingRegisters.Enqueue((evt, priority, listener));
            return;
        }

        _listeners[evt][priority].evts.Add(listener);

#if DEVELOPMENT
        // RecordHistory("REGISTER", evt, priority, listener);
#endif
    }

    public bool Unregister(GLOBAL_EVENT evt, Action<EventContext> listener, GLOBAL_EVENT_PRIORITY priority = GLOBAL_EVENT_PRIORITY.Low)
    {
        if (_eventPublishingCount > 0)
        {
            _pendingUnregisters.Enqueue((evt, priority, listener));
            return false;
        }

        bool result = _listeners[evt][priority].evts.Remove(listener);

#if DEVELOPMENT
        if (result)
        {
            // RecordHistory("UNREGISTER", evt, priority, listener);
        }
#endif

        return result;
    }

    public void Publish(GLOBAL_EVENT evt, EventArgBase arg = null)
    {
        _eventPublishingCount++;

        var context = new EventContext(arg);
        var dicsByPriority = _listeners[evt];

        // Used 된 이벤트가 있다면 True 반환으로 탈출
        //        try
        {
#if UNITY_EDITOR && DEVELOPMENT
            // RecordPublishHistory(evt, "BEFORE");
#endif

            if (dicsByPriority[GLOBAL_EVENT_PRIORITY.High].Invoke(context) == false)
            {
                if (dicsByPriority[GLOBAL_EVENT_PRIORITY.Medium].Invoke(context) == false)
                {
                    dicsByPriority[GLOBAL_EVENT_PRIORITY.Low].Invoke(context);
                }
            }

#if UNITY_EDITOR && DEVELOPMENT
            // RecordPublishHistory(evt, "AFTER");
#endif
        }
        // catch (Exception exp)
        {
            //    TEMP_Logger.Err($"Exception ({evt}) | {exp.Message} | History : {_strHistory}");
        }

        _eventPublishingCount--;

        // 싱글 쓰레드 환경이라 문제없겠지 ..?
        // 만약 멀티쓰레드면 이 구간은 크리티컬섹션될듯
        // 이 시점부터 _eventPublishingCount 값이 변하게 되면
        // 조건문안에서 무한루프 돌거나 할수도있음.
        // 하지만 싱글쓰레드니 문제없을것
        // 멀티쓰레드 환경이 된다면 이 시점에서 lock 이 필요할것임.
        if (_eventPublishingCount == 0)
        {
            while (_pendingUnregisters.Count > 0)
            {
                var unreg = _pendingUnregisters.Dequeue();
                Unregister(unreg.evt, unreg.listener, unreg.priority);
            }

            while (_pendingRegisters.Count > 0)
            {
                var reg = _pendingRegisters.Dequeue();
                Register(reg.evt, reg.listener, reg.priority);
            }
        }
    }

    //public bool RegisterInternal(GLOBAL_EVENT evt, Action<EventContext> listener, GLOBAL_EVENT_PRIORITY priority)
    //{

    //}

    //public bool UnregisterInternal(GLOBAL_EVENT evt, Action<EventContext> listener, GLOBAL_EVENT_PRIORITY priority)
    //{

    //}

#if DEVELOPMENT

    //private StringBuilder _eventHistoryBuilder = new StringBuilder();
    //public string _strHistory;

    //private void RecordPublishHistory(GLOBAL_EVENT evt, string prefixEx)
    //{
    //    _eventHistoryBuilder.AppendLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {prefixEx} PublishEvent | Event: {evt}, PublishingCount : {_eventPublishingCount}");
    //}

    //private void RecordHistory(string action, GLOBAL_EVENT evt, GLOBAL_EVENT_PRIORITY priority, Action<EventContext> listener)
    //{
    //    string listenerInfo = listener != null ?
    //        $"{listener.Method.DeclaringType?.Name}.{listener.Method.Name}" : "Null";

    //    // Console/Debug.Log 대신 StringBuilder에 기록
    //    string log = $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {action} | Event: {evt}, Priority: {priority}, Listener: {listenerInfo}";

    //    _eventHistoryBuilder.AppendLine(log);
    //    _strHistory = _eventHistoryBuilder.ToString();
    //}

    //public string GetEventHistory()
    //{
    //    return _eventHistoryBuilder.ToString();
    //}

    //public string GetListenerDebugString()
    //{
    //    var sb = new StringBuilder();
    //    sb.AppendLine("--- Event Listener Status ---");
    //    foreach (var eventKVP in _listeners)
    //    {
    //        sb.AppendLine($"Event: {eventKVP.Key}");
    //        foreach (var priorityKVP in eventKVP.Value)
    //        {
    //            sb.AppendLine($"  - Priority {priorityKVP.Key}: {priorityKVP.Value.evts.Count} listeners");
    //        }
    //    }
    //    return sb.ToString();
    //}

#endif
}
