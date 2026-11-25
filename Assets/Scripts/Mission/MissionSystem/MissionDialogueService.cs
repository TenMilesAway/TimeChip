using System.Collections.Generic;
using System.Threading.Tasks;
using DS.Data;
using DS.Enumerations;
using DS.ScriptableObjects;
using UnityEngine;

public static class MissionDialogueService
{
    private const string DialogueAssetPathFormat =
        "Assets/DialogueSystem/Dialogues/{0}/{0}.asset";
    private const string ResourceTag = "MissionDialogueService";

    private static readonly Dictionary<string, DSDialogueContainerSO> ContainerCache =
        new Dictionary<string, DSDialogueContainerSO>();
    private static readonly Queue<DialogueRequest> DialogueQueue = new Queue<DialogueRequest>();

    private static bool isLoadingDialogue;
    private static bool isPlayingDialogue;
    private static bool isPlayingGuide;

    public static void TryPlayStartDialogue(cfg.Mission missionConfig)
    {
        EnqueueDialogue(missionConfig, missionConfig?.DialogueStart, "开始");
    }

    public static void TryPlayEndDialogue(cfg.Mission missionConfig)
    {
        EnqueueDialogue(missionConfig, missionConfig?.DialogueEnd, "结束");
    }

    public static bool TryPlayOneOffDialogue(
        cfg.Mission missionConfig,
        string dialogueConfig,
        System.Action<bool> onCompleted)
    {
        if (missionConfig == null || string.IsNullOrWhiteSpace(dialogueConfig))
        {
            return false;
        }

        return EnqueueStandaloneDialogue(missionConfig, dialogueConfig, "临时", onCompleted);
    }

    public static bool TryPlayStandaloneDialogue(
        string dialogueConfig,
        System.Action<bool> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(dialogueConfig))
        {
            return false;
        }

