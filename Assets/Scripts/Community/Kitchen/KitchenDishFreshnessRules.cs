/// <summary>灶台成品新鲜度：超时未取则变为不新鲜菜品。</summary>
public static class KitchenDishFreshnessRules
{
    public const int StaleDishItemId = 7010;
    public const float SpoilDelaySeconds = 10f;

    private const int HearthDishIdMin = 7000;
    private const int HearthDishIdMax = 7009;

    public static bool CanSpoilOnHearth(int itemId)
    {
        return itemId >= HearthDishIdMin && itemId <= HearthDishIdMax;
    }
}