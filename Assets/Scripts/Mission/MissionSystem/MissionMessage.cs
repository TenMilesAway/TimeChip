using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionMessage
{
    public readonly MissionEventType type;
    public readonly string args;
    public readonly int amount;
    public bool hasUsed { get; private set; }

    public MissionMessage(MissionEventType type, int amount = 1, string args = null)
    {
        this.type = type;
        this.args = args;
        this.amount = amount;
        this.hasUsed = false;
    }

    /// <summary>
    /// 使用当前消息
    /// </summary>
    public void Use() => hasUsed = true;
}

/// <summary>
/// 消息事件类型枚举
/// </summary>
public enum MissionEventType
{
    Coin,
    Health,
}
