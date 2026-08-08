using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolFactory : Singleton<ObjectPoolFactory>, IDisposable
{
    // 对象池字典: <对象类型, ObjectPool>
    private readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();

    private const int DefaultPoolSize = 2;         // 默认的池大小
    private const int DefaultPoolMaxSize = 500;    // 默认的池最大容量
    private bool _disposed;                        // 释放资源

    public ObjectPoolFactory()
    {
        _disposed = false;
    }

    #region 主要方法
    /// <summary>
    /// 获得对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectGenerator"></param>
    /// <param name="poolSize"></param>
    /// <returns></returns>
    private ObjectPool<T> GetPool<T>(Func<T> objectGenerator = null, int poolSize = DefaultPoolSize) where T : new()
    {
        var type = typeof(T);

        // 是否有对应的池
        if (!_pools.TryGetValue(type, out var pool))
        {
            // 没有，创建
            pool = new ObjectPool<T>(objectGenerator, poolSize, DefaultPoolMaxSize);
            _pools.Add(type, pool);
        }

        return pool as ObjectPool<T>;
    }

    public T GetItem<T>() where T : new()
    {
        return GetPool<T>().Get();
    }

    public void PutItem<T>(T item) where T : new()
    {
        GetPool<T>().Put(item);
    }
    #endregion

    #region 释放资源
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ObjectPoolFactory()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            foreach (var pool in _pools)
            {
                (pool.Value as IDisposable)?.Dispose();
            }

            _pools.Clear();
        }
        _disposed = true;
    }
    #endregion
}