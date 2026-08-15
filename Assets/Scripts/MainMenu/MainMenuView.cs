using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player data in the main menu and handles turn progression.
/// </summary>
public class MainMenuView : UIBasePanel
{

    [SerializeField] private Text _ageText;
    [SerializeField] private Text _monthText;
    [SerializeField] private Text _healthText;
    [SerializeField] private Text _maxHealthText;
    [SerializeField] private Text _simulationCoinsText;
    [SerializeField] private Button _nextMonthButton;

    private void Awake()
    {
        _nextMonthButton.onClick.AddListener(AdvanceTurn);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        RefreshPlayerInfo(PlayerInfoManager.GetInstance());
    }

    protected override void ShowHandle()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshPlayerInfo;
        playerInfoManager.PlayerInfoChanged += RefreshPlayerInfo;
        RefreshPlayerInfo(playerInfoManager);
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        if (_nextMonthButton != null)
        {
            _nextMonthButton.onClick.RemoveListener(AdvanceTurn);
        }

        base.OnDestroy();
    }

    private void AdvanceTurn()
    {
        PlayerInfoManager.GetInstance().AdvanceTurn();
    }

    private void RefreshPlayerInfo(PlayerInfoManager playerInfoManager)
    {
        _ageText.text = playerInfoManager.CurrentAge.ToString();
        _monthText.text = playerInfoManager.CurrentMonth.ToString();
        _healthText.text = playerInfoManager.Health.ToString();
        _maxHealthText.text = playerInfoManager.MaxHealth.ToString();
        _simulationCoinsText.text = playerInfoManager.SimulationCoins.ToString();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.MainMenuView;
    }
}
