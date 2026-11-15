using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KitchenInventoryItem : MonoBehaviour, IPointerDownHandler
{
    private const float RewardScaleDivisor = 10000f;
    private const float DetailLongPressDuration = 0.75f;

    [SerializeField] private Text _txtType;
    [SerializeField] private Text _txtNum;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Button _btnDetail;
    [SerializeField] private GameObject _goNum;
    [SerializeField] private GameObject _goType;

    private UIButtonLongPressHold _longPressHold;
    private Action<int> _cuttingSelectHandler;
    private bool _useCuttingInteract;
    private bool _longPressTriggered;

    public int ItemId { get; private set; }

    private void Awake()
    {
        EnsureLongPressHold();
        ApplyFridgeInteractMode();
    }

    private void OnDestroy()
    {
        if (_btnDetail != null)
        {
            _btnDetail.onClick.RemoveListener(OnSlotClick);
        }

        if (_longPressHold != null)
        {
            _longPressHold.HoldCompleted -= OnLongPressCompleted;
        }
    }

    public void ApplyFridgeInteractMode()
    {
        _useCuttingInteract = false;
        _cuttingSelectHandler = null;
        _longPressTriggered = false;
        _longPressHold?.ResetHoldState();
        RebindSlotButton();
    }

    public void ApplyCuttingInteractMode(Action<int> onSelect)
    {
        _useCuttingInteract = true;
        _cuttingSelectHandler = onSelect;
        _longPressTriggered = false;
        _longPressHold?.ResetHoldState();
        RebindSlotButton();
    }

    public void SetData(int itemId, Sprite icon, int amount, string type, int rewardScale)
    {
        ItemId = itemId;
        bool hasItem = itemId > 0 && icon != null && amount > 0;
        if (_imgIcon != null)
        {
            _imgIcon.gameObject.SetActive(hasItem);
        }

        if (_goNum != null)
        {
            _goNum.SetActive(hasItem);
        }

        if (_goType != null)
        {
            _goType.SetActive(hasItem);
        }

        if (!hasItem)
        {
            if (_imgIcon != null)
            {
                _imgIcon.sprite = null;
            }

            if (_txtNum != null)
            {
                _txtNum.text = string.Empty;
            }

            if (_txtType != null)
            {
                _txtType.text = string.Empty;
            }

            if (_btnDetail != null)
            {
                _btnDetail.interactable = false;
            }

            return;
        }

        if (_btnDetail != null)
        {
            _btnDetail.interactable = true;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _txtNum.text = $"×{amount}";
        if (_txtType != null)
        {
            _txtType.text = type;
        }
    }

    public void Clear()
    {
        SetData(0, null, 0, string.Empty, 0);
    }

    public void SetSlotInteractable(bool interactable)
    {
        if (_btnDetail == null)
        {
            return;
        }

        _btnDetail.interactable = interactable && ItemId > 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _longPressTriggered = false;
    }

    private void EnsureLongPressHold()
    {
        if (_btnDetail == null)
        {
            return;
        }

        _longPressHold = _btnDetail.GetComponent<UIButtonLongPressHold>();
        if (_longPressHold == null)
        {
            _longPressHold = _btnDetail.gameObject.AddComponent<UIButtonLongPressHold>();
        }

        _longPressHold.SetHoldDuration(DetailLongPressDuration);
        _longPressHold.HoldCompleted -= OnLongPressCompleted;
        _longPressHold.HoldCompleted += OnLongPressCompleted;
    }

    private void RebindSlotButton()
    {
        if (_btnDetail == null)
        {
            return;
        }

        _btnDetail.onClick.RemoveListener(OnSlotClick);
        UiButtonAudio.AttachClickSound(_btnDetail);
        _btnDetail.onClick.AddListener(OnSlotClick);
    }

    private void OnSlotClick()
    {
        if (_longPressTriggered)
        {
            _longPressTriggered = false;
            return;
        }

        if (_useCuttingInteract)
        {
            if (ItemId <= 0)
            {
                return;
            }

            _cuttingSelectHandler?.Invoke(ItemId);
            return;
        }

        OpenItemDetail();
    }

    private void OnLongPressCompleted()
    {
        if (!_useCuttingInteract || ItemId <= 0)
        {
            return;
        }

        _longPressTriggered = true;
        OpenItemDetail();
    }

    private void OpenItemDetail()
    {
        if (ItemId <= 0)
        {
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables.ItemTable.GetOrDefault(ItemId) == null &&
            tables.BaseTable.GetOrDefault(ItemId) == null)
        {
            Debug.LogError($"厨房道具详情配置不存在: [{ItemId}]", this);
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            UILayer.System,
            new OpenUIParam { data = ItemId });
    }
}