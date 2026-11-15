using System.Collections.Generic;

/// <summary>
/// 烹饪：配方 materials 写普通档 ID，可投普通或同种优质（ID+1）。
/// 切菜：每种品质单独配方行，materials 与投料 ID 精确一致。
/// </summary>
public static class KitchenIngredientRules
{
    private const int KitchenMaterialIdMin = 7100;
    private const int KitchenMaterialIdMax = 7125;

    public static bool IsKitchenIngredientPairId(int itemId)
    {
        return itemId >= KitchenMaterialIdMin && itemId <= KitchenMaterialIdMax;
    }

    public static int GetNormalTierItemId(int itemId)
    {
        if (!IsKitchenIngredientPairId(itemId))
        {
            return itemId;
        }

        return itemId % 2 == 0 ? itemId : itemId - 1;
    }

    public static bool IsPremiumTierItemId(int itemId)
    {
        return IsKitchenIngredientPairId(itemId) && itemId % 2 == 1;
    }

    public static bool MatchesCookMaterialSlot(int configuredItemId, int selectedItemId)
    {
        if (configuredItemId <= 0 || selectedItemId <= 0)
        {
            return false;
        }

        if (selectedItemId == configuredItemId)
        {
            return true;
        }

        int normalId = GetNormalTierItemId(configuredItemId);
        return IsKitchenIngredientPairId(normalId) && selectedItemId == normalId + 1;
    }

    public static bool MatchesCutMaterialsExactly(
        IReadOnlyList<ExchangeCostEntry> required,
        IReadOnlyList<int> selectedItemIds)
    {
        if (required == null || selectedItemIds == null || required.Count != selectedItemIds.Count)
        {
            return false;
        }

        for (int i = 0; i < required.Count; i++)
        {
            if (required[i].ItemId != selectedItemIds[i])
            {
                return false;
            }
        }

        return true;
    }
}