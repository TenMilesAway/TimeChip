using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortView : UIBasePanel
{
    private const int MinContainerCount = 2;
    private const int MaxContainerCount = 4;
    private const int FirstContainerLotteryPoolId = 12;
    private const int LastContainerLotteryPoolId = 16;

    [SerializeField] private Button _btnBack;
    [SerializeField] private Transform _containerParent;

    private readonly List<ContainerItem> _containerItems =
        new List<ContainerItem>();
    private int _displayVersion;
    private bool _isPurchasing;

    private void Awake()
    {
        if (_btnBack == null || _containerParent == null)
        {
            Debug.LogError("PortView 缺少必要的 UI 引用。", this);
            enabled = false;
            return;
        }

        _btnBack.onClick.AddListener(OnClickBack);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.TurnAdvanced -= RefreshContainers;
        playerInfoManager.TurnAdvanced += RefreshContainers;
        RefreshContainers();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshContainers;
        base.HideHandle();
    }

    protected override void CloseHandle()
    {
        _displayVersion++;
        ClearContainerItems();
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshContainers;
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.PortView;
    }

    private void RefreshContainers()
    {
        if (!TryGetCurrentTurnOffers(out List<PlayerPortContainerOffer> offers))
        {
            return;
        }

        _displayVersion++;
        ClearContainerItems();
        CreateContainerItemsAsync(offers, _displayVersion);
    }

    private bool TryGetCurrentTurnOffers(out List<PlayerPortContainerOffer> offers)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.TryGetCurrentTurnPortContainerOffers(out offers))
        {
            return true;
        }

        offers = new List<PlayerPortContainerOffer>();
        int count = TurnRandom.Range(
            "Port.Container.Count",
            MinContainerCount,
            MaxContainerCount + 1);
        for (int i = 0; i < count; i++)
        {
            int lotteryPoolId = TurnRandom.Range(
                $"Port.Container.{i}.Pool",
                FirstContainerLotteryPoolId,
                LastContainerLotteryPoolId + 1);
            if (!LotteryView.TryCreateContainerLotteryOffer(
                    lotteryPoolId,
                    $"Port.Container.{i}",
                    out ContainerLotteryOffer lotteryOffer))
            {
                Debug.LogError($"港口集装箱奖池配置无效: [{lotteryPoolId}]。", this);
                offers = null;
                return false;
            }

            offers.Add(new PlayerPortContainerOffer
            {
                lotteryPoolId = lotteryOffer.LotteryPoolId,
                price = lotteryOffer.Price,
                rewardCount = lotteryOffer.RewardCount
            });
        }

        playerInfoManager.SetCurrentTurnPortContainerOffers(offers);
        return playerInfoManager.TryGetCurrentTurnPortContainerOffers(out offers);
    }

    private async void CreateContainerItemsAsync(
        List<PlayerPortContainerOffer> offers,
        int displayVersion)
    {
        string resourceTag = GetInstanceID().ToString();
        for (int i = 0; i < offers.Count; i++)
        {
            int index = i;
            PlayerPortContainerOffer offer = offers[i];
            cfg.Lottery lotteryConfig = DataTableMananger.GetInstance().Tables.LotteryTable
                .GetOrDefault(offer.lotteryPoolId);
            if (lotteryConfig == null)
            {
                Debug.LogError($"港口集装箱奖池不存在: [{offer.lotteryPoolId}]。", this);
                continue;
            }

            GameObject containerObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.ContainerItem, resourceTag);
            if (!IsCurrentDisplay(displayVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.ContainerItem,
                    containerObject);
                return;
            }

            containerObject.SetActive(false);
            containerObject.transform.SetParent(_containerParent, false);
            ContainerItem containerItem = containerObject.GetComponent<ContainerItem>();
            if (containerItem == null)
            {
                Debug.LogError("ContainerItem 预制体缺少 ContainerItem 组件。", this);
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.ContainerItem,
                    containerObject);
                continue;
            }

            containerItem.SetData(
                offer,
                lotteryConfig.Icon,
                () => ShowPurchaseConfirmation(index));
            containerObject.SetActive(true);
            _containerItems.Add(containerItem);
        }
    }

    private void ShowPurchaseConfirmation(int index)
    {
        if (_isPurchasing ||
            !PlayerInfoManager.GetInstance().TryGetCurrentTurnPortContainerOffers(
                out List<PlayerPortContainerOffer> offers) ||
            index < 0 ||
            index >= offers.Count ||
            offers[index].purchased)
        {
            return;
        }

        PlayerPortContainerOffer offer = offers[index];
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "港口集装箱",
                    $"购买将花费 {offer.price} 模拟币",
                    () => PurchaseContainer(index, offer),
                    null,
                    "购买",
                    "取消")
            });
    }

    private void PurchaseContainer(int index, PlayerPortContainerOffer offer)
    {
        if (_isPurchasing)
        {
            return;
        }

        List<CommonRewardItemData> rewards = new List<CommonRewardItemData>(
            offer.rewardCount);
        for (int i = 0; i < offer.rewardCount; i++)
        {
            if (!LotteryView.TryDrawReward(
                    offer.lotteryPoolId,
                    $"Port.Container.{index}.Reward.{i}",
                    out CommonRewardItemData reward))
            {
                Debug.LogError($"港口集装箱奖励生成失败: [{offer.lotteryPoolId}]。", this);
                CommonTipView.Show("集装箱奖励暂时无法获取");
                return;
            }

            rewards.Add(reward);
        }

        _isPurchasing = true;
        if (!PlayerInfoManager.GetInstance().TryPurchaseCurrentTurnPortContainer(
                index,
                offer.price))
        {
            _isPurchasing = false;
            CommonTipView.Show("模拟币不足或集装箱已售出");
            return;
        }

        int simulationCoinDisplayStart = PlayerInfoManager.GetInstance().SimulationCoins;
        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
        LotteryView.GrantRewards(rewards);
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView != null &&
            ContainsSimulationCoinReward(rewards))
        {
            mainMenuView.SetSimulationCoinDisplay(simulationCoinDisplayStart);
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.PortContainerView,
            UILayer.System,
            new OpenUIParam
            {
                data = rewards,
                simulationCoinDisplayStart = simulationCoinDisplayStart
            });
        _isPurchasing = false;
        RefreshContainers();
    }

    private void OnClickBack()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.SubwayView);
    }

    private bool IsCurrentDisplay(int displayVersion)
    {
        return displayVersion == _displayVersion && isActiveAndEnabled;
    }

    private static bool ContainsSimulationCoinReward(
        List<CommonRewardItemData> rewards)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] != null &&
                rewards[i].itemId == BasePropertyId.SimulationCoin &&
                rewards[i].itemCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearContainerItems()
    {
        for (int i = 0; i < _containerItems.Count; i++)
        {
            ContainerItem containerItem = _containerItems[i];
            if (containerItem == null)
            {
                continue;
            }

            containerItem.Clear();
            UnityObjectPoolFactory.GetInstance().PutItem(
                GlobalDefine.ContainerItem,
                containerItem.gameObject);
        }

        _containerItems.Clear();
    }
}
