using System.Collections.Generic;
using UnityEngine;

/// <summary>切菜配方查询（kitchen 表 recipeType=Cut）。</summary>
public static class KitchenCutRecipeCatalog
{
    private const string RecipeTypeCut = "Cut";

    public static bool TryGetRecipeForInput(int inputItemId, out cfg.Kitchen recipe)
    {
        recipe = null;
        if (inputItemId <= 0)
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
            if (candidate == null || candidate.RecipeType != RecipeTypeCut)
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

            if (required.Count == 1 && required[0].ItemId == inputItemId)
            {
                recipe = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool IsCutProductItem(int itemId)
    {
        if (itemId <= 0)
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
            cfg.Kitchen recipe = recipes[i];
            if (recipe == null || recipe.RecipeType != RecipeTypeCut)
            {
                continue;
            }

            if (recipe.OutputItemId == itemId || recipe.PerfectOutputItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsAlreadyCutMaterial(int itemId)
    {
        return IsCutProductItem(itemId) && !TryGetRecipeForInput(itemId, out _);
    }
}