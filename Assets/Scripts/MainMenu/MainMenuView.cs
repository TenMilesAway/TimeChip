using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenuView : UIBasePanel
{
    private const float AdvanceTurnConfirmDuration = 3f;


    [SerializeField] private Text _ageText;
    [SerializeField] private Text _monthText;
    [SerializeField] private Text _healthText;
    [SerializeField] private Text _maxHealthText;
    [SerializeField] private Text _simulationCoinsText;
    [SerializeField] private Image _simulationCoinIcon;
    [SerializeField] private Button _nextMonthButton;
    [SerializeField] private Button _communityButton;
    [SerializeField] private Button _lotteryButton;
    [SerializeField] private Button _missionButton;
    [SerializeField] private Button _homeButton;

    private MainContentPage _currentContentPage = MainContentPage.Community;    // 当前页面
    private bool _isWaitingForAdvanceTurnConfirmation;
    private float _advanceTurnConfirmDeadline;

    private void Awake()
    {
        _nextMonthButton.onClick.AddListener(TryAdvanceTurn);
        _communityButton.onClick.AddListener(OpenCommunity);
        _lotteryButton.onClick.AddListener(OpenLottery);
        _missionButton.onClick.AddListener(OpenMission);
        _homeButton.onClick.AddListener(OpenHome);
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
        _isWaitingForAdvanceTurnConfirmation = false;
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        if (_nextMonthButton != null)
        {
            _nextMonthButton.onClick.RemoveListener(TryAdvanceTurn);
        }

        if (_communityButton != null)
        {
            _communityButton.onClick.RemoveListener(OpenCommunity);
        }

        if (_lotteryButton != null)
        {
            _lotteryButton.onClick.RemoveListener(OpenLottery);
        }

        if (_missionButton != null)
        {
            _missionButton.onClick.RemoveListener(OpenMission);
        }

        if (_homeButton != null)
        {
            _homeButton.onClick.RemoveListener(OpenHome);
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 在三秒确认窗口内再次点击时才推进到下一月
    /// </summary>
    private void TryAdvanceTurn()
    {
        if (!_isWaitingForAdvanceTurnConfirmation ||
            Time.unscaledTime > _advanceTurnConfirmDeadline)
        {
            _isWaitingForAdvanceTurnConfirmation = true;
            _advanceTurnConfirmDeadline = Time.unscaledTime + AdvanceTurnConfirmDuration;
            CommonTipView.Show("再次点击进入下一月");
            return;
        }

        _isWaitingForAdvanceTurnConfirmation = false;
        PlayerInfoManager.GetInstance().AdvanceTurn(); // 推进下一月
    }

    #region 打开面板
    private void OpenCommunity()
    {
        NavigateTo(MainContentPage.Community);
    }

    private void OpenLottery()
    {
        NavigateTo(MainContentPage.Lottery);
    }

    private void OpenMission()
    {
        NavigateTo(MainContentPage.Mission);
    }

    private void OpenHome()
    {
        NavigateTo(MainContentPage.Home);
    }

    private void NavigateTo(MainContentPage targetPage)
    {
        if (_currentContentPage == targetPage)
        {
            return;
        }

        if (!TryGetPanelName(targetPage, out string targetPanelName))
        {
            return;
        }

        if (TryGetPanelName(_currentContentPage, out string currentPanelName))
        {
            UIManager.GetInstance().ClosePanel(currentPanelName);
        }

        UIManager.GetInstance().OpenPanel(targetPanelName);
        _currentContentPage = targetPage;
    }
    #endregion

    private static bool TryGetPanelName(MainContentPage page, out string panelName)
    {
        switch (page)
        {
            case MainContentPage.Community:
                panelName = GlobalDefine.CommunityView;
                return true;
            case MainContentPage.Lottery:
                panelName = GlobalDefine.LotteryView;
                return true;
            case MainContentPage.Home:
                panelName = GlobalDefine.HomeView;
                return true;
            default:
                panelName = null;
                return false;
        }
    }

    /// <summary>
    /// 更新用户数据
    /// </summary>
    private void RefreshPlayerInfo(PlayerInfoManager playerInfoManager)
    {
        _ageText.text = playerInfoManager.CurrentAge.ToString();
        _monthText.text = playerInfoManager.CurrentMonth.ToString();
        _healthText.text = playerInfoManager.Health.ToString();
        _maxHealthText.text = playerInfoManager.MaxHealth.ToString();
        _simulationCoinsText.text = playerInfoManager.SimulationCoins.ToString();
    }

    /// <summary>
    /// 平滑滚动显示模拟币数量, 用于飞币奖励抵达后的视觉反馈
    /// </summary>
    /// <param name="from">动画开始数值</param>
    /// <param name="to">动画目标数值</param>
    public void PlaySimulationCoinCountAnimation(int from, int to)
    {
        DOTween.Kill(_simulationCoinsText);
        DOVirtual.Int(from, to, 0.5f, value => _simulationCoinsText.text = value.ToString())
            .SetEase(Ease.OutQuad)
            .SetTarget(_simulationCoinsText);
    }

    /// <summary>
    /// 获取主界面模拟币图标，供飞币特效设为终点并播放抵达反馈。
    /// </summary>
    public Image SimulationCoinIcon
    {
        get { return _simulationCoinIcon; }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.MainMenuView;
    }
}

/// <summary>
/// 主界面可切换的内容页; 任务和小屋待对应面板实现后接入
/// </summary>
public enum MainContentPage
{
    Community,
    Lottery,
    Mission,
    Home
}
