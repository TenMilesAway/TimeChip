using UnityEngine;
using UnityEngine.UI;

public class CardLotteryAdditionItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _num;  // 例：×8
    [SerializeField] private GameObject _got; // 是否获得物品

    private int _presentationVersion;

    public async void SetData(CommonRewardItemData reward, bool obtained)
    {
        _presentationVersion++;
        int presentationVersion = _presentationVersion;
        _icon.sprite = null;
        _num.text = $"×{reward.itemCount}";
        _got.SetActive(obtained);

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(reward.itemId);
        cfg.Item itemConfig = baseConfig == null ? tables.ItemTable.GetOrDefault(reward.itemId) : null;
        if (baseConfig == null && itemConfig == null)
        {
            Debug.LogError($"时光星阵附加奖励配置不存在: [{reward.itemId}]", this);
            return;
        }

        string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
        int rewardScale = baseConfig == null ? itemConfig.RewardScale : baseConfig.RewardScale;
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            $"{GetInstanceID()}_{presentationVersion}");
        if (presentationVersion != _presentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"时光星阵附加奖励图标加载失败: [{reward.itemId}], [{iconPath}]", this);
            return;
        }

        _icon.sprite = icon;
        _icon.SetNativeSize();
        _icon.rectTransform.localScale = Vector3.one * (rewardScale / ScaleDivisor);
    }

    public void SetObtained(bool obtained)
    {
        _got.SetActive(obtained);
    }
}
