using System.Collections.Generic;

public readonly struct ExchangeCostEntry
{
    public int ItemId { get; }
    public int Count { get; }

    public ExchangeCostEntry(int itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }
}

public static class ExchangeConfigParser
{
    public static bool TryParseCostItems(
        string costItems,
        out List<ExchangeCostEntry> costs,
        out string errorMessage)
    {
        costs = new List<ExchangeCostEntry>();
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(costItems))
        {
            errorMessage = "兑换消耗不能为空。";
            return false;
        }

        string[] entries = costItems.Split(';');
        if (entries.Length < 1 || entries.Length > 3)
        {
            errorMessage = "兑换消耗条目数量无效，请配置 1 至 3 种物品。";
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (!TryParseItemAmount(entries[i], out int itemId, out int count))
            {
                errorMessage = $"兑换消耗格式无效: {entries[i]}";
                costs.Clear();
                return false;
            }

            costs.Add(new ExchangeCostEntry(itemId, count));
        }

        return true;
    }

    public static bool TryParseRewardItem(
        string rewardItems,
        out int itemId,
        out int count,
        out string errorMessage)
    {
        itemId = 0;
        count = 0;
        errorMessage = string.Empty;

        if (!TryParseItemAmount(rewardItems, out itemId, out count))
        {
            errorMessage = $"兑换奖励格式无效: {rewardItems}";
            return false;
        }

        return true;
    }

    private static bool TryParseItemAmount(string entry, out int itemId, out int count)
    {
        itemId = 0;
        count = 0;
        if (string.IsNullOrWhiteSpace(entry))
        {
            return false;
        }

        string[] values = entry.Split(',');
        return values.Length == 2 &&
            int.TryParse(values[0].Trim(), out itemId) &&
            int.TryParse(values[1].Trim(), out count) &&
            itemId > 0 &&
            count > 0;
    }
}
