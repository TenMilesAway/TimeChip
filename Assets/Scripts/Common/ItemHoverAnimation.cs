using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation")]
    [SerializeField, Min(0f)] private float _hoverScale = 1.1f;
    [SerializeField, Min(0f)] private float _duration = 0.15f;

    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayScaleAnimation(_originalScale * _hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayScaleAnimation(_originalScale);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        transform.localScale = _originalScale;
    }

    private void PlayScaleAnimation(Vector3 targetScale)
    {
        DOTween.Kill(this);
        transform.DOScale(targetScale, _duration)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);
    }
}