using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommonOptionalPanelData
{
    public int SourceItemId { get; }
    public Action OnClosed { get; }

    public CommonOptionalPanelData(int sourceItemId, Action onClosed = null)
    {
        SourceItemId = sourceItemId;
        OnClosed = onClosed;
    }
}

public class CommonOptionalPanel : UIBasePanel
{
    private const float RewardScaleDivisor = 10000f;
    private const int OptionalPanelScaleId = 12;

    private static readonly Color RareLevelColor = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EpicLevelColor = new Color(0.7f, 0.3f, 1f);
    private static readonly Color LegendaryLevelColor = new Color(1f, 0.75f, 0.1f);
    private static readonly Color MythicLevelColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtSelectName; 
    [SerializeField] private Transform _itemParent;
    [SerializeField] private Button _btnSelect;
    [SerializeField] private Button _btnClose;

    private readonly List<CommonOptionalItem> _optionalItems = new List<CommonOptionalItem>();

    private int _sourceItemId;
    private int _initVersion;
    private Action _onClosed;
    private CommonOptionalItem _selectedItem;
    private string _resourceTag;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        UiButtonAudio.Bind(_btnSelect, ConfirmSelection);
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            UiButtonAudio.Bind(_btnClose, ClosePanel);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!(param?.data is CommonOptionalPanelData panelData) || panelData.SourceItemId <= 0)
        {
            Debug.LogError("CommonOptionalPanel 初始化参数无效。", this);
            ClosePanel();
            return;
        }

        _onClosed = panelData.OnClosed;
        _sourceItemId = panelData.SourceItemId;
        _selectedItem = null;
        _btnSelect.interactable = false;

        cfg.Item sourceItemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(_sourceItemId);
        if (sourceItemConfig == null)
        {
            Debug.LogError($"CommonOptionalPanel 找不到道具配置: [{_sourceItemId}]", this);
            ClosePanel();
            return;
        }

        _txtTitle.text = sourceItemConfig.Name;
        ClearSelectNameDisplay();

        if (!TryParseUseEffectPool(
                sourceItemConfig.UseEffect,
                out List<OptionalRewardEntry> rewardEntries))
        {
            Debug.LogError(
                $"CommonOptionalPanel 奖池配置无效: [{_sourceItemId}], [{sourceItemConfig.UseEffect}]",
                this);
            ClosePanel();
            return;
        }

        ClearOptionalItems();
        InitializeOptionalItemsAsync(rewardEntries, ++_initVersion);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
        ClearOptionalItems();
        GameManager.Audio.Play(AudioDefine.SFXClose);
        _onClosed?.Invoke();
        _onClosed = null;
    }

    protected override void OnDestroy()
    {
        if (_btnSelect != null)
        {
            _btnSelect.onClick.RemoveListener(ConfirmSelection);
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonOptionalPanel;
    }

    private async void InitializeOptionalItemsAsync(
        List<OptionalRewardEntry> rewardEntries,
        int initVersion)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;

        for (int i = 0; i < rewardEntries.Count; i++)
        {
            OptionalRewardEntry rewardEntry = rewardEntries[i];
            cfg.Item rewardItemConfig = tables.ItemTable.GetOrDefault(rewardEntry.ItemId);
            if (rewardItemConfig == null)
            {
                Debug.LogError($"CommonOptionalPanel 奖池道具不存在: [{rewardEntry.ItemId}]", this);
                continue;
            }

            GameObject itemObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.CommonOptionalItem, _resourceTag);
            if (initVersion != _initVersion || itemObject == null)
            {
                if (itemObject != null)
                {
                    UnityObjectPoolFactory.GetInstance()
                        .PutItem(GlobalDefine.CommonOptionalItem, itemObject);
                }

                return;
            }

            itemObject.transform.SetParent(_itemParent, false);
            CommonOptionalItem optionalItem = itemObject.GetComponent<CommonOptionalItem>();
            if (optionalItem == null)
            {
                Debug.LogError("CommonOptionalItem 预制体缺少组件。", this);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonOptionalItem, itemObject);
                continue;
            }

            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
                rewardItemConfig.Icon,
                _resourceTag);
            if (initVersion != _initVersion)
            {
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonOptionalItem, itemObject);
                return;
            }

            if (icon == null)
            {
                Debug.LogError(
                    $"CommonOptionalPanel 图标加载失败: [{rewardEntry.ItemId}], [{rewardItemConfig.Icon}]",
                    this);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonOptionalItem, itemObject);
                continue;
            }

            float iconScale = GetOptionalItemIconScale(rewardEntry.ItemId, tables);
            optionalItem.Configure(
                rewardEntry.ItemId,
                rewardEntry.ItemCount,
                icon,
                iconScale,
                OnOptionalItemClicked);
            _optionalItems.Add(optionalItem);
        }
    }

    private void OnOptionalItemClicked(CommonOptionalItem optionalItem)
    {
        if (optionalItem == null || optionalItem == _selectedItem)
        {
            return;
        }

        if (_selectedItem != null)
        {
            _selectedItem.SetSelected(false);
        }

        _selectedItem = optionalItem;
        _selectedItem.SetSelected(true);
        _btnSelect.interactable = true;
        RefreshSelectNameDisplay(_selectedItem.RewardItemId);
    }

    private void ConfirmSelection()
    {
        if (_selectedItem == null)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.GetItemCount(_sourceItemId) < 1)
        {
            Debug.LogError($"CommonOptionalPanel 自选宝箱数量不足: [{_sourceItemId}]", this);
            ClosePanel();
            return;
        }

        CommonRewardItemData reward = new CommonRewardItemData
        {
            itemId = _selectedItem.RewardItemId,
            itemCount = _selectedItem.RewardItemCount
        };

        if (!playerInfoManager.TryConsumeItem(_sourceItemId))
        {
            Debug.LogError($"CommonOptionalPanel 消耗自选宝箱失败: [{_sourceItemId}]", this);
            ClosePanel();
            return;
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
        {
            data = new List<CommonRewardItemData> { reward },
            rewardsAlreadyGranted = false
        });
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void ClearOptionalItems()
    {
        _initVersion++;
        _selectedItem = null;
        for (int i = 0; i < _optionalItems.Count; i++)
        {
            CommonOptionalItem optionalItem = _optionalItems[i];
            if (optionalItem == null)
            {
                continue;
            }

            UnityObjectPoolFactory.GetInstance()
                .PutItem(GlobalDefine.CommonOptionalItem, optionalItem.gameObject);
        }

        _optionalItems.Clear();
        ClearSelectNameDisplay();
    }

    private void ClearSelectNameDisplay()
    {
        if (_txtSelectName == null)
        {
            return;
        }

        _txtSelectName.text = string.Empty;
        _txtSelectName.color = Color.white;
    }

    private void RefreshSelectNameDisplay(int itemId)
    {
        if (_txtSelectName == null)
        {
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig != null)
        {
            _txtSelectName.text = itemConfig.Name;
            _txtSelectName.color = GetLevelColor(itemConfig.Level);
            return;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        if (baseConfig != null)
        {
            _txtSelectName.text = baseConfig.Name;
            _txtSelectName.color = GetLevelColor(1);
            return;
        }

        _txtSelectName.text = string.Empty;
        _txtSelectName.color = Color.white;
    }

    private static Color GetLevelColor(int level)
    {
        switch (level)
        {
            case 2:
                return RareLevelColor;
            case 3:
                return EpicLevelColor;
            case 4:
                return LegendaryLevelColor;
            case 5:
                return MythicLevelColor;
            default:
                return Color.white;
        }
    }

    private static bool TryParseUseEffectPool(
        string useEffect,
        out List<OptionalRewardEntry> rewardEntries)
    {
        rewardEntries = new List<OptionalRewardEntry>();
        if (string.IsNullOrWhiteSpace(useEffect))
        {
            return false;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        string[] effectEntries = useEffect.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < effectEntries.Length; i++)
        {
            string[] values = effectEntries[i].Split(',');
            if (values.Length != 3 ||
                !int.TryParse(values[0], out int itemId) ||
                !int.TryParse(values[1], out int itemCount) ||
                !int.TryParse(values[2], out int weight) ||
                itemId <= 0 || itemCount <= 0 || weight <= 0 ||
                (tables.ItemTable.GetOrDefault(itemId) == null &&
                 tables.BaseTable.GetOrDefault(itemId) == null))
            {
                return false;
            }

            rewardEntries.Add(new OptionalRewardEntry(itemId, itemCount, weight));
        }

        return rewardEntries.Count > 0;
    }

    private static float GetOptionalItemIconScale(int itemId, cfg.Tables tables)
    {
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        int rewardScale = itemConfig != null
            ? itemConfig.RewardScale
            : baseConfig != null
                ? baseConfig.RewardScale
                : (int)RewardScaleDivisor;
        cfg.Scale optionalScaleConfig = tables.ScaleTable.GetOrDefault(OptionalPanelScaleId);
        if (optionalScaleConfig == null)
        {
            Debug.LogError($"Scale 配置不存在: [{OptionalPanelScaleId}]");
            return rewardScale / RewardScaleDivisor;
        }

        float sceneScale = optionalScaleConfig.ScaleValue / RewardScaleDivisor;
        return rewardScale / RewardScaleDivisor * sceneScale;
    }

    private readonly struct OptionalRewardEntry
    {
        public readonly int ItemId;
        public readonly int ItemCount;
        public readonly int Weight;

        public OptionalRewardEntry(int itemId, int itemCount, int weight)
        {
            ItemId = itemId;
            ItemCount = itemCount;
            Weight = weight;
        }
    }
}
