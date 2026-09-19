using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubwayView : UIBasePanel
{
    [System.Serializable]
    private class SubwayDestination
    {
        [SerializeField] private int _locationId;
        [SerializeField] private string _panelName;

        public SubwayDestination(int locationId, string panelName)
        {
            _locationId = locationId;
            _panelName = panelName;
        }

        public bool Matches(int locationId)
        {
            return _locationId == locationId;
        }

        public string PanelName
        {
            get { return _panelName; }
        }
    }

    [SerializeField] private Text _txtLockTip;  // 未解锁时提示文本
    [SerializeField] private Text _txtUnlockName; // 解锁时：地点名称
    [SerializeField] private Text _txtUnlockDetail; // 解锁时：地点详情
    [SerializeField] private Text _txtUnlockPrice;  // 解锁时：地点价格
    [SerializeField] private Image _imgUnlock;      // 地点图片
    [SerializeField] private Button _btnGo;         // 解锁时：前往按钮
    [SerializeField] private Button _btnBack;   // 回到社区按钮
    [SerializeField] private SubwayPoint[] _subwayPoints; // 地铁点, 目前仅 2 个, 根据表中顺序进行初始化
    [SerializeField] private SubwayDestination[] _destinations =
    {
        new SubwayDestination(2, GlobalDefine.FishView),
    };


    [Space(10)]
    [SerializeField] private GameObject _goDetailLock;   // 未解锁时下方详情显示
    [SerializeField] private GameObject _goDatailUnlock; // 解锁时下方详情显示
    [SerializeField] private GameObject _goPrice;        // 车费区域
    [SerializeField] private GameObject _goButtonGo;     // 前往按钮区域

    private readonly List<cfg.Subway> _locations = new List<cfg.Subway>();
    private cfg.Subway _selectedLocation;
    private bool _isUiReady;
    private bool _isNavigating;
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
        _btnGo.onClick.AddListener(OnClickGo);
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
        UIManager.GetInstance().SetMainMenuNavigationVisible(false);
        RefreshLocationStates();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshLocationStates;
        UIManager.GetInstance().SetMainMenuNavigationVisible(!_isNavigating);
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshLocationStates;
        UIManager.GetInstance().SetMainMenuNavigationVisible(!_isNavigating);
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        if (_btnGo != null)
        {
            _btnGo.onClick.RemoveListener(OnClickGo);
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
        bool shouldShowTravelControls = _selectedLocation != null &&
                                        !IsCurrentLocation(_selectedLocation) &&
                                        isUnlocked;
        _goDetailLock.SetActive(_selectedLocation != null && !isUnlocked);
        _goDatailUnlock.SetActive(_selectedLocation != null && isUnlocked);
        _goPrice.SetActive(shouldShowTravelControls);
        _goButtonGo.SetActive(shouldShowTravelControls);

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
            ? $"{_selectedLocation.Fare}"
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
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
    }

    private async void OnClickGo()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_isNavigating ||
            _selectedLocation == null ||
            !IsLocationUnlocked(_selectedLocation) ||
            IsCurrentLocation(_selectedLocation))
        {
            return;
        }

        if (!TryGetDestinationPanelName(_selectedLocation.Id, out string destinationPanelName))
        {
            CommonTipView.Show($"【{_selectedLocation.Name}】暂未开放");
            return;
        }

        if (!TryGetTravelCosts(_selectedLocation, out List<KeyValuePair<int, int>> travelCosts))
        {
            Debug.LogError($"地铁消耗配置无效: [{_selectedLocation.Id}]", this);
            CommonTipView.Show("前往配置异常");
            return;
        }

        if (!CanAffordTravelCosts(travelCosts))
        {
            CommonTipView.Show("车费或所需道具不足");
            GameManager.Audio.Play(AudioDefine.SFXClickFail);
            return;
        }

        _isNavigating = true;
        try
        {
            UIBasePanel destinationPanel = await UIManager.GetInstance()
                .OpenPanelAsync(destinationPanelName);
            if (destinationPanel == null)
            {
                return;
            }

            if (!TrySpendTravelCosts(travelCosts))
            {
                Debug.LogError($"地铁扣除消耗失败: [{_selectedLocation.Id}]", this);
                UIManager.GetInstance().ClosePanel(destinationPanelName);
                UIManager.GetInstance().SetMainMenuNavigationVisible(false);
                CommonTipView.Show("车费或所需道具不足");
                GameManager.Audio.Play(AudioDefine.SFXClickFail);
                return;
            }

            UIManager.GetInstance().ClosePanel(GetPanelName());
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private bool TryGetDestinationPanelName(int locationId, out string panelName)
    {
        if (_destinations != null)
        {
            for (int i = 0; i < _destinations.Length; i++)
            {
                SubwayDestination destination = _destinations[i];
                if (destination != null &&
                    destination.Matches(locationId) &&
                    !string.IsNullOrEmpty(destination.PanelName))
                {
                    panelName = destination.PanelName;
                    return true;
                }
            }
        }

        if (locationId == 2)
        {
            panelName = GlobalDefine.FishView;
            return true;
        }

        panelName = null;
        return false;
    }

    private static bool TryGetTravelCosts(
        cfg.Subway locationConfig,
        out List<KeyValuePair<int, int>> travelCosts)
    {
        travelCosts = new List<KeyValuePair<int, int>>();
        if (locationConfig == null || locationConfig.Fare < 0)
        {
            return false;
        }

        Dictionary<int, int> costsByItemId = new Dictionary<int, int>();
        if (locationConfig.Fare > 0 &&
            !TryAddTravelCost(
                costsByItemId,
                BasePropertyId.SimulationCoin,
                locationConfig.Fare))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(locationConfig.ItemCosts))
        {
            string[] entries = locationConfig.ItemCosts.Split(
                new[] { ';' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] values = entries[i].Split(',');
                if (values.Length != 2 ||
                    !int.TryParse(values[0], out int itemId) ||
                    !int.TryParse(values[1], out int amount) ||
                    !TryAddTravelCost(costsByItemId, itemId, amount))
                {
                    return false;
                }
            }
        }

        foreach (KeyValuePair<int, int> cost in costsByItemId)
        {
            travelCosts.Add(cost);
        }

        return true;
    }

    private static bool TryAddTravelCost(
        Dictionary<int, int> costsByItemId,
        int itemId,
        int amount)
    {
        if (itemId <= 0 || amount <= 0)
        {
            return false;
        }

        if (costsByItemId.TryGetValue(itemId, out int existingAmount))
        {
            if (existingAmount > int.MaxValue - amount)
            {
                return false;
            }

            costsByItemId[itemId] = existingAmount + amount;
            return true;
        }

        costsByItemId.Add(itemId, amount);
        return true;
    }

    private static bool CanAffordTravelCosts(
        List<KeyValuePair<int, int>> travelCosts)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < travelCosts.Count; i++)
        {
            KeyValuePair<int, int> cost = travelCosts[i];
            if (playerInfoManager.GetConsumableCount(cost.Key) < cost.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TrySpendTravelCosts(
        List<KeyValuePair<int, int>> travelCosts)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < travelCosts.Count; i++)
        {
            KeyValuePair<int, int> cost = travelCosts[i];
            if (!playerInfoManager.TrySpendConsumable(cost.Key, cost.Value))
            {
                return false;
            }
        }

        return true;
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
            _goDatailUnlock != null &&
            _goPrice != null &&
            _goButtonGo != null)
        {
            return true;
        }

        Debug.LogError("SubwayView 存在未绑定的 UI 引用。", this);
        return false;
    }
}
