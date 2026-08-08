using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DelayedTaskComponent : BaseComponent, IDisposable
{
    // Token -> 任务 的映射，通过唯一标识符定位和取消单个任务
    private Dictionary<string, DelayedTaskData> _taskDic = new Dictionary<string, DelayedTaskData>();
    // 时间戳 -> 任务列表 的映射，通过执行时间获取所有到期的任务，批量执行
    private Dictionary<long, DelayedTaskList> _delayedTaskDic = new Dictionary<long, DelayedTaskList>();
    // 作用: 快速获取最小时间的任务列表，因此使用红黑树
    private SortedDictionary<long, DelayedTaskList> _delayedTaskQueue = new SortedDictionary<long, DelayedTaskList>();
    private bool _disposed = false;
    private long _currentTime;

    protected override void Awake()
    {
        base.Awake();

        UpdateTime(TimerUtil.GetTimeStamp(true));
    }

    public void Update()
    {
        UpdateTime(TimerUtil.GetTimeStamp(true));
    }

    private void OnDestroy()
    {
        Dispose();
    }

    #region 主要方法
    /// <summary>
    /// 增加一个事件管理对象
    /// </summary>
    /// <param name="time">毫秒数</param>
    /// <param name="action">回调</param>
    /// <param name="earlyRemoveCallback">取消回调</param>
    /// <returns></returns>
    public string AddDelayedTask(long time, Action action, Action earlyRemoveCallback = null)
    {
        if (time < _currentTime)
        {
            Debug.LogErrorFormat("延迟任务设定时间已经过期: 设定时间[{0}], 当前时间[{1}]", time, _currentTime);
            return null;
        }

        // 查找是否已有相同时间的任务列表存在
        if (!_delayedTaskDic.TryGetValue(time, out DelayedTaskList delayedTasks))
        {
            // 没有则创建任务列表
            delayedTasks = ObjectPoolFactory.GetInstance().GetItem<DelayedTaskList>();
            delayedTasks.time = time;
            delayedTasks.delayedTaskDataList = ObjectPoolFactory.GetInstance().GetItem<List<DelayedTaskData>>();
            _delayedTaskQueue[time] = delayedTasks;
            _delayedTaskDic.Add(time, delayedTasks);
        }

        // 新建任务
        string token = Guid.NewGuid().ToString();
        DelayedTaskData delayedTaskData = ObjectPoolFactory.GetInstance().GetItem<DelayedTaskData>();
        delayedTaskData.time = time;
        delayedTaskData.action = action;
        delayedTaskData.token = token;
        delayedTaskData.earlyRemoveCallback = earlyRemoveCallback;

        // 将任务加入列表
        delayedTasks.delayedTaskDataList.Add(delayedTaskData);
        _taskDic.Add(token, delayedTaskData);
        return token;
    }

    public bool RemoveDelayedTask(string token)
    {
        _taskDic.TryGetValue(token, out DelayedTaskData delayedTaskData);

        if (delayedTaskData == null) return false;

        _taskDic.Remove(token);

        if (_delayedTaskDic.TryGetValue(delayedTaskData.time, out DelayedTaskList delayedTasks))
        {
            bool isRemoveSuccess = delayedTasks.delayedTaskDataList.Remove(delayedTaskData);

            if (isRemoveSuccess) delayedTaskData.earlyRemoveCallback?.Invoke();

            if (delayedTasks.delayedTaskDataList.Count == 0)
            {
                _delayedTaskDic.Remove(delayedTaskData.time);

                _delayedTaskQueue.Remove(delayedTaskData.time);

                ObjectPoolFactory.GetInstance().PutItem(delayedTasks.delayedTaskDataList);
                ObjectPoolFactory.GetInstance().PutItem(delayedTasks);
                ObjectPoolFactory.GetInstance().PutItem(delayedTaskData);
            }
        }
        else
        {
            ObjectPoolFactory.GetInstance().PutItem(delayedTaskData);
        }

        return true;
    }

    public void UpdateTime(long time)
    {
        _currentTime = time;

        // 处理所有到期任务
        while (_delayedTaskQueue.Count > 0)
        {
            // 获取最小时间键值对
            KeyValuePair<long, DelayedTaskList> minTimePair = _delayedTaskQueue.First();

            // 还没到时间
            if (minTimePair.Key > time) break;

            long targetTime = minTimePair.Key;
            _delayedTaskDic.Remove(targetTime);
            _delayedTaskQueue.Remove(targetTime);

            DelayedTaskList minTimelist = minTimePair.Value;

            foreach (DelayedTaskData data in minTimelist.delayedTaskDataList)
            {
                data.action?.Invoke();
                _taskDic.Remove(data.token);
                ObjectPoolFactory.GetInstance().PutItem(data);
            }

            minTimelist.delayedTaskDataList.Clear();
            ObjectPoolFactory.GetInstance().PutItem(minTimelist.delayedTaskDataList);
            ObjectPoolFactory.GetInstance().PutItem(minTimelist);
        }
    }
    #endregion

    #region 资源释放
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DelayedTaskComponent()
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
            _taskDic.Clear();
            _delayedTaskDic.Clear();
            _delayedTaskQueue.Clear();
        }
        // 释放非托管资源
        // ...
        _disposed = true;
    }
    #endregion
}