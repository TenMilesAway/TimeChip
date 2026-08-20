using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedSaw.MissionSystem;

public class MissionRewardCommon : MissionReward
{
    public int simulationCoinAmount;
    public int timeCoinAmount;
    public int healthAmount;

    public override void ApplyReward()
    {
        List<CommonRewardItemData> data = new List<CommonRewardItemData>();

        AddBasePropertyReward(data, BasePropertyId.SimulationCoin, simulationCoinAmount);
        AddBasePropertyReward(data, BasePropertyId.TimeCoin, timeCoinAmount);
        AddBasePropertyReward(data, BasePropertyId.Health, healthAmount);

        if (data.Count == 0)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonRewardPanel, param: new OpenUIParam
        {
            data = data
        });
    }

    private static void AddBasePropertyReward(
        List<CommonRewardItemData> rewards,
        int propertyId,
        int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        rewards.Add(new CommonRewardItemData
        {
            itemId = propertyId,
            itemCount = amount
        });
    }
}