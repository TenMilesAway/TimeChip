using System.Collections.Generic;
using System.Text;
using cfg;
using UnityEngine;

/// <summary>
/// 局外已解锁的 Grow2 天赋效果；进入存档后生效，不在 BUFF 栏展示。
/// </summary>
public sealed class Grow2TalentEffectSystem : Singleton<Grow2TalentEffectSystem>
{
    private const int FishRareRarityThreshold = 3;
    /// <summary>抽奖「高品阶」：物品品质（level）严格大于该阈值。</summary>
    private const int LotteryHighTierItemLevelThreshold = 3;

    private bool _isActive;
    private readonly List<ActiveGrow2TalentEntry> _activeTalents = new List<ActiveGrow2TalentEntry>();

    private float _memoryGainPct;
    private int _healthCostReductionFlat;
    private int _workIncomeFlat;
    private float _communityRandomBonus;
    private float _healthFreeChancePercent;
    private int _shopDiscountFlat;
    private float _fishRarePct;
    private int _offlineCapHoursBonus;
    private int _missionRewardFlat;
    private float _lotteryLuckPct;

    public bool IsActive => _isActive;

    /// <summary>根据全局存档重建本局天赋加成，并写入 Debug 日志。</summary>
    public void ApplyForCurrentRun()
    {
        ResetAggregates();
        _isActive = true;

        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        Tables tables = DataTableMananger.GetInstance().Tables;
        if (globalInfo == null || tables?.Grow2Table == null)
        {
            Debug.LogWarning("[Grow2 天赋] 数据未就绪，跳过本局天赋效果。");
            return;
        }

        HashSet<int> unlockedIds = globalInfo.GetUnlockedGrow2TalentIdSet();
        IReadOnlyList<Grow2> nodes = tables.Grow2Table.DataList;
        for (int i = 0; i < nodes.Count; i++)
        {
            Grow2 node = nodes[i];
            if (node == null || !unlockedIds.Contains(node.Id))
            {
                continue;
            }

            _activeTalents.Add(new ActiveGrow2TalentEntry(node.Id, node.Name, node.EffectDesc));
            AccumulateEffect(node.Effect, node.EffectValue);
        }

        LogActiveTalentsToDebug();
    }

    public void Clear()
    {
        _isActive = false;
        ResetAggregates();
    }

    public int ApplyMemoryGainBonus(int baseAmount)
    {
        if (!_isActive || baseAmount <= 0 || _memoryGainPct <= 0f)
        {
            return baseAmount;
        }

        int bonus = Mathf.RoundToInt(baseAmount * (_memoryGainPct / 100f));
        return baseAmount + Mathf.Max(0, bonus);
    }

    public int ApplyShopPriceDiscount(int priceAfterBuffs)
    {
        if (!_isActive || priceAfterBuffs <= 0 || _shopDiscountFlat <= 0)
        {
            return priceAfterBuffs;
        }

        return Mathf.Max(0, priceAfterBuffs - _shopDiscountFlat);
    }

    public void ApplyWorkCoinBonus(ref int coinReward)
    {
        if (!_isActive || _workIncomeFlat <= 0)
        {
            return;
        }

        coinReward = Mathf.Max(0, coinReward + _workIncomeFlat);
    }

    public static float GetConfiguredCommunitySpawnProbability(string probabilityId)
    {
        return GetInstance().ApplyCommunitySpawnProbability(GameProbability.Get(probabilityId));
    }

    public int GetHealthCostPreview(int healthCostAfterBuffs)
    {
        if (!_isActive || healthCostAfterBuffs <= 0)
        {
            return healthCostAfterBuffs;
        }

        return Mathf.Max(0, healthCostAfterBuffs - _healthCostReductionFlat);
    }

    public int ResolveHealthCost(int healthCostAfterBuffs, string randomKey)
    {
        int cost = GetHealthCostPreview(healthCostAfterBuffs);
        if (cost <= 0 || _healthFreeChancePercent <= 0f ||
            string.IsNullOrEmpty(randomKey))
        {
            return cost;
        }

        if (TurnRandom.Chance(randomKey, _healthFreeChancePercent / 100f))
        {
            return 0;
        }

        return cost;
    }

    public float ApplyCommunitySpawnProbability(float baseProbability)
    {
        if (!_isActive || _communityRandomBonus <= 0f)
        {
            return baseProbability;
        }

        return Mathf.Clamp01(baseProbability + _communityRandomBonus);
    }

    public long GetFishWeight(cfg.Fish fishConfig, long baseWeight)
    {
        if (!_isActive || baseWeight <= 0 || fishConfig == null ||
            _fishRarePct <= 0f || fishConfig.Rarity < FishRareRarityThreshold)
        {
            return baseWeight;
        }

        return Mathf.Max(
            1,
            Mathf.RoundToInt(baseWeight * (1f + _fishRarePct / 100f)));
    }

