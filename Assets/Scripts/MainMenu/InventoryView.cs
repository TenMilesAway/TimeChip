using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : UIBasePanel
{
    private const int ItemsPerPage = 16;
    private const float RewardScaleDivisor = 10000f;
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
    [SerializeField] private Button _btnUse;
    [SerializeField] private Button _previousPageButton;
    [SerializeField] private Button _nextPageButton;

    [SerializeField] private InventoryItem[] _inventoryItems;

    private readonly List<InventoryEntry> _items = new List<InventoryEntry>();

    private int _currentPage = 1;
    private int _refreshVersion;
    private int _detailVersion;
    private bool _isUiReady;

    private void Awake()
    {
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        _previousPageButton.onClick.AddListener(ShowPreviousPage);
        _nextPageButton.onClick.AddListener(ShowNextPage);
        HideDetail();
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        GameManager.Audio.Play(AudioDefine.SFXClick);

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

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.InventoryView;
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
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
            if (itemConfig != null && playerItem.amount > 0)
            {
                _items.Add(new InventoryEntry(itemConfig, playerItem.amount));
            }
        }

        _currentPage = Mathf.Clamp(_currentPage, 1, GetMaxPage());
        HideDetail();
        RefreshPageAsync(++_refreshVersion);
    }

    private void ShowPreviousPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
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
                entry.Item.RewardScale,
                () => SelectItem(selectedItemIndex));
        }
    }

    private async void SelectItem(int itemIndex)
    {
        await GameManager.Audio.Play(AudioDefine.SFXClick);

        if (itemIndex < 0 || itemIndex >= _items.Count)
        {
            return;
        }

        InventoryEntry entry = _items[itemIndex];
        int detailVersion = ++_detailVersion;
        _txtName.text = entry.Item.Name;
        _txtDetail.text = entry.Item.Desc;
        _txtNum.text = entry.Amount.ToString();
        SetLevel(entry.Item.Level);
        _txtName.gameObject.SetActive(true);
        _txtLevel.gameObject.SetActive(true);
        _txtDetail.gameObject.SetActive(true);
        _txtNum.gameObject.SetActive(true);
        _txtNumSplit.gameObject.SetActive(true);
        _txtNumPrefix.gameObject.SetActive(true);
        _btnUse.gameObject.SetActive(entry.Item.CanUse == 1);
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

    private void HideDetail()
    {
        _detailVersion++;
        _imgIcon.gameObject.SetActive(false);
        _imgIconBg.gameObject.SetActive(false);
        _txtName.gameObject.SetActive(false);
        _txtLevel.gameObject.SetActive(false);
        _txtDetail.gameObject.SetActive(false);
        _txtNumSplit.gameObject.SetActive(false);
        _txtNumPrefix.gameObject.SetActive(false);
        _txtNum.gameObject.SetActive(false);
        _btnUse.gameObject.SetActive(false);
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
}
