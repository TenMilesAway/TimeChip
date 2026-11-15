using System.Collections.Generic;
using UnityEngine;

/// <summary>厨房冰箱：背包类型 3 食材、4 菜品。</summary>
public static class KitchenFridgeInventory
{
    public const int IngredientInventoryType = 3;
    public const int DishInventoryType = 4;
    public const int SlotsPerPage = 10;

    public static bool IsKitchenFridgeItem(cfg.Item itemConfig)
    {
        if (itemConfig == null)
        {
            return false;
        }

        return itemConfig.InventoryType == IngredientInventoryType ||
            itemConfig.InventoryType == DishInventoryType;
    }

    public static void CollectOwnedItems(List<KitchenFridgeEntry> result)
    {
        CollectOwnedItems(result, includeIngredients: true, includeDishes: true);
    }

    /// <summary>砧板储物：仅背包类型 3 食材，不含类型 4 菜品。</summary>
    public static void CollectOwnedCuttingBoardItems(List<KitchenFridgeEntry> result)
    {
        CollectOwnedItems(result, includeIngredients: true, includeDishes: false);
    }

    /// <summary>灶台面板下方储物：仅背包类型 3（厨房食材）。</summary>
    public static void CollectOwnedHearthPanelItems(List<KitchenFridgeEntry> result)
    {
        CollectOwnedItems(result, includeIngredients: true, includeDishes: false);
    }

    public static void CollectOwnedItems(
        List<KitchenFridgeEntry> result,
        bool includeIngredients,
        bool includeDishes)
    {
        result.Clear();
        if (!includeIngredients && !includeDishes)
        {
            return;
        }

        PlayerInfoData playerData = PlayerInfoManager.GetInstance().GetSnapshot();
        if (playerData.inventory == null)
        {
            return;
        }

        for (int i = 0; i < playerData.inventory.Count; i++)
        {
            PlayerInventoryItem playerItem = playerData.inventory[i];
            if (playerItem == null || playerItem.amount <= 0)
            {
                continue;
            }

            cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
                .GetOrDefault(playerItem.itemId);
            if (itemConfig == null)
            {
                continue;
            }

            if (itemConfig.InventoryType == IngredientInventoryType)
            {
                if (!includeIngredients)
                {
                    continue;
                }
            }
            else if (itemConfig.InventoryType == DishInventoryType)
            {
                if (!includeDishes)
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            result.Add(new KitchenFridgeEntry(itemConfig, playerItem.amount));
        }

        result.Sort((left, right) => left.Item.Id.CompareTo(right.Item.Id));
    }

    public static int GetMaxPage(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 1;
        }

        return Mathf.CeilToInt(itemCount / (float)SlotsPerPage);
    }
}

public readonly struct KitchenFridgeEntry
{
    public KitchenFridgeEntry(cfg.Item item, int amount)
    {
        Item = item;
        Amount = amount;
    }

    public cfg.Item Item { get; }
    public int Amount { get; }
}