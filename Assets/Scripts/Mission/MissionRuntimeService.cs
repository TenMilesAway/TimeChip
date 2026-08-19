using RedSaw.MissionSystem;
using UnityEngine;

public sealed class MissionRuntimeService : Singleton<MissionRuntimeService>, IMissionSystemComponent<MissionRuntimeMessage>
{
    private MissionManager<MissionRuntimeMessage> _missionManager = new MissionManager<MissionRuntimeMessage>();
    private bool _isSubscribed;

    public void Init()
    {
        UnsubscribePlayerEvents();

        _missionManager = new MissionManager<MissionRuntimeMessage>();
        _missionManager.AddComponent(this);

        cfg.MissionTable missionTable = DataTableMananger.GetInstance().Tables.MissionTable;
        for (int i = 0; i < missionTable.DataList.Count; i++)
        {
            cfg.Mission missionConfig = missionTable.DataList[i];
            if (!MissionRuntimeFactory.TryCreatePrototype(missionConfig, out var prototype))
            {
                continue;
            }

            _missionManager.StartMission(prototype);
        }

        SubscribePlayerEvents();
        PushPlayerSnapshot();
        RemoveExpiredMissions(PlayerInfoManager.GetInstance());
    }

    public Mission<MissionRuntimeMessage>[] GetActiveMissions()
    {
        return _missionManager.GetMissions();
    }

    public void OnMissionStarted(Mission<MissionRuntimeMessage> mission)
    {
    }

    public void OnMissionRemoved(Mission<MissionRuntimeMessage> mission, bool isFinished)
    {
        if (!(mission.property is TableMissionProperty tableMissionProperty))
        {
            return;
        }

        cfg.Mission missionConfig = tableMissionProperty.missionConfig;
        if (isFinished)
        {
            CommonTipView.Show($"任务完成：{missionConfig.Name}");
            return;
        }

        if (MissionRuntimeFactory.IsMissionExpired(
                missionConfig,
                PlayerInfoManager.GetInstance().CurrentAge,
                PlayerInfoManager.GetInstance().CurrentMonth))
        {
            CommonTipView.Show($"任务过期：{missionConfig.Name}");
        }
    }

    public void OnMissionStatusChanged(Mission<MissionRuntimeMessage> mission, bool isFinished)
    {
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        _missionManager.SendMessage(MissionRuntimeMessage.CreateSnapshot(playerInfoManager));
        RemoveExpiredMissions(playerInfoManager);
    }

    private void OnTimeCoinsSpent(int amount)
    {
        _missionManager.SendMessage(MissionRuntimeMessage.CreateTimeCoinSpent(amount));
    }

    private void PushPlayerSnapshot()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        _missionManager.SendMessage(MissionRuntimeMessage.CreateSnapshot(playerInfoManager));
    }

    private void RemoveExpiredMissions(PlayerInfoManager playerInfoManager)
    {
        Mission<MissionRuntimeMessage>[] missions = _missionManager.GetMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            Mission<MissionRuntimeMessage> mission = missions[i];
            if (!(mission.property is TableMissionProperty tableMissionProperty))
            {
                continue;
            }

            if (!MissionRuntimeFactory.IsMissionExpired(
                    tableMissionProperty.missionConfig,
                    playerInfoManager.CurrentAge,
                    playerInfoManager.CurrentMonth))
            {
                continue;
            }

            _missionManager.RemoveMission(mission.id);
        }
    }

    private void SubscribePlayerEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        playerInfoManager.TimeCoinsSpent += OnTimeCoinsSpent;
        _isSubscribed = true;
    }

    private void UnsubscribePlayerEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.TimeCoinsSpent -= OnTimeCoinsSpent;
        _isSubscribed = false;
    }
}
