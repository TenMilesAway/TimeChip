using System.Collections.Generic;
using UnityEngine;

public static class KitchenRecipeResolver
{
    private const string RecipeTypeCut = "Cut";
    private const string RecipeTypeCook = "Cook";

    public static int ResolveOutputItemId(
        cfg.Kitchen recipe,
        IReadOnlyList<int> selectedMaterialItemIds,
        string randomKey)
    {
        if (recipe == null || selectedMaterialItemIds == null)
        {
            return 0;
        }

        if (recipe.RecipeType == RecipeTypeCut)
        {
            return ResolveCutOutput(recipe, selectedMaterialItemIds);
        }

        if (recipe.RecipeType == RecipeTypeCook)
        {
            return ResolveCookOutput(recipe, selectedMaterialItemIds, randomKey);
        }

        Debug.LogError($"未知厨房配方类型: [{recipe.RecipeType}], recipe=[{recipe.Id}]");
        return recipe.OutputItemId;
    }

    private static int ResolveCutOutput(cfg.Kitchen recipe, IReadOnlyList<int> selectedMaterialItemIds)
    {
        if (!KitchenMaterialsParser.TryParse(recipe.Materials, out List<ExchangeCostEntry> required, out string error))
        {
            Debug.LogError($"切菜配方材料无效: [{recipe.Id}], {error}");
            return recipe.OutputItemId;
        }

        if (!KitchenIngredientRules.MatchesCutMaterialsExactly(required, selectedMaterialItemIds))
        {
            Debug.LogError($"切菜投料与配方不一致: recipe=[{recipe.Id}]");
            return recipe.OutputItemId;
        }

        return recipe.OutputItemId;
    }

    private static int ResolveCookOutput(
        cfg.Kitchen recipe,
        IReadOnlyList<int> selectedMaterialItemIds,
        string randomKey)
    {
        if (!KitchenMaterialsParser.TryParse(recipe.Materials, out List<ExchangeCostEntry> required, out string error))
        {
            Debug.LogError($"烹饪配方材料无效: [{recipe.Id}], {error}");
            return recipe.OutputItemId;
        }

        int premiumSlotCount = 0;
        int compareCount = Mathf.Min(required.Count, selectedMaterialItemIds.Count);
        for (int i = 0; i < compareCount; i++)
        {
            int selectedId = selectedMaterialItemIds[i];
            if (KitchenIngredientRules.IsPremiumTierItemId(selectedId) &&
                KitchenIngredientRules.MatchesCookMaterialSlot(required[i].ItemId, selectedId))
            {
                premiumSlotCount++;
            }
        }

        float perfectRate = recipe.BasePerfectRate +
            premiumSlotCount * recipe.PremiumPerfectBonus;
        perfectRate = Mathf.Clamp01(perfectRate);
        return TurnRandom.Chance(randomKey, perfectRate)
            ? recipe.PerfectOutputItemId
            : recipe.OutputItemId;
    }
}