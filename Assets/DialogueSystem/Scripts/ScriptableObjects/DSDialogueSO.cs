using System;
using System.Collections.Generic;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using Data;
    using Enumerations;

    public class DSDialogueSO : ScriptableObject
    {
        [field: SerializeField] public string DialogueName { get; set; }
        [field: SerializeField] [field: TextArea()] public string Text { get; set; }
        [field: SerializeField] public List<DSDialogueChoiceData> Choices { get; set; }
        [field: SerializeField] public DSDialogueType DialogueType { get; set; }
        [field: SerializeField] public DSDialogueSpeaker Speaker { get; set; }
        [field: SerializeField] public string SpeakerExpressionPath { get; set; }
        [field: SerializeField] public string Reward { get; set; }
        [field: SerializeField] public bool IsStartingDialogue { get; set; }

        public void Initialize(
            string dialogueName,
            string text,
            List<DSDialogueChoiceData> choices,
            DSDialogueType dialogueType,
            DSDialogueSpeaker speaker,
            string speakerExpressionPath,
            string reward,
            bool isStartingDialogue)
        {
            DialogueName = dialogueName;
            Text = text;
            Choices = choices;
            DialogueType = dialogueType;
            Speaker = speaker;
            SpeakerExpressionPath = speakerExpressionPath;
            Reward = reward;
            IsStartingDialogue = isStartingDialogue;
        }
    }
}

namespace DS.Data
{
    [Serializable]
    public class DSDialogueRewardData
    {
        public int ItemID;
        public int Amount;
    }

    public static class DSDialogueRewardParser
    {
        public static bool TryParse(
            string value,
            out List<DSDialogueRewardData> rewards,
            out string errorMessage)
        {
            rewards = new List<DSDialogueRewardData>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string[] entries = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] values = entries[i].Split(',');
                if (values.Length != 2 ||
                    !int.TryParse(values[0].Trim(), out int itemID) ||
                    !int.TryParse(values[1].Trim(), out int amount) ||
                    itemID <= 0 ||
                    amount <= 0)
                {
                    errorMessage = $"奖励条目格式无效: {entries[i]}";
                    rewards.Clear();
                    return false;
                }

                rewards.Add(new DSDialogueRewardData
                {
                    ItemID = itemID,
                    Amount = amount
                });
            }

            if (rewards.Count == 0)
            {
                errorMessage = "奖励格式无效，请使用 itemID,num;itemID,num。";
                return false;
            }

            return true;
        }
    }
}