        return EnqueueStandaloneDialogue(null, dialogueConfig, "独立", onCompleted);
    }

    private static bool EnqueueStandaloneDialogue(
        cfg.Mission missionConfig,
        string dialogueConfig,
        string phase,
        System.Action<bool> onCompleted)
    {
        DialogueQueue.Enqueue(new DialogueRequest
        {
            MissionConfig = missionConfig,
            DialogueConfig = dialogueConfig,
            Phase = phase,
            OnCompleted = onCompleted
        });
        PlayNextDialogue();
        return true;
    }

    public static void TryPlayMissionGuides(cfg.Mission missionConfig)
    {
        if (missionConfig == null)
        {
            return;
        }

        DialogueQueue.Enqueue(new DialogueRequest
        {
            MissionConfig = missionConfig,
            DialogueConfig = string.Empty,
            Phase = "引导"
        });
        PlayNextDialogue();
    }

    private static void EnqueueDialogue(
        cfg.Mission missionConfig,
        string dialogueConfig,
        string phase)
    {
        if (missionConfig == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(dialogueConfig) && phase != "开始")
        {
            return;
        }

        DialogueQueue.Enqueue(new DialogueRequest
        {
            MissionConfig = missionConfig,
            DialogueConfig = dialogueConfig,
            Phase = phase
        });
        PlayNextDialogue();
    }

    private static async void PlayNextDialogue()
    {
        if (isLoadingDialogue || isPlayingDialogue || isPlayingGuide)
        {
            return;
        }

        while (DialogueQueue.Count > 0)
        {
            DialogueRequest request = DialogueQueue.Dequeue();
            if (string.IsNullOrEmpty(request.DialogueConfig))
            {
                isPlayingGuide = true;
                GuideService.TryRunMissionGuides(
                    request.MissionConfig.Id,
                    OnMissionGuidesCompleted);
                return;
            }

            if (!TryParseDialoguePointer(
                    request.DialogueConfig,
                    out string fileName,
                    out int groupIndex))
            {
                Debug.LogWarning(
                    $"[任务系统] 任务[{FormatMissionId(request.MissionConfig)}]对话配置格式错误({request.Phase}): {request.DialogueConfig}");
                request.OnCompleted?.Invoke(false);
                continue;
            }

            isLoadingDialogue = true;
            DSDialogueContainerSO container = await LoadContainerAsync(fileName);
            isLoadingDialogue = false;
            if (container == null)
            {
                Debug.LogWarning(
                    $"[任务系统] 任务[{FormatMissionId(request.MissionConfig)}]无法加载对话文件({request.Phase}): {fileName}");
                request.OnCompleted?.Invoke(false);
                if (TryStartGuidesAfterSkippedStartDialogue(request))
                {
                    return;
                }

                continue;
            }

            DialoguePlaybackContext playbackContext = new DialoguePlaybackContext();
            List<MissionDialogueLineData> lines =
                BuildDialogueLines(container, groupIndex, playbackContext, out string groupName);
            if (lines.Count == 0)
            {
                Debug.LogWarning(
                    $"[任务系统] 任务[{FormatMissionId(request.MissionConfig)}]对话组为空({request.Phase}): {fileName},{groupIndex}");
                request.OnCompleted?.Invoke(false);
                if (TryStartGuidesAfterSkippedStartDialogue(request))
                {
                    return;
                }

                continue;
            }

            isPlayingDialogue = true;
            UIBasePanel dialogueView = await UIManager.GetInstance().OpenPanelAsync(
                GlobalDefine.DialogueView,
                UILayer.System,
                new OpenUIParam
                {
                    data = new MissionDialogueViewData
                    {
                        title = groupName,
                        lines = lines,
                        onSelectChoice = choiceIndex =>
                            ResolveChoiceLines(playbackContext, choiceIndex),
                        onApplySelectTarget = request.MissionConfig == null
                            ? null
                            : selectTargetIndex => MissionAPI.TryApplyMissionSelectTarget(
                                request.MissionConfig.Id.ToString(),
                                selectTargetIndex),
                        onCompleted = () => OnDialogueCompleted(request)
                    }
                });
            if (dialogueView != null)
            {
                return;
            }

            isPlayingDialogue = false;
            if (TryStartGuidesAfterSkippedStartDialogue(request))
            {
                return;
            }

            request.OnCompleted?.Invoke(false);
        }
    }

    private static void OnDialogueCompleted(DialogueRequest request)
    {
        isPlayingDialogue = false;
        if (request.Phase == "开始")
        {
            isPlayingGuide = true;
            GuideService.TryRunMissionGuides(
                request.MissionConfig.Id,
                OnMissionGuidesCompleted);
            return;
        }

        request.OnCompleted?.Invoke(true);
        PlayNextDialogue();
    }

    private static void OnMissionGuidesCompleted()
    {
        isPlayingGuide = false;
        PlayNextDialogue();
    }

    private static bool TryStartGuidesAfterSkippedStartDialogue(DialogueRequest request)
    {
        if (request.Phase != "开始")
        {
            return false;
        }

        isPlayingGuide = true;
        GuideService.TryRunMissionGuides(
            request.MissionConfig.Id,
            OnMissionGuidesCompleted);
        return true;
    }

    private static string FormatMissionId(cfg.Mission missionConfig)
    {
        return missionConfig == null ? "独立" : missionConfig.Id.ToString();
    }

    private static bool TryParseDialoguePointer(string value, out string fileName, out int groupIndex)
    {
        fileName = null;
        groupIndex = -1;

        string[] parts = value.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        fileName = parts[0].Trim();
        return !string.IsNullOrEmpty(fileName) &&
            int.TryParse(parts[1].Trim(), out groupIndex) &&
            groupIndex >= 0;
    }

    private static async Task<DSDialogueContainerSO> LoadContainerAsync(string fileName)
    {
        if (ContainerCache.TryGetValue(fileName, out DSDialogueContainerSO cachedContainer) &&
            cachedContainer != null)
        {
            return cachedContainer;
        }

        if (GameManager.Resource == null)
        {
            Debug.LogError(
                $"[任务系统] ResourceComponent 未初始化，无法加载对话 Addressable：{fileName}");
            return null;
        }

        string assetPath = string.Format(DialogueAssetPathFormat, fileName);
        DSDialogueContainerSO container =
            await GameManager.Resource.LoadResource<DSDialogueContainerSO>(assetPath, ResourceTag);
        if (container == null)
        {
            Debug.LogError($"[任务系统] 未找到对话 Addressable：{assetPath}");
            return null;
        }

        ContainerCache[fileName] = container;
        return container;
    }

    private static List<MissionDialogueLineData> BuildDialogueLines(
        DSDialogueContainerSO container,
        int groupIndex,
        DialoguePlaybackContext playbackContext,
        out string groupName)
    {
        groupName = string.Empty;
        if (container == null || container.DialogueGroups == null || container.DialogueGroups.Count == 0)
        {
            return new List<MissionDialogueLineData>();
        }

        List<DSDialogueGroupSO> groups = new List<DSDialogueGroupSO>(container.DialogueGroups.Keys);
        if (groupIndex < 0 || groupIndex >= groups.Count)
        {
            return new List<MissionDialogueLineData>();
        }

        DSDialogueGroupSO group = groups[groupIndex];
        groupName = group == null || string.IsNullOrEmpty(group.GroupName) ? container.FileName : group.GroupName;
        if (group == null ||
            !container.DialogueGroups.TryGetValue(group, out List<DSDialogueSO> dialogues) ||
            dialogues == null ||
            dialogues.Count == 0)
        {
            return new List<MissionDialogueLineData>();
        }

        DSDialogueSO startDialogue = dialogues.Find(item => item != null && item.IsStartingDialogue);
        if (startDialogue == null)
        {
            startDialogue = dialogues[0];
        }

        return BuildDialogueLineSegment(startDialogue, playbackContext);
    }

    private static List<MissionDialogueLineData> ResolveChoiceLines(
        DialoguePlaybackContext playbackContext,
        int choiceIndex)
    {
        if (playbackContext == null)
        {
            return new List<MissionDialogueLineData>();
        }

        DSDialogueSO branchNode = playbackContext.CurrentBranchNode;
        playbackContext.CurrentBranchNode = null;
        if (branchNode == null ||
            branchNode.Choices == null ||
            choiceIndex < 0 ||
            choiceIndex >= branchNode.Choices.Count)
        {
            return new List<MissionDialogueLineData>();
        }

        DSDialogueSO nextDialogue = branchNode.Choices[choiceIndex]?.NextDialogue;
        return BuildDialogueLineSegment(nextDialogue, playbackContext);
    }

    private static List<MissionDialogueLineData> BuildDialogueLineSegment(
        DSDialogueSO startDialogue,
        DialoguePlaybackContext playbackContext)
    {
        List<MissionDialogueLineData> lines = new List<MissionDialogueLineData>();
        if (startDialogue == null || playbackContext == null)
        {
            return lines;
        }

        DSDialogueSO current = startDialogue;
        while (current != null && playbackContext.Visited.Add(current))
        {
            if (!DSDialogueRewardParser.TryParse(
                    current.Reward,
                    out List<DSDialogueRewardData> rewards,
                    out string rewardError))
            {
                Debug.LogError(
                    $"[对话系统] 对话节点[{current.DialogueName}]奖励配置无效: {rewardError}");
                rewards = new List<DSDialogueRewardData>();
            }

            List<CommonRewardItemData> convertedRewards =
                ConvertRewards(current.DialogueName, rewards);
            List<MissionDialogueChoiceOptionData> choiceOptions =
                BuildChoiceOptions(current);
            bool isBranchNode = IsBranchDialogueNode(current);
            if (!string.IsNullOrEmpty(current.Text) ||
                convertedRewards.Count > 0 ||
                (isBranchNode && choiceOptions.Count > 0))
            {
                lines.Add(new MissionDialogueLineData
                {
                    speaker = current.Speaker,
                    expressionPath = current.SpeakerExpressionPath,
                    text = current.Text,
                    rewards = convertedRewards,
                    choices = isBranchNode ? choiceOptions : null
                });
            }

            if (isBranchNode)
            {
                playbackContext.CurrentBranchNode = current;
                break;
            }

            if (current.Choices == null ||
                current.Choices.Count == 0 ||
                current.Choices[0] == null)
            {
                break;
            }

            current = current.Choices[0].NextDialogue;
        }

        return lines;
    }

    private static bool IsBranchDialogueNode(DSDialogueSO dialogue)
    {
        return dialogue != null &&
            dialogue.DialogueType == DSDialogueType.MultipleChoice &&
            dialogue.Choices != null &&
            dialogue.Choices.Count > 0;
    }

    private static List<MissionDialogueChoiceOptionData> BuildChoiceOptions(DSDialogueSO dialogue)
    {
        List<MissionDialogueChoiceOptionData> choiceOptions = new List<MissionDialogueChoiceOptionData>();
        if (dialogue?.Choices == null)
        {
            return choiceOptions;
        }

        for (int i = 0; i < dialogue.Choices.Count && choiceOptions.Count < 3; i++)
        {
            DSDialogueChoiceData choice = dialogue.Choices[i];
            if (choice == null || string.IsNullOrEmpty(choice.Text))
            {
                continue;
            }

            choiceOptions.Add(new MissionDialogueChoiceOptionData
            {
                text = choice.Text,
                selectTargetIndex = choice.NextDialogue != null &&
                    choice.NextDialogue.BindSelectTarget
                    ? choice.NextDialogue.SelectTargetIndex
                    : -1
            });
        }

        return choiceOptions;
    }

    private static List<CommonRewardItemData> ConvertRewards(
        string dialogueName,
        List<DSDialogueRewardData> rewards)
    {
        List<CommonRewardItemData> convertedRewards = new List<CommonRewardItemData>();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        for (int i = 0; i < rewards.Count; i++)
        {
            DSDialogueRewardData reward = rewards[i];
            if (tables.BaseTable.GetOrDefault(reward.ItemID) == null &&
                tables.ItemTable.GetOrDefault(reward.ItemID) == null)
            {
                Debug.LogError(
                    $"[对话系统] 对话节点[{dialogueName}]奖励物品不存在: [{reward.ItemID}]");
                continue;
            }

            convertedRewards.Add(new CommonRewardItemData
            {
                itemId = reward.ItemID,
                itemCount = reward.Amount
            });
        }

        return convertedRewards;
    }

    private sealed class DialoguePlaybackContext
    {
        public HashSet<DSDialogueSO> Visited = new HashSet<DSDialogueSO>();
        public DSDialogueSO CurrentBranchNode;
    }

    private sealed class DialogueRequest
    {
        public cfg.Mission MissionConfig;
        public string DialogueConfig;
        public string Phase;
        public System.Action<bool> OnCompleted;
    }
}
