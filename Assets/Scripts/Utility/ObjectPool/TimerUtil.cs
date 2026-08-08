using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TimerUtil
{
    /// <summary>
    /// UTC + 8 = Beijing
    /// </summary>
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 获得 UTC 时间戳
    /// </summary>
    /// <param name="isMillisecond">true 获得毫秒，false 获得秒</param>
    /// <returns>时间戳</returns>
    public static long GetTimeStamp(bool isMillisecond = false)
    {
        return isMillisecond ? (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds : (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 获取延迟 time 秒后的 UTC 时间戳
    /// </summary>
    /// <param name="time">秒</param>
    /// <returns>延迟 time 秒后的时间戳</returns>
    public static long GetLaterMillisecondsBySecond(double time)
    {
        long currentTimestamp = GetTimeStamp(true);
        return currentTimestamp + (long)(time * 1000);
    }

    public static DateTime Milliseconds2DateTime(long time)
    {
        DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds(time);
        DateTime localDateTime = offset.LocalDateTime;
        return localDateTime;
    }
}