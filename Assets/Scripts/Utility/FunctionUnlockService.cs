using UnityEngine;

/// <summary>根据功能解锁配置判断玩家是否可以使用指定功能。</summary>
public static class FunctionUnlockService
{
    public static bool IsUnlocked(string functionId)
    {
        if (string.IsNullOrWhiteSpace(functionId))
        {
            Debug.LogError("功能 ID 不能为空。");
            return false;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError($"功能解锁配置尚未初始化: [{functionId}]");
            return false;
        }

        cfg.Function functionConfig = tables.FunctionTable.GetOrDefault(functionId);
        if (functionConfig == null)
        {
            Debug.LogError($"功能解锁配置不存在: [{functionId}]");
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        bool missionRequirementMet = functionConfig.UnlockMissionId <= 0 ||
            playerInfoManager.HasCompletedMission(functionConfig.UnlockMissionId);
        bool itemRequirementMet = functionConfig.UnlockItemId <= 0 ||
            playerInfoManager.GetItemCount(functionConfig.UnlockItemId) > 0;
        return missionRequirementMet && itemRequirementMet;
    }
}
