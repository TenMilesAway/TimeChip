using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理 BUFF 的激活实例、回合结算与数值修正。BUFF 定义由 buffConfig 配置表提供。
/// </summary>
public sealed class BuffSystem : Singleton<BuffSystem>
{
    private const int HomeSatisfactionBuffSourceId = -1;

    private PlayerInfoManager _playerInfoManager;
    private bool _isSynchronizingHomeSatisfactionBuff;

    /// <summary>激活 BUFF 列表变化时触发，供表现层刷新图标。</summary>
    public event Action BuffsChanged;

    public void Initialize(PlayerInfoManager playerInfoManager)
    {
        if (playerInfoManager == null)
        {
            throw new ArgumentNullException(nameof(playerInfoManager));
        }

        if (_playerInfoManager != null)
        {
            _playerInfoManager.TurnEnding -= OnTurnEnding;
            _playerInfoManager.TurnAdvanced -= OnTurnAdvanced;
            _playerInfoManager.PlayerInfoChanged -= SynchronizeHomeSatisfactionBuff;
        }

        _playerInfoManager = playerInfoManager;
        _playerInfoManager.TurnEnding += OnTurnEnding;
        _playerInfoManager.TurnAdvanced += OnTurnAdvanced;
        _playerInfoManager.PlayerInfoChanged += SynchronizeHomeSatisfactionBuff;
        RemoveMissingConfigurations();
        SynchronizeHomeSatisfactionBuff(_playerInfoManager);
    }

    /// <summary>按配置添加 BUFF。即时 BUFF 会立刻结算而不会写入存档。</summary>
    public bool TryAddBuff(
        int buffId,
        int sourceId = 0,
        ISet<int> buffIdsToReplace = null)
    {
        cfg.BuffConfig config = GetConfig(buffId);
        if (config == null || !MeetsSatisfactionRequirement(config))
        {
            return false;
        }

        if (ParseDurationType(config.DurationType) == BuffDurationType.Instant)
        {
            ApplyTriggeredEffect(config, 1);
            CommonMessageView.Show($"BUFF 激活：{config.Name}");
            return true;
        }

        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        if (buffIdsToReplace != null)
        {
            activeBuffs.RemoveAll(activeBuff =>
                activeBuff.buffId != buffId &&
                buffIdsToReplace.Contains(activeBuff.buffId));
        }

        ActiveBuffData activeBuff = FindActiveBuff(activeBuffs, buffId);
        if (activeBuff == null)
        {
            activeBuffs.Add(new ActiveBuffData
            {
                buffId = buffId,
                remainingTurns = GetRemainingTurns(config),
                stacks = 1,
                sourceId = sourceId
            });
        }
        else
        {
            UpdateExistingBuff(activeBuff, config, sourceId);
        }

        _playerInfoManager.SetActiveBuffs(activeBuffs);
        BuffsChanged?.Invoke();
        CommonMessageView.Show($"BUFF 激活：{config.Name}");
        return true;
    }

    /// <summary>计算零工结算前应采用的金币与体力数值。</summary>
    public WorkBuffResult CalculateWorkResult(cfg.Work workConfig)
    {
        if (workConfig == null)
        {
            throw new ArgumentNullException(nameof(workConfig));
        }

        int coinReward = workConfig.CoinReward;
        int healthCost = workConfig.HealthCost;
        float coinMultiplier = 1f;
        float healthCostMultiplier = 1f;
        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig config = GetConfig(activeBuff.buffId);
            if (config == null || !MeetsSatisfactionRequirement(config))
            {
                continue;
            }

            ApplyWorkEffect(
                config.EffectType,
                config.EffectValue,
                activeBuff.stacks,
                ref coinReward,
                ref healthCost,
                ref coinMultiplier,
                ref healthCostMultiplier);
            ApplyWorkEffect(
                config.ExtraEffectType,
                config.ExtraEffectValue,
                activeBuff.stacks,
                ref coinReward,
                ref healthCost,
                ref coinMultiplier,
                ref healthCostMultiplier);
        }

