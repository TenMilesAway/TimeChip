using System;
using System.Collections.Generic;

/// <summary>
/// 普通奖池抽奖测试：单抽必中大奖；多抽时仅第一格为大奖，其余走常规权重抽取。
/// 通过 GM 面板开关，勿在正式包体中启用。
/// </summary>
public static class LotteryGrandPrizeTestDraw
{
    public static bool IsEnabled { get; private set; }

    public static void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public delegate bool TryDrawRewardDelegate(string drawKey, out CommonRewardItemData reward);

    public static bool TryBuildNormalPoolRewards(
        int drawCount,
        string drawKey,
        int grandItemId,
        int grandItemCount,
        ref int grandPrizeProgress,
        TryDrawRewardDelegate tryDrawReward,
        out List<CommonRewardItemData> rewards)
    {
        rewards = null;
        if (drawCount <= 0 ||
            grandItemId <= 0 ||
            grandItemCount <= 0 ||
            tryDrawReward == null)
        {
            return false;
        }

        rewards = new List<CommonRewardItemData>(drawCount);
        for (int i = 0; i < drawCount; i++)
        {
            grandPrizeProgress++;
            if (i == 0)
            {
                rewards.Add(new CommonRewardItemData
                {
                    itemId = grandItemId,
                    itemCount = grandItemCount
                });
                grandPrizeProgress = 0;
                continue;
            }

            if (!tryDrawReward($"{drawKey}.{i}", out CommonRewardItemData reward))
            {
                rewards = null;
                return false;
            }

            rewards.Add(reward);
        }

        return true;
    }
}
