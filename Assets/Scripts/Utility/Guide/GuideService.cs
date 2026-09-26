using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class GuideService
{
    private const string CompletionKeyPrefix = "TimeChip.Guide.";
    private const string WorkCenterTarget = "WorkCenter";
    private const string FirstWorkItemTarget = "FirstWorkItem";
    private const string MissionButtonTarget = "MissionButton";
    private const string FirstMissionRewardTarget = "FirstMissionReward";
    private const string LotteryButtonTarget = "LotteryButton";
    private const string SingleLotteryButtonTarget = "SingleLotteryButton";
    private const string CommunityButtonTarget = "CommunityButton";
    private const string HomeStoreButtonTarget = "HomeStoreButton";
    private const string SixthHomeStoreTagTarget = "SixthHomeStoreTag";
    private const string ThirdHomeStoreItemTarget = "ThirdHomeStoreItem";
    private const string HomePurchaseButtonTarget = "HomePurchaseButton";
    private const string HomeButtonTarget = "HomeButton";
    private const string HomeSatisfactionDetailButtonTarget = "HomeSatisfactionDetailButton";
    private const float PanelWaitTimeout = 5f;

    public static void TryRunMissionGuides(int missionId, Action onCompleted)
    {
        List<cfg.Guide> guides = new List<cfg.Guide>();
        IReadOnlyList<cfg.Guide> configuredGuides =
            DataTableMananger.GetInstance().Tables.GuideTable.DataList;
        for (int i = 0; i < configuredGuides.Count; i++)
        {
            cfg.Guide guide = configuredGuides[i];
            if (guide.MissionId == missionId && !IsCompleted(guide.Id))
            {
                guides.Add(guide);
            }
        }

        guides.Sort((left, right) =>
        {
            int stepComparison = left.Step.CompareTo(right.Step);
            return stepComparison != 0 ? stepComparison : left.Id.CompareTo(right.Id);
        });
        RunNextGuide(guides, 0, onCompleted);
    }

    private static void RunNextGuide(
        List<cfg.Guide> guides,
        int index,
        Action onCompleted)
    {
        if (index >= guides.Count)
        {
            onCompleted?.Invoke();
            return;
        }

        cfg.Guide guide = guides[index];
        StartGuide(guide, isCompleted =>
        {
            if (isCompleted)
            {
                MarkCompleted(guide.Id);
            }

            RunNextGuide(guides, index + 1, onCompleted);
        });
    }

    private static void StartGuide(cfg.Guide guide, Action<bool> onCompleted)
    {
        switch (guide.Target)
        {
            case WorkCenterTarget:
                ShowWorkCenterGuide(guide.Content, onCompleted);
                break;
            case FirstWorkItemTarget:
                ShowFirstWorkItemGuide(guide.Content, onCompleted);
                break;
            case MissionButtonTarget:
                ShowMissionButtonGuide(guide.Content, onCompleted);
                break;
            case FirstMissionRewardTarget:
                ShowFirstMissionRewardGuide(guide.Content, onCompleted);
                break;
            case LotteryButtonTarget:
                ShowLotteryButtonGuide(guide.Content, onCompleted);
                break;
            case SingleLotteryButtonTarget:
                ShowSingleLotteryButtonGuide(guide.Content, onCompleted);
                break;
            case CommunityButtonTarget:
                ShowCommunityButtonGuide(guide.Content, onCompleted);
                break;
            case HomeStoreButtonTarget:
                ShowHomeStoreButtonGuide(guide.Content, onCompleted);
                break;
            case SixthHomeStoreTagTarget:
                ShowSixthHomeStoreTagGuide(guide.Content, onCompleted);
                break;
            case ThirdHomeStoreItemTarget:
                ShowThirdHomeStoreItemGuide(guide.Content, onCompleted);
                break;
            case HomePurchaseButtonTarget:
                ShowHomePurchaseButtonGuide(guide.Content, onCompleted);
                break;
            case HomeButtonTarget:
                ShowHomeButtonGuide(guide.Content, onCompleted);
                break;
            case HomeSatisfactionDetailButtonTarget:
                ShowHomeSatisfactionDetailButtonGuide(guide.Content, onCompleted);
                break;
            default:
                Debug.LogError($"引导目标未实现: [{guide.Id}], [{guide.Target}]");
                onCompleted?.Invoke(false);
                break;
        }
    }

    private static async void ShowWorkCenterGuide(string instruction, Action<bool> onCompleted)
    {
        UIBasePanel panel = await UIManager.GetInstance().OpenPanelAsync(GlobalDefine.CommunityView);
        CommunityView communityView = panel as CommunityView;
        if (communityView == null || !communityView.TryGetWorkButton(out Button workButton))
        {
            Debug.LogError("无法启动零工中心引导。");
            onCompleted?.Invoke(false);
            return;
        }

        if (GuideMask.Show(workButton, instruction, () => onCompleted?.Invoke(true)) == null)
        {
            onCompleted?.Invoke(false);
        }
    }

    private static async void ShowFirstWorkItemGuide(string instruction, Action<bool> onCompleted)
    {
        WorkView workView = await GetOrOpenPanelAsync<WorkView>(GlobalDefine.WorkView);
        if (workView == null ||
            !workView.TryGetFirstWorkGuideTarget(
                out RectTransform guideTarget,
                out Button completionButton))
        {
            Debug.LogError("无法启动第一个零工的引导。");
            onCompleted?.Invoke(false);
            return;
        }

        if (GuideMask.Show(guideTarget, completionButton, instruction, () => onCompleted?.Invoke(true)) == null)
        {
            onCompleted?.Invoke(false);
        }
    }

    private static void ShowMissionButtonGuide(string instruction, Action<bool> onCompleted)
    {
        UIManager.GetInstance().ClosePanel(GlobalDefine.WorkView);
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null || !mainMenuView.TryGetMissionButton(out Button missionButton))
        {
            Debug.LogError("无法启动任务按钮引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(missionButton, instruction, onCompleted);
    }

    private static async void ShowFirstMissionRewardGuide(string instruction, Action<bool> onCompleted)
    {
        MissionView missionView = await GetOrOpenPanelAsync<MissionView>(GlobalDefine.MissionView);
        if (missionView == null || !missionView.TryGetFirstMissionClaimButton(out Button claimButton))
        {
            Debug.LogError("无法启动第一个任务领奖引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(claimButton, instruction, onCompleted);
    }

    private static void ShowLotteryButtonGuide(string instruction, Action<bool> onCompleted)
    {
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null || !mainMenuView.TryGetLotteryButton(out Button lotteryButton))
        {
            Debug.LogError("无法启动抽奖入口引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(lotteryButton, instruction, onCompleted);
    }

    private static async void ShowSingleLotteryButtonGuide(string instruction, Action<bool> onCompleted)
    {
        LotteryView lotteryView = await GetOrOpenPanelAsync<LotteryView>(GlobalDefine.LotteryView);
        if (lotteryView == null || !lotteryView.TryGetSingleLotteryButton(out Button lotteryButton))
        {
            Debug.LogError("无法启动单次抽奖引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(lotteryButton, instruction, onCompleted);
    }

    private static void ShowCommunityButtonGuide(string instruction, Action<bool> onCompleted)
    {
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null || !mainMenuView.TryGetCommunityButton(out Button communityButton))
        {
            Debug.LogError("无法启动社区按钮引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(communityButton, instruction, onCompleted);
    }

    private static async void ShowHomeStoreButtonGuide(string instruction, Action<bool> onCompleted)
    {
        CommunityView communityView = await GetOrOpenPanelAsync<CommunityView>(
            GlobalDefine.CommunityView);
        if (communityView == null ||
            !communityView.TryGetHomeStoreButton(out Button homeStoreButton))
        {
            Debug.LogError("无法启动家具店按钮引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(homeStoreButton, instruction, onCompleted);
    }

    private static async void ShowSixthHomeStoreTagGuide(string instruction, Action<bool> onCompleted)
    {
        HomeStoreView homeStoreView = await GetOrOpenPanelAsync<HomeStoreView>(
            GlobalDefine.HomeStoreView);
        if (homeStoreView == null ||
            !homeStoreView.TryGetTagButton(6, out Button tagButton))
        {
            Debug.LogError("无法启动家具店第六个分类标签引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(tagButton, instruction, onCompleted);
    }

    private static async void ShowThirdHomeStoreItemGuide(string instruction, Action<bool> onCompleted)
    {
        HomeStoreView homeStoreView = await GetOrOpenPanelAsync<HomeStoreView>(
            GlobalDefine.HomeStoreView);
        if (homeStoreView == null)
        {
            onCompleted?.Invoke(false);
            return;
        }

        float deadline = Time.realtimeSinceStartup + PanelWaitTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (homeStoreView.TryGetHomeItemGuideTarget(
                    3,
                    out RectTransform guideTarget,
                    out Button itemButton))
            {
                if (GuideMask.Show(
                        guideTarget,
                        itemButton,
                        instruction,
                        () => onCompleted?.Invoke(true)) == null)
                {
                    onCompleted?.Invoke(false);
                }

                return;
            }

            await Task.Yield();
        }

        Debug.LogError("等待家具店第三个商品加载超时。");
        onCompleted?.Invoke(false);
    }

    private static async void ShowHomePurchaseButtonGuide(string instruction, Action<bool> onCompleted)
    {
        HomeItemDetail detailView = await GetOrOpenPanelAsync<HomeItemDetail>(
            GlobalDefine.HomeItemDetail);
        if (detailView == null ||
            !detailView.TryGetPurchaseButton(out Button purchaseButton))
        {
            Debug.LogError("无法启动家具购买按钮引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(purchaseButton, instruction, onCompleted);
    }

    private static void ShowHomeButtonGuide(string instruction, Action<bool> onCompleted)
    {
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null || !mainMenuView.TryGetHomeButton(out Button homeButton))
        {
            Debug.LogError("无法启动小屋按钮引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(homeButton, instruction, onCompleted);
    }

    private static async void ShowHomeSatisfactionDetailButtonGuide(
        string instruction,
        Action<bool> onCompleted)
    {
        HomeView homeView = await GetOrOpenPanelAsync<HomeView>(GlobalDefine.HomeView);
        if (homeView == null || !homeView.TryGetHomeDetailButton(out Button detailButton))
        {
            Debug.LogError("无法启动满意度面板引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(detailButton, instruction, onCompleted);
    }

    private static void ShowButtonGuide(
        Button targetButton,
        string instruction,
        Action<bool> onCompleted)
    {
        if (GuideMask.Show(targetButton, instruction, () => onCompleted?.Invoke(true)) == null)
        {
            onCompleted?.Invoke(false);
        }
    }

    private static async Task<T> WaitForOpeningPanelAsync<T>(string panelName)
        where T : UIBasePanel
    {
        float deadline = Time.realtimeSinceStartup + PanelWaitTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            T panel = UIManager.GetInstance().GetOpeningPanel(panelName) as T;
            if (panel != null)
            {
                return panel;
            }

            await Task.Yield();
        }

        Debug.LogError($"等待引导面板超时: [{panelName}]");
        return null;
    }

    private static async Task<T> GetOrOpenPanelAsync<T>(string panelName)
        where T : UIBasePanel
    {
        T panel = UIManager.GetInstance().GetOpeningPanel(panelName) as T;
        if (panel != null)
        {
            return panel;
        }

        panel = await UIManager.GetInstance().OpenPanelAsync(panelName) as T;
        return panel ?? await WaitForOpeningPanelAsync<T>(panelName);
    }

    private static bool IsCompleted(int guideId)
    {
        return PlayerPrefs.GetInt(GetCompletionKey(guideId), 0) == 1;
    }

    private static void MarkCompleted(int guideId)
    {
        PlayerPrefs.SetInt(GetCompletionKey(guideId), 1);
        PlayerPrefs.Save();
    }

    private static string GetCompletionKey(int guideId)
    {
        return CompletionKeyPrefix + guideId;
    }
}
