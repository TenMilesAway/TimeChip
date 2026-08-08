using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : new()
{
    private Queue<T> _objects;               // 对象队列
    private Func<T> _objectFactory;          // 工厂创建方法
    private int _initialPoolSize = 0;        // 初始池大小
    private int _curCount = 0;               // 当前池大小
    private bool _disposed = false;          // 释放资源

    private readonly int MaxPoolSize = 200;  // 最大池容量

    public ObjectPool(Func<T> objectFactory, int initialPoolSize = 0, int maxPoolSize = 200)
    {
        _objects         = new Queue<T>();
        _objectFactory   = objectFactory;
        _initialPoolSize = initialPoolSize;
        _curCount        = 0;
        _disposed        = false;

        MaxPoolSize = maxPoolSize;

        for (int i = 0; i < _initialPoolSize; i++)
        {
            var obj = CreateObject();
            if (obj != null)  _objects.Enqueue(obj);
        }
    }

    public T Get(Vector3 vec = default)
    {
        T item = _objects.Count == 0 ? CreateObject() : _objects.Dequeue();

        if (item != null) DequeueHandle(item);

        return item;
    }

    public void Put(T item)
    {
        if (item == null) return;

        // 比较引用地址，去重检查
        if (!_objects.Contains(item))
        {
            EnqueueHandle(item);
            _objects.Enqueue(item);
        }
    }

    /// <summary>
    /// 清理
    /// </summary>
    /// <param name="shouldClear">参数为 T，返回 bool 的委托</param>
    public void Clear(Func<T, bool> shouldClear)
    {
        int count = _objects.Count;

        for (int i = 0; i < count; i++)
        {
            T item = _objects.Dequeue();

            if (!shouldClear(item)) _objects.Enqueue(item);
            else _curCount--;
        }
    }

    public void EnqueueHandle(T item)
    {
        if (item is IPoolObjectItem iPoolItem) iPoolItem.OnPutHandle();

        if (item is IList list) list.Clear();
        else if (item is IDictionary dictionary) dictionary.Clear();
    }

    public void DequeueHandle(T item)
    {
        if (item is IPoolObjectItem iPoolItem) iPoolItem.OnGetHandle();
    }

    #region 主要方法
    /// <summary>
    /// 创建对象
    /// </summary>
    private T CreateObject()
    {
        // 超过池最大数量，无法创建
        if (_curCount >= MaxPoolSize) return default;

        var newObject = _objectFactory != null ? _objectFactory() : new T();

        _curCount++;

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

    ~ObjectPool()
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
            if (_objects != null)
            {
                _objects.Clear();
                _objects = null;
            }

            _objectFactory = null;
        }
        // 释放非托管资源
        // ...
        _curCount = 0;
        _disposed = true;
    }
    #endregion
}