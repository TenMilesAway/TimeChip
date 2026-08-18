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

        _purchaseButton.interactable = !PlayerInfoManager.GetInstance().IsHomeUnlocked(_homeConfig.Id);
        LoadIconAsync(_homeConfig, GetInstanceID().ToString(), _presentationVersion);
    }

    /// <summary>
    /// 购买当前家具, 未解锁前置家具时一并购买
    /// </summary>
    public void Purchase()
    {
        if (_homeConfig == null || PlayerInfoManager.GetInstance().IsHomeUnlocked(_homeConfig.Id)) return;

        int purchaseCost = GetPurchaseCost();
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TrySpendSimulationCoins(purchaseCost))
        {
            CommonTipView.Show($"模拟币不足，无法购买家具: [{_homeConfig.Name}]");
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
        _prerequisiteConfig = null;
        _imageTip.SetActive(false);
        _priceText.text = $"-{_homeConfig.Price}";
        _purchaseButton.interactable = false;
        OnClose();
        CommonTipView.Show($"购买成功，获得家具: {suffix}");
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
    private int GetPurchaseCost()
    {
        return _homeConfig.Price + (_prerequisiteConfig == null ? 0 : _prerequisiteConfig.Price);
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

    public override string GetPanelName()
    {
        return GlobalDefine.HomeItemDetail;
    }
}
