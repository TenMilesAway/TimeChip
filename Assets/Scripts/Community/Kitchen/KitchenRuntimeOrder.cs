using System.Collections.Generic;

/// <summary>运行时生成的一条厨房订单实例。</summary>
public sealed class KitchenRuntimeOrder
{
    public int OrderTypeId { get; set; }
    public string AvatarIcon { get; set; }
    public List<int> RequiredDishItemIds { get; } = new List<int>(3);
    public int RewardSimulationCoins { get; set; }
    public int RewardGiftItemId { get; set; }
    public int RewardGiftCount { get; set; }
}