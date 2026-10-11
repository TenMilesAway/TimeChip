using System;
using UnityEngine;

/// <summary>从随机概率配置表读取游戏中的固定触发概率。</summary>
public static class GameProbability
{
    public static float Get(string probabilityId)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null || tables.RandomTable == null)
        {
            throw new InvalidOperationException("随机概率数据表尚未初始化。");
        }

        cfg.RandomProbability config = tables.RandomTable.GetOrDefault(probabilityId);
        if (config == null)
        {
            throw new InvalidOperationException($"随机概率配置不存在: [{probabilityId}]");
        }

        if (float.IsNaN(config.Probability) ||
            float.IsInfinity(config.Probability) ||
            config.Probability < 0f ||
            config.Probability > 1f)
        {
            throw new InvalidOperationException(
                $"随机概率配置超出范围 [0, 1]: [{probabilityId}], [{config.Probability}]");
        }

        return config.Probability;
    }
}
