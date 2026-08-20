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
        else
        {
            Debug.LogWarning("[任务系统] 不支持的任务消息类型: " + missionConfig.Message);
            return false;
        }

        missionProto = new MissionPrototype<MissionMessage>(
            missionConfig.Id.ToString(),
            new MissionRequire<MissionMessage>[] { missionRequire },
            CreateRewards(missionConfig.Reward));
        return true;
    }

    private static MissionReward[] CreateRewards(string rewardText)
    {
        if (string.IsNullOrEmpty(rewardText))
        {
            return null;
        }

        string[] values = rewardText.Split(',');
        int simulationCoins = ParseNonNegativeInt(values, 0);
        int timeCoins = ParseNonNegativeInt(values, 1);
        int health = ParseNonNegativeInt(values, 2);
        if (simulationCoins == 0 && timeCoins == 0 && health == 0)
        {
            return null;
        }

        return new MissionReward[]
        {
            new MissionRewardCommon
            {
                simulationCoinAmount = simulationCoins,
                timeCoinAmount = timeCoins,
                healthAmount = health
            }
        };
    }

    private static int ParseNonNegativeInt(string[] values, int index)
    {
        return index < values.Length && int.TryParse(values[index], out int value)
            ? Mathf.Max(0, value)
            : 0;
    }
}
