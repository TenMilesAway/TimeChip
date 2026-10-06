using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PortContainerItem : MonoBehaviour
{
    [SerializeField] private Text _txtType;
    [SerializeField] private Text _txtNum;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private GameObject _goType;

    private const float ScaleDivisor = 10000f;
    private const int PortIconScaleId = 9;

    private int _loadVersion;

    public int ItemId { get; private set; }
    public int Count { get; private set; }
    public RectTransform IconTransform { get { return _imgIcon.rectTransform; } }

    public async Task<bool> SetDataAsync(
        CommonRewardItemData reward,
        string resourceTag)
    {
        _loadVersion++;
        int loadVersion = _loadVersion;
        if (reward == null || reward.itemId <= 0 || reward.itemCount <= 0)
        {
            Debug.LogError("港口奖励数据无效。", this);
            return false;
        }

        ItemId = reward.itemId;
        Count = reward.itemCount;
        cfg.Base baseConfig = DataTableMananger.GetInstance().Tables.BaseTable
            .GetOrDefault(reward.itemId);
        cfg.Item itemConfig = baseConfig == null
            ? DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(reward.itemId)
            : null;
        if (baseConfig == null && itemConfig == null)
        {
            Debug.LogError($"港口奖励配置不存在: [{reward.itemId}]。", this);
            return false;
        }

        string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
        int rewardScale = baseConfig == null
            ? itemConfig.RewardScale
            : baseConfig.RewardScale;
        cfg.Scale portIconScaleConfig = DataTableMananger.GetInstance()
            .Tables
            .ScaleTable
            .GetOrDefault(PortIconScaleId);
        if (portIconScaleConfig == null)
        {
            Debug.LogError($"港口奖励图标缩放配置不存在: [{PortIconScaleId}]。", this);
            return false;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(iconPath, resourceTag);
        if (loadVersion != _loadVersion || icon == null)
        {
            if (icon == null)
            {
                Debug.LogError($"港口奖励图标加载失败: [{reward.itemId}], [{iconPath}]。", this);
            }

            return false;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        float portScaleMultiplier = portIconScaleConfig.ScaleValue / ScaleDivisor;
        _imgIcon.rectTransform.localScale = Vector3.one *
            (rewardScale / ScaleDivisor) *
            portScaleMultiplier;
        _txtNum.text = $"x{reward.itemCount}";
        _goType.SetActive(itemConfig != null);
        if (itemConfig != null)
        {
            _txtType.text = itemConfig.Type;
        }

        return true;
    }

    public void Clear()
    {
        _loadVersion++;
    }
}
