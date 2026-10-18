using System;
using DG.Tweening;
using UnityEngine;

public class GameStampFile : MonoBehaviour
{
    [SerializeField] private GameObject _goRight;

    private RectTransform _rectTransform;
    private Vector3 _defaultScale = Vector3.one;
    private bool _canMove;
    private bool _isDespawning;

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            return _rectTransform;
        }
    }

    public bool IsStamped { get; private set; }

    public float MoveSpeed { get; private set; }

    public bool CanMove => _canMove && !_isDespawning;

    public bool IsDespawning => _isDespawning;

    private void Awake()
    {
        _defaultScale = transform.localScale;
    }

    public void Configure(float moveSpeed)
    {
        MoveSpeed = moveSpeed;
        ResetState();
    }

    public void PrepareForSpawn()
    {
        DOTween.Kill(this);
        _canMove = false;
        _isDespawning = false;
        transform.localScale = Vector3.zero;
    }

    public void PlaySpawnAnimation(float duration, Action onComplete)
    {
        DOTween.Kill(this);
        _canMove = false;
        _isDespawning = false;
        transform.localScale = Vector3.zero;
        transform.DOScale(_defaultScale, duration)
            .SetEase(Ease.OutBack)
            .SetTarget(this)
            .OnComplete(() =>
            {
                _canMove = true;
                onComplete?.Invoke();
            });
    }

    public void PlayDespawnAnimation(float duration, Action onComplete)
    {
        if (_isDespawning)
        {
            return;
        }

        _isDespawning = true;
        _canMove = false;
        DOTween.Kill(this);
        transform.DOScale(0f, duration)
            .SetEase(Ease.InBack)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void ResetForPool()
    {
        DOTween.Kill(this);
        _canMove = false;
        _isDespawning = false;
        transform.localScale = _defaultScale;
        ResetState();
    }

    public void ResetState()
    {
        IsStamped = false;
        if (_goRight != null)
        {
            _goRight.SetActive(false);
        }
    }

    public void ApplyStamp()
    {
        if (IsStamped)
        {
            return;
        }

        IsStamped = true;
        if (_goRight != null)
        {
            _goRight.SetActive(true);
        }
    }

    public bool IsFullyInsideJudgeZone(RectTransform judgeZone)
    {
        if (judgeZone == null || RectTransform == null)
        {
            return false;
        }

        return GameStampRectUtility.IsFullyContained(RectTransform, judgeZone);
    }
}

public static class GameStampRectUtility
{
    public static bool IsFullyContained(RectTransform inner, RectTransform outer)
    {
        Rect innerRect = GetScreenRect(inner);
        Rect outerRect = GetScreenRect(outer);
        return innerRect.xMin >= outerRect.xMin &&
            innerRect.yMin >= outerRect.yMin &&
            innerRect.xMax <= outerRect.xMax &&
            innerRect.yMax <= outerRect.yMax;
    }

    private static Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 corner = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