        return new WorkBuffResult(
            Mathf.Max(0, Mathf.RoundToInt(coinReward * coinMultiplier)),
            Mathf.Max(0, Mathf.RoundToInt(healthCost * healthCostMultiplier)));
    }

    /// <summary>计算受当前永久 BUFF 影响后的商店价格。</summary>
    public int CalculateShopPrice(int basePrice)
    {
        return Mathf.Max(
            0,
            Mathf.CeilToInt(Mathf.Max(0, basePrice) * GetEffectMultiplier("ShopPriceMultiplier")));
    }

    /// <summary>返回指定倍率效果的最终乘数。</summary>
    public float GetEffectMultiplier(string effectType)
    {
        float multiplier = 1f;
        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig config = GetConfig(activeBuff.buffId);
            if (config == null || !MeetsSatisfactionRequirement(config))
            {
                continue;
            }

            multiplier *= GetMultiplierContribution(
                config.EffectType,
                config.EffectValue,
                activeBuff.stacks,
                effectType);
            multiplier *= GetMultiplierContribution(
                config.ExtraEffectType,
                config.ExtraEffectValue,
                activeBuff.stacks,
                effectType);
        }

        return Mathf.Max(0f, multiplier);
    }

    /// <summary>判断指定解锁效果是否处于生效状态。</summary>
    public bool HasActiveEffect(string effectType)
    {
        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig config = GetConfig(activeBuff.buffId);
            if (config == null || !MeetsSatisfactionRequirement(config))
            {
                continue;
            }

            if ((config.EffectType == effectType && config.EffectValue > 0f) ||
                (config.ExtraEffectType == effectType && config.ExtraEffectValue > 0f))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>按当前小屋满意度同步唯一生效的档位 BUFF。</summary>
    public void RefreshHomeSatisfactionBuff()
    {
        if (_playerInfoManager == null)
        {
            Debug.LogError("BUFF 系统尚未初始化，无法同步小屋满意度 BUFF。");
            return;
        }

        SynchronizeHomeSatisfactionBuff(_playerInfoManager);
    }

    private void OnTurnAdvanced()
    {
        AddAutomaticBuffs();
        ApplyEffectsForTrigger("TurnStart");
    }

    private void OnTurnEnding()
    {
        ApplyEffectsForTrigger("TurnEnd");

        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        List<string> expiredBuffMessages = new List<string>();
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            if (activeBuff.remainingTurns < 0)
            {
                continue;
            }

            activeBuff.remainingTurns--;
            if (activeBuff.remainingTurns <= 0)
            {
                cfg.BuffConfig config = GetConfig(activeBuff.buffId);
                if (config != null)
                {
                    expiredBuffMessages.Add($"BUFF 已失效：{config.Name}");
                }

                activeBuffs.RemoveAt(i);
            }
        }

        _playerInfoManager.SetActiveBuffs(activeBuffs);
        BuffsChanged?.Invoke();
        if (expiredBuffMessages.Count > 0)
        {
            CommonMessageView.Show(string.Join("\n", expiredBuffMessages));
        }
    }

    private void AddAutomaticBuffs()
    {
        IReadOnlyList<cfg.BuffConfig> configurations = DataTableMananger.GetInstance()
            .Tables.BuffConfigTable.DataList;
        for (int i = 0; i < configurations.Count; i++)
        {
            cfg.BuffConfig config = configurations[i];
            if (config.ActivationType == "RandomOnTurnStart" &&
                UnityEngine.Random.value < Mathf.Clamp01(config.ActivationChance))
            {
                TryAddBuff(config.Id);
            }
        }
    }

    private void ApplyEffectsForTrigger(string trigger)
    {
        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig config = GetConfig(activeBuff.buffId);
            if (config != null &&
                config.Trigger == trigger &&
                MeetsSatisfactionRequirement(config))
            {
                ApplyTriggeredEffect(config, activeBuff.stacks);
            }
        }
    }

    private void ApplyTriggeredEffect(cfg.BuffConfig config, int stacks)
    {
        int amount = Mathf.RoundToInt(config.EffectValue * stacks);
        switch (config.EffectType)
        {
            case "HealthChange":
                _playerInfoManager.ChangeHealth(amount);
                break;
            case "SimulationCoinChange":
                _playerInfoManager.AddSimulationCoins(amount);
                break;
        }
    }

    private void UpdateExistingBuff(ActiveBuffData activeBuff, cfg.BuffConfig config, int sourceId)
    {
        switch (ParseStackRule(config.StackRule))
        {
            case BuffStackRule.Ignore:
                return;
            case BuffStackRule.AddStack:
                activeBuff.stacks = Mathf.Clamp(activeBuff.stacks + 1, 1, Mathf.Max(1, config.MaxStacks));
                break;
            case BuffStackRule.Replace:
                activeBuff.stacks = 1;
                break;
        }

        activeBuff.remainingTurns = GetRemainingTurns(config);
        activeBuff.sourceId = sourceId;
    }

    private void RemoveMissingConfigurations()
    {
        List<ActiveBuffData> activeBuffs = _playerInfoManager.GetActiveBuffs();
        activeBuffs.RemoveAll(activeBuff => GetConfig(activeBuff.buffId) == null);
        _playerInfoManager.SetActiveBuffs(activeBuffs);
    }

    private void SynchronizeHomeSatisfactionBuff(PlayerInfoManager playerInfoManager)
    {
        if (_isSynchronizingHomeSatisfactionBuff || playerInfoManager == null)
        {
            return;
        }

        cfg.HomeSatisfactionBuff selectedTier = null;
        IReadOnlyList<cfg.HomeSatisfactionBuff> tiers = DataTableMananger.GetInstance()
            .Tables.HomeSatisfactionBuffTable.DataList;
        for (int i = 0; i < tiers.Count; i++)
        {
            cfg.HomeSatisfactionBuff tier = tiers[i];
            if (tier.MinSatisfaction <= playerInfoManager.Satisfaction &&
                (selectedTier == null || tier.MinSatisfaction > selectedTier.MinSatisfaction))
            {
                selectedTier = tier;
            }
        }

        List<ActiveBuffData> activeBuffs = playerInfoManager.GetActiveBuffs();
        ActiveBuffData currentTierBuff = activeBuffs.Find(
            activeBuff => activeBuff.sourceId == HomeSatisfactionBuffSourceId);
        if ((selectedTier == null && currentTierBuff == null) ||
            (selectedTier != null &&
             currentTierBuff != null &&
             currentTierBuff.buffId == selectedTier.BuffId))
        {
            return;
        }

        activeBuffs.RemoveAll(
            activeBuff => activeBuff.sourceId == HomeSatisfactionBuffSourceId);
        if (selectedTier != null)
        {
            if (GetConfig(selectedTier.BuffId) == null)
            {
                Debug.LogError($"小屋满意度 BUFF 配置不存在: [{selectedTier.BuffId}]");
                return;
            }

            activeBuffs.Add(new ActiveBuffData
            {
                buffId = selectedTier.BuffId,
                remainingTurns = -1,
                stacks = 1,
                sourceId = HomeSatisfactionBuffSourceId
            });
        }

        _isSynchronizingHomeSatisfactionBuff = true;
        playerInfoManager.SetActiveBuffs(activeBuffs);
        _isSynchronizingHomeSatisfactionBuff = false;
        BuffsChanged?.Invoke();
    }

    private static void ApplyWorkEffect(
        string effectType,
        float effectValue,
        int stacks,
        ref int coinReward,
        ref int healthCost,
        ref float coinMultiplier,
        ref float healthCostMultiplier)
    {
        switch (effectType)
        {
            case "WorkCoinFlat":
                coinReward += Mathf.RoundToInt(effectValue * stacks);
                break;
            case "WorkCoinMultiplier":
                coinMultiplier *= 1f + effectValue * stacks;
                break;
            case "WorkHealthCostFlat":
                healthCost += Mathf.RoundToInt(effectValue * stacks);
                break;
            case "WorkHealthCostMultiplier":
                healthCostMultiplier *= 1f + effectValue * stacks;
                break;
        }
    }

    private static float GetMultiplierContribution(
        string configuredEffectType,
        float effectValue,
        int stacks,
        string requestedEffectType)
    {
        return configuredEffectType == requestedEffectType
            ? 1f + effectValue * stacks
            : 1f;
    }

    private static ActiveBuffData FindActiveBuff(List<ActiveBuffData> activeBuffs, int buffId)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buffId == buffId)
            {
                return activeBuffs[i];
            }
        }

        return null;
    }

    private static BuffDurationType ParseDurationType(string durationType)
    {
        return Enum.TryParse(durationType, out BuffDurationType result)
            ? result
            : BuffDurationType.Instant;
    }

    private static BuffStackRule ParseStackRule(string stackRule)
    {
        return Enum.TryParse(stackRule, out BuffStackRule result)
            ? result
            : BuffStackRule.RefreshDuration;
    }

    private static int GetRemainingTurns(cfg.BuffConfig config)
    {
        return ParseDurationType(config.DurationType) == BuffDurationType.Permanent
            ? -1
            : Mathf.Max(1, config.DurationTurns);
    }

    private bool MeetsSatisfactionRequirement(cfg.BuffConfig config)
    {
        return _playerInfoManager.Satisfaction >= config.MinSatisfaction;
    }

    private static cfg.BuffConfig GetConfig(int buffId)
    {
        return DataTableMananger.GetInstance().Tables.BuffConfigTable.GetOrDefault(buffId);
    }
}
