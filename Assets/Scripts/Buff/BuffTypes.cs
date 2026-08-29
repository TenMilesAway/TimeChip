using System;

public enum BuffDurationType
{
    Instant,
    Turns,
    Permanent
}

public enum BuffStackRule
{
    RefreshDuration,
    AddStack,
    Replace,
    Ignore
}

[Serializable]
public sealed class ActiveBuffData
{
    public int buffId;
    public int remainingTurns;
    public int stacks = 1;
    public int sourceId;
}

public readonly struct WorkBuffResult
{
    public WorkBuffResult(int coinReward, int healthCost)
    {
        CoinReward = coinReward;
        HealthCost = healthCost;
    }

    public int CoinReward { get; }
    public int HealthCost { get; }
}
