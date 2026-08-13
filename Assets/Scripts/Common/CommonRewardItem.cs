using UnityEngine;
using UnityEngine.UI;

public class CommonRewardItem : MonoBehaviour
{
    private const float RewardScaleDivisor = 10000f;

    private Image _rewardIcon;
    private Text _rewardCountText;

    private void Awake()
    {
        _rewardIcon = transform.Find("Reward").GetComponent<Image>();
        _rewardCountText = transform.Find("Reward Num").GetComponent<Text>();
    }

    public void SetData(Sprite icon, int count, int rewardScale)
    {
        _rewardIcon.sprite = icon;
        _rewardIcon.SetNativeSize();
        _rewardIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _rewardCountText.text = count.ToString();
    }
}
