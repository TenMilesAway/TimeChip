using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class KitchenOrderReward : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Text _txtNum;

    private int _loadVersion;

    public async Task<bool> SetDataAsync(int itemId, int count, string resourceTag)
    {
        _loadVersion++;
        int loadVersion = _loadVersion;
        if (itemId <= 0 || count <= 0)
        {
            gameObject.SetActive(false);
            return false;
        }

        cfg.Base baseConfig = DataTableMananger.GetInstance().Tables.BaseTable
            .GetOrDefault(itemId);
        cfg.Item itemConfig = baseConfig == null
            ? DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId)
            : null;
        if (baseConfig == null && itemConfig == null)
        {
            Debug.LogError($"厨房订单奖励配置不存在: [{itemId}]。", this);
            return false;
        }

        string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
        bool isSimulationCoin = itemId == BasePropertyId.SimulationCoin;
        float iconScale;
        if (isSimulationCoin)
        {
            if (!KitchenOrderUiScale.TryGetScaleMultiplier(
                    KitchenOrderUiScale.RewardCoinScaleId,
                    out iconScale))
            {
                Debug.LogError(
                    $"厨房订单模拟币缩放配置不存在: [{KitchenOrderUiScale.RewardCoinScaleId}]。",
                    this);
                return false;
            }
        }
        else if (!KitchenOrderUiScale.TryGetRewardItemIconScale(out iconScale))
        {
            Debug.LogError("厨房订单道具奖励缩放配置不存在。", this);
            return false;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(iconPath, resourceTag);
        if (loadVersion != _loadVersion || icon == null)
        {
            if (icon == null)
            {
                Debug.LogError($"厨房订单奖励图标加载失败: [{itemId}], [{iconPath}]。", this);
            }

            return false;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        KitchenOrderUiScale.ApplyIconScale(_imgIcon.rectTransform, iconScale);
        _txtNum.text = count.ToString();
        gameObject.SetActive(true);
        return true;
    }

    public void Clear()
    {
        _loadVersion++;
        _imgIcon.sprite = null;
    }
}