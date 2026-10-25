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
    private const int RandomMissionId = 3001;
    private const int RandomFishMissionId = 3004;
    private const int RandomStampMissionId = 3005;
    private const string CommunityCentreNoticeMission3005FunctionId =
        "CommunityCentreNoticeMission3005";
    /// <summary>由社区 _may 点击开启，不参与自动接取。</summary>
    private const int DragonBoatPromotionMissionId = 4000;

    private const string CollectTargetType = "collect";
    private const string SubmitTargetType = "submit";
    private const string UnlockedHomeIdRangeCountPrefix = "unlockedHomeIdRangeCount:";

    private static PlayerInfoManager _playerInfoManager;
    private static bool _isInitialized;
    private static bool _isRestoringMissions;
    private static bool _isSynchronizingMissions;
    private static bool _isEvaluatingMissions;
    private static bool _isClaimingMission;
    private static bool _hasBroadcastSimulationCoinBalance;
    private static int _lastBroadcastSimulationCoinBalance;
    private static bool _dragonBoatPromotionMayButtonStartAuthorized;

    /// <summary>离开游戏或重新初始化前清空运行时任务，避免串档。</summary>
    public static void Shutdown()
    {
        UnsubscribePlayerEvents();
        _isInitialized = false;
        _hasBroadcastSimulationCoinBalance = false;
        ClearRuntimeMissionsWithoutSave();
    }

    /// <summary>由玩家存档初始化任务，并恢复未完成任务。</summary>
    public static void Initialize(PlayerInfoManager playerInfoManager, bool isNewGame)
    {
        if (playerInfoManager == null)
        {
            throw new ArgumentNullException(nameof(playerInfoManager));
        }

        Shutdown();
        _playerInfoManager = playerInfoManager;
        _playerInfoManager.TurnAdvanced += OnTurnAdvanced;
        _playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        MissionManager.AddComponent(SaveComponent);
        _isInitialized = true;

        if (isNewGame)
        {
            _isSynchronizingMissions = true;
            try
            {
                _playerInfoManager.SetActiveMissions(new List<PlayerMissionData>());
            }
            finally
            {
                _isSynchronizingMissions = false;
            }
        }

        _isRestoringMissions = true;
        try
        {
            RestoreMissions(_playerInfoManager.GetSnapshot().activeMissions);
        }
        finally
        {
            _isRestoringMissions = false;
        }

        EnsureMonthlyRandomMissionOffer();
        EvaluateAvailableMissions(isNewGame);
        SynchronizeSubmitMissionProgresses();
        BroadcastSimulationCoinBalance(force: true);
        SynchronizeAdvanceTurnMissions();
        CheckDeadlines();
    }

    /// <summary>广播游戏任务消息</summary>
    public static void Broadcast(MissionMessage message)
    {
        if (!_isInitialized)
        {
            return;
        }

        MissionManager.SendMessage(message);
    }

    /// <summary>领取已完成任务的奖励。</summary>
    public static bool TryClaimMission(string missionId)
    {
        if (!_isInitialized)
        {
            return false;
        }

        Mission<MissionMessage> mission = MissionManager.GetMission(missionId);
        if (mission == null || !mission.IsFinished)
        {
            return false;
        }

        _isClaimingMission = true;
        try
        {
            return TrySubmitMissionTarget(missionId) &&
                MissionManager.TryClaimMission(missionId);
        }
        finally
        {
            _isClaimingMission = false;
        }
    }

    /// <summary>获取当前进行中或待领取的任务。</summary>
    public static Mission<MissionMessage>[] GetActiveMissions()
    {
        return _isInitialized ? MissionManager.GetMissions() : Array.Empty<Mission<MissionMessage>>();
    }

    public static bool IsMissionInProgress(int missionId)
    {
        if (!_isInitialized || missionId <= 0)
        {
            return false;
        }

        Mission<MissionMessage> mission = MissionManager.GetMission(missionId.ToString());
        return mission != null && !mission.IsFinished;
    }

    /// <summary>仅由社区 <c>_may</c> 按钮点击开启端午促销（任务 4000）。</summary>
    public static bool TryStartDragonBoatPromotionMission(bool playStartDialogue = true)
    {
        if (!_isInitialized || _playerInfoManager == null)
        {
            return false;
        }

        string missionIdText = DragonBoatPromotionMissionId.ToString();
        PlayerInfoData playerData = _playerInfoManager.GetSnapshot();
        if (playerData.completedMissionIds.Contains(missionIdText) ||
            MissionManager.GetMission(missionIdText) != null)
        {
            return false;
        }

        if (!TryGetMissionConfig(missionIdText, out cfg.Mission missionConfig))
        {
            return false;
        }

        _dragonBoatPromotionMayButtonStartAuthorized = true;
        try
        {
            StartMission(missionConfig, playStartDialogue);
        }
        finally
        {
            _dragonBoatPromotionMayButtonStartAuthorized = false;
        }

        return MissionManager.GetMission(missionIdText) != null;
    }

    /// <summary>获取已启动任务固定后的随机道具目标。</summary>
    public static bool TryGetMissionTarget(
        string missionId,
        out int targetItemId,
        out int targetItemCount)
    {
        targetItemId = 0;
        targetItemCount = 0;
        if (!_isInitialized ||
            !MissionTimings.TryGetValue(missionId, out PlayerMissionData missionData) ||
            missionData.targetItemId <= 0 ||
            missionData.targetItemCount <= 0)
        {
            return false;
        }

        targetItemId = missionData.targetItemId;
        targetItemCount = missionData.targetItemCount;
        return true;
    }

    /// <summary>尝试获取任务的截止日期；未设置截止日期时返回 false。</summary>
    public static bool TryGetMissionDeadline(string missionId, out int deadlineAge, out int deadlineMonth)
    {
        deadlineAge = 0;
        deadlineMonth = 0;
        if (!_isInitialized ||
            !MissionTimings.TryGetValue(missionId, out PlayerMissionData missionData) ||
            missionData.deadlineAge <= 0 ||
            missionData.deadlineMonth < 1 ||
            missionData.deadlineMonth > 12)
        {
            return false;
        }

        deadlineAge = missionData.deadlineAge;
        deadlineMonth = missionData.deadlineMonth;
        return true;
    }

    private static bool TrySubmitMissionTarget(string missionId)
    {
        if (!TryGetMissionConfig(missionId, out cfg.Mission missionConfig))
        {
            return false;
        }

        string targetType = string.IsNullOrWhiteSpace(missionConfig.TargetType)
            ? CollectTargetType
            : missionConfig.TargetType.Trim();
        if (string.Equals(targetType, CollectTargetType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(targetType, SubmitTargetType, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[任务系统] 不支持的任务目标类型: [{targetType}]，任务 [{missionId}]。");
            return false;
        }

        if (missionConfig.Message != "Item" ||
            !TryGetMissionTarget(missionId, out int targetItemId, out int targetItemCount))
        {
            Debug.LogError($"[任务系统] 提交类型任务缺少有效的物品目标: [{missionId}]。");
            return false;
        }

        if (_playerInfoManager.GetItemCount(targetItemId) < targetItemCount)
        {
            Debug.LogWarning($"[任务系统] 提交任务所需物品不足: [{missionId}]。");
            return false;
        }

        return _playerInfoManager.TryConsumeItem(targetItemId, targetItemCount);
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
                    missionConfig, data.targetItemId, data.targetItemCount,
                    out MissionPrototype<MissionMessage> missionProto))
            {
                continue;
            }

            MissionTimings[data.missionId] = CreateMissionData(missionConfig, data);
            if (MissionManager.StartMission(missionProto))
            {
                MissionManager
                    .GetMission(data.missionId)
                    .RestoreProgress(data.requirementProgress);
                MissionDialogueService.TryPlayMissionGuides(missionConfig);
            }
        }
    }

    private static void OnTurnAdvanced()
    {
        CheckDeadlines();
        EnsureMonthlyRandomMissionOffer();
        Broadcast(new MissionMessage(MissionEventType.AdvanceTurn, 1));
        EvaluateAvailableMissions(false);
    }

    private static void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        if (!_isSynchronizingMissions)
        {
            EvaluateAvailableMissions(false);
            if (!_isClaimingMission)
            {
                SynchronizeSubmitMissionProgresses();
            }
        }

        BroadcastSimulationCoinBalance();
        SynchronizeAdvanceTurnMissions();
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
                if (missionConfig.Id == DragonBoatPromotionMissionId)
                {
                    continue;
                }

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

    private static void SynchronizeSubmitMissionProgresses()
    {
        bool hasProgressChanged = false;
        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            Mission<MissionMessage> mission = missions[i];
            if (!TryGetMissionConfig(mission.id, out cfg.Mission missionConfig) ||
                missionConfig.Message != "Item" ||
                !string.Equals(
                    missionConfig.TargetType,
                    SubmitTargetType,
                    StringComparison.OrdinalIgnoreCase) ||
                !TryGetMissionTarget(
                    mission.id,
                    out int targetItemId,
                    out int targetItemCount))
            {
                continue;
            }

            MissionProgress[] progresses = mission.Progresses;
            int currentCount = Mathf.Clamp(
                _playerInfoManager.GetItemCount(targetItemId),
                0,
                targetItemCount);
            if (progresses.Length == 0 ||
                progresses[0].currentCount == currentCount)
            {
                continue;
            }

            mission.RestoreProgress(new[] { currentCount });
            hasProgressChanged = true;
        }

        if (hasProgressChanged)
        {
            SaveMissions();
        }
    }

    private static bool CanStartMission(cfg.Mission missionConfig, bool allowInitialMissions)
    {
        if (missionConfig.Id == DragonBoatPromotionMissionId)
        {
            return false;
        }

        if (IsMonthlyRandomMission(missionConfig.Id))
        {
            return false;
        }

        PlayerInfoData playerData = _playerInfoManager.GetSnapshot();
        string condition = missionConfig.Condition;
        if (string.IsNullOrEmpty(condition))
        {
            return allowInitialMissions;
        }

        string[] conditions = condition.Split(';');
        for (int i = 0; i < conditions.Length; i++)
        {
            string itemCondition = conditions[i].Trim();
            if (itemCondition.StartsWith("currentMonth:"))
            {
                if (int.TryParse(itemCondition.Substring(13), out int requiredMonth) &&
                    requiredMonth >= 1 &&
                    requiredMonth <= 12 &&
                    _playerInfoManager.CurrentMonth == requiredMonth)
                {
                    return true;
                }

                continue;
            }

            if (itemCondition.StartsWith("date:") || itemCondition.StartsWith("force:"))
            {
                int separatorIndex = itemCondition.IndexOf(':');
                if (TryParseYearMonth(
                        itemCondition.Substring(separatorIndex + 1),
                        out int age,
                        out int month) &&
                    IsAtOrAfter(_playerInfoManager.CurrentAge, _playerInfoManager.CurrentMonth, age, month))
                {
                    return true;
                }

                continue;
            }

            if (itemCondition.StartsWith("mission:"))
            {
                string requiredMissionId = itemCondition.Substring(8);
                if (_playerInfoManager.GetSnapshot().completedMissionIds.Contains(requiredMissionId))
                {
                    return true;
                }

                continue;
            }

            if (itemCondition.StartsWith("healthBelow:"))
            {
                if (int.TryParse(itemCondition.Substring(12), out int health) &&
                    _playerInfoManager.Health <= health)
                {
                    return true;
                }

                continue;
            }

            if (itemCondition.StartsWith("satisfactionAtLeast:"))
            {
                if (float.TryParse(
                        itemCondition.Substring(20),
                        out float requiredSatisfaction) &&
                    _playerInfoManager.Satisfaction >= requiredSatisfaction)
                {
                    return true;
                }

                continue;
            }

            if (itemCondition.StartsWith(UnlockedHomeIdRangeCountPrefix))
            {
                if (TryParseUnlockedHomeIdRangeCount(
                        itemCondition.Substring(UnlockedHomeIdRangeCountPrefix.Length),
                        out int minimumHomeId,
                        out int maximumHomeId,
                        out int requiredCount) &&
                    HasAtLeastUnlockedHomesInRange(
                        playerData.unlockedHomeIds,
                        minimumHomeId,
                        maximumHomeId,
                        requiredCount))
                {
                    return true;
                }

                continue;
            }

            if (itemCondition == "wheelCoinObtained")
            {
                if (_playerInfoManager.HasUnlockedMysteryWheelLottery)
                {
                    return true;
                }

                continue;
            }

            if (missionConfig.Message == "Health" &&
                int.TryParse(itemCondition, out int legacyHealth))
            {
                if (_playerInfoManager.Health <= legacyHealth)
                {
                    return true;
                }

                continue;
            }

            Debug.LogWarning("[任务系统] 不支持的开启条件: " + itemCondition);
        }

        return false;
    }

    private static void StartMission(cfg.Mission missionConfig, bool playStartDialogue = true)
    {
        if (missionConfig.Id == DragonBoatPromotionMissionId &&
            !_dragonBoatPromotionMayButtonStartAuthorized)
        {
            return;
        }

        PlayerMissionData missionData = CreateMissionData(missionConfig, null);
        if (IsMonthlyRandomMission(missionConfig.Id))
        {
            if (!_playerInfoManager.HasMonthlyRandomMissionOffer())
            {
                return;
            }

            PlayerInfoData playerData = _playerInfoManager.GetSnapshot();
            missionData.targetItemId = playerData.randomMissionTargetItemId;
            missionData.targetItemCount = playerData.randomMissionTargetCount;
        }

        if (!MissionProtoManager.GetInstance().TryCreateMissionProto(
                missionConfig, missionData.targetItemId, missionData.targetItemCount,
                out MissionPrototype<MissionMessage> missionProto))
        {
            return;
        }

        string missionId = missionConfig.Id.ToString();
        MissionTimings[missionId] = missionData;
        if (MissionManager.StartMission(missionProto))
        {
            if (IsMonthlyRandomMission(missionConfig.Id))
            {
                _playerInfoManager.ClearMonthlyRandomMissionOffer();
            }

            Debug.Log("[任务系统] 开启任务: " + missionConfig.Id + " - " + missionConfig.Name);
            ShowMissionStartedTip(missionConfig);
            if (playStartDialogue)
            {
                MissionDialogueService.TryPlayStartDialogue(missionConfig);
            }

            BroadcastSimulationCoinBalance(force: true);
            SynchronizeAdvanceTurnMissions();
            return;
        }

        MissionTimings.Remove(missionId);
    }

    private static void ShowMissionStartedTip(cfg.Mission missionConfig)
    {
        if (missionConfig == null)
        {
            return;
        }

        CommonTipView.Show("新任务开启：【" + missionConfig.Name + "】");
    }

    /// <summary>公告栏展示本月待领取的随机委托时返回 true。</summary>
    public static bool HasMonthlyRandomMissionOffer()
    {
        if (!_isInitialized || _playerInfoManager == null ||
            !_playerInfoManager.HasMonthlyRandomMissionOffer())
        {
            return false;
        }

        return IsMonthlyRandomMissionOfferAccessible(
            _playerInfoManager.GetMonthlyRandomMissionOfferId());
    }

    /// <summary>由公告栏领取本月的随机委托。</summary>
    public static bool TryStartMonthlyRandomMission(
        out cfg.Mission missionConfig,
        out int targetItemId,
        out int targetItemCount)
    {
        missionConfig = null;
        targetItemId = 0;
        targetItemCount = 0;
        if (!_isInitialized ||
            !_playerInfoManager.HasMonthlyRandomMissionOffer() ||
            !TryGetMissionConfig(
                _playerInfoManager.GetMonthlyRandomMissionOfferId().ToString(),
                out missionConfig) ||
            (missionConfig.Id == RandomFishMissionId && !SubwayView.IsFishingUnlocked()) ||
            (missionConfig.Id == RandomStampMissionId &&
             !IsCommunityCentreNoticeStampMissionUnlocked()) ||
            MissionManager.GetMission(missionConfig.Id.ToString()) != null)
        {
            return false;
        }

        PlayerInfoData playerData = _playerInfoManager.GetSnapshot();
        if (missionConfig.Id == RandomFishMissionId &&
            (playerData.randomMissionTargetItemId <= 0 ||
             playerData.randomMissionTargetCount <= 0))
        {
            if (!TryPickRandomMissionTarget(
                    missionConfig.RandomTarget,
                    out int fishItemId,
                    out int fishCount))
            {
                return false;
            }

            _playerInfoManager.SetMonthlyRandomMissionOffer(
                RandomFishMissionId,
                fishItemId,
                fishCount);
            playerData = _playerInfoManager.GetSnapshot();
        }

        targetItemId = playerData.randomMissionTargetItemId;
        targetItemCount = playerData.randomMissionTargetCount;
        StartMission(missionConfig);
        return MissionManager.GetMission(missionConfig.Id.ToString()) != null;
    }

    private static void EnsureMonthlyRandomMissionOffer()
    {
        if (_playerInfoManager.HasGeneratedMonthlyRandomMissionOffer() ||
            MissionManager.GetMission(RandomMissionId.ToString()) != null ||
            MissionManager.GetMission(RandomFishMissionId.ToString()) != null ||
            MissionManager.GetMission(RandomStampMissionId.ToString()) != null)
        {
            return;
        }

        if (!TurnRandom.Chance(
                "Mission.MonthlyOffer.Spawn",
                GameProbability.Get("Mission.MonthlyOffer.Spawn")))
        {
            _playerInfoManager.MarkMonthlyRandomMissionOfferGenerated();
            return;
        }

        List<int> monthlyOfferCandidates = new List<int> { RandomMissionId };
        if (SubwayView.IsFishingUnlocked())
        {
            monthlyOfferCandidates.Add(RandomFishMissionId);
        }

        if (IsCommunityCentreNoticeStampMissionUnlocked())
        {
            monthlyOfferCandidates.Add(RandomStampMissionId);
        }

        int missionId = monthlyOfferCandidates[
            TurnRandom.Range("Mission.MonthlyOffer.Type", 0, monthlyOfferCandidates.Count)];

        if (!TryGetMissionConfig(missionId.ToString(), out cfg.Mission missionConfig))
        {
            _playerInfoManager.MarkMonthlyRandomMissionOfferGenerated();
            return;
        }

        if (missionId == RandomStampMissionId)
        {
            _playerInfoManager.SetMonthlyRandomMissionOfferForMission(RandomStampMissionId);
            return;
        }

        int itemId = 0;
        int itemCount = 0;
        if (!TryPickRandomMissionTarget(
                missionConfig.RandomTarget,
                out itemId,
                out itemCount))
        {
            _playerInfoManager.MarkMonthlyRandomMissionOfferGenerated();
            return;
        }

        _playerInfoManager.SetMonthlyRandomMissionOffer(
            missionId,
            itemId,
            itemCount);
    }

    private static bool IsMonthlyRandomMission(int missionId)
    {
        return missionId == RandomMissionId ||
            missionId == RandomFishMissionId ||
            missionId == RandomStampMissionId;
    }

    private static bool IsCommunityCentreNoticeStampMissionUnlocked()
    {
        return FunctionUnlockService.IsUnlocked(CommunityCentreNoticeMission3005FunctionId);
    }

    private static bool IsMonthlyRandomMissionOfferAccessible(int missionId)
    {
        if (missionId == RandomFishMissionId && !SubwayView.IsFishingUnlocked())
        {
            return false;
        }

        if (missionId == RandomStampMissionId &&
            !IsCommunityCentreNoticeStampMissionUnlocked())
        {
            return false;
        }

        return true;
    }

    private static bool TryPickRandomMissionTarget(
        string targets,
        out int itemId,
        out int itemCount)
    {
        itemId = 0;
        itemCount = 0;
        string[] entries = string.IsNullOrEmpty(targets)
            ? Array.Empty<string>()
            : targets.Split(';');
        List<(int itemId, int itemCount, int weight)> candidates =
            new List<(int, int, int)>();
        int totalWeight = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            string[] values = entries[i].Split(',');
            if (values.Length != 3 ||
                !int.TryParse(values[0], out int candidateId) ||
                !int.TryParse(values[1], out int candidateCount) ||
                !int.TryParse(values[2], out int weight) ||
                candidateId <= 0 || candidateCount <= 0 || weight <= 0 ||
                DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(candidateId) == null)
            {
                continue;
            }

            candidates.Add((candidateId, candidateCount, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning("[任务系统] 随机任务目标配置无效: " + targets);
            return false;
        }

        int roll = TurnRandom.Range("Mission.MonthlyOffer.Target", 0, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].weight;
            if (roll < 0)
            {
                itemId = candidates[i].itemId;
                itemCount = candidates[i].itemCount;
                return true;
            }
        }

        return false;
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
            if (missions[i].IsFinished ||
                !MissionTimings.TryGetValue(missions[i].id, out PlayerMissionData data) ||
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

    private static void SynchronizeAdvanceTurnMissions()
    {
        if (!_isInitialized || _playerInfoManager == null || _isRestoringMissions)
        {
            return;
        }

        if (!IsAtOrAfter(
                _playerInfoManager.CurrentAge,
                _playerInfoManager.CurrentMonth,
                22,
                2) ||
            !HasIncompleteAdvanceTurnMission())
        {
            return;
        }

        Broadcast(new MissionMessage(MissionEventType.AdvanceTurn, 1));
    }

    private static bool HasIncompleteAdvanceTurnMission()
    {
        Mission<MissionMessage>[] missions = MissionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            if (!TryGetMissionConfig(missions[i].id, out cfg.Mission missionConfig) ||
                missionConfig.Message != "AdvanceTurn" ||
                !int.TryParse(missionConfig.Target, out int targetCount))
            {
                continue;
            }

            MissionProgress[] progresses = missions[i].Progresses;
            if (progresses.Length == 0 || progresses[0].currentCount < targetCount)
            {
                return true;
            }
        }

        return false;
    }

    private static void BroadcastSimulationCoinBalance(bool force = false)
    {
        if (!_isInitialized || _playerInfoManager == null)
        {
            return;
        }

        int simulationCoins = _playerInfoManager.SimulationCoins;
        if (!force &&
            _hasBroadcastSimulationCoinBalance &&
            _lastBroadcastSimulationCoinBalance == simulationCoins)
        {
            return;
        }

        _hasBroadcastSimulationCoinBalance = true;
        _lastBroadcastSimulationCoinBalance = simulationCoins;
        Broadcast(new MissionMessage(
            MissionEventType.SimulationCoinBalance,
            simulationCoins));
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
        if (string.IsNullOrEmpty(missionConfig.Failure))
        {
            return;
        }

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
            deadlineMonth = savedData == null ? 0 : savedData.deadlineMonth,
            targetItemId = savedData == null ? 0 : savedData.targetItemId,
            targetItemCount = savedData == null ? 0 : savedData.targetItemCount
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

        if (missionConfig.Message == "Item" &&
            data.targetItemId <= 0 &&
            !TryParseItemTarget(
                missionConfig.Target,
                out data.targetItemId,
                out data.targetItemCount))
        {
            Debug.LogWarning($"[任务系统] 物品任务目标配置无效: [{missionConfig.Id}], [{missionConfig.Target}]");
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

    private static void ClearRuntimeMissionsWithoutSave()
    {
        _isRestoringMissions = true;
        try
        {
            Mission<MissionMessage>[] missions = MissionManager.GetMissions();
            for (int i = 0; i < missions.Length; i++)
            {
                MissionManager.RemoveMission(missions[i].id);
            }

            MissionTimings.Clear();
        }
        finally
        {
            _isRestoringMissions = false;
        }
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

    private static bool TryParseItemTarget(string value, out int itemId, out int itemCount)
    {
        itemId = 0;
        itemCount = 0;
        string[] values = string.IsNullOrEmpty(value)
            ? Array.Empty<string>()
            : value.Split(',');
        return values.Length == 2 &&
            int.TryParse(values[0], out itemId) &&
            int.TryParse(values[1], out itemCount) &&
            itemId > 0 &&
            itemCount > 0 &&
            DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId) != null;
    }

    private static bool TryParseUnlockedHomeIdRangeCount(
        string value,
        out int minimumHomeId,
        out int maximumHomeId,
        out int requiredCount)
    {
        minimumHomeId = 0;
        maximumHomeId = 0;
        requiredCount = 0;
        string[] values = string.IsNullOrEmpty(value)
            ? Array.Empty<string>()
            : value.Split(',');
        return values.Length == 3 &&
            int.TryParse(values[0], out minimumHomeId) &&
            int.TryParse(values[1], out maximumHomeId) &&
            int.TryParse(values[2], out requiredCount) &&
            minimumHomeId > 0 &&
            maximumHomeId >= minimumHomeId &&
            requiredCount > 0;
    }

    private static bool HasAtLeastUnlockedHomesInRange(
        List<int> unlockedHomeIds,
        int minimumHomeId,
        int maximumHomeId,
        int requiredCount)
    {
        if (unlockedHomeIds == null)
        {
            return false;
        }

        int count = 0;
        for (int i = 0; i < unlockedHomeIds.Count; i++)
        {
            int homeId = unlockedHomeIds[i];
            if (homeId >= minimumHomeId && homeId <= maximumHomeId &&
                ++count >= requiredCount)
            {
                return true;
            }
        }

        return false;
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
            deadlineMonth = source.deadlineMonth,
            targetItemId = source.targetItemId,
            targetItemCount = source.targetItemCount
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
            cfg.Mission missionConfig = null;
            if (isFinished)
            {
                TryGetMissionConfig(mission.id, out missionConfig);
            }

            MissionTimings.Remove(mission.id);
            if (isFinished)
            {
                MarkMissionCompleted(mission.id);
            }

            SaveMissions();
            if (isFinished)
            {
                MissionDialogueService.TryPlayEndDialogue(missionConfig);
                EvaluateAvailableMissions(false);
            }
        }

        public void OnMissionStatusChanged(Mission<MissionMessage> mission, bool isFinished)
        {
            SaveMissions();
        }
    }
}
