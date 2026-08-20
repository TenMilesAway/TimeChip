using System;
using System.Collections.Generic;
using RedSaw.MissionSystem;
using UnityEngine;

/// <summary>游戏任务系统的统一入口。</summary>
public static class MissionAPI
{
    public static readonly MissionManager<MissionMessage> MissionManager =
        new MissionManager<MissionMessage>();

    public static event Action GameOverRequested;

    private static readonly MissionSaveComponent SaveComponent = new MissionSaveComponent();
    private static readonly Dictionary<string, PlayerMissionData> MissionTimings =
        new Dictionary<string, PlayerMissionData>();

    private static PlayerInfoManager _playerInfoManager;
    private static bool _isInitialized;
    private static bool _isRestoringMissions;
    private static bool _isSynchronizingMissions;
    private static bool _isEvaluatingMissions;

    /// <summary>由玩家存档初始化任务，并恢复未完成任务。</summary>
    public static void Initialize(PlayerInfoManager playerInfoManager, bool isNewGame)
    {
        if (playerInfoManager == null)
        {
            throw new ArgumentNullException(nameof(playerInfoManager));
        }

        UnsubscribePlayerEvents();
        _playerInfoManager = playerInfoManager;
        _playerInfoManager.TurnAdvanced += OnTurnAdvanced;
        _playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        MissionManager.AddComponent(SaveComponent);
        _isInitialized = true;

        _isRestoringMissions = true;
        try
        {
            RemoveActiveMissions();
            RestoreMissions(_playerInfoManager.GetSnapshot().activeMissions);
        }
        finally
        {
            _isRestoringMissions = false;
        }

        EvaluateAvailableMissions(isNewGame);
        CheckDeadlines();
    }

    /// <summary>广播游戏任务消息。</summary>
    public static void Broadcast(MissionMessage message)
    {
        if (!_isInitialized)
        {
            return;
        }

        MissionManager.SendMessage(message);
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
            if (data == null || string.IsNullOrEmpty(data.missionId) ||
                !TryGetMissionConfig(data.missionId, out cfg.Mission missionConfig) ||
                !MissionProtoManager.GetInstance().TryCreateMissionProto(
                    missionConfig,
                    out MissionPrototype<MissionMessage> missionProto))
            {
                continue;
            }

            MissionTimings[data.missionId] = CreateMissionData(missionConfig, data);
            if (MissionManager.StartMission(missionProto))
            {
                RestoreMissionProgress(missionProto, data.requirementProgress);
            }
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

    private static void OnTurnAdvanced()
    {
        EvaluateAvailableMissions(false);
        CheckDeadlines();
    }

    private static void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        if (!_isSynchronizingMissions)
        {
            EvaluateAvailableMissions(false);
        }
    }

    private static void EvaluateAvailableMissions(bool allowInitialMissions)
    {
        if (!_isInitialized || _isEvaluatingMissions)
        {
            return;
        }

        _isEvaluatingMissions = true;
        try
        {
            IReadOnlyList<cfg.Mission> configs =
                DataTableMananger.GetInstance().Tables.MissionTable.DataList;
            PlayerInfoData playerData = _playerInfoManager.GetSnapshot();
            for (int i = 0; i < configs.Count; i++)
            {
                cfg.Mission missionConfig = configs[i];
                string missionId = missionConfig.Id.ToString();
                if (MissionManager.GetMission(missionId) != null ||
                    playerData.completedMissionIds.Contains(missionId) ||
                    !CanStartMission(missionConfig, allowInitialMissions))
                {
                    continue;
                }

                StartMission(missionConfig);
            }
        }
        finally
        {
            _isEvaluatingMissions = false;
        }
    }

