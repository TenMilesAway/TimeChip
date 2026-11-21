using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 家具详情数据
/// </summary>
public sealed class HomeItemDetailData
{
    public cfg.Home HomeConfig { get; }
    public cfg.Home PrerequisiteConfig { get; }

    public HomeItemDetailData(cfg.Home homeConfig, cfg.Home prerequisiteConfig)
    {
        HomeConfig = homeConfig;
        PrerequisiteConfig = prerequisiteConfig;
    }
}

public class HomeItemDetail : UIBasePanel
{
    private const int FurnitureDiscountCouponItemId = 2000;
    private const float FurnitureDiscountMultiplier = 0.8f;

    [SerializeField] private Image _icon;                                                     // 图片
    [SerializeField] private Text _nameText;                                                  // 名称
    [SerializeField] private Text _satisfactionText;                                          // 满意度
    [SerializeField] private Text _priceText;                                                 // 价格
    [SerializeField] private GameObject _imageTip;                                            // 提示
    [SerializeField] private Text _tipText;                                                   // 提示文本
    [SerializeField] private Button _purchaseButton;                                          // 购买按钮
    [SerializeField] private string _tipFormat = "{0}未购买，购买时将额外花费{1}一并购买";    // 有前置文本时的提示词
    [SerializeField] private string _tipDefaultFormat = "购买后将解锁{0}";                    // 无前置文本时的提示词

