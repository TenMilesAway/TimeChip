using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KitchenView : UIBasePanel
{
    private const float MatIconScaleDivisor = 10000f;
    private const string HearthProgressCookingText = "烹饪中...";
    private const string HearthProgressSpoilWarningText = "快糊了!!!";
    private static readonly Color HearthProgressSpoilColor = new Color(0.92f, 0.12f, 0.1f, 1f);
    private static readonly Color HearthProgressSpoilColorBright = new Color(1f, 0.32f, 0.18f, 1f);
    private const float HearthSpoilWarningScalePeakMultiplier = 1.2f;
    private const float HearthSpoilWarningPulseUpDuration = 0.11f;
    private const float HearthSpoilWarningPulseDownDuration = 0.09f;
    private const float HearthSpoilWarningPulseHoldDuration = 0.05f;
    public const int HearthMaterialSlotCount = KitchenBackgroundWork.HearthMaterialSlotCount;
    [Header("上方订单")]
    [SerializeField] private KitchenOrderItem[] _orders;

    [Header("下方厨房")]
    [SerializeField] private Button _btnInventory;
    [SerializeField] private Button _btnHearth;
    [SerializeField] private Button _btnCutting;
    [SerializeField] private Button _btnMenu;
    [SerializeField] private Slider _sliderCutting;
    [SerializeField] private Slider _sliderHearth;
    [SerializeField] private Text _txtHearthProgress;
    [SerializeField] private GameObject _txtCutting;
    [SerializeField] private GameObject _txtHearth;
    [SerializeField] private GameObject _inventoryView;
    [SerializeField] private GameObject _hearthView;
    [SerializeField] private GameObject _cuttingView;
    [SerializeField] private GameObject _menuView;

    [Header("冰箱")]
    [SerializeField] private Button _btnLeft;
    [SerializeField] private Button _btnRight;
    [SerializeField] private Button _btnCloseInventoryView;
    [SerializeField] private KitchenInventoryItem[] _items;

    [Header("砧板")]
    [SerializeField] private Text _txtMatLeft;
    [SerializeField] private Text _txtMatRight;
    [SerializeField] private Image _imgMatLeft;
    [SerializeField] private Image _imgMatRight;
    [SerializeField] private Image _imgProgress;
    [SerializeField] private Button _btnMatLeft;
    [SerializeField] private Button _btnMatRight;
    [SerializeField] private Button _btnCut;
    [SerializeField] private Button _btnCuttingLeft;
    [SerializeField] private Button _btnCuttingRight;
    [SerializeField] private Button _btnCloseCuttingView;
    [SerializeField] private KitchenInventoryItem[] _cuttingItems;

    [Header("灶台")]
    [SerializeField] private Text[] _txtHearthMatLeft;
    [SerializeField] private Text _txtHearthMatRight;
    [SerializeField] private Image[] _imgHearthMatLeft;
    [SerializeField] private Image _imgHearthMatRight;
    [SerializeField] private Image _imgHearthProgress;
    [SerializeField] private Button[] _btnHearthMatLeft;
    [SerializeField] private Button _btnHearthMatRight;
    [SerializeField] private Button _btnCook;
    [SerializeField] private Button _btnHearthLeft;
    [SerializeField] private Button _btnHearthRight;
    [SerializeField] private Button _btnCloseHearthView;
    [SerializeField] private KitchenInventoryItem[] _hearthItems;

    [SerializeField] private Button _btnBack;

    private readonly List<KitchenFridgeEntry> _fridgeItems = new List<KitchenFridgeEntry>();
    private readonly List<KitchenFridgeEntry> _cuttingGridItems = new List<KitchenFridgeEntry>();
    private readonly List<KitchenFridgeEntry> _hearthGridItems = new List<KitchenFridgeEntry>();
    private int _fridgeCurrentPage = 1;
    private int _fridgeRefreshVersion;
    private bool _isInventoryViewVisible;
    private int _cuttingCurrentPage = 1;
    private int _cuttingRefreshVersion;
    private bool _isCuttingViewVisible;
    private int _matIconLoadVersion;
    private int _hearthCurrentPage = 1;
    private int _hearthRefreshVersion;
    private bool _isHearthViewVisible;
    private int _hearthIconLoadVersion;
    private Color _hearthProgressDefaultColor = Color.white;
    private Vector3 _hearthProgressDefaultScale = Vector3.one;
    private bool _hearthSpoilWarningUiActive;

    private KitchenWorkstationState Workstation => KitchenBackgroundWork.Workstation;

    private bool IsCuttingInProgress => KitchenBackgroundWork.IsCuttingInProgress;

    private bool IsCookingInProgress => KitchenBackgroundWork.IsCookingInProgress;

    private bool IsHearthDishSpoilInProgress => KitchenBackgroundWork.IsDishSpoilInProgress;

    private bool ShouldShowHearthProgress =>
        IsCookingInProgress || IsHearthDishSpoilInProgress;

    private void Awake()
    {
        if (_btnBack != null)
        {
            _btnBack.onClick.AddListener(OnClickBack);
        }

        UiButtonAudio.Bind(_btnInventory, OnClickInventory);
        UiButtonAudio.Bind(_btnCloseInventoryView, OnClickCloseInventory);
        UiButtonAudio.Bind(_btnLeft, OnClickFridgePreviousPage);
        UiButtonAudio.Bind(_btnRight, OnClickFridgeNextPage);
        UiButtonAudio.Bind(_btnCutting, OnClickCutting);
        UiButtonAudio.Bind(_btnCut, OnClickCut);
        UiButtonAudio.Bind(_btnMatLeft, OnClickTakeMatLeft);
        UiButtonAudio.Bind(_btnMatRight, OnClickCollectMatRight);
        UiButtonAudio.Bind(_btnCuttingLeft, OnClickCuttingPreviousPage);
        UiButtonAudio.Bind(_btnCuttingRight, OnClickCuttingNextPage);
        UiButtonAudio.Bind(_btnCloseCuttingView, OnClickCloseCutting);
        UiButtonAudio.Bind(_btnHearth, OnClickHearth);
        UiButtonAudio.Bind(_btnCloseHearthView, OnClickCloseHearth);
        UiButtonAudio.Bind(_btnHearthLeft, OnClickHearthPreviousPage);
        UiButtonAudio.Bind(_btnHearthRight, OnClickHearthNextPage);
        UiButtonAudio.Bind(_btnCook, OnClickCook);
        UiButtonAudio.Bind(_btnHearthMatRight, OnClickCollectHearthDish);

        if (_btnHearthMatLeft != null)
        {
            for (int i = 0; i < _btnHearthMatLeft.Length; i++)
            {
                if (_btnHearthMatLeft[i] == null)
                {
                    continue;
                }

                int slotIndex = i;
                UiButtonAudio.Bind(_btnHearthMatLeft[i], () => OnClickTakeHearthMatLeft(slotIndex));
            }
        }

        if (_hearthItems != null)
        {
            for (int i = 0; i < _hearthItems.Length; i++)
            {
                KitchenInventoryItem hearthItem = _hearthItems[i];
                if (hearthItem != null)
                {
                    hearthItem.ApplyCuttingInteractMode(OnSelectHearthInventoryItem);
                }
            }
        }

        if (_cuttingItems != null)
        {
            for (int i = 0; i < _cuttingItems.Length; i++)
            {
                KitchenInventoryItem cuttingItem = _cuttingItems[i];
                if (cuttingItem != null)
                {
                    cuttingItem.ApplyCuttingInteractMode(OnSelectCuttingInventoryItem);
                }
            }
        }

        if (_orders != null)
        {
            for (int i = 0; i < _orders.Length; i++)
            {
                KitchenOrderItem orderItem = _orders[i];
                if (orderItem == null)
                {
                    continue;
                }

                int index = i;
                orderItem.Bind(index, OnDeliverKitchenOrder);
            }
        }

        CacheHearthProgressVisualDefaults();
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        KitchenOrderTurnService.EnsureCurrentTurnOrders(playerInfoManager);
        playerInfoManager.TurnAdvanced -= RefreshOrderSection;
        playerInfoManager.TurnAdvanced += RefreshOrderSection;
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        KitchenBackgroundWork.WorkstationChanged += OnKitchenWorkstationChanged;
        SetInventoryViewVisible(false);
        SetCuttingViewVisible(false);
        SetHearthViewVisible(false);
        SyncKitchenProgressUiOnShow();
        RefreshOrderSection();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.TurnAdvanced -= RefreshOrderSection;
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        KitchenBackgroundWork.WorkstationChanged -= OnKitchenWorkstationChanged;
        base.HideHandle();
    }

    protected override void CloseHandle()
    {
        _fridgeRefreshVersion++;
        _cuttingRefreshVersion++;
        _hearthRefreshVersion++;
        SetInventoryViewVisible(false);
        SetCuttingViewVisible(false);
        SetHearthViewVisible(false);
        ReturnMatLeftToInventoryIfNeeded();
        ReturnHearthMaterialsToInventoryIfNeeded();
        ClearOrderPresentation();
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.TurnAdvanced -= RefreshOrderSection;
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        KitchenBackgroundWork.WorkstationChanged -= OnKitchenWorkstationChanged;
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        StopHearthSpoilWarningAnimation();

        if (_btnInventory != null)
        {
            _btnInventory.onClick.RemoveListener(OnClickInventory);
        }

        if (_btnCloseInventoryView != null)
        {
            _btnCloseInventoryView.onClick.RemoveListener(OnClickCloseInventory);
        }

        if (_btnLeft != null)
        {
            _btnLeft.onClick.RemoveListener(OnClickFridgePreviousPage);
        }

        if (_btnRight != null)
        {
            _btnRight.onClick.RemoveListener(OnClickFridgeNextPage);
        }

        if (_btnCutting != null)
        {
            _btnCutting.onClick.RemoveListener(OnClickCutting);
        }

        if (_btnCut != null)
        {
            _btnCut.onClick.RemoveListener(OnClickCut);
        }

        if (_btnMatLeft != null)
        {
            _btnMatLeft.onClick.RemoveListener(OnClickTakeMatLeft);
        }

        if (_btnMatRight != null)
        {
            _btnMatRight.onClick.RemoveListener(OnClickCollectMatRight);
        }

        if (_btnCuttingLeft != null)
        {
            _btnCuttingLeft.onClick.RemoveListener(OnClickCuttingPreviousPage);
        }

        if (_btnCuttingRight != null)
        {
            _btnCuttingRight.onClick.RemoveListener(OnClickCuttingNextPage);
        }

        if (_btnCloseCuttingView != null)
        {
            _btnCloseCuttingView.onClick.RemoveListener(OnClickCloseCutting);
        }

        if (_btnHearth != null)
        {
            _btnHearth.onClick.RemoveListener(OnClickHearth);
        }

        if (_btnCloseHearthView != null)
        {
            _btnCloseHearthView.onClick.RemoveListener(OnClickCloseHearth);
        }

        if (_btnHearthLeft != null)
        {
            _btnHearthLeft.onClick.RemoveListener(OnClickHearthPreviousPage);
        }

        if (_btnHearthRight != null)
        {
            _btnHearthRight.onClick.RemoveListener(OnClickHearthNextPage);
        }

        if (_btnCook != null)
        {
            _btnCook.onClick.RemoveListener(OnClickCook);
        }

        if (_btnHearthMatRight != null)
        {
            _btnHearthMatRight.onClick.RemoveListener(OnClickCollectHearthDish);
        }

        if (_btnHearthMatLeft != null)
        {
            for (int i = 0; i < _btnHearthMatLeft.Length; i++)
            {
                if (_btnHearthMatLeft[i] != null)
                {
                    _btnHearthMatLeft[i].onClick.RemoveAllListeners();
                }
            }
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.KitchenView;
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        RefreshOrderSection();
        if (_isInventoryViewVisible)
        {
            RefreshFridgeInventory();
        }

        if (_isCuttingViewVisible)
        {
            RefreshCuttingInventory();
        }

        if (_isHearthViewVisible)
        {
            RefreshHearthInventory();
        }
    }

    private void OnClickInventory()
    {
        SetHearthViewVisible(false);
        SetCuttingViewVisible(false);
        SetInventoryViewVisible(true);
        _fridgeCurrentPage = 1;
        RefreshFridgeInventory();
    }

    private void OnClickCutting()
    {
        SetInventoryViewVisible(false);
        SetHearthViewVisible(false);
        _cuttingCurrentPage = 1;
        SetCuttingViewVisible(true);
    }

    private void OnClickHearth()
    {
        SetInventoryViewVisible(false);
        SetCuttingViewVisible(false);
        _hearthCurrentPage = 1;
        SetHearthViewVisible(true);
    }

    private void OnClickCloseHearth()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        SetHearthViewVisible(false);
        _hearthRefreshVersion++;
    }

    private void OnKitchenWorkstationChanged()
    {
        if (IsCuttingInProgress)
        {
            SetCuttingProgressVisible(true);
            SyncCuttingProgressVisual();
            SetCuttingBoardInteractable(false);
        }
        else
        {
            SetCuttingProgressVisible(false);
            ResetCuttingProgressVisual();
            SetCuttingBoardInteractable(true);
            RefreshMatSlotsVisual();
            if (_isCuttingViewVisible)
            {
                RefreshCuttingInventory();
            }
        }

        if (IsCookingInProgress)
        {
            SetCookingProgressVisible(true);
            SyncCookingProgressVisual();
            SetHearthBoardInteractable(false);
        }
        else if (IsHearthDishSpoilInProgress)
        {
            SetCookingProgressVisible(true);
            SyncCookingProgressVisual();
            SetHearthBoardInteractable(true);
        }
        else
        {
            SetCookingProgressVisible(false);
            ResetCookingProgressVisual();
            SetHearthBoardInteractable(true);
            RefreshHearthSlotsVisual();
            if (_isHearthViewVisible)
            {
                RefreshHearthInventory();
            }
        }
    }

    private void SyncKitchenProgressUiOnShow()
    {
        if (IsCuttingInProgress)
        {
            SetCuttingProgressVisible(true);
            SyncCuttingProgressVisual();
        }
        else
        {
            SetCuttingProgressVisible(false);
            ResetCuttingProgressVisual();
        }

        if (ShouldShowHearthProgress)
        {
            SetCookingProgressVisible(true);
            SyncCookingProgressVisual();
        }
        else
        {
            SetCookingProgressVisible(false);
            ResetCookingProgressVisual();
        }
    }

    private void OnClickCloseInventory()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        SetInventoryViewVisible(false);
        _fridgeRefreshVersion++;
    }

    private void OnClickCloseCutting()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        SetCuttingViewVisible(false);
        _cuttingRefreshVersion++;
    }

    private void SetInventoryViewVisible(bool visible)
    {
        _isInventoryViewVisible = visible;
        if (_inventoryView != null)
        {
            _inventoryView.SetActive(visible);
        }
    }

    private void SetCuttingViewVisible(bool visible)
    {
        if (_isCuttingViewVisible && !visible)
        {
            ReturnMatLeftToInventoryIfPresent();
        }

        _isCuttingViewVisible = visible;
        if (_cuttingView != null)
        {
            _cuttingView.SetActive(visible);
        }

        if (visible)
        {
            if (IsCuttingInProgress)
            {
                SetCuttingProgressVisible(true);
                SyncCuttingProgressVisual();
                SetCuttingBoardInteractable(false);
            }
            else
            {
                SetCuttingProgressVisible(false);
                SetCuttingBoardInteractable(true);
            }

            RefreshMatSlotsVisual();
            RefreshCuttingInventory();
        }
        else if (IsCuttingInProgress)
        {
            SetCuttingProgressVisible(true);
            SyncCuttingProgressVisual();
        }
    }

    private void SetHearthViewVisible(bool visible)
    {
        if (_isHearthViewVisible && !visible)
        {
            ReturnHearthMaterialsToInventoryIfPresent();
        }

        _isHearthViewVisible = visible;
        if (_hearthView != null)
        {
            _hearthView.SetActive(visible);
        }

        if (visible)
        {
            if (IsCookingInProgress)
            {
                SetCookingProgressVisible(true);
                SyncCookingProgressVisual();
                SetHearthBoardInteractable(false);
            }
            else if (IsHearthDishSpoilInProgress)
            {
                SetCookingProgressVisible(true);
                SyncCookingProgressVisual();
                SetHearthBoardInteractable(true);
            }
            else
            {
                SetCookingProgressVisible(false);
                SetHearthBoardInteractable(true);
            }

            RefreshHearthSlotsVisual();
            RefreshHearthInventory();
        }
        else if (ShouldShowHearthProgress)
        {
            SetCookingProgressVisible(true);
            SyncCookingProgressVisual();
        }
    }

    private void ReturnMatLeftToInventoryIfPresent()
    {
        if (IsCuttingInProgress || Workstation.CutMatLeftItemId <= 0)
        {
            return;
        }

        PlayerInfoManager.GetInstance().AddItem(Workstation.CutMatLeftItemId, 1);
        Workstation.CutMatLeftItemId = 0;
        RefreshMatSlotsVisual();
        if (_isCuttingViewVisible)
        {
            RefreshCuttingInventory();
        }
    }

    private void SetCuttingProgressVisible(bool visible)
    {
        if (_sliderCutting != null)
        {
            _sliderCutting.gameObject.SetActive(visible);
        }

        if (_txtCutting != null)
        {
            _txtCutting.SetActive(visible);
        }
    }

    private void ResetCuttingProgressVisual()
    {
        ApplyCuttingProgressVisual(0f);
    }

    private void SyncCuttingProgressVisual()
    {
        float progress = Mathf.Clamp01(
            KitchenBackgroundWork.CuttingElapsedSeconds / KitchenBackgroundWork.CutDurationSeconds);
        ApplyCuttingProgressVisual(progress);
    }

    private void ApplyCuttingProgressVisual(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (_imgProgress != null)
        {
            _imgProgress.fillAmount = progress;
        }

        if (_sliderCutting != null)
        {
            _sliderCutting.value = progress;
        }
    }

    private void SetCookingProgressVisible(bool visible)
    {
        if (_sliderHearth != null)
        {
            _sliderHearth.gameObject.SetActive(visible);
        }

        if (_txtHearth != null)
        {
            _txtHearth.SetActive(visible);
        }

        if (!visible)
        {
            ApplyHearthProgressStyle(spoilWarning: false);
        }
    }

    private void ResetCookingProgressVisual()
    {
        ApplyCookingProgressVisual(0f, string.Empty);
    }

    private void SyncCookingProgressVisual()
    {
        if (IsCookingInProgress)
        {
            float progress = Mathf.Clamp01(
                KitchenBackgroundWork.CookingElapsedSeconds / KitchenBackgroundWork.CookDurationSeconds);
            ApplyCookingProgressVisual(progress, HearthProgressCookingText);
            return;
        }

        if (IsHearthDishSpoilInProgress)
        {
            float progress = Mathf.Clamp01(
                KitchenBackgroundWork.DishSpoilElapsedSeconds / KitchenBackgroundWork.DishSpoilDurationSeconds);
            ApplyCookingProgressVisual(progress, HearthProgressSpoilWarningText);
            return;
        }

        ResetCookingProgressVisual();
    }

    private void ApplyCookingProgressVisual(float progress, string statusText)
    {
        progress = Mathf.Clamp01(progress);
        if (_imgHearthProgress != null)
        {
            _imgHearthProgress.fillAmount = progress;
        }

        if (_sliderHearth != null)
        {
            _sliderHearth.value = progress;
        }

        if (_txtHearthProgress != null)
        {
            _txtHearthProgress.text = statusText ?? string.Empty;
            ApplyHearthProgressStyle(statusText == HearthProgressSpoilWarningText);
        }
    }

    private void CacheHearthProgressVisualDefaults()
    {
        if (_txtHearthProgress == null)
        {
            return;
        }

        _hearthProgressDefaultColor = _txtHearthProgress.color;
        _hearthProgressDefaultScale = _txtHearthProgress.rectTransform.localScale;
    }

    private void ApplyHearthProgressStyle(bool spoilWarning)
    {
        if (_txtHearthProgress == null)
        {
            return;
        }

        if (spoilWarning)
        {
            if (_hearthSpoilWarningUiActive)
            {
                return;
            }

            _hearthSpoilWarningUiActive = true;
            _txtHearthProgress.color = HearthProgressSpoilColor;
            StartHearthSpoilWarningAnimation();
            return;
        }

        if (!_hearthSpoilWarningUiActive)
        {
            _txtHearthProgress.color = _hearthProgressDefaultColor;
            return;
        }

        StopHearthSpoilWarningAnimation();
    }

    private void StartHearthSpoilWarningAnimation()
    {
        if (_txtHearthProgress == null)
        {
            return;
        }

        DOTween.Kill(_txtHearthProgress);
        RectTransform progressRect = _txtHearthProgress.rectTransform;
        progressRect.localScale = _hearthProgressDefaultScale;

        DOTween.Sequence()
            .Append(progressRect
                .DOScale(_hearthProgressDefaultScale * HearthSpoilWarningScalePeakMultiplier,
                    HearthSpoilWarningPulseUpDuration)
                .SetEase(Ease.OutBack))
            .Join(_txtHearthProgress.DOColor(HearthProgressSpoilColorBright, HearthSpoilWarningPulseUpDuration))
            .Append(progressRect
                .DOScale(_hearthProgressDefaultScale, HearthSpoilWarningPulseDownDuration)
                .SetEase(Ease.InQuad))
            .Join(_txtHearthProgress.DOColor(HearthProgressSpoilColor, HearthSpoilWarningPulseDownDuration))
            .AppendInterval(HearthSpoilWarningPulseHoldDuration)
            .SetLoops(-1, LoopType.Restart)
            .SetTarget(_txtHearthProgress);
    }

    private void StopHearthSpoilWarningAnimation()
    {
        _hearthSpoilWarningUiActive = false;
        if (_txtHearthProgress == null)
        {
            return;
        }

        DOTween.Kill(_txtHearthProgress);
        _txtHearthProgress.color = _hearthProgressDefaultColor;
        _txtHearthProgress.rectTransform.localScale = _hearthProgressDefaultScale;
    }

    private void OnClickFridgePreviousPage()
    {
        if (_fridgeCurrentPage <= 1)
        {
            return;
        }

        _fridgeCurrentPage--;
        RefreshFridgeInventoryPage();
    }

    private void OnClickFridgeNextPage()
    {
        int maxPage = KitchenFridgeInventory.GetMaxPage(_fridgeItems.Count);
        if (_fridgeCurrentPage >= maxPage)
        {
            return;
        }

        _fridgeCurrentPage++;
        RefreshFridgeInventoryPage();
    }

    private void RefreshFridgeInventory()
    {
        KitchenFridgeInventory.CollectOwnedItems(_fridgeItems);
        _fridgeCurrentPage = Mathf.Clamp(
            _fridgeCurrentPage,
            1,
            KitchenFridgeInventory.GetMaxPage(_fridgeItems.Count));
        RefreshFridgeInventoryPage();
    }

    private void RefreshFridgeInventoryPage()
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogError("厨房冰箱未配置展示格子。", this);
            return;
        }

        int refreshVersion = ++_fridgeRefreshVersion;
        int maxPage = KitchenFridgeInventory.GetMaxPage(_fridgeItems.Count);
        _fridgeCurrentPage = Mathf.Clamp(_fridgeCurrentPage, 1, maxPage);

        if (_btnLeft != null)
        {
            _btnLeft.interactable = _fridgeCurrentPage > 1;
        }

        if (_btnRight != null)
        {
            _btnRight.interactable = _fridgeCurrentPage < maxPage;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null)
            {
                _items[i].Clear();
            }
        }

        RefreshFridgeInventoryPageAsync(refreshVersion);
    }

    private async void RefreshFridgeInventoryPageAsync(int refreshVersion)
    {
        if (_items == null)
        {
            return;
        }

        int firstItemIndex = (_fridgeCurrentPage - 1) * KitchenFridgeInventory.SlotsPerPage;
        string resourceTag = $"{GetInstanceID()}.Fridge";
        for (int slotIndex = 0; slotIndex < _items.Length; slotIndex++)
        {
            int itemIndex = firstItemIndex + slotIndex;
            KitchenInventoryItem slotItem = _items[slotIndex];
            if (slotItem == null)
            {
                continue;
            }

            if (itemIndex >= _fridgeItems.Count)
            {
                slotItem.Clear();
                continue;
            }

            KitchenFridgeEntry entry = _fridgeItems[itemIndex];
            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
                entry.Item.Icon,
                resourceTag);
            if (refreshVersion != _fridgeRefreshVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (icon == null)
            {
                Debug.LogError(
                    $"冰箱道具图标加载失败: [{entry.Item.Id}], [{entry.Item.Icon}]",
                    this);
                slotItem.Clear();
                continue;
            }

            slotItem.ApplyFridgeInteractMode();
            slotItem.SetData(
                entry.Item.Id,
                icon,
                entry.Amount,
                entry.Item.Type,
                entry.Item.RewardScale);
        }
    }

    private void OnClickCuttingPreviousPage()
    {
        if (_cuttingCurrentPage <= 1)
        {
            return;
        }

        _cuttingCurrentPage--;
        RefreshCuttingInventoryPage();
    }

    private void OnClickCuttingNextPage()
    {
        int maxPage = KitchenFridgeInventory.GetMaxPage(_cuttingGridItems.Count);
        if (_cuttingCurrentPage >= maxPage)
        {
            return;
        }

        _cuttingCurrentPage++;
        RefreshCuttingInventoryPage();
    }

    private void RefreshCuttingInventory()
    {
        KitchenFridgeInventory.CollectOwnedCuttingBoardItems(_cuttingGridItems);
        _cuttingCurrentPage = Mathf.Clamp(
            _cuttingCurrentPage,
            1,
            KitchenFridgeInventory.GetMaxPage(_cuttingGridItems.Count));
        RefreshCuttingInventoryPage();
    }

    private void RefreshCuttingInventoryPage()
    {
        if (_cuttingItems == null || _cuttingItems.Length == 0)
        {
            Debug.LogError("砧板储物格未配置。", this);
            return;
        }

        int refreshVersion = ++_cuttingRefreshVersion;
        int maxPage = KitchenFridgeInventory.GetMaxPage(_cuttingGridItems.Count);
        _cuttingCurrentPage = Mathf.Clamp(_cuttingCurrentPage, 1, maxPage);

        if (_btnCuttingLeft != null)
        {
            _btnCuttingLeft.interactable = _cuttingCurrentPage > 1;
        }

        if (_btnCuttingRight != null)
        {
            _btnCuttingRight.interactable = _cuttingCurrentPage < maxPage;
        }

        for (int i = 0; i < _cuttingItems.Length; i++)
        {
            if (_cuttingItems[i] != null)
            {
                _cuttingItems[i].Clear();
            }
        }

        RefreshCuttingInventoryPageAsync(refreshVersion);
    }

    private async void RefreshCuttingInventoryPageAsync(int refreshVersion)
    {
        if (_cuttingItems == null)
        {
            return;
        }

        int firstItemIndex = (_cuttingCurrentPage - 1) * KitchenFridgeInventory.SlotsPerPage;
        string resourceTag = $"{GetInstanceID()}.Cutting";
        for (int slotIndex = 0; slotIndex < _cuttingItems.Length; slotIndex++)
        {
            int itemIndex = firstItemIndex + slotIndex;
            KitchenInventoryItem slotItem = _cuttingItems[slotIndex];
            if (slotItem == null)
            {
                continue;
            }

            slotItem.ApplyCuttingInteractMode(OnSelectCuttingInventoryItem);
            if (itemIndex >= _cuttingGridItems.Count)
            {
                slotItem.Clear();
                continue;
            }

            KitchenFridgeEntry entry = _cuttingGridItems[itemIndex];
            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
                entry.Item.Icon,
                resourceTag);
            if (refreshVersion != _cuttingRefreshVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (icon == null)
            {
                Debug.LogError(
                    $"砧板储物图标加载失败: [{entry.Item.Id}], [{entry.Item.Icon}]",
                    this);
                slotItem.Clear();
                continue;
            }

            slotItem.SetData(
                entry.Item.Id,
                icon,
                entry.Amount,
                entry.Item.Type,
                entry.Item.RewardScale);
        }

        ApplyCuttingInventoryGridInteractable(!IsCuttingInProgress);
    }

    private void OnSelectCuttingInventoryItem(int itemId)
    {
        if (IsCuttingInProgress)
        {
            return;
        }

        if (KitchenCutRecipeCatalog.IsAlreadyCutMaterial(itemId))
        {
            CommonTipView.Show("该材料已经切过了");
            return;
        }

        if (!KitchenCutRecipeCatalog.TryGetRecipeForInput(itemId, out _))
        {
            CommonTipView.Show("该材料无法在此切块");
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();

        if (Workstation.CutMatLeftItemId > 0)
        {
            if (Workstation.CutMatLeftItemId == itemId)
            {
                return;
            }

            playerInfoManager.AddItem(Workstation.CutMatLeftItemId, 1);
            Workstation.CutMatLeftItemId = 0;
        }

        if (playerInfoManager.GetItemCount(itemId) <= 0)
        {
            CommonTipView.Show("背包中没有该材料");
            return;
        }

        if (!playerInfoManager.TryConsumeItem(itemId))
        {
            CommonTipView.Show("背包中没有该材料");
            return;
        }

        Workstation.CutMatLeftItemId = itemId;
        RefreshMatSlotsVisual();
        RefreshCuttingInventory();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void OnClickTakeMatLeft()
    {
        if (IsCuttingInProgress || Workstation.CutMatLeftItemId <= 0)
        {
            return;
        }

        PlayerInfoManager.GetInstance().AddItem(Workstation.CutMatLeftItemId, 1);
        Workstation.CutMatLeftItemId = 0;
        RefreshMatSlotsVisual();
        RefreshCuttingInventory();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void OnClickCollectMatRight()
    {
        if (IsCuttingInProgress || Workstation.CutMatRightItemId <= 0)
        {
            return;
        }

        int itemId = Workstation.CutMatRightItemId;
        PlayerInfoManager.GetInstance().AddItem(itemId, 1);
        Workstation.CutMatRightItemId = 0;
        RefreshMatSlotsVisual();
        RefreshCuttingInventory();
        PresentKitchenCollectedReward(itemId);
    }

    private void OnClickCut()
    {
        if (IsCuttingInProgress)
        {
            return;
        }

        if (Workstation.CutMatLeftItemId <= 0)
        {
            CommonTipView.Show("请先将可切材料放到砧板左侧");
            return;
        }

        if (Workstation.CutMatRightItemId > 0)
        {
            CommonTipView.Show("请先将右侧切好的材料收入背包");
            return;
        }

        if (!KitchenCutRecipeCatalog.TryGetRecipeForInput(Workstation.CutMatLeftItemId, out cfg.Kitchen recipe))
        {
            CommonTipView.Show("该材料无法切块");
            return;
        }

        if (!KitchenBackgroundWork.TryStartCutting(recipe))
        {
            return;
        }

        SetCuttingBoardInteractable(false);
        SetCuttingProgressVisible(true);
        SyncCuttingProgressVisual();
    }

    private void ReturnMatLeftToInventoryIfNeeded()
    {
        if (IsCuttingInProgress)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (Workstation.CutMatLeftItemId > 0)
        {
            playerInfoManager.AddItem(Workstation.CutMatLeftItemId, 1);
            Workstation.CutMatLeftItemId = 0;
        }

        RefreshMatSlotsVisual();
    }

    private void SetCuttingBoardInteractable(bool interactable)
    {
        if (_btnCut != null)
        {
            _btnCut.interactable = interactable;
        }

        if (_btnMatLeft != null)
        {
            _btnMatLeft.interactable = interactable;
        }

        if (_btnMatRight != null)
        {
            _btnMatRight.interactable = interactable;
        }

        if (_btnCuttingLeft != null)
        {
            _btnCuttingLeft.interactable = _cuttingCurrentPage > 1;
        }

        if (_btnCuttingRight != null)
        {
            int maxPage = KitchenFridgeInventory.GetMaxPage(_cuttingGridItems.Count);
            _btnCuttingRight.interactable = _cuttingCurrentPage < maxPage;
        }

        ApplyCuttingInventoryGridInteractable(interactable);
    }

    private void ApplyCuttingInventoryGridInteractable(bool interactable)
    {
        if (_cuttingItems == null)
        {
            return;
        }

        for (int i = 0; i < _cuttingItems.Length; i++)
        {
            KitchenInventoryItem cuttingItem = _cuttingItems[i];
            if (cuttingItem != null)
            {
                cuttingItem.SetSlotInteractable(interactable);
            }
        }
    }

    private void RefreshMatSlotsVisual()
    {
        _matIconLoadVersion++;
        RefreshMatLabel(_txtMatLeft, Workstation.CutMatLeftItemId);
        RefreshMatLabel(_txtMatRight, Workstation.CutMatRightItemId);
        LoadMatIconAsync(_imgMatLeft, Workstation.CutMatLeftItemId, _matIconLoadVersion);
        LoadMatIconAsync(_imgMatRight, Workstation.CutMatRightItemId, _matIconLoadVersion);
    }

    private static void RefreshMatLabel(Text target, int itemId)
    {
        if (target == null)
        {
            return;
        }

        if (itemId <= 0)
        {
            target.text = string.Empty;
            target.gameObject.SetActive(false);
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig == null)
        {
            target.text = string.Empty;
            target.gameObject.SetActive(false);
            return;
        }

        target.text = itemConfig.Name;
        target.gameObject.SetActive(true);
    }

    private async void LoadMatIconAsync(Image target, int itemId, int loadVersion)
    {
        if (target == null)
        {
            return;
        }

        if (itemId <= 0)
        {
            ClearMatImage(target);
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig == null)
        {
            ClearMatImage(target);
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            $"{GetInstanceID()}.Mat");
        if (loadVersion != _matIconLoadVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"砧板材料图标加载失败: [{itemId}]", this);
            ClearMatImage(target);
            return;
        }

        target.gameObject.SetActive(true);
        target.sprite = icon;
        target.SetNativeSize();
        target.rectTransform.localScale = Vector3.one * (itemConfig.RewardScale / MatIconScaleDivisor);
    }

    private static void ClearMatImage(Image target)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = null;
        target.gameObject.SetActive(false);
    }

    private void OnClickHearthPreviousPage()
    {
        if (_hearthCurrentPage <= 1)
        {
            return;
        }

        _hearthCurrentPage--;
        RefreshHearthInventoryPage();
    }

    private void OnClickHearthNextPage()
    {
        int maxPage = KitchenFridgeInventory.GetMaxPage(_hearthGridItems.Count);
        if (_hearthCurrentPage >= maxPage)
        {
            return;
        }

        _hearthCurrentPage++;
        RefreshHearthInventoryPage();
    }

    private void RefreshHearthInventory()
    {
        KitchenFridgeInventory.CollectOwnedHearthPanelItems(_hearthGridItems);
        _hearthCurrentPage = Mathf.Clamp(
            _hearthCurrentPage,
            1,
            KitchenFridgeInventory.GetMaxPage(_hearthGridItems.Count));
        RefreshHearthInventoryPage();
    }

    private void RefreshHearthInventoryPage()
    {
        if (_hearthItems == null || _hearthItems.Length == 0)
        {
            Debug.LogError("灶台储物格未配置。", this);
            return;
        }

        int refreshVersion = ++_hearthRefreshVersion;
        int maxPage = KitchenFridgeInventory.GetMaxPage(_hearthGridItems.Count);
        _hearthCurrentPage = Mathf.Clamp(_hearthCurrentPage, 1, maxPage);

        if (_btnHearthLeft != null)
        {
            _btnHearthLeft.interactable = _hearthCurrentPage > 1;
        }

        if (_btnHearthRight != null)
        {
            _btnHearthRight.interactable = _hearthCurrentPage < maxPage;
        }

        for (int i = 0; i < _hearthItems.Length; i++)
        {
            if (_hearthItems[i] != null)
            {
                _hearthItems[i].Clear();
            }
        }

        RefreshHearthInventoryPageAsync(refreshVersion);
    }

    private async void RefreshHearthInventoryPageAsync(int refreshVersion)
    {
        if (_hearthItems == null)
        {
            return;
        }

        int firstItemIndex = (_hearthCurrentPage - 1) * KitchenFridgeInventory.SlotsPerPage;
        string resourceTag = $"{GetInstanceID()}.Hearth";
        for (int slotIndex = 0; slotIndex < _hearthItems.Length; slotIndex++)
        {
            int itemIndex = firstItemIndex + slotIndex;
            KitchenInventoryItem slotItem = _hearthItems[slotIndex];
            if (slotItem == null)
            {
                continue;
            }

            slotItem.ApplyCuttingInteractMode(OnSelectHearthInventoryItem);
            if (itemIndex >= _hearthGridItems.Count)
            {
                slotItem.Clear();
                continue;
            }

            KitchenFridgeEntry entry = _hearthGridItems[itemIndex];
            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
                entry.Item.Icon,
                resourceTag);
            if (refreshVersion != _hearthRefreshVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (icon == null)
            {
                Debug.LogError(
                    $"灶台储物图标加载失败: [{entry.Item.Id}], [{entry.Item.Icon}]",
                    this);
                slotItem.Clear();
                continue;
            }

            slotItem.SetData(
                entry.Item.Id,
                icon,
                entry.Amount,
                entry.Item.Type,
                entry.Item.RewardScale);
        }

        ApplyHearthInventoryGridInteractable(!IsCookingInProgress);
    }

    private void OnSelectHearthInventoryItem(int itemId)
    {
        if (IsCookingInProgress)
        {
            return;
        }

        int emptySlotIndex = FindFirstEmptyHearthMaterialSlot();
        if (emptySlotIndex < 0)
        {
            CommonTipView.Show("材料已满");
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.GetItemCount(itemId) <= 0)
        {
            CommonTipView.Show("背包中没有该材料");
            return;
        }

        if (!playerInfoManager.TryConsumeItem(itemId))
        {
            CommonTipView.Show("背包中没有该材料");
            return;
        }

        Workstation.HearthMatSlotItemIds[emptySlotIndex] = itemId;
        RefreshHearthSlotsVisual();
        RefreshHearthInventory();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private int FindFirstEmptyHearthMaterialSlot()
    {
        for (int i = 0; i < Workstation.HearthMatSlotItemIds.Length && i < HearthMaterialSlotCount; i++)
        {
            if (Workstation.HearthMatSlotItemIds[i] <= 0)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnClickTakeHearthMatLeft(int slotIndex)
    {
        if (IsCookingInProgress ||
            slotIndex < 0 ||
            slotIndex >= Workstation.HearthMatSlotItemIds.Length)
        {
            return;
        }

        int itemId = Workstation.HearthMatSlotItemIds[slotIndex];
        if (itemId <= 0)
        {
            return;
        }

        PlayerInfoManager.GetInstance().AddItem(itemId, 1);
        Workstation.HearthMatSlotItemIds[slotIndex] = 0;
        RefreshHearthSlotsVisual();
        RefreshHearthInventory();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void OnClickCollectHearthDish()
    {
        if (IsCookingInProgress || Workstation.HearthDishItemId <= 0)
        {
            return;
        }

        int itemId = Workstation.HearthDishItemId;
        PlayerInfoManager.GetInstance().AddItem(itemId, 1);
        Workstation.HearthDishItemId = 0;
        RefreshHearthSlotsVisual();
        RefreshHearthInventory();
        PresentKitchenCollectedReward(itemId);
    }

    private void OnClickCook()
    {
        if (IsCookingInProgress)
        {
            return;
        }

        if (Workstation.HearthDishItemId > 0)
        {
            CommonTipView.Show("请先将右侧成品菜收入背包");
            return;
        }

        if (!KitchenCookRecipeCatalog.TryCollectPlacedCookMaterials(
                Workstation.HearthMatSlotItemIds,
                out List<int> cookMaterialInputs))
        {
            CommonTipView.Show("请先在灶台放入材料");
            return;
        }

        if (cookMaterialInputs.Count < KitchenCookRecipeCatalog.MinCookMaterialCount)
        {
            CommonTipView.Show("烹饪至少需要放入 2 个材料");
            return;
        }

        if (KitchenCookRecipeCatalog.TryMatchCookRecipe(
                Workstation.HearthMatSlotItemIds,
                out cfg.Kitchen recipe,
                out List<int> matchedCookMaterials))
        {
            if (!KitchenBackgroundWork.TryStartCooking(recipe, matchedCookMaterials))
            {
                return;
            }
        }
        else if (!KitchenBackgroundWork.TryStartMishmashCooking(cookMaterialInputs))
        {
            return;
        }

        SetHearthBoardInteractable(false);
        SetCookingProgressVisible(true);
        SyncCookingProgressVisual();
    }

    private void ReturnHearthMaterialsToInventoryIfPresent()
    {
        if (IsCookingInProgress)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < Workstation.HearthMatSlotItemIds.Length; i++)
        {
            int itemId = Workstation.HearthMatSlotItemIds[i];
            if (itemId > 0)
            {
                playerInfoManager.AddItem(itemId, 1);
                Workstation.HearthMatSlotItemIds[i] = 0;
            }
        }
    }

    private void ReturnHearthMaterialsToInventoryIfNeeded()
    {
        if (IsCookingInProgress)
        {
            return;
        }

        ReturnHearthMaterialsToInventoryIfPresent();

        RefreshHearthSlotsVisual();
    }

    private void SetHearthBoardInteractable(bool interactable)
    {
        if (_btnCook != null)
        {
            _btnCook.interactable = interactable;
        }

        if (_btnHearthMatRight != null)
        {
            _btnHearthMatRight.interactable = interactable;
        }

        if (_btnHearthMatLeft != null)
        {
            for (int i = 0; i < _btnHearthMatLeft.Length; i++)
            {
                if (_btnHearthMatLeft[i] != null)
                {
                    _btnHearthMatLeft[i].interactable = interactable;
                }
            }
        }

        if (_btnHearthLeft != null)
        {
            _btnHearthLeft.interactable = _hearthCurrentPage > 1;
        }

        if (_btnHearthRight != null)
        {
            int maxPage = KitchenFridgeInventory.GetMaxPage(_hearthGridItems.Count);
            _btnHearthRight.interactable = _hearthCurrentPage < maxPage;
        }

        ApplyHearthInventoryGridInteractable(interactable);
    }

    private void ApplyHearthInventoryGridInteractable(bool interactable)
    {
        if (_hearthItems == null)
        {
            return;
        }

        for (int i = 0; i < _hearthItems.Length; i++)
        {
            KitchenInventoryItem hearthItem = _hearthItems[i];
            if (hearthItem != null)
            {
                hearthItem.SetSlotInteractable(interactable);
            }
        }
    }

    private void RefreshHearthSlotsVisual()
    {
        _hearthIconLoadVersion++;
        for (int i = 0; i < HearthMaterialSlotCount; i++)
        {
            int itemId = i < Workstation.HearthMatSlotItemIds.Length ? Workstation.HearthMatSlotItemIds[i] : 0;
            Text nameText = GetHearthMatLeftText(i);
            Image icon = GetHearthMatLeftImage(i);
            RefreshMatLabel(nameText, itemId);
            LoadHearthMatIconAsync(icon, itemId, _hearthIconLoadVersion);
        }

        RefreshMatLabel(_txtHearthMatRight, Workstation.HearthDishItemId);
        LoadHearthMatIconAsync(_imgHearthMatRight, Workstation.HearthDishItemId, _hearthIconLoadVersion);
    }

    private Text GetHearthMatLeftText(int slotIndex)
    {
        if (_txtHearthMatLeft == null ||
            slotIndex < 0 ||
            slotIndex >= _txtHearthMatLeft.Length)
        {
            return null;
        }

        return _txtHearthMatLeft[slotIndex];
    }

    private Image GetHearthMatLeftImage(int slotIndex)
    {
        if (_imgHearthMatLeft == null ||
            slotIndex < 0 ||
            slotIndex >= _imgHearthMatLeft.Length)
        {
            return null;
        }

        return _imgHearthMatLeft[slotIndex];
    }

    private async void LoadHearthMatIconAsync(Image target, int itemId, int loadVersion)
    {
        if (target == null)
        {
            return;
        }

        if (itemId <= 0)
        {
            ClearMatImage(target);
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig == null)
        {
            ClearMatImage(target);
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            $"{GetInstanceID()}.HearthMat");
        if (loadVersion != _hearthIconLoadVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"灶台材料图标加载失败: [{itemId}]", this);
            ClearMatImage(target);
            return;
        }

        target.gameObject.SetActive(true);
        target.sprite = icon;
        target.SetNativeSize();
        target.rectTransform.localScale = Vector3.one * (itemConfig.RewardScale / MatIconScaleDivisor);
    }

    private void RefreshOrderSection()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnKitchenOrders(out List<PlayerKitchenOrder> orders))
        {
            KitchenOrderTurnService.EnsureCurrentTurnOrders(playerInfoManager);
            if (!playerInfoManager.TryGetCurrentTurnKitchenOrders(out orders))
            {
                Debug.LogError("厨房订单刷新失败。", this);
                return;
            }
        }

        if (_orders == null)
        {
            return;
        }

        string resourceTag = GetInstanceID().ToString();
        int displayCount = Mathf.Min(_orders.Length, orders.Count);
        for (int i = 0; i < displayCount; i++)
        {
            KitchenOrderItem orderItem = _orders[i];
            if (orderItem == null)
            {
                continue;
            }

            orderItem.SetData(orders[i], resourceTag);
        }

        for (int i = displayCount; i < _orders.Length; i++)
        {
            if (_orders[i] != null)
            {
                _orders[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnDeliverKitchenOrder(int orderIndex)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryDeliverKitchenOrderDish(
                orderIndex,
                out bool orderJustCompleted,
                out List<CommonRewardItemData> completionRewards,
                out int? simulationCoinDisplayStart))
        {
            CommonTipView.Show("背包中没有当前订单所需的菜品");
            return;
        }

        RefreshOrderSection();
        if (!orderJustCompleted ||
            completionRewards == null ||
            completionRewards.Count == 0)
        {
            GameManager.Audio.Play(AudioDefine.SFXBuy);
            return;
        }

        if (simulationCoinDisplayStart.HasValue)
        {
            MainMenuView mainMenuView = UIManager.GetInstance()
                .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
            if (mainMenuView != null)
            {
                mainMenuView.SetSimulationCoinDisplay(simulationCoinDisplayStart.Value);
            }
        }

        UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
        {
            data = completionRewards,
            rewardsAlreadyGranted = true,
            simulationCoinDisplayStart = simulationCoinDisplayStart
        });
    }

    private void ClearOrderPresentation()
    {
        if (_orders == null)
        {
            return;
        }

        for (int i = 0; i < _orders.Length; i++)
        {
            if (_orders[i] != null)
            {
                _orders[i].ClearPresentation();
            }
        }
    }

    private void OnClickBack()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private static void PresentKitchenCollectedReward(int itemId)
    {
        if (itemId <= 0)
        {
            return;
        }

        UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
        {
            data = new List<CommonRewardItemData>
            {
                new CommonRewardItemData
                {
                    itemId = itemId,
                    itemCount = 1
                }
            },
            rewardsAlreadyGranted = true
        });
    }
}