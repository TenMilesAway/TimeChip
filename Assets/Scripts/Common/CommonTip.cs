using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单条通用提示，负责自身的显示与消失动画。
/// </summary>
public class CommonTip : MonoBehaviour
{
    [SerializeField] private Text _tipText;
    [SerializeField, Min(0f)] private float _enterDuration = 0.25f;
    [SerializeField, Min(0f)] private float _displayDuration = 3f;
    [SerializeField, Min(0f)] private float _exitDuration = 0.25f;
    [SerializeField] private float _moveDistance = 40f;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Action<CommonTip> _completedCallback;
    private Vector2 _restingPosition;
    private bool _hasRestingPosition;
    private bool _isDismissing;
    private bool _hasCompleted;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        _completedCallback = null;
        _hasRestingPosition = false;
        _isDismissing = false;
        _hasCompleted = false;
    }

    /// <summary>
    /// 设置提示内容并播放完整显示动画。
    /// </summary>
    /// <param name="message">要展示的提示文本。</param>
    /// <param name="completedCallback">提示结束时的回调。</param>
    public void Play(string message, Action<CommonTip> completedCallback)
    {
        DOTween.Kill(this);
        _completedCallback = completedCallback;
        _isDismissing = false;
        _hasCompleted = false;
        _tipText.text = message;

        Canvas.ForceUpdateCanvases();
        if (!_hasRestingPosition)
        {
            _restingPosition = _rectTransform.anchoredPosition;
            _hasRestingPosition = true;
        }

        Vector2 targetPosition = _restingPosition;
        Vector2 startPosition = targetPosition - Vector2.up * _moveDistance;
        Vector2 exitPosition = targetPosition + Vector2.up * _moveDistance;

        _rectTransform.anchoredPosition = startPosition;
        _canvasGroup.alpha = 0f;

        DOTween.Sequence()
            .Append(_rectTransform.DOAnchorPos(targetPosition, _enterDuration))
            .Join(_canvasGroup.DOFade(1f, _enterDuration))
            .AppendInterval(_displayDuration)
            .Append(_rectTransform.DOAnchorPos(exitPosition, _exitDuration))
            .Join(_canvasGroup.DOFade(0f, _exitDuration))
            .SetTarget(this)
            .OnComplete(Finish);
    }

    /// <summary>
    /// 立即结束停留状态，播放当前提示的消失动画。
    /// </summary>
    public void Dismiss()
    {
        if (_isDismissing || _hasCompleted)
        {
            return;
        }

        _isDismissing = true;
        DOTween.Kill(this);

        DOTween.Sequence()
            .Append(_rectTransform.DOAnchorPos(
                _rectTransform.anchoredPosition + Vector2.up * _moveDistance,
                _exitDuration))
            .Join(_canvasGroup.DOFade(0f, _exitDuration))
            .SetTarget(this)
            .OnComplete(Finish);
    }

    /// <summary>
    /// 通知容器该提示已结束，以便回收至对象池。
    /// </summary>
    private void Finish()
    {
        if (_hasCompleted)
        {
            return;
        }

        _hasCompleted = true;
        _rectTransform.anchoredPosition = _restingPosition;
        Action<CommonTip> callback = _completedCallback;
        _completedCallback = null;
        callback?.Invoke(this);
    }
}
