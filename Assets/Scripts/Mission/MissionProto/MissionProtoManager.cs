using RedSaw.MissionSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionProtoManager : Singleton<MissionProtoManager>
{
    /// <summary>根据任务 ID 创建对应原型，用于从存档恢复任务。</summary>
    public bool TryCreateMissionProto(
        string missionId,
        out MissionPrototype<MissionMessage> missionProto)
    {
        if (missionId == "Coin")
        {
            missionProto = CreateCoinProto();
            return true;
        }

        missionProto = null;
        return false;
    }

    public MissionPrototype<MissionMessage> CreateCoinProto()
    {
        var missionRequire = new MissionRequireCoin(1001);
        var missionReward = new MissionRewardCommon
        {
            simulationCoinAmount = 505,
            timeCoinAmount = 1,
        };
        var requires = new MissionRequire<MissionMessage>[] { missionRequire };
        var rewards = new MissionReward[] { missionReward };
        // 这里的 "Coin" 应该是 ID, 后续可以用表中的 ID 代替
        var missionProto = new MissionPrototype<MissionMessage>("Coin", requires, rewards);
        return missionProto;
    }
}
