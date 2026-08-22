using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    private const float RewardScaleDivisor = 10000f;

    [SerializeField] private Image _imgIcon;
    [SerializeField] private Image _imgNumBg;
    [SerializeField] private Text _txtNum;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = GetComponent<Graphic>();
        }
    }

    public void SetData(Sprite icon, int amount, int rewardScale, UnityAction clickHandler)
    {
        bool hasItem = icon != null && amount > 0;
        _imgIcon.gameObject.SetActive(hasItem);
        _txtNum.gameObject.SetActive(hasItem);
        _imgNumBg.gameObject.SetActive(hasItem);
        _button.interactable = hasItem;
        _button.onClick.RemoveAllListeners();

        if (!hasItem)
        {
            _imgIcon.sprite = null;
            _txtNum.text = string.Empty;
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _txtNum.text = amount.ToString();
        _button.onClick.AddListener(clickHandler);
    }

    public void Clear()
    {
        SetData(null, 0, 0, null);
    }
}
