using System;
using System.Collections.Generic;
using RedSaw.MissionSystem;
using UnityEngine;

/// <summary>游戏任务系统的统一入口</summary>
public static class MissionAPI
{
    public static readonly MissionManager<MissionMessage> MissionManager = new MissionManager<MissionMessage>();

    private static PlayerInfoManager _playerInfoManager;
    private static bool _isInitialized;

    /// <summary>由玩家存档初始化任务, 并恢复全部未完成任务</summary>
    public static void Initialize(PlayerInfoManager playerInfoManager)
    {
        _playerInfoManager = playerInfoManager;
        _isInitialized = true;
    }

    /// <summary>广播游戏任务消息</summary>
    public static void Broadcast(MissionMessage message)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("任务系统尚未初始化，忽略任务消息");
            return;
        }

        MissionManager.SendMessage(message);
    }

    public static void StartMission(MissionPrototype<MissionMessage> missionProto)
    {
        MissionManager.StartMission(missionProto);
        Debug.Log("[任务系统] 开启任务" + missionProto.id);
    }
}
