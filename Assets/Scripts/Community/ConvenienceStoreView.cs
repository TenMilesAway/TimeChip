using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConvenienceStoreView : UIBasePanel
{
    private const int ItemsPerMonth = 6;
    private const int ConvenienceScaleId = 4;
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Text _txtCoin;                 // 模拟币数量
    [SerializeField] private ConvenienceStoreItem[] _items; // 便利店商品

    private readonly List<cfg.Convenience> _monthlyOffers = new List<cfg.Convenience>(ItemsPerMonth);
    private bool _isUiReady;

    private void Awake()
    {
        _isUiReady = _txtCoin != null && _items != null && _items.Length == ItemsPerMonth;
        if (!_isUiReady)
        {
            Debug.LogError("ConvenienceStoreView 的 UI 引用未在 Inspector 中完整配置。", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                Debug.LogError($"ConvenienceStoreView 的第 {i + 1} 个商品栏未在 Inspector 中配置。", this);
                _isUiReady = false;
                enabled = false;
                return;
            }
        }
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (!_isUiReady)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged += RefreshSimulationCoins;
        playerInfoManager.TurnAdvanced += RefreshMonthlyOffers;
        RefreshMonthlyOffers();
        RefreshSimulationCoins(playerInfoManager);
    }

    protected override void HideHandle()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshSimulationCoins;
        playerInfoManager.TurnAdvanced -= RefreshMonthlyOffers;
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshSimulationCoins;
        playerInfoManager.TurnAdvanced -= RefreshMonthlyOffers;
        base.OnDestroy();
    }

    private void RefreshMonthlyOffers()
    {
        if (!TryLoadMonthlyOffers())
        {
            return;
        }

        cfg.Scale scaleConfig = DataTableMananger.GetInstance().Tables.ScaleTable
            .GetOrDefault(ConvenienceScaleId);
        if (scaleConfig == null)
        {
            Debug.LogError($"便利店缩放配置不存在: [{ConvenienceScaleId}]", this);
            return;
        }

        float scaleMultiplier = scaleConfig.ScaleValue / ScaleDivisor;
        for (int i = 0; i < _items.Length; i++)
        {
            cfg.Convenience offer = _monthlyOffers[i];
            if (!TryGetProductPresentation(offer.ItemId, out string iconPath, out int iconScale))
            {
                Debug.LogError($"便利店商品配置不存在: [{offer.Id}], [{offer.ItemId}]", this);
                continue;
            }

            _items[i].SetData(
                offer,
                iconPath,
                iconScale,
                scaleMultiplier,
                PlayerInfoManager.GetInstance().GetConvenienceOfferRemainingCount(offer.Id),
                TryPurchase);
        }
    }

    private bool TryLoadMonthlyOffers()
    {
        IReadOnlyList<cfg.Convenience> configurations = DataTableMananger.GetInstance()
            .Tables.ConvenienceTable.DataList;
        List<cfg.Convenience> candidates = new List<cfg.Convenience>();
        for (int i = 0; i < configurations.Count; i++)
        {
            cfg.Convenience config = configurations[i];
            if (config.Weight <= 0 ||
                config.Num <= 0 ||
                !TryGetProductPresentation(config.ItemId, out _, out _))
            {
                continue;
            }

            candidates.Add(config);
        }

        if (candidates.Count < ItemsPerMonth)
        {
            Debug.LogError($"便利店可用商品少于 {ItemsPerMonth} 个，无法生成本月商品。", this);
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        bool shouldRefresh = !playerInfoManager.HasMonthlyConvenienceOffers(ItemsPerMonth);
        List<cfg.Convenience> savedOffers = new List<cfg.Convenience>(ItemsPerMonth);
        if (!shouldRefresh)
        {
            for (int i = 0; i < ItemsPerMonth; i++)
            {
                int offerId = playerInfoManager.GetMonthlyConvenienceOfferIdAt(i);
                cfg.Convenience savedOffer = null;
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (candidates[j].Id == offerId)
                    {
                        savedOffer = candidates[j];
                        break;
                    }
                }

                if (savedOffer != null)
                {
                    savedOffers.Add(savedOffer);
                }
            }

            shouldRefresh = savedOffers.Count != ItemsPerMonth;
        }

        if (shouldRefresh)
        {
            savedOffers = SelectOffers(candidates);
            playerInfoManager.SetMonthlyConvenienceOffers(savedOffers);
        }

        _monthlyOffers.Clear();
        _monthlyOffers.AddRange(savedOffers);

        return _monthlyOffers.Count == ItemsPerMonth;
    }

    private static List<cfg.Convenience> SelectOffers(List<cfg.Convenience> candidates)
    {
        List<cfg.Convenience> pool = new List<cfg.Convenience>(candidates);
        List<cfg.Convenience> selectedOffers = new List<cfg.Convenience>(ItemsPerMonth);
        for (int i = 0; i < ItemsPerMonth; i++)
        {
            long totalWeight = 0;
            for (int j = 0; j < pool.Count; j++)
            {
                totalWeight += pool[j].Weight;
            }

            long roll = (long)(Random.value * totalWeight);
            long accumulatedWeight = 0;
            int selectedIndex = pool.Count - 1;
            for (int j = 0; j < pool.Count; j++)
            {
                accumulatedWeight += pool[j].Weight;
                if (roll < accumulatedWeight)
                {
                    selectedIndex = j;
                    break;
                }
            }

            selectedOffers.Add(pool[selectedIndex]);
            pool.RemoveAt(selectedIndex);
        }

        return selectedOffers;
    }

    private void TryPurchase(cfg.Convenience offerConfig)
    {
        ConveniencePurchaseResult purchaseResult = PlayerInfoManager.GetInstance()
            .TryPurchaseConvenienceOffer(offerConfig);
        switch (purchaseResult)
        {
            case ConveniencePurchaseResult.Success:
                RefreshMonthlyOffers();
                break;
            case ConveniencePurchaseResult.SoldOut:
                CommonTipView.Show("剩余购买次数不足");
                break;
            case ConveniencePurchaseResult.InsufficientCoins:
                CommonTipView.Show("模拟币不足");
                break;
            default:
                Debug.LogError($"便利店商品购买失败: [{offerConfig.Id}]", this);
                break;
        }
    }

    private static bool TryGetProductPresentation(int productId, out string iconPath, out int iconScale)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(productId);
        if (itemConfig != null)
        {
            iconPath = itemConfig.Icon;
            iconScale = itemConfig.RewardScale;
            return true;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(productId);
        if (baseConfig != null)
        {
            iconPath = baseConfig.Icon;
            iconScale = baseConfig.RewardScale;
            return true;
        }

        iconPath = null;
        iconScale = 0;
        return false;
    }

    private void RefreshSimulationCoins(PlayerInfoManager playerInfoManager)
    {
        _txtCoin.text = playerInfoManager.SimulationCoins.ToString();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.ConvenienceStoreView;
    }
}
