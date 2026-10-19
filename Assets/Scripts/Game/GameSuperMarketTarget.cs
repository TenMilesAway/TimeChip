using UnityEngine;
using UnityEngine.UI;

public class GameSuperMarketTarget : MonoBehaviour
{
    [SerializeField] private Image _imgTargetBG;
    [SerializeField] private Image _imgTarget;

    private int _itemIndex = -1;
    private bool _isFound;

    public int ItemIndex
    {
        get { return _itemIndex; }
    }

    public bool IsFound
    {
        get { return _isFound; }
    }

    public RectTransform RectTransform
    {
        get { return transform as RectTransform; }
    }

    public void Configure(int itemIndex, Sprite sprite)
    {
        _itemIndex = itemIndex;
        _isFound = false;

        if (_imgTargetBG != null)
        {
            _imgTargetBG.sprite = sprite;
            _imgTargetBG.enabled = sprite != null;
        }

        if (_imgTarget != null)
        {
            _imgTarget.sprite = sprite;
            _imgTarget.gameObject.SetActive(false);
        }
    }

    public void SetGameplayVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void MarkFound()
    {
        if (_isFound)
        {
            return;
        }

        _isFound = true;
        if (_imgTarget != null)
        {
            _imgTarget.gameObject.SetActive(true);
        }
    }

    public void ResetFoundState()
    {
        _isFound = false;
        if (_imgTarget != null)
        {
            _imgTarget.gameObject.SetActive(false);
        }
    }
}
