using System;
using System.Collections.Generic;
using RedSaw.MissionSystem;
using UnityEngine;

public static class MissionRuntimeFactory
{
    private const string RequireTypeReachSimulationCoinBeforeMonth = "ReachSimulationCoinBeforeMonth";
    private const string RequireTypeConsumeTimeCoin = "ConsumeTimeCoin";

    public static bool TryCreatePrototype(
        cfg.Mission missionConfig,
        out MissionPrototype<MissionRuntimeMessage> prototype)
    {
        prototype = null;
        if (missionConfig == null)
        {
            return false;
        }

        if (missionConfig.Id <= 0)
        {
            Debug.LogError("任务配置 id 必须大于 0");
            return false;
        }

        MissionRequire<MissionRuntimeMessage> require = CreateRequire(missionConfig);
        if (require == null)
        {
            Debug.LogError($"未知任务需求类型: [{missionConfig.RequireType}], missionId=[{missionConfig.Id}]");
            return false;
        }

        MissionReward[] rewards = ParseRewards(missionConfig.Rewards);
        prototype = new MissionPrototype<MissionRuntimeMessage>(
            missionConfig.Id.ToString(),
            new[] { require },
            rewards,
            MissionRequireMode.All,
            new TableMissionProperty(missionConfig));
        return true;
    }

    private static MissionRequire<MissionRuntimeMessage> CreateRequire(cfg.Mission missionConfig)
    {
        switch (missionConfig.RequireType)
        {
            case RequireTypeReachSimulationCoinBeforeMonth:
                if (!IsValidDeadline(missionConfig.DeadlineYear, missionConfig.DeadlineMonth))
                {
                    Debug.LogError(
                        $"任务截止时间无效: missionId=[{missionConfig.Id}], year=[{missionConfig.DeadlineYear}], month=[{missionConfig.DeadlineMonth}]");
                    return null;
                }

                return new ReachSimulationCoinBeforeMonthRequire(
                    missionConfig.TargetAmount,
                    missionConfig.DeadlineYear,
                    missionConfig.DeadlineMonth);
            case RequireTypeConsumeTimeCoin:
                return new ConsumeTimeCoinRequire(missionConfig.TargetAmount);
            default:
                return null;
        }
    }

    private static MissionReward[] ParseRewards(string rewardConfig)
    {
        if (string.IsNullOrWhiteSpace(rewardConfig))
        {
            return null;
        }

        string[] rewardEntries = rewardConfig.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
        List<MissionReward> rewards = new List<MissionReward>();

        for (int i = 0; i < rewardEntries.Length; i++)
        {
            string[] values = rewardEntries[i].Split(',');
            if (values.Length < 2 ||
                !int.TryParse(values[0], out int itemId) ||
                !int.TryParse(values[1], out int itemCount) ||
                itemId <= 0 ||
                itemCount <= 0)
            {
                Debug.LogWarning($"任务奖励配置无效，已忽略: [{rewardEntries[i]}]");
                continue;
            }

            rewards.Add(new MissionItemReward(itemId, itemCount));
        }

        return rewards.Count == 0 ? null : rewards.ToArray();
    }

    public static bool IsMissionExpired(cfg.Mission missionConfig, int currentAge, int currentMonth)
    {
        if (missionConfig == null ||
            missionConfig.RequireType != RequireTypeReachSimulationCoinBeforeMonth ||
            !IsValidDeadline(missionConfig.DeadlineYear, missionConfig.DeadlineMonth))
        {
            return false;
        }

        return CompareYearMonth(currentAge, currentMonth, missionConfig.DeadlineYear, missionConfig.DeadlineMonth) > 0;
    }

    public static int CompareYearMonth(int lhsYear, int lhsMonth, int rhsYear, int rhsMonth)
    {
        int lhs = lhsYear * 12 + lhsMonth;
        int rhs = rhsYear * 12 + rhsMonth;
        if (lhs == rhs)
        {
            return 0;
        }

        return lhs > rhs ? 1 : -1;
    }

    public static bool IsValidDeadline(int year, int month)
    {
        return year > 0 && month >= 1 && month <= 12;
    }
}

