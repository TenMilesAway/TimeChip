using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : UIBasePanel
{
    private const int ItemsPerPage = 16;
    private const int InventoryCanUseNormal = 1;
    private const int InventoryCanUseOptionalBox = 2;
    private const float RewardScaleDivisor = 10000f;
    [SerializeField, Min(1)] private int _batchUseItemsPerFrame = 10;
    private static readonly Color RareLevelColor = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EpicLevelColor = new Color(0.7f, 0.3f, 1f);
    private static readonly Color LegendaryLevelColor = new Color(1f, 0.75f, 0.1f);
    private static readonly Color MythicLevelColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField] private Image _imgIcon;
    [SerializeField] private Image _imgIconBg;
    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtLevel;                  // 品质
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Text _txtNum;
    [SerializeField] private Text _txtNumSplit;
    [SerializeField] private Text _txtNumPrefix;
    [SerializeField] private Text _currentPageText;
    [SerializeField] private Text _maxPageText;
    [SerializeField] private Text _txtType;
    [SerializeField] private Button _btnUse;
    [SerializeField] private Button _previousPageButton;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private Button _btnUseMore;               // 批量使用

    [SerializeField] private InventoryItem[] _inventoryItems;
    [SerializeField] private GameObject _goType;

    private readonly List<InventoryEntry> _items = new List<InventoryEntry>();

    private int _currentPage = 1;
    private int _refreshVersion;
    private int _detailVersion;
    private int _selectedItemId;
    private bool _isUsingSelectedItem;
    private bool _isUiReady;

    private void Awake()
    {
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        UiButtonAudio.Bind(_previousPageButton, ShowPreviousPage);
        UiButtonAudio.Bind(_nextPageButton, ShowNextPage);
        UiButtonAudio.Bind(_btnUse, UseSelectedItem);
        UiButtonAudio.Bind(_btnUseMore, OpenBatchUsePanel);
        HideDetail();
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (_isUiReady)
        {
            RefreshInventory();
        }
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();

        GameManager.Audio.Play(AudioDefine.SFXClose);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (!_isUiReady)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        RefreshInventory();
    }

    protected override void HideHandle()
    {
        base.HideHandle();
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        _refreshVersion++;
        HideDetail();
    }

    protected override void OnDestroy()
    {
        if (_previousPageButton != null)
        {
            _previousPageButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (_nextPageButton != null)
        {
            _nextPageButton.onClick.RemoveListener(ShowNextPage);
        }

        if (_btnUse != null)
        {
            _btnUse.onClick.RemoveListener(UseSelectedItem);
        }

        if (_btnUseMore != null)
        {
            _btnUseMore.onClick.RemoveListener(OpenBatchUsePanel);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.InventoryView;
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        if (_isUsingSelectedItem)
        {
            return;
        }

        RefreshInventory();
    }

    private void RefreshInventory()
    {
        _items.Clear();
        PlayerInfoData playerData = PlayerInfoManager.GetInstance().GetSnapshot();
        for (int i = 0; i < playerData.inventory.Count; i++)
        {
            PlayerInventoryItem playerItem = playerData.inventory[i];
            cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(playerItem.itemId);
            if (itemConfig != null &&
                itemConfig.InventoryType == 1 &&
                playerItem.amount > 0)
            {
                _items.Add(new InventoryEntry(itemConfig, playerItem.amount));
            }
        }

        _items.Sort((left, right) => left.Item.Id.CompareTo(right.Item.Id));
        _currentPage = Mathf.Clamp(_currentPage, 1, GetMaxPage());
        int selectedItemIndex = FindItemIndex(_selectedItemId);
        if (selectedItemIndex >= 0)
        {
            _currentPage = selectedItemIndex / ItemsPerPage + 1;
        }
        else
        {
            HideDetail();
        }

        RefreshPageAsync(++_refreshVersion);

        if (selectedItemIndex >= 0)
        {
            ShowItemDetailAsync(selectedItemIndex);
        }
    }

    private void ShowPreviousPage()
    {
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;
        HideDetail();
        RefreshPageAsync(++_refreshVersion);
    }

    private void ShowNextPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        if (_currentPage >= GetMaxPage())
        {
            return;
        }

        _currentPage++;
        HideDetail();
        RefreshPageAsync(++_refreshVersion);
    }

    private async void RefreshPageAsync(int refreshVersion)
    {
        int firstItemIndex = (_currentPage - 1) * ItemsPerPage;
        for (int i = 0; i < _inventoryItems.Length; i++)
        {
            _inventoryItems[i].Clear();
        }

        _currentPageText.text = _currentPage.ToString();
        _maxPageText.text = GetMaxPage().ToString();
        _previousPageButton.interactable = _currentPage > 1;
        _nextPageButton.interactable = _currentPage < GetMaxPage();

        string resourceTag = GetInstanceID().ToString();
        for (int slotIndex = 0; slotIndex < _inventoryItems.Length; slotIndex++)
        {
            int itemIndex = firstItemIndex + slotIndex;
            if (itemIndex >= _items.Count)
            {
                break;
            }

            InventoryEntry entry = _items[itemIndex];
            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(entry.Item.Icon, resourceTag);
            if (refreshVersion != _refreshVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (icon == null)
            {
                Debug.LogError($"背包道具图标加载失败: [{entry.Item.Id}], [{entry.Item.Icon}]", this);
                continue;
            }

            int selectedItemIndex = itemIndex;
            _inventoryItems[slotIndex].SetData(
                icon,
                entry.Amount,
                entry.Item.Type,
                entry.Item.RewardScale,
                () => SelectItem(selectedItemIndex));
        }
    }

    private void SelectItem(int itemIndex)
    {
        if (_isUsingSelectedItem)
        {
            return;
        }

        ShowItemDetailAsync(itemIndex);
    }

    private async void ShowItemDetailAsync(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count)
        {
            return;
        }

        InventoryEntry entry = _items[itemIndex];
        _selectedItemId = entry.Item.Id;
        int detailVersion = ++_detailVersion;
        _txtName.text = entry.Item.Name;
        _txtDetail.text = entry.Item.Desc;
        _txtNum.text = entry.Amount.ToString();
        SetLevel(entry.Item.Level);
        _txtType.text = entry.Item.Type;
        _txtName.gameObject.SetActive(true);
        _txtLevel.gameObject.SetActive(true);
        _txtDetail.gameObject.SetActive(true);
        _txtNum.gameObject.SetActive(true);
        _txtNumSplit.gameObject.SetActive(false);  // 默认隐藏
        _txtNumPrefix.gameObject.SetActive(true);
        bool canUseInInventory = CanUseItemInInventory(entry.Item);
        bool canBatchUse = CanBatchUseItemInInventory(entry.Item, entry.Amount);
        _btnUse.gameObject.SetActive(canUseInInventory);
        _btnUse.interactable = canUseInInventory && !_isUsingSelectedItem;
        _btnUseMore.gameObject.SetActive(canBatchUse);
        _btnUseMore.interactable = canBatchUse && !_isUsingSelectedItem;
        _goType.SetActive(true);
        _imgIcon.gameObject.SetActive(false);
        _imgIconBg.gameObject.SetActive(true);

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            entry.Item.Icon,
            GetInstanceID().ToString());
        if (detailVersion != _detailVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"背包详情图标加载失败: [{entry.Item.Id}], [{entry.Item.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * (entry.Item.RewardScale / RewardScaleDivisor);
        _imgIcon.gameObject.SetActive(true);
    }

    private int FindItemIndex(int itemId)
    {
        if (itemId <= 0)
        {
            return -1;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Item.Id == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    private void HideDetail()
    {
        _detailVersion++;
        _selectedItemId = 0;
        _imgIcon.gameObject.SetActive(false);
        _imgIconBg.gameObject.SetActive(false);
        _txtName.gameObject.SetActive(false);
        _txtLevel.gameObject.SetActive(false);
        _txtDetail.gameObject.SetActive(false);
        _txtNumSplit.gameObject.SetActive(false);
        _txtNumPrefix.gameObject.SetActive(false);
        _txtNum.gameObject.SetActive(false);
        _btnUse.gameObject.SetActive(false);
        _btnUseMore.gameObject.SetActive(false);
        _goType.SetActive(false);
    }

    private async void UseSelectedItem()
    {
        if (_isUsingSelectedItem)
        {
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(_selectedItemId);
        if (!CanUseItemInInventory(itemConfig))
        {
            Debug.LogError($"无法使用背包道具: [{_selectedItemId}]", this);
            return;
        }

        if (itemConfig.CanUse == InventoryCanUseOptionalBox)
        {
            OpenOptionalBoxPanel(itemConfig);
            return;
        }

        _isUsingSelectedItem = true;
        _btnUse.interactable = false;
        await UseItemsAsync(itemConfig, 1);
    }

    private void OpenOptionalBoxPanel(cfg.Item itemConfig)
    {
        _isUsingSelectedItem = true;
        _btnUse.interactable = false;
        _btnUseMore.interactable = false;

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonOptionalPanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonOptionalPanelData(
                    itemConfig.Id,
                    onClosed: () =>
                    {
                        _isUsingSelectedItem = false;
                        RefreshInventory();
                    })
            });
    }

    private void OpenBatchUsePanel()
    {
        if (_isUsingSelectedItem)
        {
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(_selectedItemId);
        int availableAmount = itemConfig == null ? 0 : PlayerInfoManager.GetInstance().GetItemCount(itemConfig.Id);
        if (!CanBatchUseItemInInventory(itemConfig, availableAmount))
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonUsePanel, UILayer.System, new OpenUIParam
        {
            data = new CommonUsePanelData
            {
                itemName = itemConfig.Name,
                maxAmount = availableAmount,
                useCallback = amount => UseSelectedItems(itemConfig.Id, amount)
            }
        });
    }

    private async void UseSelectedItems(int itemId, int amount)
    {
        if (_isUsingSelectedItem)
        {
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId);
        if (!CanBatchUseItemInInventory(itemConfig, amount) || amount <= 0 ||
            amount > PlayerInfoManager.GetInstance().GetItemCount(itemId))
        {
            Debug.LogError($"无法批量使用背包道具: [{itemId}], [{amount}]", this);
            RefreshInventory();
            return;
        }

        _isUsingSelectedItem = true;
        _btnUse.interactable = false;
        _btnUseMore.interactable = false;
        await UseItemsAsync(itemConfig, amount);
    }

    private async Task UseItemsAsync(cfg.Item itemConfig, int amount)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int availableAmount = playerInfoManager.GetItemCount(itemConfig.Id);
        if (amount <= 0 || amount > availableAmount)
        {
            Debug.LogError($"背包道具数量无效: [{itemConfig.Id}], [{amount}], 当前数量 [{availableAmount}]");
            _isUsingSelectedItem = false;
            RefreshInventory();
            return;
        }

        if (!TryParseUseEffect(
                itemConfig.UseEffect,
                out List<UseEffectReward> useEffectRewards,
                out long totalWeight))
        {
            Debug.LogError($"背包道具使用效果配置无效: [{itemConfig.Id}], [{itemConfig.UseEffect}]", this);
            _isUsingSelectedItem = false;
            RefreshInventory();
            return;
        }

        UseRewardSequence sequence = new UseRewardSequence();
        List<CommonRewardItemData> rewardBatch = new List<CommonRewardItemData>(5);
        int itemsPerFrame = Mathf.Max(1, _batchUseItemsPerFrame);
        bool completed = true;

        for (int i = 0; i < amount; i++)
        {
            if (i > 0 && i % itemsPerFrame == 0)
            {
                await Task.Yield();
            }

            string randomKey = $"Inventory.UseEffect.{itemConfig.Id}.{availableAmount - i}";
            if (!TryDrawUseEffect(useEffectRewards, totalWeight, randomKey, out CommonRewardItemData reward))
            {
                Debug.LogError($"背包道具使用效果计算失败: [{itemConfig.Id}]", this);
                completed = false;
                break;
            }

            if (!playerInfoManager.TryConsumeItem(itemConfig.Id))
            {
                Debug.LogError($"背包道具消耗失败: [{itemConfig.Id}]", this);
                completed = false;
                break;
            }

            rewardBatch.Add(reward);
            if (rewardBatch.Count == 5)
            {
                QueueUseRewardBatch(rewardBatch, sequence);
                rewardBatch = new List<CommonRewardItemData>(5);
            }
        }

        if (rewardBatch.Count > 0)
        {
            QueueUseRewardBatch(rewardBatch, sequence);
        }

        sequence.ProcessingCompleted = true;
        if (!completed)
        {
            Debug.LogWarning($"背包道具使用未完成: [{itemConfig.Id}]");
        }

        if (sequence.PendingPanelCount == 0)
        {
            CompleteUseRewardSequence(sequence);
        }
        else
        {
            RefreshInventory();
        }
    }

    private void QueueUseRewardBatch(
        List<CommonRewardItemData> rewards,
        UseRewardSequence sequence)
    {
        if (sequence.SkipFurtherPanels)
        {
            UIManager.GetInstance().GrantRewardsImmediately(rewards);
            return;
        }

        sequence.PendingPanelCount++;
        UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
        {
            data = rewards,
            rewardsAlreadyGranted = false,
            callback = () => OnUseRewardPanelClosed(sequence),
            skipQueuedRewardPanelsCallback = () => sequence.SkipFurtherPanels = true
        });
    }

    private void OnUseRewardPanelClosed(UseRewardSequence sequence)
    {
        sequence.PendingPanelCount--;
        if (sequence.ProcessingCompleted && sequence.PendingPanelCount == 0)
        {
            CompleteUseRewardSequence(sequence);
        }
    }

    private void CompleteUseRewardSequence(UseRewardSequence sequence)
    {
        if (!sequence.ProcessingCompleted || sequence.PendingPanelCount != 0)
        {
            return;
        }

        _isUsingSelectedItem = false;
        RefreshInventory();
    }

    private bool TryParseUseEffect(
        string useEffect,
        out List<UseEffectReward> rewards,
        out long totalWeight)
    {
        rewards = new List<UseEffectReward>();
        totalWeight = 0;
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
                (tables.BaseTable.GetOrDefault(itemId) == null &&
                 tables.ItemTable.GetOrDefault(itemId) == null))
            {
                Debug.LogError($"背包道具使用效果条目无效: [{effectEntries[i]}]", this);
                return false;
            }

            rewards.Add(new UseEffectReward(itemId, itemCount, weight));
            totalWeight += weight;
        }

        return totalWeight > 0;
    }

    private bool TryDrawUseEffect(
        List<UseEffectReward> rewards,
        long totalWeight,
        string randomKey,
        out CommonRewardItemData reward)
    {
        reward = null;
        if (rewards == null || rewards.Count == 0 || totalWeight <= 0)
        {
            return false;
        }

        long randomValue = TurnRandom.Range(randomKey, totalWeight);
        long accumulatedWeight = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            UseEffectReward effectReward = rewards[i];
            accumulatedWeight += effectReward.Weight;
            if (randomValue < accumulatedWeight)
            {
                reward = new CommonRewardItemData
                {
                    itemId = effectReward.ItemId,
                    itemCount = effectReward.ItemCount
                };
                return true;
            }
        }

        return false;
    }

    private int GetMaxPage()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)_items.Count / ItemsPerPage));
    }

    private bool HasValidUiReferences()
    {
        if (_imgIcon == null ||
            _txtName == null ||
        _txtLevel == null ||
        _txtDetail == null ||
            _txtNum == null ||
            _btnUse == null ||
            _previousPageButton == null ||
            _nextPageButton == null ||
            _btnUseMore == null ||
            _currentPageText == null ||
            _maxPageText == null ||
            _inventoryItems == null ||
            _inventoryItems.Length != ItemsPerPage)
        {
            Debug.LogError("InventoryView 的 UI 引用未完整配置，或背包格子数量不是 16 个。", this);
            return false;
        }

        for (int i = 0; i < _inventoryItems.Length; i++)
        {
            if (_inventoryItems[i] == null)
            {
                Debug.LogError($"InventoryView 的第 {i + 1} 个背包格子未配置。", this);
                return false;
            }
        }

        return true;
    }

    private void SetLevel(int level)
    {
        switch (level)
        {
            case 2:
                _txtLevel.text = "稀有";
                _txtLevel.color = RareLevelColor;
                break;
            case 3:
                _txtLevel.text = "史诗";
                _txtLevel.color = EpicLevelColor;
                break;
            case 4:
                _txtLevel.text = "传说";
                _txtLevel.color = LegendaryLevelColor;
                break;
            case 5:
                _txtLevel.text = "神话";
                _txtLevel.color = MythicLevelColor;
                break;
            default:
                _txtLevel.text = "普通";
                _txtLevel.color = Color.white;
                break;
        }
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static bool CanUseItemInInventory(cfg.Item itemConfig)
    {
        if (itemConfig == null)
        {
            return false;
        }

        return itemConfig.CanUse == InventoryCanUseNormal ||
            itemConfig.CanUse == InventoryCanUseOptionalBox;
    }

    private static bool CanBatchUseItemInInventory(cfg.Item itemConfig, int amount)
    {
        if (itemConfig == null || amount <= 1)
        {
            return false;
        }

        return itemConfig.CanUse == InventoryCanUseNormal && itemConfig.NoBatchUse != 1;
    }

    private readonly struct InventoryEntry
    {
        public readonly cfg.Item Item;
        public readonly int Amount;

        public InventoryEntry(cfg.Item item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    private readonly struct UseEffectReward
    {
        public readonly int ItemId;
        public readonly int ItemCount;
        public readonly int Weight;

        public UseEffectReward(int itemId, int itemCount, int weight)
        {
            ItemId = itemId;
            ItemCount = itemCount;
            Weight = weight;
        }
    }

    private sealed class UseRewardSequence
    {
        public int PendingPanelCount;
        public bool ProcessingCompleted;
        public bool SkipFurtherPanels;
    }
}
