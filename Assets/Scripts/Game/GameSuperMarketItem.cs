using System;
using UnityEngine;
using UnityEngine.UI;

public class GameSuperMarketItem : MonoBehaviour
{
    [SerializeField] private Image _imgItem;
    [SerializeField] private Button _btnClick;

    private int _itemIndex = -1;
    private Action<GameSuperMarketItem> _clickHandler;

    public int ItemIndex
    {
        get { return _itemIndex; }
    }

    public RectTransform RectTransform
    {
        get { return transform as RectTransform; }
    }

    private void Awake()
    {
        if (_btnClick != null)
        {
            _btnClick.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (_btnClick != null)
        {
            _btnClick.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void Configure(int itemIndex, Sprite sprite, Action<GameSuperMarketItem> clickHandler)
    {
        _itemIndex = itemIndex;
        _clickHandler = clickHandler;

        if (_imgItem != null)
        {
            _imgItem.sprite = sprite;
            _imgItem.enabled = sprite != null;
        }
    }

    public void SetGameplayVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetInteractable(bool interactable)
    {
        if (_btnClick != null)
        {
            _btnClick.interactable = interactable;
        }
    }

    private void OnButtonClicked()
    {
        _clickHandler?.Invoke(this);
    }
}
