using UnityEngine;
using UnityEngine.UI;

public class CommonRewardItem : MonoBehaviour
{
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private Text _rewardCountText;

    private const float RewardScaleDivisor = 10000f;

    /// <summary>
    /// 设置奖励数据
    /// </summary>
    public void SetData(Sprite icon, int count, int rewardScale)
    {
        _rewardIcon.sprite = icon;
        _rewardIcon.SetNativeSize();
        _rewardIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _rewardCountText.text = count.ToString();
    }
}
