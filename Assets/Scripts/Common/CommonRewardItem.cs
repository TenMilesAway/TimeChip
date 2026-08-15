using UnityEngine;
using UnityEngine.UI;

public class CommonRewardItem : MonoBehaviour
{
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private Text _rewardCountText;

    private const float RewardScaleDivisor = 10000f;

    /// <summary>奖励道具的配置 ID。</summary>
    public int ItemId { get; private set; }

    /// <summary>奖励道具的数量。</summary>
    public int Count { get; private set; }

    /// <summary>奖励图标的 UI 位置。</summary>
    public RectTransform IconTransform { get { return _rewardIcon.rectTransform; } }

    /// <summary>奖励图标的精灵资源。</summary>
    public Sprite Icon { get { return _rewardIcon.sprite; } }

    /// <summary>
    /// 设置奖励数据
    /// </summary>
    public void SetData(int itemId, Sprite icon, int count, int rewardScale)
    {
        ItemId = itemId;
        Count = count;
        _rewardIcon.sprite = icon;
        _rewardIcon.SetNativeSize();
        _rewardIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _rewardCountText.text = count.ToString();
    }
}
