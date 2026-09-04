using System.Collections.Generic;
using System.Threading.Tasks;
using DS.ScriptableObjects;
using UnityEngine;

public static class MissionDialogueService
{
    private const string DialogueAssetPathFormat =
        "Assets/DialogueSystem/Dialogues/{0}/{0}.asset";
    private const string ResourceTag = "MissionDialogueService";

    private static readonly Dictionary<string, DSDialogueContainerSO> ContainerCache =
        new Dictionary<string, DSDialogueContainerSO>();

    public static void TryPlayStartDialogue(cfg.Mission missionConfig)
    {
        TryPlayDialogue(missionConfig, missionConfig?.DialogueStart, "开始");
    }

    public static void TryPlayEndDialogue(cfg.Mission missionConfig)
    {
        TryPlayDialogue(missionConfig, missionConfig?.DialogueEnd, "结束");
    }

    private static async void TryPlayDialogue(
        cfg.Mission missionConfig,
        string dialogueConfig,
        string phase)
    {
        if (missionConfig == null || string.IsNullOrEmpty(dialogueConfig))
        {
            return;
        }

        if (!TryParseDialoguePointer(dialogueConfig, out string fileName, out int groupIndex))
        {
            Debug.LogWarning(
                $"[任务系统] 任务[{missionConfig.Id}]对话配置格式错误({phase}): {dialogueConfig}");
            return;
        }

        DSDialogueContainerSO container = await LoadContainerAsync(fileName);
        if (container == null)
        {
            Debug.LogWarning(
                $"[任务系统] 任务[{missionConfig.Id}]无法加载对话文件({phase}): {fileName}");
            return;
        }

        List<MissionDialogueLineData> lines = BuildDialogueLines(container, groupIndex, out string groupName);
        if (lines.Count == 0)
        {
            Debug.LogWarning(
                $"[任务系统] 任务[{missionConfig.Id}]对话组为空({phase}): {fileName},{groupIndex}");
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.DialogueView,
            UILayer.System,
            new OpenUIParam
            {
                data = new MissionDialogueViewData
                {
                    title = groupName,
                    lines = lines
                }
            });
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
        out string groupName)
    {
        groupName = string.Empty;
        List<MissionDialogueLineData> lines = new List<MissionDialogueLineData>();
        if (container == null || container.DialogueGroups == null || container.DialogueGroups.Count == 0)
        {
            return lines;
        }

        List<DSDialogueGroupSO> groups = new List<DSDialogueGroupSO>(container.DialogueGroups.Keys);
        if (groupIndex < 0 || groupIndex >= groups.Count)
        {
            return lines;
        }

        DSDialogueGroupSO group = groups[groupIndex];
        groupName = group == null || string.IsNullOrEmpty(group.GroupName) ? container.FileName : group.GroupName;
        if (group == null ||
            !container.DialogueGroups.TryGetValue(group, out List<DSDialogueSO> dialogues) ||
            dialogues == null ||
            dialogues.Count == 0)
        {
            return lines;
        }

        DSDialogueSO startDialogue = dialogues.Find(item => item != null && item.IsStartingDialogue);
        if (startDialogue == null)
        {
            startDialogue = dialogues[0];
        }

        HashSet<DSDialogueSO> visited = new HashSet<DSDialogueSO>();
        DSDialogueSO current = startDialogue;
        while (current != null && visited.Add(current))
        {
            if (!string.IsNullOrEmpty(current.Text))
            {
                lines.Add(new MissionDialogueLineData
                {
                    speaker = current.Speaker,
                    expressionPath = current.SpeakerExpressionPath,
                    text = current.Text
                });
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
}
