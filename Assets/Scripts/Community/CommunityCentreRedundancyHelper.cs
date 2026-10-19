using System.Collections.Generic;

/// <summary>社区中心冗余道具筛选规则。</summary>
public static class CommunityCentreRedundancyHelper
{
    private const int RedundancyItemCategory = 4;
    private const int CommunitySupplyGiftBoxItemId = 3010;
    private const int MidCommunitySupplyGiftBoxItemId = 3011;
    private const int HighCommunitySupplyGiftBoxItemId = 3012;
    private const string WorkItemType = "零工";
    private const string OpportunityWorkType = "机遇";

    private static HashSet<int> _opportunityItemIds;

    /// <summary>道具是否可作为社区冗余道具提交。</summary>
    public static bool CanSubmitAsRedundancy(cfg.Item itemConfig)
    {
        if (itemConfig == null ||
            itemConfig.Category != RedundancyItemCategory ||
            itemConfig.Id == CommunitySupplyGiftBoxItemId ||
            IsOpportunityWorkItem(itemConfig.Id))
        {
            return false;
        }

        return true;
    }

    /// <summary>根据提交的冗余零工道具阶数决定回馈礼盒。</summary>
    public static int GetSubmitRewardItemId(cfg.Item itemConfig)
    {
        if (itemConfig == null || itemConfig.Type != WorkItemType)
        {
            return CommunitySupplyGiftBoxItemId;
        }

        if (itemConfig.Level >= 5)
        {
            return HighCommunitySupplyGiftBoxItemId;
        }

        if (itemConfig.Level >= 3)
        {
            return MidCommunitySupplyGiftBoxItemId;
        }

        return CommunitySupplyGiftBoxItemId;
    }

    private static bool IsOpportunityWorkItem(int itemId)
    {
        EnsureOpportunityItemIds();
        return _opportunityItemIds.Contains(itemId);
    }

    private static void EnsureOpportunityItemIds()
    {
        if (_opportunityItemIds != null)
        {
            return;
        }

        _opportunityItemIds = new HashSet<int>();
        IReadOnlyList<cfg.Work> works = DataTableMananger.GetInstance().Tables.WorkTable.DataList;
        for (int i = 0; i < works.Count; i++)
        {
            cfg.Work work = works[i];
            if (work.WorkType == OpportunityWorkType && work.IsUseItem > 0)
            {
                _opportunityItemIds.Add(work.IsUseItem);
            }
        }
    }
}