public sealed class TableMissionProperty : MissionProperty
{
    public readonly cfg.Mission missionConfig;

    public TableMissionProperty(cfg.Mission missionConfig)
    {
        this.missionConfig = missionConfig;
    }
}

public sealed class ReachSimulationCoinBeforeMonthRequire : MissionRequire<MissionRuntimeMessage>
{
    public readonly int targetAmount;
    public readonly int deadlineYear;
    public readonly int deadlineMonth;

    public ReachSimulationCoinBeforeMonthRequire(int targetAmount, int deadlineYear, int deadlineMonth)
    {
        this.targetAmount = Mathf.Max(1, targetAmount);
        this.deadlineYear = deadlineYear;
        this.deadlineMonth = deadlineMonth;
    }

    public override bool CheckMessage(MissionRuntimeMessage message)
    {
        return message.messageType == MissionRuntimeMessageType.PlayerSnapshot;
    }

    public sealed class Handle : MissionRequireHandle<MissionRuntimeMessage>
    {
        private readonly ReachSimulationCoinBeforeMonthRequire _require;
        private bool _isFinished;
        private int _lastSimulationCoins;

        public Handle(ReachSimulationCoinBeforeMonthRequire require) : base(require)
        {
            _require = require;
        }

        protected override bool UseMessage(MissionRuntimeMessage message)
        {
            _lastSimulationCoins = message.simulationCoins;
            if (_isFinished)
            {
                return true;
            }

            if (MissionRuntimeFactory.CompareYearMonth(
                    message.currentAge,
                    message.currentMonth,
                    _require.deadlineYear,
                    _require.deadlineMonth) > 0)
            {
                return false;
            }

            if (message.simulationCoins < _require.targetAmount)
            {
                return false;
            }

            _isFinished = true;
            return true;
        }

        public override string ToString()
        {
            int progress = Mathf.Min(_lastSimulationCoins, _require.targetAmount);
            return $"{progress}/{_require.targetAmount}";
        }
    }
}

public sealed class ConsumeTimeCoinRequire : MissionRequire<MissionRuntimeMessage>
{
    public readonly int targetAmount;

    public ConsumeTimeCoinRequire(int targetAmount)
    {
        this.targetAmount = Mathf.Max(1, targetAmount);
    }

    public override bool CheckMessage(MissionRuntimeMessage message)
    {
        return message.messageType == MissionRuntimeMessageType.TimeCoinSpent;
    }

    public sealed class Handle : MissionRequireHandle<MissionRuntimeMessage>
    {
        private readonly ConsumeTimeCoinRequire _require;
        private int _consumedAmount;

        public Handle(ConsumeTimeCoinRequire require) : base(require)
        {
            _require = require;
        }

        protected override bool UseMessage(MissionRuntimeMessage message)
        {
            _consumedAmount += message.spentTimeCoins;
            return _consumedAmount >= _require.targetAmount;
        }

        public override string ToString()
        {
            int progress = Mathf.Min(_consumedAmount, _require.targetAmount);
            return $"{progress}/{_require.targetAmount}";
        }
    }
}

public sealed class MissionItemReward : MissionReward
{
    private const int SmallSimulationCoinItemId = 1000;
    private const int LargeSimulationCoinItemId = 1001;
    private const int SmallSimulationCoinValue = 100;
    private const int LargeSimulationCoinValue = 1000;

    private readonly int _itemId;
    private readonly int _itemCount;

    public MissionItemReward(int itemId, int itemCount)
    {
        _itemId = itemId;
        _itemCount = itemCount;
    }

    public override void ApplyReward()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        switch (_itemId)
        {
            case SmallSimulationCoinItemId:
                playerInfoManager.AddSimulationCoins(_itemCount * SmallSimulationCoinValue);
                return;
            case LargeSimulationCoinItemId:
                playerInfoManager.AddSimulationCoins(_itemCount * LargeSimulationCoinValue);
                return;
            default:
                playerInfoManager.AddItem(_itemId, _itemCount);
                return;
        }
    }
}
