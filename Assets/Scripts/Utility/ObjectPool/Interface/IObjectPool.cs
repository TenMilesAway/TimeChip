using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectPool<T>
{
    /// <summary>
    /// 获取对象
    /// </summary>
    /// <returns></returns>
    T Get(Vector3 vec = default);

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="item"></param>
    void Put(T item);

    /// <summary>
    /// 清理池对象
    /// </summary>
    void Clear(Func<T, bool> shouldClear);

    /// <summary>
    /// 入队列 Handle
    /// </summary>
    /// <param name="item"></param>
    void EnqueueHandle(T item);

    /// <summary>
    /// 出队列 Handle
    /// </summary>
    /// <param name="item"></param>
    void DequeueHandle(T item);
}