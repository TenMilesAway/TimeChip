using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using RedSaw.MissionSystem;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private Button _settingButton;

    [SerializeField] private Transform _buffParent;
    [SerializeField] private GameObject _goLoad;
    [SerializeField] private GameObject _goBottom;
    [SerializeField] private GameObject _goInventory;

    #region 任务
    [Space(10)]
    [SerializeField] private Button _btnMove;             // 移动面板按钮
    [SerializeField] private Button _btnShowMission;      // 展示任务按钮
    [SerializeField] private Transform _missionParent;    // 任务列表挂载节点
    [SerializeField] private RectTransform _missionPanel; // 任务面板
    [SerializeField] private GameObject _goMissions;      // 任务列表
    [SerializeField] private GameObject _goTip;           // 文本提示
    #endregion

    private readonly List<BuffItem> _buffItems = new List<BuffItem>();
    private readonly List<MainMenuMissionItem> _missionItems =
        new List<MainMenuMissionItem>();

    private MainContentPage _currentContentPage = MainContentPage.Community;    // 当前页面
    private bool _isWaitingForAdvanceTurnConfirmation;
    private bool _isNavigating;
    private float _advanceTurnConfirmDeadline;
    private int _buffRefreshVersion;
    private int _missionRefreshVersion;
    private Vector2 _moveOffset;
    private UnityAction<BaseEventData> _beginMoveAction;
    private UnityAction<BaseEventData> _moveAction;
    private readonly Vector3[] _missionPanelCorners = new Vector3[4];

    private void Awake()
    {
        SetLoadingVisible(false);
        _nextMonthButton.onClick.AddListener(TryAdvanceTurn);
        _communityButton.onClick.AddListener(OpenCommunity);
        _lotteryButton.onClick.AddListener(OpenLottery);
        _missionButton.onClick.AddListener(OpenMission);
        _homeButton.onClick.AddListener(OpenHome);
        _inventoryButton.onClick.AddListener(OpenInventory);
        _settingButton.onClick.AddListener(OpenSetting);
        _btnShowMission.onClick.AddListener(ToggleMissions);

        _beginMoveAction = BeginMoveMissionPanel;
        _moveAction = MoveMissionPanel;
        UIManager.GetInstance().AddCustomEventListener(
            _btnMove, EventTriggerType.BeginDrag, _beginMoveAction);
        UIManager.GetInstance().AddCustomEventListener(
            _btnMove, EventTriggerType.Drag, _moveAction);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        _currentContentPage = MainContentPage.Community;
        _isNavigating = false;
        RefreshPlayerInfo(PlayerInfoManager.GetInstance());
        SetMissionsVisible(false);
    }

    protected override void ShowHandle()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshPlayerInfo;
        playerInfoManager.PlayerInfoChanged += RefreshPlayerInfo;
        playerInfoManager.PlayerInfoChanged -= OnMissionDataChanged;
        playerInfoManager.PlayerInfoChanged += OnMissionDataChanged;
        RefreshPlayerInfo(playerInfoManager);
        RefreshMissionItems();

        BuffSystem buffSystem = BuffSystem.GetInstance();
        buffSystem.BuffsChanged -= RefreshBuffItems;
        buffSystem.BuffsChanged += RefreshBuffItems;
        RefreshBuffItems();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnMissionDataChanged;
        BuffSystem.GetInstance().BuffsChanged -= RefreshBuffItems;
        ClearBuffItems();
        ClearMissionItems();
        _isWaitingForAdvanceTurnConfirmation = false;
        SetLoadingVisible(false);
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnMissionDataChanged;
        BuffSystem.GetInstance().BuffsChanged -= RefreshBuffItems;
        ClearBuffItems();
        ClearMissionItems();
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

        if (_inventoryButton != null)
        {
            _inventoryButton.onClick.RemoveListener(OpenInventory);
        }

        if (_settingButton != null)
        {
            _settingButton.onClick.RemoveListener(OpenSetting);
        }

        if (_btnShowMission != null)
        {
            _btnShowMission.onClick.RemoveListener(ToggleMissions);
        }

        if (_btnMove != null)
        {
            UIManager.GetInstance().RemoveCustomEventListener(
                _btnMove, EventTriggerType.BeginDrag, _beginMoveAction);
            UIManager.GetInstance().RemoveCustomEventListener(
                _btnMove, EventTriggerType.Drag, _moveAction);
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 在三秒确认窗口内再次点击时才推进到下一月
    /// </summary>
    private void TryAdvanceTurn()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);

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

    private void OpenInventory()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.InventoryView);
    }

    private void OpenSetting()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.SettingView,
            param: new OpenUIParam { data = true });
    }

    /// <summary>
    /// 设置会遮挡主菜单的全屏页面打开时需要隐藏的导航区域。
    /// </summary>
    public void SetNavigationVisible(bool visible)
    {
        if (_goBottom != null)
        {
            _goBottom.SetActive(visible);
        }

        if (_goInventory != null)
        {
            _goInventory.SetActive(visible);
        }
    }

    private async void NavigateTo(MainContentPage targetPage)
    {
        if (_isNavigating || _currentContentPage == targetPage)
        {
            return;
        }

        if (!TryGetPanelName(targetPage, out string targetPanelName))
        {
            return;
        }

        _isNavigating = true;
        try
        {
            UIBasePanel targetPanel = await UIManager.GetInstance()
                .OpenPanelAsync(targetPanelName);
            if (targetPanel == null)
            {
                return;
            }

            if (TryGetPanelName(_currentContentPage, out string currentPanelName))
            {
                UIManager.GetInstance().ClosePanel(currentPanelName);
            }

            _currentContentPage = targetPage;
            GameManager.Audio.Play(AudioDefine.SFXClick);
        }
        finally
        {
            _isNavigating = false;
        }
    }
    #endregion

    /// <summary>
    /// 设置页面异步加载遮罩的显示状态
    /// </summary>
    public void SetLoadingVisible(bool visible)
    {
        if (_goLoad == null)
        {
            Debug.LogError("MainMenuView 的 Load 节点未在 Inspector 中配置。", this);
            return;
        }

        _goLoad.SetActive(visible);
    }

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
            case MainContentPage.Mission:
                panelName = GlobalDefine.MissionView;
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

    private void RefreshBuffItems()
    {
        _buffRefreshVersion++;
        ClearBuffItems();
        if (_buffParent == null)
        {
            Debug.LogError("MainMenuView 的 BUFF 挂载节点未在 Inspector 中配置。", this);
            return;
        }

        CreateBuffItemsAsync(_buffRefreshVersion);
    }

    private async void CreateBuffItemsAsync(int refreshVersion)
    {
        List<ActiveBuffData> activeBuffs = PlayerInfoManager.GetInstance().GetActiveBuffs();
        string resourceTag = GetInstanceID().ToString();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(activeBuff.buffId);
            if (buffConfig == null)
            {
                Debug.LogError($"激活的 BUFF 配置不存在: [{activeBuff.buffId}]", this);
                continue;
            }

            GameObject buffItemObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.BuffItem, resourceTag);
            if (refreshVersion != _buffRefreshVersion || !isActiveAndEnabled)
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.BuffItem, buffItemObject);
                return;
            }

            BuffItem buffItem = buffItemObject.GetComponent<BuffItem>();
            if (buffItem == null)
            {
                Debug.LogError("BuffItem 预制体缺少 BuffItem 组件。", buffItemObject);
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.BuffItem, buffItemObject);
                continue;
            }

            buffItem.transform.SetParent(_buffParent, false);
            _buffItems.Add(buffItem);
            buffItem.SetData(buffConfig, activeBuff, resourceTag);
        }
    }

    private void ClearBuffItems()
    {
        _buffRefreshVersion++;
        for (int i = 0; i < _buffItems.Count; i++)
        {
            BuffItem buffItem = _buffItems[i];
            if (buffItem == null)
            {
                continue;
            }

            buffItem.Clear();
            UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.BuffItem, buffItem.gameObject);
        }

        _buffItems.Clear();
    }

    private void OnMissionDataChanged(PlayerInfoManager playerInfoManager)
    {
        RefreshMissionItems();
    }

    private void ToggleMissions()
    {
        SetMissionsVisible(!_goMissions.activeSelf);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void SetMissionsVisible(bool visible)
    {
        _goMissions.SetActive(visible);
        _goTip.SetActive(!visible);
    }

    private void BeginMoveMissionPanel(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData pointerEventData) ||
            !TryGetMissionPanelLocalPosition(pointerEventData, out Vector2 pointerPosition))
        {
            return;
        }

        _moveOffset = pointerPosition - _missionPanel.anchoredPosition;
    }

    private void MoveMissionPanel(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData pointerEventData) ||
            !TryGetMissionPanelLocalPosition(pointerEventData, out Vector2 pointerPosition))
        {
            return;
        }

        _missionPanel.anchoredPosition = pointerPosition - _moveOffset;
        ClampMissionPanelPosition();
    }

    private bool TryGetMissionPanelLocalPosition(
        PointerEventData pointerEventData,
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        RectTransform parent = _missionPanel.parent as RectTransform;
        return parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                pointerEventData.position,
                pointerEventData.pressEventCamera,
                out localPosition);
    }

    private void ClampMissionPanelPosition()
    {
        RectTransform parent = _missionPanel.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        _missionPanel.GetWorldCorners(_missionPanelCorners);
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < _missionPanelCorners.Length; i++)
        {
            Vector3 corner = parent.InverseTransformPoint(_missionPanelCorners[i]);
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
        }

        Rect parentRect = parent.rect;
        Vector2 offset = Vector2.zero;
        offset.x = GetContainedOffset(
            minX,
            maxX,
            parentRect.xMin,
            parentRect.xMax);
        offset.y = GetContainedOffset(
            minY,
            maxY,
            parentRect.yMin,
            parentRect.yMax);
        _missionPanel.anchoredPosition += offset;
    }

    private static float GetContainedOffset(
        float contentMin,
        float contentMax,
        float containerMin,
        float containerMax)
    {
        float contentSize = contentMax - contentMin;
        float containerSize = containerMax - containerMin;
        if (contentSize > containerSize)
        {
            return (containerMin + containerMax - contentMin - contentMax) * 0.5f;
        }

        if (contentMin < containerMin)
        {
            return containerMin - contentMin;
        }

        return contentMax > containerMax ? containerMax - contentMax : 0f;
    }

    private void RefreshMissionItems()
    {
        _missionRefreshVersion++;
        ClearMissionItems();
        if (_missionParent == null)
        {
            Debug.LogError("MainMenuView 的任务挂载节点未在 Inspector 中配置。", this);
            return;
        }

        List<Mission<MissionMessage>> missions =
            new List<Mission<MissionMessage>>(MissionAPI.GetActiveMissions());
        missions.Sort(CompareMissionDeadline);
        CreateMissionItemsAsync(missions, _missionRefreshVersion);
    }

    private async void CreateMissionItemsAsync(
        List<Mission<MissionMessage>> missions,
        int refreshVersion)
    {
        string resourceTag = GetInstanceID().ToString();
        int missionCount = Mathf.Min(3, missions.Count);
        for (int i = 0; i < missionCount; i++)
        {
            Mission<MissionMessage> mission = missions[i];
            if (!int.TryParse(mission.id, out int missionId))
            {
                Debug.LogError($"主菜单任务 ID 无效: [{mission.id}]", this);
                continue;
            }

            cfg.Mission missionConfig = DataTableMananger.GetInstance().Tables.MissionTable
                .GetOrDefault(missionId);
            if (missionConfig == null)
            {
                Debug.LogError($"主菜单任务配置不存在: [{missionId}]", this);
                continue;
            }

            GameObject missionItemObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.MainMenuMissionItem, resourceTag);
            if (refreshVersion != _missionRefreshVersion || !isActiveAndEnabled)
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.MainMenuMissionItem,
                    missionItemObject);
                return;
            }

            MainMenuMissionItem missionItem =
                missionItemObject.GetComponent<MainMenuMissionItem>();
            if (missionItem == null)
            {
                Debug.LogError("MainMenuMissionItem 预制体缺少 MainMenuMissionItem 组件。",
                    missionItemObject);
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.MainMenuMissionItem,
                    missionItemObject);
                continue;
            }

            string claimMissionId = mission.id;
            missionItem.transform.SetParent(_missionParent, false);
            missionItem.SetData(
                missionConfig,
                mission,
                () => ClaimMission(claimMissionId));
            _missionItems.Add(missionItem);
            RebuildMissionLayout();
        }
    }

    private void ClearMissionItems()
    {
        _missionRefreshVersion++;
        for (int i = 0; i < _missionItems.Count; i++)
        {
            MainMenuMissionItem missionItem = _missionItems[i];
            if (missionItem == null)
            {
                continue;
            }

            missionItem.Clear();
            UnityObjectPoolFactory.GetInstance().PutItem(
                GlobalDefine.MainMenuMissionItem,
                missionItem.gameObject);
        }

        _missionItems.Clear();
        RebuildMissionLayout();
    }

    private void RebuildMissionLayout()
    {
        RectTransform missionParent = _missionParent as RectTransform;
        if (missionParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(missionParent);
        }

        if (_missionPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_missionPanel);
        }
    }

    private void ClaimMission(string missionId)
    {
        if (!MissionAPI.TryClaimMission(missionId))
        {
            Debug.LogWarning($"任务不可领取: [{missionId}]", this);
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private static int CompareMissionDeadline(
        Mission<MissionMessage> left,
        Mission<MissionMessage> right)
    {
        int leftDeadline = GetMissionDeadlineValue(left.id);
        int rightDeadline = GetMissionDeadlineValue(right.id);
        if (leftDeadline != rightDeadline)
        {
            return leftDeadline.CompareTo(rightDeadline);
        }

        return string.CompareOrdinal(left.id, right.id);
    }

    private static int GetMissionDeadlineValue(string missionId)
    {
        return MissionAPI.TryGetMissionDeadline(
            missionId,
            out int deadlineAge,
            out int deadlineMonth)
            ? deadlineAge * 12 + deadlineMonth
            : int.MaxValue;
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
    /// 获取主界面模拟币图标，供飞币特效设为终点并播放抵达反馈
    /// </summary>
    public Image SimulationCoinIcon
    {
        get { return _simulationCoinIcon; }
    }

    public bool TryGetMissionButton(out Button missionButton)
    {
        if (_missionButton != null && _missionButton.gameObject.activeInHierarchy)
        {
            missionButton = _missionButton;
            return true;
        }

        missionButton = null;
        Debug.LogError("MainMenuView 未绑定任务按钮。", this);
        return false;
    }

    public bool TryGetLotteryButton(out Button lotteryButton)
    {
        if (_lotteryButton != null && _lotteryButton.gameObject.activeInHierarchy)
        {
            lotteryButton = _lotteryButton;
            return true;
        }

        lotteryButton = null;
        Debug.LogError("MainMenuView 未绑定抽奖按钮。", this);
        return false;
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
