using System;

public enum BuffDurationType
{
    Instant,
    Turns,
    Permanent
}

[Serializable]
public sealed class ActiveBuffData
{
    public int buffId;
    public int remainingTurns;
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
