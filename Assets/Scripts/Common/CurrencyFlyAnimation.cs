using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 在同一 Canvas 内播放货币图标从起点爆炸并飞往终点的动画。
/// </summary>
public class CurrencyFlyAnimation : MonoBehaviour
{
    private const float ExplosionDuration = 0.2f;
    private const float FlightDuration = 0.45f;
    private const float ExplosionRadius = 70f;
    private const float IconSize = 36f;

    private readonly List<GameObject> _icons = new List<GameObject>();
    private bool _hasTriggeredFirstArrival;
    private int _remainingIconCount;
    private Action _firstArrivalCallback;
    private Action _completedCallback;
    private RectTransform _target;
    private CurrencyFlyTargetFeedback _targetFeedback;

    /// <summary>
    /// 播放飞币动画。
    /// </summary>
    /// <param name="start">图标爆炸的起点。</param>
    /// <param name="target">图标飞行的终点。</param>
    /// <param name="icon">用于飞行的图标。</param>
    /// <param name="iconCount">爆炸产生的图标数量。</param>
    /// <param name="waitForFirstArrival">是否在第一个图标抵达后执行回调。</param>
    /// <param name="firstArrivalCallback">第一个图标抵达时执行的回调。</param>
    /// <param name="completedCallback">全部图标动画结束时执行的回调。</param>
    /// <returns>成功创建动画时返回 true。</returns>
    public static bool Play(
        RectTransform start,
        RectTransform target,
        Sprite icon,
        int iconCount,
        bool waitForFirstArrival,
        Action firstArrivalCallback,
        Action completedCallback = null)
    {
        if (start == null || target == null || icon == null || iconCount <= 0)
        {
            return false;
        }

        Canvas canvas = start.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.rootCanvas != target.GetComponentInParent<Canvas>()?.rootCanvas)
        {
            return false;
        }

        GameObject animationObject = new GameObject("CurrencyFlyAnimation", typeof(RectTransform));
        animationObject.transform.SetParent(canvas.rootCanvas.transform, false);

        CurrencyFlyAnimation animation = animationObject.AddComponent<CurrencyFlyAnimation>();
        animation.PlayInternal(
            start,
            target,
            icon,
            iconCount,
            waitForFirstArrival ? firstArrivalCallback : null,
            completedCallback);

        if (!waitForFirstArrival)
        {
            firstArrivalCallback?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 创建飞行图标并播放爆炸、飞行和目标缩放动画。
    /// </summary>
    private void PlayInternal(
        RectTransform start,
        RectTransform target,
        Sprite icon,
        int iconCount,
        Action firstArrivalCallback,
        Action completedCallback)
    {
        _target = target;
        _targetFeedback = target.GetComponent<CurrencyFlyTargetFeedback>();
        if (_targetFeedback == null)
        {
            _targetFeedback = target.gameObject.AddComponent<CurrencyFlyTargetFeedback>();
        }

        _firstArrivalCallback = firstArrivalCallback;
        _completedCallback = completedCallback;
        _remainingIconCount = iconCount;

        RectTransform layer = transform.parent as RectTransform;
        Vector2 startPosition = ToLocalPosition(layer, start.position);
        Vector2 targetPosition = ToLocalPosition(layer, target.position);

        for (int i = 0; i < iconCount; i++)
        {
            GameObject iconObject = CreateIcon(icon, layer, startPosition);
            _icons.Add(iconObject);

            Vector2 explosionPosition = startPosition + UnityEngine.Random.insideUnitCircle * ExplosionRadius;
            float delay = UnityEngine.Random.Range(0f, 0.2f);

            DOTween.Sequence()
                .Append(iconObject.GetComponent<RectTransform>()
                    .DOAnchorPos(explosionPosition, ExplosionDuration)
                    .SetEase(Ease.OutQuad))
                .AppendInterval(delay)
                .Append(iconObject.GetComponent<RectTransform>()
                    .DOAnchorPos(targetPosition, FlightDuration)
                    .SetEase(Ease.InQuad))
                .SetTarget(iconObject)
                .OnComplete(() => OnIconArrived(iconObject));
        }
    }

    /// <summary>
    /// 创建单个用于飞行的图标。
    /// </summary>
    private static GameObject CreateIcon(Sprite icon, RectTransform parent, Vector2 position)
    {
        GameObject iconObject = new GameObject(
            "CurrencyIcon",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasRenderer),
            typeof(SortingGroup),
            typeof(Image));
        RectTransform rectTransform = iconObject.transform as RectTransform;
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = Vector2.one * IconSize;
        rectTransform.anchoredPosition = position;

        Canvas canvas = iconObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "System";
        canvas.sortingOrder = 100;

        SortingGroup sortingGroup = iconObject.GetComponent<SortingGroup>();
        sortingGroup.sortingLayerName = "System";
        sortingGroup.sortingOrder = 100;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return iconObject;
    }

    /// <summary>
    /// 处理单个图标抵达终点后的目标缩放与完成回调。
    /// </summary>
    private void OnIconArrived(GameObject iconObject)
    {
        if (!_hasTriggeredFirstArrival)
        {
            _hasTriggeredFirstArrival = true;
            _firstArrivalCallback?.Invoke();
        }

        _targetFeedback.PlayArrivalFeedback();

        Destroy(iconObject);
        _remainingIconCount--;

        if (_remainingIconCount == 0)
        {
            _completedCallback?.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 管理飞币终点的缩放反馈，保证连续抵达时不会超过设定的最大缩放。
    /// </summary>
    public class CurrencyFlyTargetFeedback : MonoBehaviour
    {
        private const float PeakScaleMultiplier = 1.1f;
        private const float ExpandDuration = 0.1f;
        private const float RestoreDuration = 0.2f;

        private Vector3 _originalScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// 播放单次抵达反馈；新的抵达会重置回弹计时并保持最大为原始缩放的 1.1 倍。
        /// </summary>
        public void PlayArrivalFeedback()
        {
            _scaleTween?.Kill();

            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(_originalScale * PeakScaleMultiplier, ExpandDuration))
                .Append(transform.DOScale(_originalScale, RestoreDuration))
                .SetTarget(this);
        }
    }

    /// <summary>
    /// 将世界坐标转换为指定 UI 容器的局部坐标。
    /// </summary>
    private static Vector2 ToLocalPosition(RectTransform parent, Vector3 worldPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(null, worldPosition),
            null,
            out Vector2 localPosition);
        return localPosition;
    }
}