    private static bool CanStartMission(cfg.Mission missionConfig, bool allowInitialMissions)
    {
        string condition = missionConfig.Condition;
        if (string.IsNullOrEmpty(condition))
        {
            return allowInitialMissions;
        }

        if (condition.StartsWith("date:"))
        {
            return TryParseYearMonth(condition.Substring(5), out int age, out int month) &&
                IsAtOrAfter(_playerInfoManager.CurrentAge, _playerInfoManager.CurrentMonth, age, month);
        }

        if (condition.StartsWith("mission:"))
        {
            string requiredMissionId = condition.Substring(8);
            return _playerInfoManager.GetSnapshot().completedMissionIds.Contains(requiredMissionId);
        }

        if (condition.StartsWith("healthBelow:"))
        {
            return int.TryParse(condition.Substring(12), out int health) &&
                _playerInfoManager.Health <= health;
        }

        if (missionConfig.Message == "Health" && int.TryParse(condition, out int legacyHealth))
        {
            return _playerInfoManager.Health <= legacyHealth;
        }

        Debug.LogWarning("[任务系统] 不支持的开启条件: " + condition);
        return false;
    }

    private static void StartMission(cfg.Mission missionConfig)
    {
        if (!MissionProtoManager.GetInstance().TryCreateMissionProto(
                missionConfig,
                out MissionPrototype<MissionMessage> missionProto))
        {
            return;
        }

        string missionId = missionConfig.Id.ToString();
        MissionTimings[missionId] = CreateMissionData(missionConfig, null);
        if (MissionManager.StartMission(missionProto))
        {
            Debug.Log("[任务系统] 开启任务: " + missionConfig.Id + " - " + missionConfig.Name);
            return;
        }

        MissionTimings.Remove(missionId);
    }

