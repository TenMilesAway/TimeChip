using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventComponent : BaseComponent
{
    private Dictionary<GameEventType, Delegate> _eventDic = new Dictionary<GameEventType, Delegate>();

    public delegate void Callback();
    public delegate void Callback<T>(T arg);
    public delegate void Callback<T, X>(T arg1, X arg2);
    public delegate void Callback<T, X, Y>(T arg1, X arg2, Y arg3);
    public delegate void Callback<T, X, Y, Z>(T arg1, X arg2, Y arg3, Z arg4);
    public delegate void Callback<T, X, Y, Z, W>(T arg1, X arg2, Y arg3, Z arg4, W arg5);

    #region 检查方法
    /// <summary>
    /// 添加监听时检查
    /// </summary>
    private void OnListenerAdding(GameEventType eventType, Delegate callBack)
    {
        if (!_eventDic.ContainsKey(eventType))
        {
            _eventDic.Add(eventType, null);
            return;
        }

        Delegate d = _eventDic[eventType];
        if (d != null && d.GetType() != callBack.GetType())
        {
            Debug.LogWarningFormat("尝试为事件[{0}]添加不同类型的委托，当前事件所对应的委托是[{1}]，要添加的委托类型为[{2}]", eventType, d.GetType(), callBack.GetType());
            throw new Exception(string.Format("尝试为事件[{0}]添加不同类型的委托，当前事件所对应的委托是[{1}]，要添加的委托类型为[{2}]", eventType, d.GetType(), callBack.GetType()));
        }
    }

    /// <summary>
    /// 移除监听时检查
    /// </summary>
    private void OnListenerRemoving(GameEventType eventType, Delegate callBack)
    {
        if (_eventDic.ContainsKey(eventType))
        {
            Delegate d = _eventDic[eventType];
            if (d == null)
            {
                Debug.LogWarningFormat("移除监听错误：事件[{0}]没有对应的委托", eventType);
                throw new Exception(string.Format("移除监听错误：事件[{0}]没有对应的委托", eventType));
            }
            else if (d.GetType() != callBack.GetType())
            {
                Debug.LogWarningFormat("移除监听错误：尝试为事件[{0}]移除不同类型的委托，当前委托类型为[{1}]，要移除的委托类型为[{2}]", eventType, d.GetType(), callBack.GetType());
                throw new Exception(string.Format("移除监听错误：尝试为事件[{0}]移除不同类型的委托，当前委托类型为[{1}]，要移除的委托类型为[{2}]", eventType, d.GetType(), callBack.GetType()));
            }
        }
        else
        {
            Debug.LogWarningFormat("移除监听错误：没有事件码[{0}]", eventType);
            throw new Exception(string.Format("移除监听错误：没有事件码[{0}]", eventType));
        }
    }

    /// <summary>
    /// 完成监听移除后检查
    /// </summary>
    private void OnListenerRemoved(GameEventType eventType)
    {
        if (_eventDic[eventType] == null)
        {
            _eventDic.Remove(eventType);
        }
    }
    #endregion

    #region 添加监听
    public void AddListener(GameEventType eventType, Callback callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback)_eventDic[eventType] + callback;
    }

    public void AddListener<T>(GameEventType eventType, Callback<T> callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback<T>)_eventDic[eventType] + callback;
    }

    public void AddListener<T, X>(GameEventType eventType, Callback<T, X> callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback<T, X>)_eventDic[eventType] + callback;
    }

    public void AddListener<T, X, Y>(GameEventType eventType, Callback<T, X, Y> callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y>)_eventDic[eventType] + callback;
    }

    public void AddListener<T, X, Y, Z>(GameEventType eventType, Callback<T, X, Y, Z> callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y, Z>)_eventDic[eventType] + callback;
    }

    public void AddListener<T, X, Y, Z, W>(GameEventType eventType, Callback<T, X, Y, Z, W> callback)
    {
        OnListenerAdding(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y, Z, W>)_eventDic[eventType] + callback;
    }
    #endregion

    #region 移除监听
    public void RemoveListener(GameEventType eventType, Callback callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }

    public void RemoveListener<T>(GameEventType eventType, Callback<T> callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback<T>)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }

    public void RemoveListener<T, X>(GameEventType eventType, Callback<T, X> callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback<T, X>)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }

    public void RemoveListener<T, X, Y>(GameEventType eventType, Callback<T, X, Y> callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y>)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }

    public void RemoveListener<T, X, Y, Z>(GameEventType eventType, Callback<T, X, Y, Z> callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y, Z>)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }

    public void RemoveListener<T, X, Y, Z, W>(GameEventType eventType, Callback<T, X, Y, Z, W> callback)
    {
        if (!_eventDic.ContainsKey(eventType)) return;
        OnListenerRemoving(eventType, callback);
        _eventDic[eventType] = (Callback<T, X, Y, Z, W>)_eventDic[eventType] - callback;
        OnListenerRemoved(eventType);
    }
    #endregion

    #region 分帧广播
    public void Broadcast(GameEventType eventType) 
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback callback = d as Callback;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void Broadcast<T>(GameEventType eventType, T arg)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T> callback = d as Callback<T>;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback, arg));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void Broadcast<T, X>(GameEventType eventType, T arg1, X arg2)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X> callback = d as Callback<T, X>;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback, arg1, arg2));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void Broadcast<T, X, Y>(GameEventType eventType, T arg1, X arg2, Y arg3)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y> callback = d as Callback<T, X, Y>;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback, arg1, arg2, arg3));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void Broadcast<T, X, Y, Z>(GameEventType eventType, T arg1, X arg2, Y arg3, Z arg4)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y, Z> callback = d as Callback<T, X, Y, Z>;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback, arg1, arg2, arg3, arg4));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void Broadcast<T, X, Y, Z, W>(GameEventType eventType, T arg1, X arg2, Y arg3, Z arg4, W arg5)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y, Z, W> callback = d as Callback<T, X, Y, Z, W>;
            if (callback != null)
            {
                StartCoroutine(BroadcastCoroutine(callback, arg1, arg2, arg3, arg4, arg5));
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }
    #endregion

    #region 分帧推协程
    private IEnumerator BroadcastCoroutine(Callback callback)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback call = d as Callback;
            call();
            yield return null;
        }
    }

    private IEnumerator BroadcastCoroutine<T>(Callback<T> callback, T arg)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback<T> call = d as Callback<T>;
            call(arg);
            yield return null;
        }
    }

    private IEnumerator BroadcastCoroutine<T, X>(Callback<T, X> callback, T arg1, X arg2)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback<T, X> call = d as Callback<T, X>;
            call(arg1, arg2);
            yield return null;
        }
    }

    private IEnumerator BroadcastCoroutine<T, X, Y>(Callback<T, X, Y> callback, T arg1, X arg2, Y arg3)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback<T, X, Y> call = d as Callback<T, X, Y>;
            call(arg1, arg2, arg3);
            yield return null;
        }
    }

    private IEnumerator BroadcastCoroutine<T, X, Y, Z>(Callback<T, X, Y, Z> callback, T arg1, X arg2, Y arg3, Z arg4)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback<T, X, Y, Z> call = d as Callback<T, X, Y, Z>;
            call(arg1, arg2, arg3, arg4);
            yield return null;
        }
    }

    private IEnumerator BroadcastCoroutine<T, X, Y, Z, W>(Callback<T, X, Y, Z, W> callback, T arg1, X arg2, Y arg3, Z arg4, W arg5)
    {
        foreach (Delegate d in callback.GetInvocationList())
        {
            Callback<T, X, Y, Z, W> call = d as Callback<T, X, Y, Z, W>;
            call(arg1, arg2, arg3, arg4, arg5);
            yield return null;
        }
    }
    #endregion

    #region 立即广播
    public void BroadcastNow(GameEventType eventType)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback callback = d as Callback;
            if (callback != null)
            {
                callback();
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void BroadcastNow<T>(GameEventType eventType, T arg)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T> callback = d as Callback<T>;
            if (callback != null)
            {
                callback(arg);
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void BroadcastNow<T, X>(GameEventType eventType, T arg1, X arg2)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X> callback = d as Callback<T, X>;
            if (callback != null)
            {
                callback(arg1, arg2);
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void BroadcastNow<T, X, Y>(GameEventType eventType, T arg1, X arg2, Y arg3)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y> callback = d as Callback<T, X, Y>;
            if (callback != null)
            {
                callback(arg1, arg2, arg3);
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void BroadcastNow<T, X, Y, Z>(GameEventType eventType, T arg1, X arg2, Y arg3, Z arg4)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y, Z> callback = d as Callback<T, X, Y, Z>;
            if (callback != null)
            {
                callback(arg1, arg2, arg3, arg4);
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }

    public void BroadcastNow<T, X, Y, Z, W>(GameEventType eventType, T arg1, X arg2, Y arg3, Z arg4, W arg5)
    {
        Delegate d;
        if (_eventDic.TryGetValue(eventType, out d))
        {
            Callback<T, X, Y, Z, W> callback = d as Callback<T, X, Y, Z, W>;
            if (callback != null)
            {
                callback(arg1, arg2, arg3, arg4, arg5);
            }
            else
            {
                Debug.LogWarningFormat("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType);
                throw new Exception(string.Format("广播事件错误：事件[{0}]对应委托具有不同的类型", eventType));
            }
        }
    }
    #endregion
}
