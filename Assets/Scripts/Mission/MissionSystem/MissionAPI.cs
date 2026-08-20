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
    private static bool _isRestoringMissions;
    private static readonly MissionSaveComponent SaveComponent = new MissionSaveComponent();

    /// <summary>由玩家存档初始化任务, 并恢复全部未完成任务</summary>
    public static void Initialize(PlayerInfoManager playerInfoManager)
    {
        if (playerInfoManager == null)
        {
            throw new ArgumentNullException(nameof(playerInfoManager));
        }

        _playerInfoManager = playerInfoManager;
        MissionManager.AddComponent(SaveComponent);
        _isInitialized = true;

        _isRestoringMissions = true;
        try
        {
            RemoveActiveMissions();
            RestoreMissions(playerInfoManager.GetSnapshot().activeMissions);
        }
        finally
        {
            _isRestoringMissions = false;
        }
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

    private static void RestoreMissions(List<PlayerMissionData> missionData)
    {
        if (missionData == null)
        {
            return;
        }

        for (int i = 0; i < missionData.Count; i++)
        {
            PlayerMissionData data = missionData[i];
            if (data == null || string.IsNullOrEmpty(data.missionId))
            {
                continue;
            }

            if (!MissionProtoManager.GetInstance().TryCreateMissionProto(
                    data.missionId,
                    out MissionPrototype<MissionMessage> missionProto))
            {
                Debug.LogWarning("[任务系统] 找不到任务原型，无法恢复任务: " + data.missionId);
                continue;
            }

            if (!MissionManager.StartMission(missionProto))
            {
                Debug.LogWarning("[任务系统] 无法恢复重复任务: " + data.missionId);
                continue;
            }

            RestoreMissionProgress(missionProto, data.requirementProgress);
        }
    }

    private static void RemoveActiveMissions()
    {
        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            MissionManager.RemoveMission(missions[i].id);
        }
    }

    private static void RestoreMissionProgress(
        MissionPrototype<MissionMessage> missionProto,
        List<int> requirementProgress)
    {
        if (requirementProgress == null)
        {
            return;
        }

        for (int i = 0; i < missionProto.requires.Length && i < requirementProgress.Count; i++)
        {
            int progress = Mathf.Max(0, requirementProgress[i]);
            if (progress == 0)
            {
                continue;
            }

            MissionMessage message;
            if (missionProto.requires[i] is MissionRequireCoin)
            {
                message = new MissionMessage(MissionEventType.Coin, progress);
            }
            else if (missionProto.requires[i] is MissionRequireHealth)
            {
                message = new MissionMessage(MissionEventType.Health, progress);
            }
            else
            {
                Debug.LogWarning("[任务系统] 任务条件不支持进度恢复: " + missionProto.id);
                continue;
            }

            MissionManager.SendMessage(message);
        }
    }

    private static void SaveMissions()
    {
        if (!_isInitialized || _isRestoringMissions || _playerInfoManager == null)
        {
            return;
        }

        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        List<PlayerMissionData> missionData = new List<PlayerMissionData>(missions.Length);
        for (int i = 0; i < missions.Length; i++)
        {
            MissionProgress[] progresses = missions[i].Progresses;
            List<int> requirementProgress = new List<int>(progresses.Length);
            for (int j = 0; j < progresses.Length; j++)
            {
                requirementProgress.Add(progresses[j].currentCount);
            }

            missionData.Add(new PlayerMissionData
            {
                missionId = missions[i].id,
                requirementProgress = requirementProgress
            });
        }

        _playerInfoManager.SetActiveMissions(missionData);
    }

    private sealed class MissionSaveComponent : IMissionSystemComponent<MissionMessage>
    {
        public void OnMissionStarted(Mission<MissionMessage> mission)
        {
            SaveMissions();
        }

        public void OnMissionRemoved(Mission<MissionMessage> mission, bool isFinished)
        {
            SaveMissions();
        }

        public void OnMissionStatusChanged(Mission<MissionMessage> mission, bool isFinished)
        {
            SaveMissions();
        }
    }
}
