using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonExchangeGroup : MonoBehaviour
{
    private const float IconScaleDivisor = 10000f;
    private const string ExchangeLeftScaleName = "exchangeLeft";
    private const string ExchangeRightScaleName = "exchangeRight";

    [SerializeField] private Button _btnExchange;
    [SerializeField] private Button[] _btnNeedItems;             // 兑换消耗物品按钮, 左侧, 最多 3 个
    [SerializeField] private Button _btnGetItem;                 // 兑换获得物品按钮, 右侧
    [SerializeField] private GameObject[] _goExchangeNeedItems;
    [SerializeField] private Image[] _imgExchangeNeedItems;
    [SerializeField] private Text[] _txtExchangeNeedItems;
    [SerializeField] private Text[] _txtHasNum;
    [SerializeField] private Image _imgExchangeGetItem;
    [SerializeField] private Text _txtExchangeGetItem;

    private cfg.Exchange _exchangeConfig;
    private Action _onExchanged;
    private int _presentationVersion;
    private string _resourceTag;
    private int _rewardItemId;
    private readonly List<int> _needItemIds = new List<int>(3);

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        UiButtonAudio.Bind(_btnExchange, OnClickExchange);
        if (_btnNeedItems != null)
        {
            for (int index = 0; index < _btnNeedItems.Length; index++)
            {
                Button needItemButton = _btnNeedItems[index];
                if (needItemButton == null)
                {
                    continue;
                }

                int capturedIndex = index;
                UiButtonAudio.Bind(needItemButton, () => OnClickNeedItemDetail(capturedIndex));
            }
        }

        UiButtonAudio.Bind(_btnGetItem, OnClickGetItemDetail);
    }

    private void OnDestroy()
    {
        if (_btnExchange != null)
        {
            _btnExchange.onClick.RemoveListener(OnClickExchange);
        }

        if (_btnNeedItems != null)
        {
            for (int index = 0; index < _btnNeedItems.Length; index++)
            {
                _btnNeedItems[index]?.onClick.RemoveAllListeners();
            }
        }

        if (_btnGetItem != null)
        {
            _btnGetItem.onClick.RemoveListener(OnClickGetItemDetail);
        }
    }

    public void Setup(cfg.Exchange exchangeConfig, Action onExchanged)
    {
        _exchangeConfig = exchangeConfig;
        _onExchanged = onExchanged;
        RefreshPresentation();
    }

    public void RefreshPresentation()
    {
        if (_exchangeConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _presentationVersion++;

        if (!ExchangeConfigParser.TryParseCostItems(
                _exchangeConfig.CostItems,
                out List<ExchangeCostEntry> costs,
                out string costError))
        {
            Debug.LogError($"兑换配置消耗无效: [{_exchangeConfig.Id}], {costError}", this);
            gameObject.SetActive(false);
            return;
        }

        if (!ExchangeConfigParser.TryParseRewardItem(
                _exchangeConfig.RewardItems,
                out int rewardItemId,
                out int rewardCount,
                out string rewardError))
        {
            Debug.LogError($"兑换配置奖励无效: [{_exchangeConfig.Id}], {rewardError}", this);
            gameObject.SetActive(false);
            return;
        }

        float exchangeLeftScaleMultiplier = GetScaleMultiplier(ExchangeLeftScaleName);
        float exchangeRightScaleMultiplier = GetScaleMultiplier(ExchangeRightScaleName);
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        _needItemIds.Clear();
        _rewardItemId = rewardItemId;
        for (int i = 0; i < _goExchangeNeedItems.Length; i++)
        {
            bool visible = i < costs.Count;
            if (_goExchangeNeedItems[i] != null)
            {
                _goExchangeNeedItems[i].SetActive(visible);
            }

            if (!visible)
            {
                continue;
            }

            ExchangeCostEntry cost = costs[i];
            _needItemIds.Add(cost.ItemId);
            if (_btnNeedItems != null && i < _btnNeedItems.Length && _btnNeedItems[i] != null)
            {
                _btnNeedItems[i].interactable = true;
            }

            if (_txtExchangeNeedItems != null && i < _txtExchangeNeedItems.Length && _txtExchangeNeedItems[i] != null)
            {
                _txtExchangeNeedItems[i].text = cost.Count.ToString();
            }

            if (_txtHasNum != null && i < _txtHasNum.Length && _txtHasNum[i] != null)
            {
                int ownedCount = playerInfoManager.GetItemCount(cost.ItemId);
                _txtHasNum[i].text = $"拥有: <color=#00FFFA>{ownedCount}</color>";
            }

            if (_imgExchangeNeedItems != null && i < _imgExchangeNeedItems.Length && _imgExchangeNeedItems[i] != null)
            {
                LoadIconAsync(
                    _imgExchangeNeedItems[i],
                    cost.ItemId,
                    _presentationVersion,
                    exchangeLeftScaleMultiplier);
            }
        }

        if (_txtExchangeGetItem != null)
        {
            _txtExchangeGetItem.text = rewardCount.ToString();
        }

        if (_imgExchangeGetItem != null)
        {
            LoadIconAsync(
                _imgExchangeGetItem,
                rewardItemId,
                _presentationVersion,
                exchangeRightScaleMultiplier);
        }

        if (_btnNeedItems != null)
        {
            for (int index = _needItemIds.Count; index < _btnNeedItems.Length; index++)
            {
                if (_btnNeedItems[index] != null)
                {
                    _btnNeedItems[index].interactable = false;
                }
            }
        }

        if (_btnGetItem != null)
        {
            _btnGetItem.interactable = _rewardItemId > 0;
        }

        if (_btnExchange != null)
        {
            _btnExchange.interactable = ExchangeService.CanAfford(_exchangeConfig.CostItems);
        }
    }

    private void OnClickNeedItemDetail(int needItemIndex)
    {
        if (needItemIndex < 0 || needItemIndex >= _needItemIds.Count)
        {
            return;
        }

        OpenItemDetail(_needItemIds[needItemIndex]);
    }

    private void OnClickGetItemDetail()
    {
        OpenItemDetail(_rewardItemId);
    }

    private static void OpenItemDetail(int itemId)
    {
        if (itemId <= 0)
        {
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null ||
            (tables.ItemTable.GetOrDefault(itemId) == null &&
             tables.BaseTable.GetOrDefault(itemId) == null))
        {
            Debug.LogError($"兑换物品详情配置不存在: [{itemId}]");
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            param: new OpenUIParam { data = itemId });
    }

    private void OnClickExchange()
    {
        if (_exchangeConfig == null)
        {
            return;
        }

        if (!ExchangeConfigParser.TryParseRewardItem(
                _exchangeConfig.RewardItems,
                out int rewardItemId,
                out int _,
                out _))
        {
            return;
        }

        if (!ExchangeService.TryExchange(_exchangeConfig.CostItems, _exchangeConfig.RewardItems, out string failMessage))
        {
            CommonTipView.Show(string.IsNullOrEmpty(failMessage) ? "兑换失败" : failMessage);
            RefreshPresentation();
            return;
        }

        string rewardName = ExchangeService.GetItemDisplayName(rewardItemId);
        CommonTipView.Show($"兑换成功，获得【<color=#00FFFA>{rewardName}</color>】");
        _onExchanged?.Invoke();
        RefreshPresentation();
    }

    private static float GetScaleMultiplier(string scaleName)
    {
        cfg.Scale scaleConfig = GetScaleConfig(scaleName);
        return scaleConfig == null ? 1f : scaleConfig.ScaleValue / IconScaleDivisor;
    }

    private static cfg.Scale GetScaleConfig(string scaleName)
    {
        IReadOnlyList<cfg.Scale> scales = DataTableMananger.GetInstance().Tables.ScaleTable.DataList;
        for (int i = 0; i < scales.Count; i++)
        {
            if (scales[i].Name == scaleName)
            {
                return scales[i];
            }
        }

        Debug.LogError($"缩放配置不存在: [{scaleName}]");
        return null;
    }

    private async void LoadIconAsync(
        Image targetImage,
        int itemId,
        int presentationVersion,
        float panelScaleMultiplier)
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.sprite = null;
        if (!ExchangeService.TryGetItemPresentation(itemId, out string iconPath, out int iconScale))
        {
            Debug.LogError($"兑换物品图标配置不存在: [{itemId}]", this);
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(iconPath, _resourceTag);
        if (presentationVersion != _presentationVersion || targetImage == null)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"兑换物品图标加载失败: [{itemId}], [{iconPath}]", this);
            return;
        }

        targetImage.sprite = icon;
        targetImage.SetNativeSize();
        float scale = iconScale / IconScaleDivisor * panelScaleMultiplier;
        targetImage.rectTransform.localScale = Vector3.one * scale;
    }
}
