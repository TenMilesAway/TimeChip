using System.Collections.Generic;
using UnityEngine;

/// <summary>按 kitchenOrder 配置随机生成订单。</summary>
public static class KitchenOrderGenerator
{
    private const char PoolSeparator = ';';
    private const int MaxDishesPerOrder = 3;

    public static bool TryGenerate(string randomKeyPrefix, out KitchenRuntimeOrder order)
    {
        order = null;
        IReadOnlyList<cfg.KitchenOrder> types = DataTableMananger.GetInstance()
            .Tables
            .KitchenOrderTable
            .DataList;
        if (types == null || types.Count == 0)
        {
            Debug.LogError("厨房订单配置为空。");
            return false;
        }

        cfg.KitchenOrder pickedType = PickOrderType(types, randomKeyPrefix);
        if (pickedType == null)
        {
            return false;
        }

        order = new KitchenRuntimeOrder
        {
            OrderTypeId = pickedType.Id,
            AvatarIcon = PickAvatar(pickedType, randomKeyPrefix),
            RewardGiftItemId = pickedType.RewardGiftItemId,
            RewardSimulationCoins = TurnRandom.Range(
                $"{randomKeyPrefix}.Coin.{pickedType.Id}",
                pickedType.RewardCoinMin,
                pickedType.RewardCoinMax + 1),
            RewardGiftCount = TurnRandom.Range(
                $"{randomKeyPrefix}.Gift.{pickedType.Id}",
                pickedType.RewardGiftCountMin,
                pickedType.RewardGiftCountMax + 1),
        };

        if (!TryPickDishes(pickedType, randomKeyPrefix, order.RequiredDishItemIds))
        {
            order = null;
            return false;
        }

        return true;
    }

    private static cfg.KitchenOrder PickOrderType(
        IReadOnlyList<cfg.KitchenOrder> types,
        string randomKeyPrefix)
    {
        int totalWeight = 0;
        for (int i = 0; i < types.Count; i++)
        {
            totalWeight += Mathf.Max(0, types[i].SpawnWeight);
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("厨房订单权重总和无效。");
            return null;
        }

        int roll = TurnRandom.Range($"{randomKeyPrefix}.Type", 0, totalWeight);
        for (int i = 0; i < types.Count; i++)
        {
            int weight = Mathf.Max(0, types[i].SpawnWeight);
            if (roll < weight)
            {
                return types[i];
            }

            roll -= weight;
        }

        return types[types.Count - 1];
    }

    private static string PickAvatar(cfg.KitchenOrder orderType, string randomKeyPrefix)
    {
        List<string> icons = SplitPool(orderType.AvatarPool);
        if (icons.Count == 0)
        {
            Debug.LogError($"订单类别 [{orderType.Id}] 未配置头像池。");
            return string.Empty;
        }

        int index = TurnRandom.Range(
            $"{randomKeyPrefix}.Avatar.{orderType.Id}",
            0,
            icons.Count);
        return icons[index];
    }

    private static bool TryPickDishes(
        cfg.KitchenOrder orderType,
        string randomKeyPrefix,
        List<int> result)
    {
        result.Clear();
        List<int> pool = ParseIntPool(orderType.DishPool);
        if (pool.Count == 0)
        {
            Debug.LogError($"订单类别 [{orderType.Id}] 菜品池为空。");
            return false;
        }

        int dishCount = TurnRandom.Range(
            $"{randomKeyPrefix}.DishCount.{orderType.Id}",
            orderType.DishCountMin,
            orderType.DishCountMax + 1);
        dishCount = Mathf.Clamp(dishCount, 1, MaxDishesPerOrder);
        dishCount = Mathf.Min(dishCount, pool.Count);

        List<int> working = new List<int>(pool);
        for (int i = 0; i < dishCount; i++)
        {
            int pickIndex = TurnRandom.Range(
                $"{randomKeyPrefix}.Dish.{orderType.Id}.{i}",
                0,
                working.Count);
            result.Add(working[pickIndex]);
            working.RemoveAt(pickIndex);
        }

        return result.Count > 0;
    }

    private static List<string> SplitPool(string raw)
    {
        List<string> list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return list;
        }

        string[] parts = raw.Split(PoolSeparator);
        for (int i = 0; i < parts.Length; i++)
        {
            string value = parts[i].Trim();
            if (!string.IsNullOrEmpty(value))
            {
                list.Add(value);
            }
        }

        return list;
    }

    private static List<int> ParseIntPool(string raw)
    {
        List<int> list = new List<int>();
        List<string> parts = SplitPool(raw);
        for (int i = 0; i < parts.Count; i++)
        {
            if (int.TryParse(parts[i], out int itemId) && itemId > 0)
            {
                list.Add(itemId);
            }
        }

        return list;
    }
}