using UnityEngine;
using UnityEngine.UI;

public enum GameSpringFestivalItemKind
{
    Firecrackers = 0,
    LuckyPouch = 1,
    RedEnvelope = 2
}

public class GameSpringFestivalItem : MonoBehaviour
{
    [SerializeField] private Image _icon;

    private RectTransform _rectTransform;
    private float _fallSpeed;
    private bool _isActive;
    private GameSpringFestivalItemKind _kind;

    public RectTransform RectTransform =>
        _rectTransform != null ? _rectTransform : _rectTransform = transform as RectTransform;

    public GameSpringFestivalItemKind Kind => _kind;

    public bool IsActive => _isActive;

    public void Configure(GameSpringFestivalItemKind kind, Sprite sprite, float fallSpeed)
    {
        _kind = kind;
        _fallSpeed = fallSpeed;
        _isActive = true;

        if (_icon != null)
        {
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
            _icon.raycastTarget = false;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive || RectTransform == null)
        {
            return;
        }

        Vector2 position = RectTransform.anchoredPosition;
        position.y -= _fallSpeed * deltaTime;
        RectTransform.anchoredPosition = position;
    }

    public void Deactivate()
    {
        _isActive = false;
    }
}
