public enum MissionRuntimeMessageType
{
    PlayerSnapshot,
    TimeCoinSpent
}

public struct MissionRuntimeMessage
{
    public MissionRuntimeMessageType messageType;
    public int currentAge;
    public int currentMonth;
    public int simulationCoins;
    public int spentTimeCoins;

    public static MissionRuntimeMessage CreateSnapshot(PlayerInfoManager playerInfoManager)
    {
        return new MissionRuntimeMessage
        {
            messageType = MissionRuntimeMessageType.PlayerSnapshot,
            currentAge = playerInfoManager.CurrentAge,
            currentMonth = playerInfoManager.CurrentMonth,
            simulationCoins = playerInfoManager.SimulationCoins,
            spentTimeCoins = 0
        };
    }

    public static MissionRuntimeMessage CreateTimeCoinSpent(int amount)
    {
        return new MissionRuntimeMessage
        {
            messageType = MissionRuntimeMessageType.TimeCoinSpent,
            spentTimeCoins = amount
        };
    }
}
