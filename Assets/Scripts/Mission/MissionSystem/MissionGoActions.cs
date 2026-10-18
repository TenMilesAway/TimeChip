using RedSaw.MissionSystem;

public static class MissionGoActions
{
    public static bool ShouldShowGoButton(cfg.Mission missionConfig, Mission<MissionMessage> mission)
    {
        if (missionConfig == null || mission == null || mission.IsFinished)
        {
            return false;
        }

        return missionConfig.Message == "GameStamp";
    }

    public static bool TryRunGoAction(cfg.Mission missionConfig)
    {
        if (missionConfig == null)
        {
            return false;
        }

        if (missionConfig.Message == "GameStamp")
        {
            GameManager.Audio.Play(AudioDefine.SFXClick);
            UIManager.GetInstance().OpenPanel(GlobalDefine.GameStamp, UILayer.System);
            return true;
        }

        return false;
    }
}