    private cfg.Home _homeConfig;
    private cfg.Home _prerequisiteConfig;
    private int _presentationVersion;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!(param?.data is HomeItemDetailData detailData) || detailData.HomeConfig == null)
        {
            Debug.LogError("HomeItemDetail 需要有效的 HomeItemDetailData");
            return;
        }

        if (!HasValidUiReferences()) return;

        _presentationVersion++;
        _homeConfig = detailData.HomeConfig;
        _prerequisiteConfig = detailData.PrerequisiteConfig;

        _nameText.text = _homeConfig.Name;
        _satisfactionText.text = $"+{_homeConfig.Satisfaction:0.##}";
        _priceText.text = $"-{GetPurchaseCost()}";
        _imageTip.SetActive(true);
        if (_prerequisiteConfig != null)
        {
            _tipText.text = string.Format(
                _tipFormat,
                _prerequisiteConfig.Name,
                _prerequisiteConfig.Price);
        }
        else
        {
            _tipText.text = string.Format(
                _tipDefaultFormat,
                _homeConfig.Name);
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);

        _purchaseButton.interactable = !PlayerInfoManager.GetInstance().IsHomeUnlocked(_homeConfig.Id);
        LoadIconAsync(_homeConfig, GetInstanceID().ToString(), _presentationVersion);
    }

    /// <summary>
    /// 购买当前家具, 未解锁前置家具时一并购买
    /// </summary>
    public void Purchase()
    {
        if (_homeConfig == null || PlayerInfoManager.GetInstance().IsHomeUnlocked(_homeConfig.Id))
        {
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.GetItemCount(FurnitureDiscountCouponItemId) <= 0)
        {
            CompletePurchase(useFurnitureDiscountCoupon: false);
            return;
        }

        List<int> linePrices = GetPurchaseLinePrices();
        int fullPrice = SumLinePrices(linePrices);
        int ownedCoupons = playerInfoManager.GetItemCount(FurnitureDiscountCouponItemId);
        int couponsToUse = Mathf.Min(ownedCoupons, linePrices.Count);
        int discountedPrice = CalculateCostWithFurnitureCoupons(linePrices, couponsToUse);
        string couponConsumeHint = couponsToUse > 0
            ? $"，将消耗<color=#00FFFA>家具折扣券</color> ×{couponsToUse}"
            : string.Empty;
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "使用家具折扣券",
                    $"是否使用<color=#00FFFA>家具折扣券</color>?本次购买花费 {discountedPrice} 模拟币 (原价 {fullPrice}){couponConsumeHint}",
                    () =>
                    {
                        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
                        CompletePurchase(useFurnitureDiscountCoupon: true);
                    },
                    () =>
                    {
                        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
                        CompletePurchase(useFurnitureDiscountCoupon: false);
                    },
                    "使用",
                    "不使用")
            });
    }

    private void CompletePurchase(bool useFurnitureDiscountCoupon)
    {
        if (_homeConfig == null || PlayerInfoManager.GetInstance().IsHomeUnlocked(_homeConfig.Id))
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        List<int> linePrices = GetPurchaseLinePrices();
        int couponsToUse = 0;
        if (useFurnitureDiscountCoupon)
        {
            couponsToUse = Mathf.Min(
                playerInfoManager.GetItemCount(FurnitureDiscountCouponItemId),
                linePrices.Count);
        }

        int purchaseCost = CalculateCostWithFurnitureCoupons(linePrices, couponsToUse);
        if (!playerInfoManager.TrySpendSimulationCoins(purchaseCost))
        {
            CommonTipView.Show(
                $"模拟币不足，无法购买家具: [<color=#00FFFA>{_homeConfig.Name}</color>]");
            return;
        }

        if (couponsToUse > 0 &&
            !playerInfoManager.TryConsumeItem(FurnitureDiscountCouponItemId, couponsToUse))
        {
            CommonTipView.Show("家具折扣券不足，购买已取消");
            playerInfoManager.AddSimulationCoins(purchaseCost);
            return;
        }

        string suffix = "";

        if (_prerequisiteConfig != null)
        {
            suffix = $"[{_homeConfig.Name}] 和 [{_prerequisiteConfig.Name}]";
            playerInfoManager.UnlockHome(_prerequisiteConfig.Id);
        }
        else
        {
            suffix = $"[{_homeConfig.Name}]";
        }

        playerInfoManager.UnlockHome(_homeConfig.Id);
        MissionAPI.Broadcast(new MissionMessage(
            MissionEventType.HomePurchase,
            args: _homeConfig.Id.ToString()));
        _prerequisiteConfig = null;
        _imageTip.SetActive(false);
        _priceText.text = $"-{purchaseCost}";
        _purchaseButton.interactable = false;
        OnClose();

        if (couponsToUse > 0)
        {
            CommonTipView.Show(
                $"购买成功，获得家具: <color=#00FFFA>{suffix}</color>，失去<color=#00FFFA>家具折扣券</color> ×{couponsToUse}");
        }
        else
        {
            CommonTipView.Show($"购买成功，获得家具: <color=#00FFFA>{suffix}</color>");
        }

        GameManager.Audio.Play(AudioDefine.SFXBuy);
    }

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private async void LoadIconAsync(cfg.Home homeConfig, string resourceTag, int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(homeConfig.Sprite, resourceTag);
        if (presentationVersion != _presentationVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"家具详情图标加载失败: [{homeConfig.Id}], [{homeConfig.Sprite}]");
            return;
        }

        _icon.sprite = icon;
        _icon.SetNativeSize();
        _icon.rectTransform.localScale = Vector3.one * (homeConfig.HomeStoreScale / 10000f);
    }

    /// <summary>
    /// 获得购买价格
    /// </summary>
    private int GetPurchaseCost(bool applyFurnitureDiscountCoupon = false)
    {
        List<int> linePrices = GetPurchaseLinePrices();
        if (!applyFurnitureDiscountCoupon)
        {
            return SumLinePrices(linePrices);
        }

        int ownedCoupons = PlayerInfoManager.GetInstance().GetItemCount(FurnitureDiscountCouponItemId);
        int couponsToUse = Mathf.Min(ownedCoupons, linePrices.Count);
        return CalculateCostWithFurnitureCoupons(linePrices, couponsToUse);
    }

    private List<int> GetPurchaseLinePrices()
    {
        List<int> linePrices = new List<int>(2);
        BuffSystem buffSystem = BuffSystem.GetInstance();
        linePrices.Add(buffSystem.CalculateShopPrice(_homeConfig.Price));
        if (_prerequisiteConfig != null)
        {
            linePrices.Add(buffSystem.CalculateShopPrice(_prerequisiteConfig.Price));
        }

        return linePrices;
    }

    private static int SumLinePrices(IReadOnlyList<int> linePrices)
    {
        int total = 0;
        for (int i = 0; i < linePrices.Count; i++)
        {
            total += linePrices[i];
        }

        return total;
    }

    /// <summary>按件打折：券不足时优先对单价最高的家具使用折扣券（8 折）。</summary>
    private static int CalculateCostWithFurnitureCoupons(IReadOnlyList<int> linePrices, int couponCount)
    {
        if (linePrices == null || linePrices.Count == 0)
        {
            return 0;
        }

        if (couponCount <= 0)
        {
            return SumLinePrices(linePrices);
        }

        List<int> pricesByDescending = new List<int>(linePrices);
        pricesByDescending.Sort((left, right) => right.CompareTo(left));

        int couponsToApply = Mathf.Min(couponCount, pricesByDescending.Count);
        int total = 0;
        for (int i = 0; i < pricesByDescending.Count; i++)
        {
            int linePrice = pricesByDescending[i];
            if (i < couponsToApply)
            {
                total += Mathf.Max(0, Mathf.CeilToInt(linePrice * FurnitureDiscountMultiplier));
            }
            else
            {
                total += linePrice;
            }
        }

        return total;
    }

    /// <summary>
    /// 是否 UI 引用有效
    /// </summary>
    private bool HasValidUiReferences()
    {
        if (_icon != null &&
            _nameText != null &&
            _satisfactionText != null &&
            _priceText != null &&
            _imageTip != null &&
            _tipText != null &&
            _purchaseButton != null)
        {
            return true;
        }

        Debug.LogError("HomeItemDetail 的 UI 引用未在 Inspector 中完整配置");
        return false;
    }

    public bool TryGetPurchaseButton(out Button purchaseButton)
    {
        if (_purchaseButton != null && _purchaseButton.gameObject.activeInHierarchy)
        {
            purchaseButton = _purchaseButton;
            return true;
        }

        purchaseButton = null;
        Debug.LogError("HomeItemDetail 未绑定购买按钮", this);
        return false;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.HomeItemDetail;
    }
}