    public long GetLotteryRewardWeight(int itemId, int itemCount, long baseWeight)
    {
        if (!_isActive || baseWeight <= 0 || _lotteryLuckPct <= 0f ||
            !IsHighTierLotteryReward(itemId))
        {
            return baseWeight;
        }

        return Mathf.Max(
            1,
            Mathf.RoundToInt(baseWeight * (1f + _lotteryLuckPct / 100f)));
    }

    public void ApplyMissionRewardBonus(List<CommonRewardItemData> rewards)
    {
        if (!_isActive || rewards == null || rewards.Count == 0 || _missionRewardFlat <= 0)
        {
            return;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            CommonRewardItemData reward = rewards[i];
            if (reward == null || reward.itemCount <= 0)
            {
                continue;
            }

            reward.itemCount = Mathf.Max(1, reward.itemCount + _missionRewardFlat);
        }
    }

    public int OfflineCapHoursBonus => _isActive ? _offlineCapHoursBonus : 0;

    private void AccumulateEffect(string effectType, int effectValue)
    {
        if (string.IsNullOrWhiteSpace(effectType) || effectValue == 0)
        {
            return;
        }

        switch (effectType)
        {
            case "MemoryGainPct":
                _memoryGainPct += effectValue;
                break;
            case "Health":
                _healthCostReductionFlat += effectValue;
                break;
            case "WorkIncome":
                _workIncomeFlat += effectValue;
                break;
            case "CommunityRandomPct":
                _communityRandomBonus += effectValue / 100f;
                break;
            case "HealthFree":
                _healthFreeChancePercent += effectValue;
                break;
            case "ShopDiscount":
                _shopDiscountFlat += effectValue;
                break;
            case "FishRarePct":
                _fishRarePct += effectValue;
                break;
            case "OfflineCapHours":
                _offlineCapHoursBonus += effectValue;
                break;
            case "MissionReward":
                _missionRewardFlat += effectValue;
                break;
            case "LotteryLuck":
                _lotteryLuckPct += effectValue;
                break;
            default:
                Debug.LogWarning($"[Grow2 天赋] 未实现的效果类型：{effectType}");
                break;
        }
    }

    private bool IsHighTierLotteryReward(int itemId)
    {
        Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            return false;
        }

        Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        return itemConfig != null &&
               itemConfig.Level > LotteryHighTierItemLevelThreshold;
    }

    private void ResetAggregates()
    {
        _activeTalents.Clear();
        _memoryGainPct = 0f;
        _healthCostReductionFlat = 0;
        _workIncomeFlat = 0;
        _communityRandomBonus = 0f;
        _healthFreeChancePercent = 0f;
        _shopDiscountFlat = 0;
        _fishRarePct = 0f;
        _offlineCapHoursBonus = 0;
        _missionRewardFlat = 0;
        _lotteryLuckPct = 0f;
    }

    private void LogActiveTalentsToDebug()
    {
        if (_activeTalents.Count == 0)
        {
            Debug.Log("[Grow2 天赋] 本局无已解锁天赋效果。");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[Grow2 天赋] 本局生效（共 {_activeTalents.Count} 项，不在 BUFF 栏显示）：");
        for (int i = 0; i < _activeTalents.Count; i++)
        {
            ActiveGrow2TalentEntry entry = _activeTalents[i];
            builder.Append("- ").Append(entry.Name)
                .Append(" (").Append(entry.Id).Append("): ")
                .Append(entry.EffectDesc).AppendLine();
        }

        builder.Append("汇总 → 回忆+").Append(_memoryGainPct).Append("%，")
            .Append("零工收益+").Append(_workIncomeFlat).Append("，")
            .Append("健康消耗-").Append(_healthCostReductionFlat).Append("，")
            .Append("免健康").Append(_healthFreeChancePercent).Append("%，")
            .Append("商店价-").Append(_shopDiscountFlat).Append("，")
            .Append("社区随机+").Append(_communityRandomBonus * 100f).Append("%，")
            .Append("稀有鱼+").Append(_fishRarePct).Append("%，")
            .Append("任务奖励+").Append(_missionRewardFlat).Append("，")
            .Append("抽奖幸运+").Append(_lotteryLuckPct).Append("%");
        if (_offlineCapHoursBonus > 0)
        {
            builder.Append("，离线上限+").Append(_offlineCapHoursBonus).Append("h（待玩法接入）");
        }

        Debug.Log(builder.ToString());
    }

    private readonly struct ActiveGrow2TalentEntry
    {
        public ActiveGrow2TalentEntry(int id, string name, string effectDesc)
        {
            Id = id;
            Name = name;
            EffectDesc = effectDesc;
        }

        public int Id { get; }
        public string Name { get; }
        public string EffectDesc { get; }
    }
}
