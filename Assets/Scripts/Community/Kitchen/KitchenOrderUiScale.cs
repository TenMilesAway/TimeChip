using UnityEngine;

/// <summary>厨房订单面板图标缩放（读取 scale 表）。</summary>
public static class KitchenOrderUiScale
{
    private const float ScaleDivisor = 10000f;

    public const int RewardCoinScaleId = 13;
    public const int RewardItemExtraScaleId = 14;
    public const int AvatarScaleId = 15;
    public const int DishScaleId = 16;

    private const int MissionRewardScaleId = 2;

    public static bool TryGetScaleMultiplier(int scaleId, out float multiplier)
    {
        multiplier = 1f;
        cfg.Scale scaleConfig = DataTableMananger.GetInstance().Tables.ScaleTable
            .GetOrDefault(scaleId);
        if (scaleConfig == null)
        {
            return false;
        }

        multiplier = scaleConfig.ScaleValue / ScaleDivisor;
        return true;
    }

    public static bool TryGetRewardItemIconScale(out float multiplier)
    {
        multiplier = 1f;
        if (!TryGetScaleMultiplier(MissionRewardScaleId, out float missionRewardScale) ||
            !TryGetScaleMultiplier(RewardItemExtraScaleId, out float kitchenItemScale))
        {
            return false;
        }

        multiplier = missionRewardScale * kitchenItemScale;
        return true;
    }

    public static void ApplyIconScale(RectTransform iconTransform, float multiplier)
    {
        if (iconTransform == null)
        {
            return;
        }

        iconTransform.localScale = Vector3.one * multiplier;
    }
}