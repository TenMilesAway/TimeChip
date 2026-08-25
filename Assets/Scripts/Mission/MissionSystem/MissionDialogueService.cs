using System.Collections.Generic;
using DS.ScriptableObjects;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MissionDialogueService
{
    private const string DialoguePanelPathFormat = "Assets/DialogueSystem/Dialogues/{0}/{0}.asset";

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

    private static void TryPlayDialogue(cfg.Mission missionConfig, string dialogueConfig, string phase)
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

        if (!TryLoadContainer(fileName, out DSDialogueContainerSO container) || container == null)
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

    private static bool TryLoadContainer(string fileName, out DSDialogueContainerSO container)
    {
        if (ContainerCache.TryGetValue(fileName, out container) && container != null)
        {
            return true;
        }

        container = Resources.Load<DSDialogueContainerSO>(fileName);
        if (container == null)
        {
            container = Resources.Load<DSDialogueContainerSO>(
                $"DialogueSystem/Dialogues/{fileName}/{fileName}");
        }

#if UNITY_EDITOR
        if (container == null)
        {
            string assetPath = string.Format(DialoguePanelPathFormat, fileName);
            container = AssetDatabase.LoadAssetAtPath<DSDialogueContainerSO>(assetPath);
        }
#endif

        if (container == null)
        {
            return false;
        }

        ContainerCache[fileName] = container;
        return true;
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
