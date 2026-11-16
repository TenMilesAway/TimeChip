using System.Collections.Generic;

/// <summary>烹饪配方匹配（kitchen 表 recipeType=Cook）。</summary>
public static class KitchenCookRecipeCatalog
{
    private const string RecipeTypeCook = "Cook";

    /// <summary>灶台材料无法匹配任一烹饪配方时产出的道具 ID。</summary>
    public const int MishmashDishItemId = 7011;

    /// <summary>开始烹饪所需的最少灶台材料数量（含配方烹饪与乱炖）。</summary>
    public const int MinCookMaterialCount = 2;

    public static bool TryCollectPlacedCookMaterials(
        IReadOnlyList<int> hearthSlotItemIds,
        out List<int> placedItemIds)
    {
        placedItemIds = null;
        if (hearthSlotItemIds == null || hearthSlotItemIds.Count == 0)
        {
            return false;
        }

        placedItemIds = new List<int>(hearthSlotItemIds.Count);
        for (int i = 0; i < hearthSlotItemIds.Count; i++)
        {
            int itemId = hearthSlotItemIds[i];
            if (itemId > 0)
            {
                placedItemIds.Add(itemId);
            }
        }

        return placedItemIds.Count > 0;
    }

    public static bool HasMinimumCookMaterials(
        IReadOnlyList<int> hearthSlotItemIds,
        out List<int> placedItemIds)
    {
        if (!TryCollectPlacedCookMaterials(hearthSlotItemIds, out placedItemIds))
        {
            return false;
        }

        return placedItemIds.Count >= MinCookMaterialCount;
    }

    public static bool TryMatchCookRecipe(
        IReadOnlyList<int> hearthSlotItemIds,
        out cfg.Kitchen recipe,
        out List<int> orderedMaterialItemIds)
    {
        recipe = null;
        orderedMaterialItemIds = null;
        if (!TryCollectPlacedCookMaterials(hearthSlotItemIds, out List<int> placedItemIds))
        {
            return false;
        }

        IReadOnlyList<cfg.Kitchen> recipes = DataTableMananger.GetInstance().Tables.KitchenTable.DataList;
        if (recipes == null)
        {
            return false;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            cfg.Kitchen candidate = recipes[i];
            if (candidate == null || candidate.RecipeType != RecipeTypeCook)
            {
                continue;
            }

            if (!KitchenMaterialsParser.TryParse(
                    candidate.Materials,
                    out List<ExchangeCostEntry> required,
                    out _))
            {
                continue;
            }

            if (required.Count != placedItemIds.Count)
            {
                continue;
            }

            if (TryAssignCookMaterials(required, placedItemIds, out List<int> alignedItemIds))
            {
                recipe = candidate;
                orderedMaterialItemIds = alignedItemIds;
                return true;
            }
        }

        return false;
    }

    /// <summary>不按格子顺序，只要每种所需材料都能与已放置材料一一对应即可。</summary>
    private static bool TryAssignCookMaterials(
        IReadOnlyList<ExchangeCostEntry> required,
        IReadOnlyList<int> placedItemIds,
        out List<int> orderedMaterialItemIds)
    {
        orderedMaterialItemIds = new List<int>(required.Count);
        if (required.Count != placedItemIds.Count)
        {
            return false;
        }

        bool[] usedPlaced = new bool[placedItemIds.Count];
        for (int requiredIndex = 0; requiredIndex < required.Count; requiredIndex++)
        {
            int configuredItemId = required[requiredIndex].ItemId;
            int matchedPlacedIndex = -1;
            for (int placedIndex = 0; placedIndex < placedItemIds.Count; placedIndex++)
            {
                if (usedPlaced[placedIndex])
                {
                    continue;
                }

                if (KitchenIngredientRules.MatchesCookMaterialSlot(
                        configuredItemId,
                        placedItemIds[placedIndex]))
                {
                    matchedPlacedIndex = placedIndex;
                    break;
                }
            }

            if (matchedPlacedIndex < 0)
            {
                orderedMaterialItemIds = null;
                return false;
            }

            usedPlaced[matchedPlacedIndex] = true;
            orderedMaterialItemIds.Add(placedItemIds[matchedPlacedIndex]);
        }

        return true;
    }
}