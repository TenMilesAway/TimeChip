using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UnityObjectPool : IObjectPool<Object>, IDisposable
{
    private readonly Queue<Object> _objects;
    private readonly Func<Object> _objectFactory;
    private Action<Object> _enterQueueHandle;
    private Action<Object> _dequeueHandle;
    private const float WaitDestroyTime = 20f;
    private string _poolName;
    private bool _disposed = false;

    private static Transform s_PoolRoot;
    private static readonly object locker = new object();

    public Object _itemPrefab;

    public static Transform PoolRoot
    {
        get
        {
            if (s_PoolRoot == null)
            {
                lock (locker)
                {
                    if (s_PoolRoot == null)
                    {
                        s_PoolRoot = new GameObject("PoolRoot").transform;
                    }
                }
            }

            return s_PoolRoot;
        }
    }

    public UnityObjectPool(Object itemPrefab, string poolName, Func<Object> objectFactory, 
        Action<Object> enterQueueHandle = null, 
        Action<Object> deQueueHandle = null)
    {
        _itemPrefab        = itemPrefab;
        _poolName         = poolName;
        _objectFactory    = objectFactory;
        _enterQueueHandle = enterQueueHandle;
        _dequeueHandle    = deQueueHandle;
        _objects          = new Queue<Object>();
        _disposed         = false;
    }

    public Object Get(Vector3 vec = default)
    {
        Object item = _objects.Count == 0 ? CreateObject() : _objects.Dequeue();

        if (item != null) DequeueHandle(item, vec);

        _dequeueHandle?.Invoke(item);

        return item;
    }

    public void Put(Object item)
    {
        if (item == null) return;

        if (!_objects.Contains(item))
        {
            _enterQueueHandle?.Invoke(item);

            EnqueueHandle(item);

            _objects.Enqueue(item);
        }
    }

    /// <summary>
    /// 入池操作
    /// </summary>
    public void EnqueueHandle(Object item)
    {
        if (item is GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(PoolRoot, false);

            long delayedUTCStamp = TimerUtil.GetLaterMillisecondsBySecond(WaitDestroyTime);
            DateTime delayedUTCDateTime = TimerUtil.Milliseconds2DateTime(delayedUTCStamp);
            string taskID = string.Format("PoolRoot-{0}", _poolName);

            // 定时任务, 在指定时间后销毁池中对象
            GameManager.Timer.AddTimeTask(delayedUTCDateTime, taskID, (ID) =>
            {
                if (_objects.Count > 0)
                {
                    Object obj = _objects.Dequeue();
                    Object.Destroy(obj);
                }
            });
        }
    }

    /// <summary>
    /// 出池操作
    /// </summary>
    public void DequeueHandle(Object item)
    {
        if (item is GameObject obj)
        {
            obj.SetActive(true);

            string taskID = string.Format("PoolRoot-{0}", _poolName);

            // 移除定时销毁任务
            GameManager.Timer.RemoveTimeTask(taskID);
        }
    }

    /// <summary>
    /// 出池操作
    /// </summary>
    public void DequeueHandle(Object item, Vector3 vec)
    {
        if (item is GameObject obj)
        {
            if (vec != default)
            {
                obj.transform.position = vec;
            }
            obj.SetActive(true);

            string taskID = string.Format("PoolRoot-{0}", _poolName);

            // 移除定时销毁任务
            GameManager.Timer.RemoveTimeTask(taskID);
        }
    }

    public void Clear(Func<Object, bool> shouldClear)
    {
        int count = _objects.Count;

        for (int i = 0; i < count; i++)
        {
            var obj = _objects.Dequeue();

            if (shouldClear(obj))
            {
                Object.Destroy(obj);
            }
            else
            {
                _objects.Enqueue(obj);
            }
        }
    }

    #region 主要方法
    public Queue<Object> GetPoolObject()
    {
        return _objects;
    }

    public Object GetItemPrefab()
    {
        return _itemPrefab;
    }

    protected Object CreateObject()
    {
        var newObject = _objectFactory != null
            ? _objectFactory()
            : GameObject.Instantiate(_itemPrefab);

        _enterQueueHandle?.Invoke(newObject);

        EnqueueHandle(newObject);

        return newObject;
    }
    #endregion

    #region 资源释放
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UnityObjectPool()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        // 已经释放
        if (_disposed) return;
        // 释放托管资源
        if (disposing)
        {

        }
        // 释放非托管资源
        // ...
        _disposed = true;
    }
    #endregion
}