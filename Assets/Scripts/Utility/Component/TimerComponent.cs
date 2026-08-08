using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTask
{
    public DateTime time;
    public string taskId;
    public Action<string> task;
}

/// <summary>
/// 以毫秒为单位
/// </summary>
public class TimerComponent : BaseComponent
{
    private List<TimeTask> timeTasks = new List<TimeTask>();

    private double serverTimeStamp = 0;
    private float tempTimeStamp = 0;

    private void FixedUpdate()
    {
        if (serverTimeStamp > 0)
        {
            serverTimeStamp += Time.deltaTime * 1000.0;
            tempTimeStamp += Time.deltaTime;
            if (tempTimeStamp >= 1) // 每秒处理一次
            {
                OneSecondTrigger();
                tempTimeStamp -= 1; // 重置时间戳
            }
        }
    }

    #region 主要方法
    /// <summary>
    /// 初始化服务器时间
    /// </summary>
    public void InitServerTime(long serverTime)
    {
        serverTimeStamp = serverTime;
    }

    /// <summary>
    /// 服务器时间
    /// </summary>
    public long ServerTimeStamp
    {
        get { return (long)serverTimeStamp; }
    }

    /// <summary>
    /// 当前时间
    /// </summary>
    public DateTime Now
    {
        get
        {
            if (serverTimeStamp == 0)
            {
                Debug.LogWarningFormat("服务器时间戳为 0, 返回当前时间");
                return DateTime.Now;
            }
            else
            {
                return new DateTime(ServerTimeStamp * 10000 + DateTime.UnixEpoch.Ticks).ToLocalTime();
            }
        }
    }

    /// <summary>
    /// 每秒轮询事件广播
    /// </summary>
    private void OneSecondTrigger()
    {
        GameManager.Event.BroadcastNow(GameEventType.OneSecondEvent);

        for (int i = timeTasks.Count - 1; i >= 0; i--)
        {
            if (Now >= timeTasks[i].time)
            {
                timeTasks[i].task.Invoke(timeTasks[i].taskId);
                timeTasks.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 添加任务
    /// </summary>
    /// <param name="dateTime">任务结束时间</param>
    /// <param name="taskId">任务标识</param>
    /// <param name="action">回调</param>
    public void AddTimeTask(DateTime dateTime, string taskId, Action<string> action)
    {
        timeTasks.Add(new TimeTask { time = dateTime, taskId = taskId, task = action });
    }

    /// <summary>
    /// 移除任务
    /// </summary>
    /// <param name="taskId">任务标识</param>
    public void RemoveTimeTask(string taskId)
    {
        for (int i = timeTasks.Count - 1; i >= 0; i--)
        {
            if (timeTasks[i].taskId.Equals(taskId))
            {
                timeTasks.RemoveAt(i);
                break;
            }
        }
    }
    #endregion
}
