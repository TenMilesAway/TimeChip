using System.Collections.Generic;

/// <summary>厨房订单每回合刷新。</summary>
public static class KitchenOrderTurnService
{
    public const int OrdersPerTurn = 4;

    public static void EnsureCurrentTurnOrders(PlayerInfoManager playerInfoManager)
    {
        if (playerInfoManager == null)
        {
            return;
        }

        if (playerInfoManager.HasCurrentTurnKitchenOrders())
        {
            return;
        }

        List<PlayerKitchenOrder> orders = new List<PlayerKitchenOrder>(OrdersPerTurn);
        for (int i = 0; i < OrdersPerTurn; i++)
        {
            if (!KitchenOrderGenerator.TryGenerate(
                    $"Kitchen.Order.{playerInfoManager.CurrentAge}." +
                    $"{playerInfoManager.CurrentMonth}.{i}",
                    out KitchenRuntimeOrder runtimeOrder))
            {
                continue;
            }

            orders.Add(ToPlayerOrder(runtimeOrder));
        }

        if (orders.Count != OrdersPerTurn)
        {
            UnityEngine.Debug.LogError(
                $"厨房订单生成数量不足: 需要 {OrdersPerTurn}，实际 {orders.Count}");
            return;
        }

        playerInfoManager.SetCurrentTurnKitchenOrders(orders);
    }

    public static PlayerKitchenOrder ToPlayerOrder(KitchenRuntimeOrder runtimeOrder)
    {
        PlayerKitchenOrder order = new PlayerKitchenOrder
        {
            orderTypeId = runtimeOrder.OrderTypeId,
            avatarIcon = runtimeOrder.AvatarIcon,
            deliveredDishCount = 0,
            isCompleted = false,
            rewardSimulationCoins = runtimeOrder.RewardSimulationCoins,
            rewardGiftItemId = runtimeOrder.RewardGiftItemId,
            rewardGiftCount = runtimeOrder.RewardGiftCount,
        };

        for (int i = 0; i < runtimeOrder.RequiredDishItemIds.Count; i++)
        {
            order.requiredDishItemIds.Add(runtimeOrder.RequiredDishItemIds[i]);
        }

        return order;
    }
}