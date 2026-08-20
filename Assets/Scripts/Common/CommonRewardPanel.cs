using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CommonRewardPanel : UIBasePanel
{
    [SerializeField] private Button _closeButton;                           // 关闭按钮
    [SerializeField] private Transform _commonRewardItemParent;             // 通用奖励物品父物体
    [SerializeField] private GameObject _textTip;                           // 提示文本
    [SerializeField, Min(0f)] private float _itemDisplayInterval = 0.35f;   // 奖励物品显示间隔
    [SerializeField, Min(1)] private int _simulationCoinIconCount = 8;      // 模拟币飞行图标数量
    [SerializeField] private bool _waitForFirstCoinArrival = true;          // 是否等待首枚图标抵达后入账

    private readonly List<CommonRewardItem> _rewardItems = new List<CommonRewardItem>(); // 奖励物品

    private int _presentationVersion; // 当前展示版本号，用于处理异步加载和展示的顺序问题

    private void Awake()
    {
        _closeButton.onClick.AddListener(ClosePanel);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        ResetPresentation();

        if (!(param?.data is List<CommonRewardItemData> rewardDataList))
        {
            Debug.LogError("CommonRewardPanel 需要 List<CommonRewardItemData> 数据");
            FinishPresentation();
            return;
        }

        InitializeRewardsAsync(rewardDataList, _presentationVersion);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();

        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonRewardPanel;
    }

    /// <summary>
    /// 异步加载奖励物品
    /// </summary>
    private async void InitializeRewardsAsync(List<CommonRewardItemData> rewardDataList, int presentationVersion)
    {
        string resourceTag = GetInstanceID().ToString();

        for (int i = 0; i < rewardDataList.Count; i++)
        {
            CommonRewardItemData rewardData = rewardDataList[i];
            cfg.Base baseConfig = DataTableMananger.GetInstance().Tables.BaseTable.GetOrDefault(rewardData.itemId);
            cfg.Item itemConfig = baseConfig == null
                ? DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(rewardData.itemId)
                : null;

            if (baseConfig == null && itemConfig == null)
            {
                Debug.LogError($"奖励物品配置不存在: [{rewardData.itemId}]");
                continue;
            }

            GameObject rewardItemObject = await UnityObjectPoolFactory.GetInstance().GetItem<GameObject>(GlobalDefine.CommonRewardItem, resourceTag);

            // 如果不是当前版本的展示框
            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                return;
            }

            rewardItemObject.SetActive(false);
            rewardItemObject.transform.SetParent(_commonRewardItemParent, false);

            string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
            int rewardScale = baseConfig == null ? itemConfig.RewardScale : baseConfig.RewardScale;
            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(iconPath, resourceTag);

            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                return;
            }

            if (icon == null)
            {
                Debug.LogError($"奖励 icon 加载失败: [{rewardData.itemId}], [{iconPath}].");
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                continue;
            }

            CommonRewardItem rewardItem = rewardItemObject.GetComponent<CommonRewardItem>();
            rewardItem.SetData(rewardData.itemId, icon, rewardData.itemCount, rewardScale);
            _rewardItems.Add(rewardItem);
        }

        if (IsCurrentPresentation(presentationVersion))
        {
            PlayRewardItemSequence(presentationVersion);
        }
    }

    /// <summary>
    /// 播放奖品逐个展示动画
    /// </summary>
    private void PlayRewardItemSequence(int presentationVersion)
    {
        if (_rewardItems.Count == 0)
        {
            FinishPresentation();
            return;
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);

        for (int i = 0; i < _rewardItems.Count; i++)
        {
            CommonRewardItem rewardItem = _rewardItems[i];
            sequence.AppendCallback(() => rewardItem.gameObject.SetActive(true));
            sequence.AppendInterval(_itemDisplayInterval);
        }

        sequence.OnComplete(() =>
        {
            if (IsCurrentPresentation(presentationVersion))
            {
                FinishPresentation();
            }
        });
    }

    /// <summary>
    /// 重置展示奖品
    /// </summary>
    private void ResetPresentation()
    {
        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
        _textTip.SetActive(false);
        _closeButton.interactable = false;
    }

    /// <summary>
    /// 结束展示奖品
    /// </summary>
    private void FinishPresentation()
    {
        _textTip.SetActive(true);
        _closeButton.interactable = true;
    }

    /// <summary>
    /// 判断是否为当前展示版本
    /// </summary>
    private bool IsCurrentPresentation(int presentationVersion)
    {
        return presentationVersion == _presentationVersion && isActiveAndEnabled;
    }

    /// <summary>
    /// 清除奖励物品
    /// </summary>
    private void ClearRewardItems()
    {
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, _rewardItems[i].gameObject);
        }

        _rewardItems.Clear();
    }

    private void ClosePanel()
    {
        GrantNonSimulationCoinBasePropertyRewards();
        PlaySimulationCoinFlyAnimations();
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    /// <summary>
    /// 将模拟币奖励从奖励图标飞往主界面货币文本，并在首枚图标抵达时入账
    /// </summary>
    private void PlaySimulationCoinFlyAnimations()
    {
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null)
        {
            GrantSimulationCoinRewardsImmediately();
            return;
        }

        for (int i = 0; i < _rewardItems.Count; i++)
        {
            CommonRewardItem rewardItem = _rewardItems[i];
            int simulationCoinAmount = GetSimulationCoinAmount(rewardItem.ItemId, rewardItem.Count);
            if (simulationCoinAmount <= 0)
            {
                continue;
            }

            if (!CurrencyFlyAnimation.Play(
                    rewardItem.IconTransform,
                    mainMenuView.SimulationCoinIcon.rectTransform,
                    mainMenuView.SimulationCoinIcon.sprite,
                    _simulationCoinIconCount,
                    _waitForFirstCoinArrival,
                    () => AddSimulationCoins(mainMenuView, simulationCoinAmount)))
            {
                AddSimulationCoins(mainMenuView, simulationCoinAmount);
            }
        }
    }

    /// <summary>
    /// 在无法播放特效时直接发放所有模拟币奖励
    /// </summary>
    private void GrantSimulationCoinRewardsImmediately()
    {
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            int simulationCoinAmount = GetSimulationCoinAmount(_rewardItems[i].ItemId, _rewardItems[i].Count);
            if (simulationCoinAmount > 0)
            {
                PlayerInfoManager.GetInstance().AddSimulationCoins(simulationCoinAmount);
            }
        }
    }

    /// <summary>
    /// 将模拟币写入玩家数据, 并平滑更新主界面的显示数值
    /// </summary>
    private static void AddSimulationCoins(MainMenuView mainMenuView, int amount)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int currentAmount = playerInfoManager.SimulationCoins;
        playerInfoManager.AddSimulationCoins(amount);
        mainMenuView.PlaySimulationCoinCountAnimation(currentAmount, currentAmount + amount);
    }

    /// <summary>
    /// 计算模拟币奖励兑换后的数量
    /// </summary>
    private static int GetSimulationCoinAmount(int itemId, int itemCount)
    {
        switch (itemId)
        {
            case BasePropertyId.SimulationCoin:
                return itemCount;
            case 1000:
                return itemCount * 100;
            case 1001:
                return itemCount * 1000;
            default:
                return 0;
        }
    }

    /// <summary>将不需要飞行表现的 base 表属性奖励写入玩家数据。</summary>
    private void GrantNonSimulationCoinBasePropertyRewards()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();

        for (int i = 0; i < _rewardItems.Count; i++)
        {
            CommonRewardItem rewardItem = _rewardItems[i];
            switch (rewardItem.ItemId)
            {
                case BasePropertyId.TimeCoin:
                    playerInfoManager.AddTimeCoins(rewardItem.Count);
                    break;
                case BasePropertyId.Health:
                    playerInfoManager.ChangeHealth(rewardItem.Count);
                    break;
            }
        }
    }
}
