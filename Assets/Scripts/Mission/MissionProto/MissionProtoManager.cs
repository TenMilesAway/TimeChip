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
        return TryCreateMissionProto(missionConfig, 0, 0, out missionProto);
    }

    /// <summary>根据固定后的随机目标创建任务原型，用于保证读档后任务目标不变。</summary>
    public bool TryCreateMissionProto(
        cfg.Mission missionConfig,
        int targetItemId,
        int targetItemCount,
        out MissionPrototype<MissionMessage> missionProto)
    {
        missionProto = null;
        if (missionConfig == null)
        {
            return false;
        }

        bool isItemMission = missionConfig.Message == "Item";
        bool isFishCatchMission = missionConfig.Message == "FishCatch";
        int target = 0;
        if ((!isItemMission && !isFishCatchMission &&
             !int.TryParse(missionConfig.Target, out target)) ||
            (isItemMission && (targetItemId <= 0 || targetItemCount <= 0)))
        {
            Debug.LogWarning("[任务系统] 任务目标必须是正整数: " + missionConfig?.Id);
            return false;
        }

        MissionRequire<MissionMessage> missionRequire;
        MissionRequire<MissionMessage>[] missionRequires = null;
        if (missionConfig.Message == "Coin")
        {
            missionRequire = new MissionRequireCoin(target);
        }
        else if (missionConfig.Message == "SimulationCoinBalance")
        {
            missionRequire = new MissionRequireSimulationCoinBalance(target);
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
        else if (missionConfig.Message == "MysteryWheelLottery")
        {
            missionRequire = new MissionRequireMysteryWheelLottery(target);
        }
        else if (missionConfig.Message == "HomePurchase" && missionConfig.Icon > 0)
        {
            missionRequire = new MissionRequireHomePurchase(missionConfig.Icon, target);
        }
        else if (missionConfig.Message == "HomePurchaseAny")
        {
            missionRequire = new MissionRequireHomePurchase(target);
        }
        else if (missionConfig.Message == "HomeSatisfactionView")
        {
            missionRequire = new MissionRequireHomeSatisfactionView(target);
        }
        else if (isFishCatchMission)
        {
            if (targetItemId > 0 && targetItemCount > 0)
            {
                cfg.Item fishItem = DataTableMananger.GetInstance()
                    .Tables
                    .ItemTable
                    .GetOrDefault(targetItemId);
                if (fishItem == null || fishItem.InventoryType != 2 ||
                    targetItemId == 6011)
                {
                    Debug.LogWarning(
                        $"[任务系统] 随机鱼获任务目标无效: [{missionConfig.Id}], [{targetItemId}]");
                    return false;
                }

                missionRequires = new MissionRequire<MissionMessage>[]
                {
                    new MissionRequireFishCatch(targetItemId, targetItemCount)
                };
            }
            else if (!TryCreateFishCatchRequires(
                         missionConfig.Target,
                         out missionRequires))
            {
                return false;
            }

            missionRequire = null;
        }
        else if (isItemMission)
        {
            bool isSubmitTarget = string.Equals(
                missionConfig.TargetType,
                "submit",
                System.StringComparison.OrdinalIgnoreCase);
            missionRequire = new MissionRequireItem(
                targetItemId,
                targetItemCount,
                !isSubmitTarget);
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
            missionRequires ?? new MissionRequire<MissionMessage>[] { missionRequire },
            rewards);
        return true;
    }

    private static bool TryCreateFishCatchRequires(
        string targetText,
        out MissionRequire<MissionMessage>[] missionRequires)
    {
        missionRequires = null;
        if (string.IsNullOrWhiteSpace(targetText))
        {
            Debug.LogWarning("[任务系统] 鱼获任务没有配置目标鱼种。");
            return false;
        }

        string[] entries = targetText.Split(';');
        List<MissionRequire<MissionMessage>> requires =
            new List<MissionRequire<MissionMessage>>(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            string[] values = entries[i].Split(',');
            if (values.Length != 2 ||
                !int.TryParse(values[0], out int fishItemId) ||
                !int.TryParse(values[1], out int targetCount) ||
                fishItemId <= 0 ||
                targetCount <= 0 ||
                fishItemId == 6011)
            {
                Debug.LogWarning(
                    $"[任务系统] 鱼获任务目标配置无效: [{entries[i]}]");
                return false;
            }

            cfg.Item fishItem = DataTableMananger.GetInstance()
                .Tables
                .ItemTable
                .GetOrDefault(fishItemId);
            if (fishItem == null || fishItem.InventoryType != 2)
            {
                Debug.LogWarning(
                    $"[任务系统] 鱼获任务目标必须是钓鱼背包道具: [{fishItemId}]");
                return false;
            }

            requires.Add(new MissionRequireFishCatch(fishItemId, targetCount));
        }

        missionRequires = requires.ToArray();
        return missionRequires.Length > 0;
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
