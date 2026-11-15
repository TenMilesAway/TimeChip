/// <summary>订单菜品与背包菜品匹配（普通菜可接受对应完美菜）。</summary>
public static class KitchenDishMatchRules
{
    private const int DishIdMin = 7000;
    private const int DishIdMax = 7009;

    public static bool MatchesRequiredDish(int requiredDishItemId, int ownedItemId)
    {
        if (requiredDishItemId <= 0 || ownedItemId <= 0)
        {
            return false;
        }

        if (ownedItemId == requiredDishItemId)
        {
            return true;
        }

        if (requiredDishItemId >= DishIdMin && requiredDishItemId <= DishIdMax &&
            requiredDishItemId % 2 == 0)
        {
            return ownedItemId == requiredDishItemId + 1;
        }

        return false;
    }

    public static int GetPreferredConsumeItemId(
        int requiredDishItemId,
        PlayerInfoManager playerInfoManager)
    {
        if (playerInfoManager == null)
        {
            return 0;
        }

        if (playerInfoManager.GetItemCount(requiredDishItemId) > 0)
        {
            return requiredDishItemId;
        }

        if (requiredDishItemId >= DishIdMin && requiredDishItemId <= DishIdMax &&
            requiredDishItemId % 2 == 0 &&
            playerInfoManager.GetItemCount(requiredDishItemId + 1) > 0)
        {
            return requiredDishItemId + 1;
        }

        return 0;
    }
}