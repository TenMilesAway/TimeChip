using System.Collections.Generic;
using cfg;

/// <summary>
/// 根据本局已完成任务，汇总游戏结算时可获得的回忆点数。
/// </summary>
public static class MissionSettlementMemory
{
    /// <summary>
    /// 统计已完成任务在配置表中的 <see cref="Mission.SettlementMemoryPoint"/> 之和。
    /// </summary>
    public static int SumForCompletedMissions(IReadOnlyList<string> completedMissionIds)
    {
        if (completedMissionIds == null || completedMissionIds.Count == 0)
        {
            return 0;
        }

        Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables?.MissionTable == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < completedMissionIds.Count; i++)
        {
            string missionIdText = completedMissionIds[i];
            if (string.IsNullOrWhiteSpace(missionIdText) ||
                !int.TryParse(missionIdText, out int missionId))
            {
                continue;
            }

            Mission missionConfig = tables.MissionTable.GetOrDefault(missionId);
            if (missionConfig == null || missionConfig.SettlementMemoryPoint <= 0)
            {
                continue;
            }

            total += missionConfig.SettlementMemoryPoint;
        }

        return total;
    }

    /// <summary>
    /// 将结算回忆点写入全局成长数据（含 Grow2 回忆加成）。
    /// </summary>
    public static void GrantForCompletedMissions(IReadOnlyList<string> completedMissionIds)
    {
        int total = SumForCompletedMissions(completedMissionIds);
        if (total <= 0)
        {
            return;
        }

        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        globalInfo.Init();
        globalInfo.AddMemoryPoints(total);
        globalInfo.Save();
    }
}
