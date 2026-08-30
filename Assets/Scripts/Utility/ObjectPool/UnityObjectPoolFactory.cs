using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Object = UnityEngine.Object;

public class UnityObjectPoolFactory : Singleton<UnityObjectPoolFactory>, IDisposable
{
    public delegate T LoadFunc<out T>(string path);

    private readonly Dictionary<string, UnityObjectPool> _pools = new Dictionary<string, UnityObjectPool>();

    private bool _disposed;

    #region ��Ҫ����
    /// <summary>
    /// �첽��ȡ������ж���
    /// </summary>
    public void GetItemAsync<T>(string itemName, string tag, Action<T> callback, Vector3 vec = default) where T : Object
    {
        T result = null;

        if (_pools.TryGetValue(itemName, out var pool) && pool.GetItemPrefab() != null)
        {
            result = pool.Get(vec) as T;
            callback(result);
        }
        else
        {
            GameManager.Resource.LoadResourceAsync<T>(itemName, tag, (Object obj, object[] args) =>
            {
                result = CreatePool((T)obj, itemName, null).Get(vec) as T;
                callback(result);
            });
        }
    }

    /// <summary>
    /// ͬ����ȡ������ж���
    /// </summary>
    public async Task<T> GetItem<T>(string itemName, string tag) where T : Object
    {
        T result = null;

        if (_pools.TryGetValue(itemName, out var pool) && pool.GetItemPrefab() != null)
        {
            result = pool.Get() as T;
            return result;
        }
        else
        {
            Task<T> task = GameManager.Resource.LoadResource<T>(itemName, tag);

            await task;

            T prefab = task.Result;

            result = CreatePool(prefab, itemName, null).Get() as T;

            return result;
        }
    }

    public void PutItem(string itemName, Object objectToReturn, Action callback = null)
    {
        if (_pools.TryGetValue(itemName, out var pool))
        {
            pool.Put(objectToReturn);
            callback?.Invoke();
            return;
        }

        Object.Destroy(objectToReturn);
    }
    #endregion

    #region ��������
    /// <summary>
    /// ���������
    /// </summary>
    private UnityObjectPool CreatePool(Object obj, string poolName, 
                                        Func<Object> objectFactory,
                                        Action<Object> enqueueHandle = null,
                                        Action<Object> dequeueHandle = null)
    {
        UnityObjectPool pool = new UnityObjectPool(obj, poolName, objectFactory, enqueueHandle, dequeueHandle);
        _pools[poolName] = pool;
        return pool;
    }

    /// <summary>
    /// ��ȡ������ֵ�
    /// </summary>
    public Dictionary<string, UnityObjectPool> GetPools()
    {
        return _pools;
    }
    #endregion

    #region ��Դ�ͷ�
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UnityObjectPoolFactory()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        // �Ѿ��ͷ�
        if (_disposed) return;
        // �ͷ��й���Դ
        if (disposing)
        {
            foreach (var pool in _pools.Values)
            {
                pool?.Dispose();
            }

            _pools.Clear();
        }
        // �ͷŷ��й���Դ
        // ...
        _disposed = true;
    }
    #endregion
}