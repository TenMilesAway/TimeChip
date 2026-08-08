using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 延迟任务
/// </summary>
public class DelayedTaskData
{
    public long time;
    public string token;
    public Action action;
    public Action earlyRemoveCallback;
}

/// <summary>
/// 延迟任务列表
/// </summary>
public class DelayedTaskList : IEnumerable<DelayedTaskData>, IDisposable
{
    public long time;
    public List<DelayedTaskData> delayedTaskDataList;

    private bool m_disposed = false;

    #region 迭代器
    /// <summary>
    /// 继承 IEnumerable 接口的实现，用于 foreach
    /// </summary>
    public IEnumerator<DelayedTaskData> GetEnumerator()
    {
        return delayedTaskDataList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    #region 资源释放
    /// <summary>
    /// 手动释放托管资源和非托管资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数，只释放非托管资源
    /// </summary>
    ~DelayedTaskList()
    {
        // 托管资源已由 GC 自动释放
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        // 已经释放
        if (m_disposed) return;
        // 释放托管资源
        if (disposing)
        {
            delayedTaskDataList.Clear();
            delayedTaskDataList = null;
        }
        // 释放非托管资源
        // ...
        m_disposed = true;
    }
    #endregion
}