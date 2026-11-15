using System;
using System.Collections.Generic;

/// <summary>当前回合一条厨房订单存档。</summary>
[Serializable]
public sealed class PlayerKitchenOrder
{
    public int orderTypeId;
    public string avatarIcon;
    public List<int> requiredDishItemIds = new List<int>();
    public int deliveredDishCount;
    public bool isCompleted;
    public int rewardSimulationCoins;
    public int rewardGiftItemId;
    public int rewardGiftCount;

    public int RequiredDishCount => requiredDishItemIds?.Count ?? 0;

    public bool TryGetCurrentRequiredDish(out int dishItemId)
    {
        dishItemId = 0;
        if (isCompleted || requiredDishItemIds == null ||
            deliveredDishCount < 0 || deliveredDishCount >= requiredDishItemIds.Count)
        {
            return false;
        }

        dishItemId = requiredDishItemIds[deliveredDishCount];
        return dishItemId > 0;
    }

    public bool IsOnLastRequiredDish()
    {
        return !isCompleted && RequiredDishCount > 0 &&
            deliveredDishCount == RequiredDishCount - 1;
    }
}