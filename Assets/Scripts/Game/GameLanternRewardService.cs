using System.Collections.Generic;
using UnityEngine;

/// <summary>八月灯谜：答对即时模拟币与答毕结算奖池。</summary>
public static class GameLanternRewardService
{
    public const int LanternScoreLotteryPoolIdBase = 17;

    private static readonly int[] CorrectAnswerSimulationCoinAmounts = { 8, 18, 68, 88 };

    public static int GetLotteryPoolIdForCorrectCount(int correctCount)
    {
        int clamped = Mathf.Clamp(correctCount, 0, PlayerInfoManager.AugustLanternRiddleSlotCount);
        return LanternScoreLotteryPoolIdBase + clamped;
    }

    public static bool TryGrantCorrectAnswerSimulationCoins(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= PlayerInfoManager.AugustLanternRiddleSlotCount)
        {
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int choiceIndex = TurnRandom.Range(
            $"AugustLantern.CorrectCoin.{playerInfoManager.CurrentAge}." +
            $"{playerInfoManager.CurrentMonth}.{playerInfoManager.TurnRandomSeed}.{slotIndex}",
            0,
            CorrectAnswerSimulationCoinAmounts.Length);
        int grantedAmount = CorrectAnswerSimulationCoinAmounts[choiceIndex];
        LotteryView.GrantAndPresentReward(new CommonRewardItemData
        {
            itemId = BasePropertyId.SimulationCoin,
            itemCount = grantedAmount
        });
        return true;
    }

    public static bool TryGrantCompletionLotteryReward()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.AreAllAugustLanternRiddlesAnswered() ||
            !playerInfoManager.TryMarkAugustLanternCompletionRewardGranted())
        {
            return false;
        }

        int correctCount = playerInfoManager.CountAugustLanternCorrectAnswers();
        int lotteryPoolId = GetLotteryPoolIdForCorrectCount(correctCount);
        if (!LotteryView.TryDrawReward(
                lotteryPoolId,
                $"AugustLantern.Completion.{playerInfoManager.CurrentAge}." +
                $"{playerInfoManager.CurrentMonth}.{playerInfoManager.TurnRandomSeed}.{correctCount}",
                out CommonRewardItemData reward) ||
            reward == null)
        {
            Debug.LogError($"八月灯谜结算奖池无效: [{lotteryPoolId}], 答对 [{correctCount}] 题。");
            return false;
        }

        LotteryView.GrantAndPresentReward(reward);
        return true;
    }
}
