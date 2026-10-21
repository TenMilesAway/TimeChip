using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameSpringFestivalBasket : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform _rectTransform;
    private RectTransform _boundsRect;
    private Func<bool> _canMove;
    private float _pointerOffsetX;

    public void Configure(RectTransform boundsRect, Func<bool> canMove)
    {
        _boundsRect = boundsRect;
        _canMove = canMove;
        _rectTransform = transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanMove() || _rectTransform == null || _boundsRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boundsRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        _pointerOffsetX = _rectTransform.anchoredPosition.x - localPoint.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanMove() || _rectTransform == null || _boundsRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boundsRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float targetX = localPoint.x + _pointerOffsetX;
        _rectTransform.anchoredPosition = new Vector2(
            ClampHorizontalPosition(targetX),
            _rectTransform.anchoredPosition.y);
    }

    private bool CanMove()
    {
        return _canMove != null && _canMove();
    }

    private float ClampHorizontalPosition(float targetX)
    {
        float boundsHalfWidth = _boundsRect.rect.width * 0.5f;
        float basketHalfWidth = _rectTransform.rect.width * 0.5f;
        float minX = -boundsHalfWidth + basketHalfWidth;
        float maxX = boundsHalfWidth - basketHalfWidth;
        if (minX > maxX)
        {
            return 0f;
        }

        return Mathf.Clamp(targetX, minX, maxX);
    }
}
