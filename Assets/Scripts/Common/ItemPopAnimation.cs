using DG.Tweening;
using UnityEngine;

public class ItemPopAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField, Min(0f)] private float _overshootScale = 1.15f;
    [SerializeField, Min(0f)] private float _popDuration = 0.2f;
    [SerializeField, Min(0f)] private float _settleDuration = 0.12f;

    private Vector3 _originalScale;

    private void OnEnable()
    {
        _originalScale = transform.localScale;

        DOTween.Kill(this);

        transform.localScale = Vector3.zero;

        DOTween.Sequence()
            .Append(transform.DOScale(_originalScale * _overshootScale, _popDuration).SetEase(Ease.OutBack))
            .Append(transform.DOScale(_originalScale, _settleDuration).SetEase(Ease.OutQuad))
            .SetTarget(this);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        transform.localScale = _originalScale;
    }
}
