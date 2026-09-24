using RedSaw.MissionSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionProtoManager : Singleton<MissionProtoManager>
{
    /// <summary>根据任务 ID 创建对应原型，用于启动和恢复任务。</summary>
    public bool TryCreateMissionProto(
        string missionId,
        out MissionPrototype<MissionMessage> missionProto)
    {
        missionProto = null;
        if (!int.TryParse(missionId, out int missionIdValue))
        {
            return false;
        }

        cfg.Mission missionConfig = DataTableMananger.GetInstance()
            .Tables
            .MissionTable
            .GetOrDefault(missionIdValue);
        return TryCreateMissionProto(missionConfig, out missionProto);
    }

    /// <summary>根据 Luban 任务配置创建任务原型。</summary>
    public bool TryCreateMissionProto(
        cfg.Mission missionConfig,
        out MissionPrototype<MissionMessage> missionProto)
    {
        missionProto = null;
        if (missionConfig == null || !int.TryParse(missionConfig.Target, out int target))
        {
            Debug.LogWarning("[任务系统] 任务目标必须是正整数: " + missionConfig?.Id);
            return false;
        }

        MissionRequire<MissionMessage> missionRequire;
        if (missionConfig.Message == "Coin")
        {
            missionRequire = new MissionRequireCoin(target);
        }
        else if (missionConfig.Message == "Health")
        {
            missionRequire = new MissionRequireHealth(target);
        }
        else if (missionConfig.Message == "Work")
        {
            missionRequire = new MissionRequireWork(target);
        }
        else if (missionConfig.Message == "NormalLottery")
        {
            missionRequire = new MissionRequireNormalLottery(target);
        }
        else if (missionConfig.Message == "HomePurchase" && missionConfig.Icon > 0)
        {
            missionRequire = new MissionRequireHomePurchase(missionConfig.Icon, target);
        }
        else
        {
            Debug.LogWarning("[任务系统] 不支持的任务消息类型: " + missionConfig.Message);
            return false;
        }

        if (!TryCreateRewards(missionConfig.Reward, out MissionReward[] rewards))
        {
            Debug.LogWarning(
                $"[任务系统] 任务奖励配置无效: [{missionConfig.Id}], [{missionConfig.Reward}]");
            return false;
        }

        missionProto = new MissionPrototype<MissionMessage>(
            missionConfig.Id.ToString(),
            new MissionRequire<MissionMessage>[] { missionRequire },
            rewards);
        return true;
    }

    private static bool TryCreateRewards(
        string rewardText,
        out MissionReward[] missionRewards)
    {
        missionRewards = null;
        if (!MissionRewardCommon.TryParseRewards(
                rewardText,
                out List<CommonRewardItemData> rewards,
                out string errorMessage))
        {
            Debug.LogWarning($"[任务系统] {errorMessage}");
            return false;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        for (int i = 0; i < rewards.Count; i++)
        {
            CommonRewardItemData reward = rewards[i];
            if (tables.BaseTable.GetOrDefault(reward.itemId) == null &&
                tables.ItemTable.GetOrDefault(reward.itemId) == null)
            {
                Debug.LogWarning($"[任务系统] 奖励物品不存在: [{reward.itemId}]");
                return false;
            }
        }

        if (rewards.Count == 0)
        {
            return true;
        }

        missionRewards = new MissionReward[]
        {
            new MissionRewardCommon
            {
                rewards = rewards
            }
        };
        return true;
    }
}
