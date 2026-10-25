using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 按住按钮持续一段时间后触发完成事件，并汇报 0~1 按住进度。
/// </summary>
public sealed class UIButtonLongPressHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float _holdDuration = 1.1f;

    public void SetHoldDuration(float holdDuration)
    {
        _holdDuration = Mathf.Max(0.05f, holdDuration);
    }

    private bool _isHolding;
    private float _holdElapsed;
    private bool _isCompleted;

    public event Action<float> HoldProgressChanged;
    public event Action HoldCompleted;
    public event Action HoldCancelled;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        _isHolding = true;
        _isCompleted = false;
        _holdElapsed = 0f;
        HoldProgressChanged?.Invoke(0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHold();
    }

    private void Update()
    {
        if (!_isHolding || _isCompleted)
        {
            return;
        }

        _holdElapsed += Time.unscaledDeltaTime;
        float progress = _holdDuration <= 0f ? 1f : Mathf.Clamp01(_holdElapsed / _holdDuration);
        HoldProgressChanged?.Invoke(progress);
        if (progress < 1f)
        {
            return;
        }

        _isCompleted = true;
        _isHolding = false;
        HoldCompleted?.Invoke();
    }

    private void CancelHold()
    {
        if (!_isHolding || _isCompleted)
        {
            return;
        }

        _isHolding = false;
        HoldCancelled?.Invoke();
    }

    public void ResetHoldState()
    {
        _isHolding = false;
        _isCompleted = false;
        _holdElapsed = 0f;
    }
}
