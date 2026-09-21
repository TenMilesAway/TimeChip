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
                ShowWorkCenterGuide(onCompleted);
                break;
            case FirstWorkItemTarget:
                ShowFirstWorkItemGuide(onCompleted);
                break;
            case MissionButtonTarget:
                ShowMissionButtonGuide(onCompleted);
                break;
            case FirstMissionRewardTarget:
                ShowFirstMissionRewardGuide(onCompleted);
                break;
            default:
                Debug.LogError($"引导目标未实现: [{guide.Id}], [{guide.Target}]");
                onCompleted?.Invoke(false);
                break;
        }
    }

    private static async void ShowWorkCenterGuide(Action<bool> onCompleted)
    {
        UIBasePanel panel = await UIManager.GetInstance().OpenPanelAsync(GlobalDefine.CommunityView);
        CommunityView communityView = panel as CommunityView;
        if (communityView == null || !communityView.TryGetWorkButton(out Button workButton))
        {
            Debug.LogError("无法启动零工中心引导。");
            onCompleted?.Invoke(false);
            return;
        }

        if (GuideMask.Show(workButton, () => onCompleted?.Invoke(true)) == null)
        {
            onCompleted?.Invoke(false);
        }
    }

    private static async void ShowFirstWorkItemGuide(Action<bool> onCompleted)
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

        if (GuideMask.Show(guideTarget, completionButton, () => onCompleted?.Invoke(true)) == null)
        {
            onCompleted?.Invoke(false);
        }
    }

    private static void ShowMissionButtonGuide(Action<bool> onCompleted)
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

        ShowButtonGuide(missionButton, onCompleted);
    }

    private static async void ShowFirstMissionRewardGuide(Action<bool> onCompleted)
    {
        MissionView missionView = await GetOrOpenPanelAsync<MissionView>(GlobalDefine.MissionView);
        if (missionView == null || !missionView.TryGetFirstMissionClaimButton(out Button claimButton))
        {
            Debug.LogError("无法启动第一个任务领奖引导。");
            onCompleted?.Invoke(false);
            return;
        }

        ShowButtonGuide(claimButton, onCompleted);
    }

    private static void ShowButtonGuide(Button targetButton, Action<bool> onCompleted)
    {
        if (GuideMask.Show(targetButton, () => onCompleted?.Invoke(true)) == null)
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
