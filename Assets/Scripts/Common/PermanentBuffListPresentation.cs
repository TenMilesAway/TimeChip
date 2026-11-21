using System;
using System.Collections.Generic;

/// <summary>永久 BUFF 列表：按 effectType 叠加数值并生成展示文案。</summary>
public static class PermanentBuffListPresentation
{
    private static readonly Dictionary<string, string> EffectTypeLinePrefixes = new Dictionary<string, string>
    {
        { "WorkCoinMultiplier", "零工模拟币 " },
        { "CommunityExperienceMultiplier", "社区经验 " },
        { "ShopPriceMultiplier", "商品价格 " },
        { "WorkHealthCostMultiplier", "体力消耗 " },
        { "TreatmentAndDrugEffectMultiplier", "治疗及药物恢复量 " },
        { "TreatmentCostMultiplier", "治疗费用 " },
        { "PositiveEncounterRewardMultiplier", "机遇奖励 " },
        { "FishCatchProgressMultiplier", "钓鱼进度 " },
        { "PositiveEncounterWeightMultiplier", "正面机遇权重 " },
        { "RareLotteryRewardWeightMultiplier", "稀有奖励权重 " },
    };

    private static readonly Dictionary<string, string> UnlockEffectLines = new Dictionary<string, string>
    {
        { "UnlockFishing", "解锁钓鱼" },
        { "UnlockPort", "解锁沿江港口" },
        { "UnlockMysteryWheelLottery", "解锁神秘转盘" },
        { "UnlockBoxLottery", "解锁宝箱抽奖" },
        { "UnlockCardLottery", "解锁卡牌抽奖" },
    };

    private static readonly string[] EffectTypeDisplayOrder =
    {
        "CommunityExperienceMultiplier",
        "ShopPriceMultiplier",
        "WorkCoinMultiplier",
        "WorkHealthCostMultiplier",
        "TreatmentAndDrugEffectMultiplier",
        "TreatmentCostMultiplier",
        "PositiveEncounterWeightMultiplier",
        "PositiveEncounterRewardMultiplier",
        "RareLotteryRewardWeightMultiplier",
        "FishCatchProgressMultiplier",
        "UnlockFishing",
        "UnlockPort",
        "UnlockMysteryWheelLottery",
        "UnlockBoxLottery",
        "UnlockCardLottery",
    };

    public static List<string> BuildStackedDisplayLines(IReadOnlyList<ActiveBuffData> permanentBuffs)
    {
        List<string> lines = new List<string>();
        if (permanentBuffs == null || permanentBuffs.Count == 0)
        {
            return lines;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        float satisfaction = playerInfoManager.Satisfaction;
        Dictionary<string, float> stackedValues = new Dictionary<string, float>();
        List<string> plainDescLines = new List<string>();

        for (int i = 0; i < permanentBuffs.Count; i++)
        {
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(permanentBuffs[i].buffId);
            if (buffConfig == null || satisfaction < buffConfig.MinSatisfaction)
            {
                continue;
            }

            bool hasEffectType = !string.IsNullOrEmpty(buffConfig.EffectType);
            bool hasExtraEffectType = !string.IsNullOrEmpty(buffConfig.ExtraEffectType);
            if (!hasEffectType && !hasExtraEffectType)
            {
                string plainDesc = GetNonEmptyDesc(buffConfig);
                if (!string.IsNullOrEmpty(plainDesc))
                {
                    plainDescLines.Add(plainDesc);
                }

                continue;
            }

            if (hasEffectType)
            {
                AddStackedValue(stackedValues, buffConfig.EffectType, buffConfig.EffectValue);
            }

            if (hasExtraEffectType)
            {
                AddStackedValue(stackedValues, buffConfig.ExtraEffectType, buffConfig.ExtraEffectValue);
            }
        }

        AppendOrderedEffectLines(lines, stackedValues);
        for (int i = 0; i < plainDescLines.Count; i++)
        {
            lines.Add(plainDescLines[i]);
        }

        return lines;
    }

    private static void AppendOrderedEffectLines(
        List<string> lines,
        Dictionary<string, float> stackedValues)
    {
        HashSet<string> added = new HashSet<string>();
        for (int i = 0; i < EffectTypeDisplayOrder.Length; i++)
        {
            string effectType = EffectTypeDisplayOrder[i];
            if (!stackedValues.TryGetValue(effectType, out float stackedValue))
            {
                continue;
            }

            if (!IsApproximatelyZero(stackedValue) || effectType.StartsWith("Unlock", StringComparison.Ordinal))
            {
                lines.Add(FormatStackedEffectLine(effectType, stackedValue));
            }

            added.Add(effectType);
        }

        List<string> remainingKeys = new List<string>(stackedValues.Keys);
        remainingKeys.Sort(StringComparer.Ordinal);
        for (int i = 0; i < remainingKeys.Count; i++)
        {
            string effectType = remainingKeys[i];
            if (added.Contains(effectType))
            {
                continue;
            }

            float stackedValue = stackedValues[effectType];
            if (IsApproximatelyZero(stackedValue))
            {
                continue;
            }

            lines.Add(FormatStackedEffectLine(effectType, stackedValue));
        }
    }

    private static void AddStackedValue(Dictionary<string, float> stackedValues, string effectType, float value)
    {
        if (string.IsNullOrEmpty(effectType))
        {
            return;
        }

        if (stackedValues.TryGetValue(effectType, out float current))
        {
            stackedValues[effectType] = current + value;
        }
        else
        {
            stackedValues[effectType] = value;
        }
    }

    private static string FormatStackedEffectLine(string effectType, float stackedValue)
    {
        if (effectType.StartsWith("Unlock", StringComparison.Ordinal))
        {
            if (UnlockEffectLines.TryGetValue(effectType, out string unlockLine))
            {
                return unlockLine;
            }

            return effectType;
        }

        if (UsesTimesMultiplierDisplay(effectType))
        {
            string prefix = EffectTypeLinePrefixes.TryGetValue(effectType, out string timesPrefix)
                ? timesPrefix
                : string.Empty;
            return $"{prefix}×{(1f + stackedValue):0.00}";
        }

        if (EffectTypeLinePrefixes.TryGetValue(effectType, out string linePrefix))
        {
            return linePrefix + FormatSignedPercent(stackedValue);
        }

        return $"{effectType} {FormatSignedPercent(stackedValue)}";
    }

    private static bool UsesTimesMultiplierDisplay(string effectType)
    {
        return effectType.EndsWith("WeightMultiplier", StringComparison.Ordinal);
    }

    private static string FormatSignedPercent(float effectValue)
    {
        float percentValue = effectValue * 100f;
        if (Math.Abs(percentValue) < 0.05f)
        {
            return "0%";
        }

        string sign = percentValue > 0f ? "+" : string.Empty;
        if (Math.Abs(percentValue - MathF.Round(percentValue)) < 0.05f)
        {
            return $"{sign}{percentValue:0}%";
        }

        return $"{sign}{percentValue:0.#}%";
    }

    private static string GetNonEmptyDesc(cfg.BuffConfig buffConfig)
    {
        if (!string.IsNullOrEmpty(buffConfig.Desc))
        {
            return buffConfig.Desc;
        }

        return buffConfig.Name ?? string.Empty;
    }

    private static bool IsApproximatelyZero(float value)
    {
        return Math.Abs(value) < 0.0001f;
    }
}