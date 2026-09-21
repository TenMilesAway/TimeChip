using System.Collections.Generic;
using UnityEngine;
using RedSaw.MissionSystem;

public class MissionRewardCommon : MissionReward
{
    public List<CommonRewardItemData> rewards;

    public override void ApplyReward()
    {
        if (rewards == null || rewards.Count == 0)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonRewardPanel, param: new OpenUIParam
        {
            data = rewards
        });
    }

    public static bool TryParseRewards(
        string rewardText,
        out List<CommonRewardItemData> rewards,
        out string errorMessage)
    {
        rewards = new List<CommonRewardItemData>();
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(rewardText))
        {
            return true;
        }

        string[] entries = rewardText.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            string[] values = entries[i].Split(',');
            if (values.Length != 2 ||
                !int.TryParse(values[0].Trim(), out int itemId) ||
                !int.TryParse(values[1].Trim(), out int itemCount) ||
                itemId <= 0 ||
                itemCount <= 0)
            {
                errorMessage = $"奖励条目格式无效: {entries[i]}";
                rewards.Clear();
                return false;
            }

            rewards.Add(new CommonRewardItemData
            {
                itemId = itemId,
                itemCount = itemCount
            });
        }

        if (rewards.Count == 0)
        {
            errorMessage = "奖励格式无效，请使用 ItemID,num;ItemID,num。";
            return false;
        }

        return true;
    }
}