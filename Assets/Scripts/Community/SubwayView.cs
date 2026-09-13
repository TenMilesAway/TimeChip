using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubwayView : UIBasePanel
{
    [SerializeField] private Text _txtLockTip;  // 未解锁时提示文本
    [SerializeField] private Text _txtUnlockName; // 解锁时：地点名称
    [SerializeField] private Text _txtUnlockDetail; // 解锁时：地点详情
    [SerializeField] private Text _txtUnlockPrice;  // 解锁时：地点价格
    [SerializeField] private Image _imgUnlock;      // 地点图片
    [SerializeField] private Button _btnGo;         // 解锁时：前往按钮
    [SerializeField] private Button _btnBack;   // 回到社区按钮
    [SerializeField] private SubwayPoint[] _subwayPoints; // 地铁点, 目前仅 2 个, 根据表中顺序进行初始化


    [Space(10)]
    [SerializeField] private GameObject _goDetailLock;   // 未解锁时下方详情显示
    [SerializeField] private GameObject _goDatailUnlock; // 解锁时下方详情显示

    private readonly List<cfg.Subway> _locations = new List<cfg.Subway>();
    private cfg.Subway _selectedLocation;
    private bool _isUiReady;
    private int _imageRequestVersion;

    private void Awake()
    {
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        _btnBack.onClick.AddListener(OnClickBack);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        if (_isUiReady)
        {
            InitializeLocations();
        }
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (!_isUiReady)
        {
            return;
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshLocationStates;
        PlayerInfoManager.GetInstance().PlayerInfoChanged += RefreshLocationStates;
        SetMainMenuNavigationVisible(false);
        RefreshLocationStates();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshLocationStates;
        SetMainMenuNavigationVisible(true);
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshLocationStates;
        SetMainMenuNavigationVisible(true);
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.SubwayView;
    }

    private void InitializeLocations()
    {
        _locations.Clear();
        IReadOnlyList<cfg.Subway> configurations = DataTableMananger.GetInstance()
            .Tables.SubwayTable.DataList;
        for (int i = 0; i < configurations.Count; i++)
        {
            _locations.Add(configurations[i]);
        }

        if (_locations.Count > _subwayPoints.Length)
        {
            Debug.LogError(
                $"地铁地点数量 [{_locations.Count}] 超出已绑定的地铁点数量 [{_subwayPoints.Length}]。",
                this);
        }

        for (int i = 0; i < _subwayPoints.Length; i++)
        {
            SubwayPoint point = _subwayPoints[i];
            if (point == null)
            {
                Debug.LogError($"第 {i + 1} 个地铁点未绑定。", this);
                continue;
            }

            cfg.Subway locationConfig = i < _locations.Count ? _locations[i] : null;
            point.SetData(
                locationConfig,
                IsCurrentLocation(locationConfig),
                IsLocationUnlocked(locationConfig),
                SelectLocation);
        }

        SelectLocation(_locations.Count > 0 ? _locations[0] : null);
    }

    private void RefreshLocationStates(PlayerInfoManager playerInfoManager = null)
    {
        if (_locations.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _subwayPoints.Length && i < _locations.Count; i++)
        {
            SubwayPoint point = _subwayPoints[i];
            if (point == null)
            {
                continue;
            }

            cfg.Subway locationConfig = _locations[i];
            point.SetData(
                locationConfig,
                IsCurrentLocation(locationConfig),
                IsLocationUnlocked(locationConfig),
                SelectLocation);
            point.SetSelected(_selectedLocation != null &&
                              _selectedLocation.Id == locationConfig.Id);
        }

        RefreshDetail();
    }

    private void SelectLocation(cfg.Subway locationConfig)
    {
        if (locationConfig == null)
        {
            _selectedLocation = null;
            RefreshDetail();
            return;
        }

        _selectedLocation = locationConfig;
        for (int i = 0; i < _subwayPoints.Length && i < _locations.Count; i++)
        {
            if (_subwayPoints[i] != null)
            {
                _subwayPoints[i].SetSelected(_locations[i].Id == locationConfig.Id);
            }
        }

        RefreshDetail();
    }

    private void RefreshDetail()
    {
        bool isUnlocked = IsLocationUnlocked(_selectedLocation);
        _goDetailLock.SetActive(_selectedLocation != null && !isUnlocked);
        _goDatailUnlock.SetActive(_selectedLocation != null && isUnlocked);

        if (_selectedLocation == null)
        {
            _imageRequestVersion++;
            _imgUnlock.sprite = null;
            _btnGo.interactable = false;
            return;
        }

        _txtLockTip.text = GetUnlockTip(_selectedLocation);
        _txtUnlockName.text = _selectedLocation.Name;
        _txtUnlockDetail.text = _selectedLocation.Desc;
        _txtUnlockPrice.text = _selectedLocation.Fare > 0
            ? $"车费：{_selectedLocation.Fare}"
            : "免费";
        _btnGo.interactable = isUnlocked && !IsCurrentLocation(_selectedLocation);
        LoadLocationImageAsync(_selectedLocation);
    }

    private static bool IsCurrentLocation(cfg.Subway locationConfig)
    {
        return locationConfig != null && locationConfig.Id == 1;
    }

    private static bool IsLocationUnlocked(cfg.Subway locationConfig)
    {
        if (locationConfig == null)
        {
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        return playerInfoManager.CommunityCentreLevel >= locationConfig.UnlockCommunityLevel &&
            (locationConfig.UnlockItemId <= 0 ||
             playerInfoManager.GetItemCount(locationConfig.UnlockItemId) > 0) &&
            playerInfoManager.SimulationCoins >= locationConfig.UnlockCoin;
    }

    private static string GetUnlockTip(cfg.Subway locationConfig)
    {
        if (locationConfig == null)
        {
            return string.Empty;
        }

        List<string> requirements = new List<string>();
        if (locationConfig.UnlockCommunityLevel > 0)
        {
            requirements.Add($"社区等级达到 LV.{locationConfig.UnlockCommunityLevel}");
        }

        if (locationConfig.UnlockItemId > 0)
        {
            cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
                .GetOrDefault(locationConfig.UnlockItemId);
            string itemName = itemConfig != null ? itemConfig.Name : locationConfig.UnlockItemId.ToString();
            requirements.Add($"拥有【{itemName}】");
        }

        if (locationConfig.UnlockCoin > 0)
        {
            requirements.Add($"拥有 {locationConfig.UnlockCoin} 模拟币");
        }

        return requirements.Count > 0 ? string.Join("及", requirements) : "已解锁";
    }

    private void OnClickBack()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
    }

    private async void LoadLocationImageAsync(cfg.Subway locationConfig)
    {
        int requestVersion = ++_imageRequestVersion;
        _imgUnlock.sprite = null;
        Sprite image = await GameManager.Resource.LoadResource<Sprite>(
            locationConfig.Image,
            GetInstanceID().ToString());
        if (requestVersion != _imageRequestVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (image == null)
        {
            Debug.LogError(
                $"地铁地点图片加载失败: [{locationConfig.Id}], [{locationConfig.Image}]",
                this);
            return;
        }

        _imgUnlock.sprite = image;
    }

    private static void SetMainMenuNavigationVisible(bool visible)
    {
        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView != null)
        {
            mainMenuView.SetNavigationVisible(visible);
        }
    }

    private bool HasValidUiReferences()
    {
        if (_txtLockTip != null &&
            _txtUnlockName != null &&
            _txtUnlockDetail != null &&
            _txtUnlockPrice != null &&
            _imgUnlock != null &&
            _btnGo != null &&
            _btnBack != null &&
            _subwayPoints != null &&
            _goDetailLock != null &&
            _goDatailUnlock != null)
        {
            return true;
        }

        Debug.LogError("SubwayView 存在未绑定的 UI 引用。", this);
        return false;
    }
}
