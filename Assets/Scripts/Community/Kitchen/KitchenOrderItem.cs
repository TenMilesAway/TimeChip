using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KitchenOrderItem : MonoBehaviour
{
    [SerializeField] private Text _txtOrderName;
    [SerializeField] private Text _txtProgress;
    [SerializeField] private Image _imgAvatar;
    [SerializeField] private Image _imgFood;
    [SerializeField] private Button _btnComplete;
    [SerializeField] private Button _btnCompleteNowFood;
    [SerializeField] private GameObject _goCompleteNowFood;
    [SerializeField] private GameObject _goOrder;
    [SerializeField] private GameObject _goCompleted;
    [SerializeField] private Transform _transRewards;

    private readonly List<KitchenOrderReward> _rewardItems = new List<KitchenOrderReward>();
    private int _orderIndex = -1;
    private int _presentationVersion;
    private Action<int> _deliverHandler;

    private void Awake()
    {
        if (_btnCompleteNowFood != null)
        {
            UiButtonAudio.Bind(_btnCompleteNowFood, OnClickDeliverCurrentDish);
        }

        if (_btnComplete != null)
        {
            UiButtonAudio.Bind(_btnComplete, OnClickDeliverCurrentDish);
        }
    }

    private void OnDestroy()
    {
        if (_btnCompleteNowFood != null)
        {
            _btnCompleteNowFood.onClick.RemoveListener(OnClickDeliverCurrentDish);
        }

        if (_btnComplete != null)
        {
            _btnComplete.onClick.RemoveListener(OnClickDeliverCurrentDish);
        }

        ClearRewardItems();
    }

    public void Bind(int orderIndex, Action<int> deliverHandler)
    {
        _orderIndex = orderIndex;
        _deliverHandler = deliverHandler;
    }

    public void SetData(PlayerKitchenOrder order, string resourceTag)
    {
        _presentationVersion++;
        int presentationVersion = _presentationVersion;
        if (order == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        cfg.KitchenOrder orderType = DataTableMananger.GetInstance().Tables.KitchenOrderTable
            .GetOrDefault(order.orderTypeId);

        bool completed = order.isCompleted;
        _goOrder.SetActive(!completed);
        _goCompleted.SetActive(completed);

        if (completed)
        {
            _txtOrderName.text = orderType != null ? orderType.Name : "订单";
            _txtProgress.text = $"{order.RequiredDishCount} / {order.RequiredDishCount}";
            SetActionButtonsVisible(false, false);
            _imgFood.sprite = null;
            LoadAvatarAsync(order.avatarIcon, resourceTag, presentationVersion);
            RefreshRewardsAsync(order, resourceTag, presentationVersion);
            return;
        }

        if (!order.TryGetCurrentRequiredDish(out int requiredDishId))
        {
            gameObject.SetActive(false);
            return;
        }

        cfg.Item dishConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(requiredDishId);
        _txtOrderName.text = dishConfig != null ? dishConfig.Name : $"菜品 {requiredDishId}";
        _txtProgress.text =
            $"{order.deliveredDishCount + 1} / {order.RequiredDishCount}";

        bool isLastDish = order.IsOnLastRequiredDish();
        SetActionButtonsVisible(!isLastDish, isLastDish);

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int consumeItemId = KitchenDishMatchRules.GetPreferredConsumeItemId(
            requiredDishId,
            playerInfoManager);
        bool canDeliver = consumeItemId > 0;
        if (_btnCompleteNowFood != null)
        {
            _btnCompleteNowFood.interactable = canDeliver;
        }

        if (_btnComplete != null)
        {
            _btnComplete.interactable = canDeliver;
        }

        _imgFood.sprite = null;
        LoadAvatarAsync(order.avatarIcon, resourceTag, presentationVersion);
        if (dishConfig != null)
        {
            LoadDishIconAsync(dishConfig, resourceTag, presentationVersion);
        }

        RefreshRewardsAsync(order, resourceTag, presentationVersion);
    }

    public void ClearPresentation()
    {
        _presentationVersion++;
        ClearRewardItems();
    }

    private void SetActionButtonsVisible(bool showNowFood, bool showComplete)
    {
        if (_goCompleteNowFood != null)
        {
            _goCompleteNowFood.SetActive(showNowFood);
        }
        else if (_btnCompleteNowFood != null)
        {
            _btnCompleteNowFood.gameObject.SetActive(showNowFood);
        }

        if (_btnComplete != null)
        {
            _btnComplete.gameObject.SetActive(showComplete);
        }
    }

    private void OnClickDeliverCurrentDish()
    {
        if (_orderIndex < 0)
        {
            return;
        }

        _deliverHandler?.Invoke(_orderIndex);
    }

    private async void LoadAvatarAsync(
        string avatarIcon,
        string resourceTag,
        int presentationVersion)
    {
        if (string.IsNullOrEmpty(avatarIcon))
        {
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(avatarIcon, resourceTag);
        if (presentationVersion != _presentationVersion || icon == null)
        {
            if (icon == null)
            {
                Debug.LogError($"厨房订单头像加载失败: [{avatarIcon}]", this);
            }

            return;
        }

        _imgAvatar.sprite = icon;
        _imgAvatar.SetNativeSize();
        if (KitchenOrderUiScale.TryGetScaleMultiplier(
                KitchenOrderUiScale.AvatarScaleId,
                out float avatarScale))
        {
            KitchenOrderUiScale.ApplyIconScale(_imgAvatar.rectTransform, avatarScale);
        }
        else
        {
            Debug.LogError(
                $"厨房订单头像缩放配置不存在: [{KitchenOrderUiScale.AvatarScaleId}]",
                this);
        }
    }

    private async void LoadDishIconAsync(
        cfg.Item dishConfig,
        string resourceTag,
        int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            dishConfig.Icon,
            resourceTag);
        if (presentationVersion != _presentationVersion || icon == null)
        {
            if (icon == null)
            {
                Debug.LogError(
                    $"厨房订单菜品图标加载失败: [{dishConfig.Id}], [{dishConfig.Icon}]",
                    this);
            }

            return;
        }

        _imgFood.sprite = icon;
        _imgFood.SetNativeSize();
        if (KitchenOrderUiScale.TryGetScaleMultiplier(
                KitchenOrderUiScale.DishScaleId,
                out float dishScale))
        {
            KitchenOrderUiScale.ApplyIconScale(_imgFood.rectTransform, dishScale);
        }
        else
        {
            Debug.LogError(
                $"厨房订单菜品缩放配置不存在: [{KitchenOrderUiScale.DishScaleId}]",
                this);
        }
    }

    private async void RefreshRewardsAsync(
        PlayerKitchenOrder order,
        string resourceTag,
        int presentationVersion)
    {
        ClearRewardItems();
        List<(int itemId, int count)> rewards = new List<(int, int)>(2);
        if (order.rewardSimulationCoins > 0)
        {
            rewards.Add((BasePropertyId.SimulationCoin, order.rewardSimulationCoins));
        }

        if (order.rewardGiftItemId > 0 && order.rewardGiftCount > 0)
        {
            rewards.Add((order.rewardGiftItemId, order.rewardGiftCount));
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            (int itemId, int count) = rewards[i];
            GameObject rewardObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.KitchenOrderReward, resourceTag);
            if (presentationVersion != _presentationVersion)
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.KitchenOrderReward,
                    rewardObject);
                return;
            }

            rewardObject.transform.SetParent(_transRewards, false);
            KitchenOrderReward rewardItem = rewardObject.GetComponent<KitchenOrderReward>();
            if (rewardItem == null ||
                !await rewardItem.SetDataAsync(itemId, count, resourceTag))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.KitchenOrderReward,
                    rewardObject);
                continue;
            }

            if (presentationVersion != _presentationVersion)
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.KitchenOrderReward,
                    rewardObject);
                return;
            }

            _rewardItems.Add(rewardItem);
        }
    }

    private void ClearRewardItems()
    {
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            KitchenOrderReward rewardItem = _rewardItems[i];
            if (rewardItem == null)
            {
                continue;
            }

            rewardItem.Clear();
            UnityObjectPoolFactory.GetInstance().PutItem(
                GlobalDefine.KitchenOrderReward,
                rewardItem.gameObject);
        }

        _rewardItems.Clear();
    }
}