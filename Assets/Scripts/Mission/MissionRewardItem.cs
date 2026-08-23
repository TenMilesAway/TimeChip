using UnityEngine;
using UnityEngine.UI;

public class MissionRewardItem : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Text _txtNum;

    public void SetData(Sprite icon, int amount, float scale)
    {
        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * scale;
        _txtNum.text = amount.ToString();
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        _imgIcon.sprite = null;
        _txtNum.text = string.Empty;
        gameObject.SetActive(false);
    }
}