    private static void CheckDeadlines()
    {
        if (!_isInitialized)
        {
            return;
        }

        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            if (!MissionTimings.TryGetValue(missions[i].id, out PlayerMissionData data) ||
                data.deadlineAge <= 0 ||
                !IsAtOrAfter(
                    _playerInfoManager.CurrentAge,
                    _playerInfoManager.CurrentMonth,
                    data.deadlineAge,
                    data.deadlineMonth))
            {
                continue;
            }

            ResolveFailedMission(missions[i].id);
            if (!_isInitialized)
            {
                return;
            }
        }
    }

    private static void ResolveFailedMission(string missionId)
    {
        if (!TryGetMissionConfig(missionId, out cfg.Mission missionConfig))
        {
            return;
        }

        Debug.Log(
            "[任务系统] 任务失败: " + missionConfig.Id +
            " - " + missionConfig.Name +
            "，结算类型: " + missionConfig.Failure);
        MissionManager.RemoveMission(missionId);
        if (string.Equals(missionConfig.Failure, "gameover", StringComparison.OrdinalIgnoreCase))
        {
            _isInitialized = false;
            GameOverRequested?.Invoke();
        }
        else
        {
            Debug.LogWarning("[任务系统] 不支持的失败结算: " + missionConfig.Failure);
        }
    }

    private static PlayerMissionData CreateMissionData(
        cfg.Mission missionConfig,
        PlayerMissionData savedData)
    {
        PlayerMissionData data = new PlayerMissionData
        {
            missionId = missionConfig.Id.ToString(),
            startedAge = savedData == null ? _playerInfoManager.CurrentAge : savedData.startedAge,
            startedMonth = savedData == null ? _playerInfoManager.CurrentMonth : savedData.startedMonth,
            deadlineAge = savedData == null ? 0 : savedData.deadlineAge,
            deadlineMonth = savedData == null ? 0 : savedData.deadlineMonth
        };

        if (data.startedAge <= 0 || data.startedMonth < 1 || data.startedMonth > 12)
        {
            data.startedAge = _playerInfoManager.CurrentAge;
            data.startedMonth = _playerInfoManager.CurrentMonth;
        }

        if (data.deadlineAge == 0)
        {
            SetDeadline(missionConfig.Deadline, data);
        }

        return data;
    }

    private static void SetDeadline(string deadline, PlayerMissionData data)
    {
        if (string.IsNullOrEmpty(deadline))
        {
            return;
        }

        if (TryParseYearMonth(deadline, out int age, out int month))
        {
            data.deadlineAge = age;
            data.deadlineMonth = month;
            return;
        }

        if (int.TryParse(deadline, out int monthsAfterStart) && monthsAfterStart > 0)
        {
            AddMonths(data.startedAge, data.startedMonth, monthsAfterStart, out data.deadlineAge, out data.deadlineMonth);
            return;
        }

        Debug.LogWarning("[任务系统] 不支持的任务期限: " + deadline);
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

            PlayerMissionData data = MissionTimings.TryGetValue(missions[i].id, out PlayerMissionData timing)
                ? CreateMissionDataForSave(timing)
                : new PlayerMissionData { missionId = missions[i].id };
            data.requirementProgress = requirementProgress;
            missionData.Add(data);
        }

        _isSynchronizingMissions = true;
        try
        {
            _playerInfoManager.SetActiveMissions(missionData);
        }
        finally
        {
            _isSynchronizingMissions = false;
        }
    }

    private static void MarkMissionCompleted(string missionId)
    {
        List<string> completedMissionIds = _playerInfoManager.GetSnapshot().completedMissionIds;
        if (completedMissionIds.Contains(missionId))
        {
            return;
        }

        completedMissionIds.Add(missionId);
        _isSynchronizingMissions = true;
        try
        {
            _playerInfoManager.SetCompletedMissionIds(completedMissionIds);
        }
        finally
        {
            _isSynchronizingMissions = false;
        }
    }

    private static void RemoveActiveMissions()
    {
        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            MissionManager.RemoveMission(missions[i].id);
        }

        MissionTimings.Clear();
    }

    private static void UnsubscribePlayerEvents()
    {
        if (_playerInfoManager == null)
        {
            return;
        }

        _playerInfoManager.TurnAdvanced -= OnTurnAdvanced;
        _playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
    }

    private static bool TryGetMissionConfig(string missionId, out cfg.Mission missionConfig)
    {
        missionConfig = null;
        return int.TryParse(missionId, out int missionIdValue) &&
            (missionConfig = DataTableMananger.GetInstance()
                .Tables
                .MissionTable
                .GetOrDefault(missionIdValue)) != null;
    }

    private static bool TryParseYearMonth(string value, out int age, out int month)
    {
        age = 0;
        month = 0;
        string[] values = value.Split(',');
        return values.Length == 2 &&
            int.TryParse(values[0], out age) &&
            int.TryParse(values[1], out month) &&
            age > 0 &&
            month >= 1 &&
            month <= 12;
    }

    private static bool IsAtOrAfter(int age, int month, int targetAge, int targetMonth)
    {
        return age > targetAge || (age == targetAge && month >= targetMonth);
    }

    private static void AddMonths(
        int age,
        int month,
        int months,
        out int resultAge,
        out int resultMonth)
    {
        int totalMonths = age * 12 + month - 1 + months;
        resultAge = totalMonths / 12;
        resultMonth = totalMonths % 12 + 1;
    }

    private static PlayerMissionData CreateMissionDataForSave(PlayerMissionData source)
    {
        return new PlayerMissionData
        {
            missionId = source.missionId,
            startedAge = source.startedAge,
            startedMonth = source.startedMonth,
            deadlineAge = source.deadlineAge,
            deadlineMonth = source.deadlineMonth
        };
    }

    private sealed class MissionSaveComponent : IMissionSystemComponent<MissionMessage>
    {
        public void OnMissionStarted(Mission<MissionMessage> mission)
        {
            SaveMissions();
        }

        public void OnMissionRemoved(Mission<MissionMessage> mission, bool isFinished)
        {
            MissionTimings.Remove(mission.id);
            if (isFinished)
            {
                MarkMissionCompleted(mission.id);
            }

            SaveMissions();
            if (isFinished)
            {
                EvaluateAvailableMissions(false);
            }
        }

        public void OnMissionStatusChanged(Mission<MissionMessage> mission, bool isFinished)
        {
            SaveMissions();
        }
    }
}
