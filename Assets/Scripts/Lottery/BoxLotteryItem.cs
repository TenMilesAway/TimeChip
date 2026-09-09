using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoxLotteryItem : MonoBehaviour
{
    [SerializeField] private GameObject _goSelected; // 选中后展示的 GO
    [SerializeField] private Button _btnSelect;      // 选中按钮
    [SerializeField, Min(0f)] private float _selectedRiseOffset = 28f;

    private RectTransform _rectTransform;
    private Vector2 _defaultAnchoredPosition;
    private Vector3 _defaultScale;

    public event Action<BoxLotteryItem> Selected;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _defaultAnchoredPosition = _rectTransform.anchoredPosition;
        _defaultScale = transform.localScale;
        _btnSelect.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);

        if (_btnSelect != null)
        {
            _btnSelect.onClick.RemoveListener(Select);
        }
    }

    public void SetSelected(bool selected)
    {
        DOTween.Kill(this);
        _goSelected.SetActive(selected);

        Vector2 targetPosition = _defaultAnchoredPosition +
            (selected ? Vector2.up * _selectedRiseOffset : Vector2.zero);
        Vector3 targetScale = _defaultScale * (selected ? 1.08f : 1f);

        DOTween.Sequence()
            .Append(_rectTransform.DOAnchorPos(targetPosition, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack))
            .SetUpdate(true)
            .SetTarget(this);
    }

    public void SetInteractable(bool interactable)
    {
        _btnSelect.interactable = interactable;
    }

    public void PlayDrawAnimation()
    {
        DOTween.Kill(this);

        DOTween.Sequence()
            .Append(transform.DOScale(_defaultScale * 0.94f, 0.08f))
            .Append(transform.DOScale(_defaultScale * 1.18f, 0.18f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(_defaultScale * 1.08f, 0.12f))
            .Append(_rectTransform.DOAnchorPos(
                _defaultAnchoredPosition + Vector2.up * (_selectedRiseOffset + 12f),
                0.1f))
            .Append(_rectTransform.DOAnchorPos(
                _defaultAnchoredPosition + Vector2.up * _selectedRiseOffset,
                0.18f).SetEase(Ease.OutBounce))
            .SetUpdate(true)
            .SetTarget(this);
    }

    public void ResetPresentation()
    {
        DOTween.Kill(this);
        _goSelected.SetActive(false);
        _rectTransform.anchoredPosition = _defaultAnchoredPosition;
        transform.localScale = _defaultScale;
    }

    private void Select()
    {
        Selected?.Invoke(this);
    }
}
