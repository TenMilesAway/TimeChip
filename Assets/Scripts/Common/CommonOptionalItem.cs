using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommonOptionalItem : MonoBehaviour, IPointerDownHandler
{
    private const float DetailLongPressDuration = 0.75f;

    private static readonly Color NormalBackgroundColor = Color.white;
    private static readonly Color SelectedBackgroundColor = new Color(0.75f, 0.88f, 1f);

    [SerializeField] private Image _imgIcon;
    [SerializeField] private Button _btnSelect;

    private Image _imgBackground;
    private UIButtonLongPressHold _longPressHold;
    private Action<CommonOptionalItem> _clickHandler;
    private bool _longPressTriggered;

    public int RewardItemId { get; private set; }
    public int RewardItemCount { get; private set; }

    private void Awake()
    {
        _imgBackground = GetComponent<Image>();
        UiButtonAudio.Bind(_btnSelect, OnClick);

        _longPressHold = _btnSelect.GetComponent<UIButtonLongPressHold>();
        if (_longPressHold == null)
        {
            _longPressHold = _btnSelect.gameObject.AddComponent<UIButtonLongPressHold>();
        }

        _longPressHold.SetHoldDuration(DetailLongPressDuration);
        _longPressHold.HoldCompleted += OnLongPressCompleted;
    }

    private void OnDestroy()
    {
        if (_btnSelect != null)
        {
            _btnSelect.onClick.RemoveListener(OnClick);
        }

        if (_longPressHold != null)
        {
            _longPressHold.HoldCompleted -= OnLongPressCompleted;
        }
    }

    public void Configure(
        int itemId,
        int itemCount,
        Sprite icon,
        float iconScale,
        Action<CommonOptionalItem> clickHandler)
    {
        RewardItemId = itemId;
        RewardItemCount = itemCount;
        _clickHandler = clickHandler;
        _longPressTriggered = false;
        _longPressHold?.ResetHoldState();

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * iconScale;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_imgBackground == null)
        {
            return;
        }

        _imgBackground.color = selected ? SelectedBackgroundColor : NormalBackgroundColor;
    }

    private void OnClick()
    {
        if (_longPressTriggered)
        {
            _longPressTriggered = false;
            return;
        }

        _clickHandler?.Invoke(this);
    }

    private void OnLongPressCompleted()
    {
        _longPressTriggered = true;
        OpenItemDetail();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 长按打开详情后，抬起可能落在详情层上而不会触发 Button.onClick，标志会残留到下次点击。
        _longPressTriggered = false;
    }

    private void OpenItemDetail()
    {
        if (RewardItemId <= 0)
        {
            Debug.LogError($"自选道具详情配置不存在: [{RewardItemId}]", this);
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables.ItemTable.GetOrDefault(RewardItemId) == null &&
            tables.BaseTable.GetOrDefault(RewardItemId) == null)
        {
            Debug.LogError($"自选道具详情配置不存在: [{RewardItemId}]", this);
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            param: new OpenUIParam { data = RewardItemId });
    }
}
