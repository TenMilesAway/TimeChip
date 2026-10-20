using System.Collections.Generic;

public static class ExchangeService
{
    public static bool CanAfford(string costItems)
    {
        if (!ExchangeConfigParser.TryParseCostItems(costItems, out List<ExchangeCostEntry> costs, out _))
        {
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < costs.Count; i++)
        {
            ExchangeCostEntry cost = costs[i];
            if (playerInfoManager.GetItemCount(cost.ItemId) < cost.Count)
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryExchange(string costItems, string rewardItems, out string failMessage)
    {
        failMessage = string.Empty;
        if (!ExchangeConfigParser.TryParseCostItems(costItems, out List<ExchangeCostEntry> costs, out string costError))
        {
            failMessage = costError;
            return false;
        }

        if (!ExchangeConfigParser.TryParseRewardItem(rewardItems, out int rewardItemId, out int rewardCount, out string rewardError))
        {
            failMessage = rewardError;
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < costs.Count; i++)
        {
            ExchangeCostEntry cost = costs[i];
            if (playerInfoManager.GetItemCount(cost.ItemId) < cost.Count)
            {
                failMessage = "材料不足";
                return false;
            }
        }

        for (int i = 0; i < costs.Count; i++)
        {
            ExchangeCostEntry cost = costs[i];
            if (!playerInfoManager.TryConsumeItem(cost.ItemId, cost.Count))
            {
                failMessage = "材料不足";
                return false;
            }
        }

        playerInfoManager.AddItem(rewardItemId, rewardCount);
        return true;
    }

    public static string GetItemDisplayName(int itemId)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig != null)
        {
            return itemConfig.Name;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        return baseConfig == null ? itemId.ToString() : baseConfig.Name;
    }

    public static bool TryGetItemPresentation(int itemId, out string iconPath, out int iconScale)
    {
        iconPath = null;
        iconScale = 0;
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig != null)
        {
            iconPath = itemConfig.Icon;
            iconScale = itemConfig.RewardScale;
            return true;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        if (baseConfig != null)
        {
            iconPath = baseConfig.Icon;
            iconScale = baseConfig.RewardScale;
            return true;
        }

        return false;
    }
}
