using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 为兼容 OpenPanel 接口而保留的 UI 层级枚举
/// </summary>
public enum UILayer
{
    Bot,
    Mid,
    Top,
    System,
}

/// <summary>
/// UI 管理器，所有打开的面板均挂载在 Launcher/UI Root 下
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private const string UIRootPath = "Canvas/UI Root";

    private readonly Dictionary<string, UIBasePanel> _panelDic = new Dictionary<string, UIBasePanel>();
    private readonly Dictionary<string, UIBasePanel> _blockingWindows = new Dictionary<string, UIBasePanel>();
    private readonly List<string> _loadingPanels = new List<string>();
    private readonly Queue<RewardPanelRequest> _rewardPanelQueue = new Queue<RewardPanelRequest>();

    private RectTransform _uiRoot;
    private int _loadingOverlayCount;
    private Action _activeRewardPanelSkipCallback;

    /// <summary>
    /// 初始化 Launcher UI Root 引用
    /// </summary>
    public Task Init()
    {
        TryGetUIRoot();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在 Launcher/UI Root 下打开 UI 面板，layer 参数仅为兼容现有接口而保留
    /// </summary>
    public async void OpenPanel(string panelName, UILayer layer = UILayer.Mid, OpenUIParam param = null, Action action = null)
    {
        await OpenPanelAsync(panelName, layer, param, action);
    }

    /// <summary>
    /// 将奖励展示请求加入队列，避免多个奖励同时打开同一个面板时覆盖彼此。
    /// </summary>
    public Task<UIBasePanel> EnqueueRewardPanel(OpenUIParam param)
    {
        if (TryCreateSplitGainAndLossRewardParams(
                param,
                out OpenUIParam gainedRewardsParam,
                out OpenUIParam lostRewardsParam))
        {
            Task<UIBasePanel> gainedRewardsTask = EnqueueRewardPanelInternal(gainedRewardsParam);
            EnqueueRewardPanelInternal(lostRewardsParam);
            return gainedRewardsTask;
        }

        return EnqueueRewardPanelInternal(param);
    }

    private Task<UIBasePanel> EnqueueRewardPanelInternal(OpenUIParam param)
    {
        TaskCompletionSource<UIBasePanel> completionSource =
            new TaskCompletionSource<UIBasePanel>();
        _rewardPanelQueue.Enqueue(new RewardPanelRequest(param, completionSource));
        RefreshRewardPanelSkipButton();
        OpenNextRewardPanel();
        return completionSource.Task;
    }

    private static bool TryCreateSplitGainAndLossRewardParams(
        OpenUIParam param,
        out OpenUIParam gainedRewardsParam,
        out OpenUIParam lostRewardsParam)
    {
        gainedRewardsParam = null;
        lostRewardsParam = null;
        if (!(param?.data is List<CommonRewardItemData> rewards))
        {
            return false;
        }

        List<CommonRewardItemData> gainedRewards = new List<CommonRewardItemData>();
        List<CommonRewardItemData> lostRewards = new List<CommonRewardItemData>();
        for (int i = 0; i < rewards.Count; i++)
        {
            CommonRewardItemData reward = rewards[i];
            if (reward == null || reward.itemCount == 0)
            {
                continue;
            }

            if (reward.itemCount < 0)
            {
                lostRewards.Add(reward);
            }
            else
            {
                gainedRewards.Add(reward);
            }
        }

        if (gainedRewards.Count == 0 || lostRewards.Count == 0)
        {
            return false;
        }

        gainedRewardsParam = CreateRewardPanelParam(param, gainedRewards, false, null);
        lostRewardsParam = CreateRewardPanelParam(param, lostRewards, true, param.callback);
        return true;
    }

    private static OpenUIParam CreateRewardPanelParam(
        OpenUIParam source,
        List<CommonRewardItemData> rewards,
        bool isPlayerItemLossPresentation,
        Action callback)
    {
        return new OpenUIParam
        {
            data = rewards,
            callback = callback,
            skipQueuedRewardPanelsCallback = source?.skipQueuedRewardPanelsCallback,
            rewardsAlreadyGranted = source != null && source.rewardsAlreadyGranted,
            simulationCoinDisplayStart = source?.simulationCoinDisplayStart,
            isPlayerItemLossPresentation = isPlayerItemLossPresentation
        };
    }

    private void RefreshRewardPanelSkipButton()
    {
        if (_panelDic.TryGetValue(GlobalDefine.CommonRewardPanel, out UIBasePanel panel) &&
            panel is CommonRewardPanel rewardPanel)
        {
            rewardPanel.RefreshSkipButtonVisibility();
        }
    }

    private static void GrantSkippedRewardItems(List<CommonRewardItemData> rewards)
    {
        if (rewards == null)
        {
            Debug.LogError("跳过的 CommonRewardPanel 缺少奖励数据。");
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        for (int i = 0; i < rewards.Count; i++)
        {
            CommonRewardItemData reward = rewards[i];
            if (reward == null)
            {
                Debug.LogError($"跳过的 CommonRewardPanel 奖励数据为空: [{i}]");
                continue;
            }

            if (tables.BaseTable.GetOrDefault(reward.itemId) == null)
            {
                if (tables.ItemTable.GetOrDefault(reward.itemId) == null)
                {
                    Debug.LogError($"跳过的 CommonRewardPanel 奖励配置不存在: [{reward.itemId}]");
                    continue;
                }

                playerInfoManager.AddItem(reward.itemId, reward.itemCount);
                continue;
            }

            switch (reward.itemId)
            {
                case BasePropertyId.SimulationCoin:
                    playerInfoManager.AddSimulationCoins(reward.itemCount);
                    break;
                case BasePropertyId.TimeCoin:
                    playerInfoManager.AddTimeCoins(reward.itemCount);
                    break;
                case BasePropertyId.Health:
                    playerInfoManager.ChangeHealth(reward.itemCount);
                    break;
                case BasePropertyId.WheelCoin:
                    playerInfoManager.AddWheelCoins(reward.itemCount);
                    break;
                case BasePropertyId.BoxCoin:
                    playerInfoManager.AddBoxCoins(reward.itemCount);
                    break;
                case BasePropertyId.CardCoin:
                    playerInfoManager.AddCardCoins(reward.itemCount);
                    break;
                default:
                    Debug.LogError($"跳过的 CommonRewardPanel 基础属性未处理: [{reward.itemId}]");
                    break;
            }
        }
    }

    /// <summary>
    /// 排队展示奖励，不需要等待面板打开完成时使用。
    /// </summary>
    public void QueueRewardPanel(OpenUIParam param)
    {
        _ = EnqueueRewardPanel(param);
    }

    public bool HasQueuedRewardPanels => _rewardPanelQueue.Count > 0;

    public void GrantRewardsImmediately(List<CommonRewardItemData> rewards)
    {
        GrantSkippedRewardItems(rewards);
    }

    public void SkipQueuedRewardPanels()
    {
        _activeRewardPanelSkipCallback?.Invoke();

        while (_rewardPanelQueue.Count > 0)
        {
            RewardPanelRequest request = _rewardPanelQueue.Dequeue();
            OpenUIParam param = request.Param;
            if (param != null && !param.rewardsAlreadyGranted)
            {
                GrantRewardsImmediately(param.data as List<CommonRewardItemData>);
            }

            request.CompletionSource.TrySetResult(null);
            param?.callback?.Invoke();
        }

        RefreshRewardPanelSkipButton();
    }

    /// <summary>
    /// 异步打开 UI 面板；首次加载时会显示主界面的加载遮罩
    /// </summary>
    public async Task<UIBasePanel> OpenPanelAsync(
        string panelName,
        UILayer layer = UILayer.Mid,
        OpenUIParam param = null,
        Action action = null)
    {
        if (_loadingPanels.Contains(panelName) || !TryGetUIRoot())
        {
            return null;
        }

        _loadingPanels.Add(panelName);
        bool isLoadingOverlayVisible = false;
        try
        {
            if (_panelDic.TryGetValue(panelName, out UIBasePanel panel))
            {
                GetPanelCompletedLogic(panel, param, action);
                return panel;
            }

            isLoadingOverlayVisible = BeginLoadingOverlay(panelName);
            GameObject panelGO = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(panelName, GetInstance().ToString());

            if (!TryGetUIRoot())
            {
                UnityObjectPoolFactory.GetInstance().PutItem(panelName, panelGO);
                return null;
            }

            panelGO.transform.SetParent(_uiRoot, false);

            UIBasePanel panelComponent = panelGO.GetComponent<UIBasePanel>();
            if (panelComponent == null)
            {
                Debug.LogError($"UI prefab '{panelName}' is missing a UIBasePanel component.");
                UnityObjectPoolFactory.GetInstance().PutItem(panelName, panelGO);
                return null;
            }

            GetPanelCompletedLogic(panelComponent, param, action);
            _panelDic.Add(panelName, panelComponent);
            return panelComponent;
        }
        finally
        {
            if (isLoadingOverlayVisible)
            {
                EndLoadingOverlay();
            }

            _loadingPanels.Remove(panelName);
        }
    }

    private bool TryGetUIRoot()
    {
        if (_uiRoot != null) return true;

        GameObject uiRootObject = GameObject.Find(UIRootPath);
        if (uiRootObject == null)
        {
            Debug.LogError($"UI Root was not found at '{UIRootPath}'.");
            return false;
        }

        _uiRoot = uiRootObject.transform as RectTransform;
        return _uiRoot != null;
    }

    private void GetPanelCompletedLogic(UIBasePanel panel, OpenUIParam param, Action action)
    {
        panel.OnInit(param);

        if (panel._isBlockingWindow && !_blockingWindows.ContainsKey(panel.GetPanelName()))
        {
            _blockingWindows.Add(panel.GetPanelName(), panel);
        }

        panel.OnShow();
        action?.Invoke();
    }

    private bool BeginLoadingOverlay(string panelName)
    {
        if (panelName == GlobalDefine.MainMenuView ||
            !_panelDic.TryGetValue(GlobalDefine.MainMenuView, out UIBasePanel panel) ||
            !(panel is MainMenuView mainMenuView))
        {
            return false;
        }

        _loadingOverlayCount++;
        mainMenuView.SetLoadingVisible(true);
        return true;
    }

    private void EndLoadingOverlay()
    {
        _loadingOverlayCount = Mathf.Max(0, _loadingOverlayCount - 1);
        if (_loadingOverlayCount != 0 ||
            !_panelDic.TryGetValue(GlobalDefine.MainMenuView, out UIBasePanel panel) ||
            !(panel is MainMenuView mainMenuView))
        {
            return;
        }

        mainMenuView.SetLoadingVisible(false);
    }

    public UIBasePanel GetOpeningPanel(string panelName)
    {
        _panelDic.TryGetValue(panelName, out UIBasePanel panel);
        return panel;
    }

    /// <summary>
    /// 设置主界面底部导航与背包入口的显示状态。
    /// </summary>
    public void SetMainMenuNavigationVisible(bool visible)
    {
        if (_panelDic.TryGetValue(GlobalDefine.MainMenuView, out UIBasePanel panel) &&
            panel is MainMenuView mainMenuView)
        {
            mainMenuView.SetNavigationVisible(visible);
        }
    }

    public void ClosePanel(string panelName)
    {
        if (_blockingWindows.ContainsKey(panelName))
        {
            _blockingWindows.Remove(panelName);
        }

        if (_panelDic.ContainsKey(panelName))
        {
            _panelDic[panelName].OnClose();
            UnityObjectPoolFactory.GetInstance().PutItem(panelName, _panelDic[panelName].gameObject);
            _panelDic.Remove(panelName);
            if (panelName == GlobalDefine.CommonRewardPanel)
            {
                _activeRewardPanelSkipCallback = null;
            }
        }
    }

    /// <summary>
    /// 关闭当前所有已打开的面板
    /// </summary>
    public void CloseAllPanels()
    {
        List<KeyValuePair<string, UIBasePanel>> panels = new List<KeyValuePair<string, UIBasePanel>>(_panelDic);
        _panelDic.Clear();
        _blockingWindows.Clear();
        _activeRewardPanelSkipCallback = null;
        CancelQueuedRewardPanels();

        foreach (KeyValuePair<string, UIBasePanel> panelEntry in panels)
        {
            panelEntry.Value.OnClose();
            UnityObjectPoolFactory.GetInstance().PutItem(panelEntry.Key, panelEntry.Value.gameObject);
        }
    }

    public void ClosePanelAndDestory(string panelName)
    {
        if (_blockingWindows.ContainsKey(panelName))
        {
            _blockingWindows.Remove(panelName);
        }

        if (_panelDic.ContainsKey(panelName))
        {
            _panelDic[panelName].OnClose();
            GameObject.Destroy(_panelDic[panelName].gameObject);
            _panelDic.Remove(panelName);
        }
    }

    private async void OpenNextRewardPanel()
    {
        if (_rewardPanelQueue.Count == 0 ||
            _loadingPanels.Contains(GlobalDefine.CommonRewardPanel) ||
            _panelDic.ContainsKey(GlobalDefine.CommonRewardPanel))
        {
            return;
        }

        RewardPanelRequest request = _rewardPanelQueue.Dequeue();
        Action previousCloseCallback = request.Param?.callback;
        OpenUIParam param = request.Param ?? new OpenUIParam();
        _activeRewardPanelSkipCallback = param.skipQueuedRewardPanelsCallback;
        param.callback = () =>
        {
            previousCloseCallback?.Invoke();
            OpenNextRewardPanel();
        };

        UIBasePanel rewardPanel = await OpenPanelAsync(
            GlobalDefine.CommonRewardPanel,
            UILayer.System,
            param);
        request.CompletionSource.TrySetResult(rewardPanel);

        if (rewardPanel == null)
        {
            _activeRewardPanelSkipCallback = null;
            if (!param.rewardsAlreadyGranted)
            {
                GrantRewardsImmediately(param.data as List<CommonRewardItemData>);
            }

            param.callback?.Invoke();
        }
    }

    private void CancelQueuedRewardPanels()
    {
        while (_rewardPanelQueue.Count > 0)
        {
            RewardPanelRequest request = _rewardPanelQueue.Dequeue();
            request.CompletionSource.TrySetResult(null);
        }
    }

    private sealed class RewardPanelRequest
    {
        public OpenUIParam Param { get; }
        public TaskCompletionSource<UIBasePanel> CompletionSource { get; }

        public RewardPanelRequest(
            OpenUIParam param,
            TaskCompletionSource<UIBasePanel> completionSource)
        {
            Param = param;
            CompletionSource = completionSource;
        }
    }

    public void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = control.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    public void RemoveCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null || trigger.triggers == null)
        {
            return;
        }

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry entry = trigger.triggers[i];
            if (entry.eventID != type)
            {
                continue;
            }

            entry.callback.RemoveListener(callback);
            if (entry.callback.GetPersistentEventCount() == 0)
            {
                trigger.triggers.RemoveAt(i);
            }
        }

        if (trigger.triggers.Count == 0)
        {
            GameObject.Destroy(trigger);
        }
    }

    public bool hasBlockingWindow()
    {
        return _blockingWindows.Count != 0;
    }
}
