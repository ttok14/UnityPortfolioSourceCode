using System;
using System.Collections.Generic;
#if UNITY_EDITOR && DEVELOPMENT
using System.Text;
#endif

/// <summary>
/// Stack 처럼 사용하되 리스트의 기능도 지원해야 해서 만든 
/// UI 전용 리스트 랩퍼 클래스 
/// </summary>
public class UIStack
{
    private List<UIBase> _uiStack = new List<UIBase>();

    public int Count => _uiStack.Count;

    public UIBase this[int index] => _uiStack[index];
    public UIBase this[string key] => _uiStack.Find(t => t.Key == key);

#if UNITY_EDITOR && DEVELOPMENT
    StringBuilder _history = new StringBuilder();
#endif
    public void Push(UIBase ui)
    {
        _uiStack.Add(ui);

#if UNITY_EDITOR && DEVELOPMENT
        _history.AppendLine($"Push UI | {ui} | ID : {ui.ID}");
#endif
    }

    public UIBase Pop<T>() where T : UIBase
    {
        var idx = _uiStack.FindIndex(t => t.GetType() == typeof(T));
        if (idx != -1)
        {
            var ui = _uiStack[idx];
            RemoveAt(idx);
            return ui;
        }

        return null;
    }

    public UIBase Pop()
    {
        if (_uiStack.Count == 0)
            return null;

        int lastIdx = _uiStack.Count - 1;
        var result = _uiStack[lastIdx];
        RemoveAt(lastIdx);
        return result;
    }

    public UIBase Pop(string key)
    {
        int idx = FindIndex(t => t.Key == key);
        var ui = _uiStack[idx];
        RemoveAt(idx);
        return ui;
    }

    public List<UIBase> PopWithCondition(Predicate<UIBase> condition)
    {
        List<UIBase> result = null;

        for (int i = 0; i < _uiStack.Count; i++)
        {
            // 역순회
            int reversedIdx = _uiStack.Count - 1 - i;
            var ui = _uiStack[reversedIdx];

            if (condition(ui))
            {
                if (result == null)
                    result = new List<UIBase>();

                result.Add(ui);
                RemoveAt(reversedIdx);
            }
        }

        return result;
    }

    public void Remove(UIBase ui)
    {
        int idx = FindIndex(t => t == ui);
        RemoveAt(idx);
    }

    public void Remove(string key)
    {
        int idx = FindIndex(t => t.Key == key);
        RemoveAt(idx);
    }

    public void RemoveAt(int at)
    {
#if UNITY_EDITOR && DEVELOPMENT
        _history.AppendLine($"RemoveAt : {at} ({_uiStack[at]})");
#endif

        _uiStack.RemoveAt(at);
    }

    public int CountFrom(Type type)
    {
        int idx = FindIndex(t => t.GetType() == type);
        return _uiStack.Count - idx;
    }

    public T Find<T>() where T : UIBase
    {
        return _uiStack.Find(t => t.GetType() == typeof(T)) as T;
    }

    public int GetCount<T>() where T : UIBase
    {
        var type = typeof(T);
        int count = 0;
        for (int i = 0; i < _uiStack.Count; i++)
        {
            if (_uiStack[i] is T)
                count++;
        }
        return count;
    }

    public UIBase Find(Type type)
    {
        return _uiStack.Find(t => t.GetType() == type);
    }

    private int FindIndex(Predicate<UIBase> condition)
    {
        for (int i = 0; i < _uiStack.Count; i++)
        {
            // 역순회
            int reversedIdx = _uiStack.Count - 1 - i;

            if (condition.Invoke(_uiStack[reversedIdx]))
                return reversedIdx;
        }

        return -1;
    }
}
