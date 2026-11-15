using System.Collections.Generic;

/// <summary>烹饪配方匹配（kitchen 表 recipeType=Cook）。</summary>
public static class KitchenCookRecipeCatalog
{
    private const string RecipeTypeCook = "Cook";

    public static bool TryMatchCookRecipe(
        IReadOnlyList<int> hearthSlotItemIds,
        out cfg.Kitchen recipe,
        out List<int> orderedMaterialItemIds)
    {
        recipe = null;
        orderedMaterialItemIds = null;
        if (hearthSlotItemIds == null || hearthSlotItemIds.Count == 0)
        {
            return false;
        }

        List<int> placedItemIds = new List<int>(hearthSlotItemIds.Count);
        for (int i = 0; i < hearthSlotItemIds.Count; i++)
        {
            int itemId = hearthSlotItemIds[i];
            if (itemId > 0)
            {
                placedItemIds.Add(itemId);
            }
        }

        if (placedItemIds.Count == 0)
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