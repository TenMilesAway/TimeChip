using UnityEngine;
using UnityEngine.UI;

public class CommonRewardItem : MonoBehaviour
{
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private Text _rewardCountText;
    [SerializeField] private Text _rewardType;
    [SerializeField] private Button _btnDetail;
    [SerializeField] private GameObject _goType;
    [SerializeField] private GameObject[] _bgs;      // 不同品质物品使用的背景

    private const float RewardScaleDivisor = 10000f;

    /// <summary>奖励道具的配置 ID</summary>
    public int ItemId { get; private set; }

    /// <summary>奖励道具的数量</summary>
    public int Count { get; private set; }

    /// <summary>奖励图标的 UI 位置</summary>
    public RectTransform IconTransform { get { return _rewardIcon.rectTransform; } }

    /// <summary>奖励图标的精灵资源</summary>
    public Sprite Icon { get { return _rewardIcon.sprite; } }

    private void Awake()
    {
        _btnDetail.onClick.AddListener(OpenItemDetail);
    }

    private void OnDestroy()
    {
        if (_btnDetail != null)
        {
            _btnDetail.onClick.RemoveListener(OpenItemDetail);
        }
    }

    /// <summary>
    /// 设置奖励数据
    /// </summary>
    public void SetData(int itemId, Sprite icon, int count, int rewardScale, int level)
    {
        ItemId = itemId;
        Count = count;
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        cfg.Base baseConfig = itemConfig == null
            ? tables.BaseTable.GetOrDefault(itemId)
            : null;
        _btnDetail.interactable = itemConfig != null || baseConfig != null;
        _goType.SetActive(itemConfig != null);
        _rewardType.text = itemConfig == null ? string.Empty : itemConfig.Type;
        _rewardIcon.sprite = icon;
        _rewardIcon.SetNativeSize();
        _rewardIcon.rectTransform.localScale = Vector3.one * (rewardScale / RewardScaleDivisor);
        _rewardCountText.text = count.ToString();
        SetBackground(level);
    }

    private void OpenItemDetail()
    {
        if (ItemId <= 0)
        {
            Debug.LogError($"奖励物品详情配置不存在: [{ItemId}]", this);
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables.ItemTable.GetOrDefault(ItemId) == null &&
            tables.BaseTable.GetOrDefault(ItemId) == null)
        {
            Debug.LogError($"奖励物品详情配置不存在: [{ItemId}]", this);
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            param: new OpenUIParam { data = ItemId });
    }

    private void SetBackground(int level)
    {
        if (_bgs == null || _bgs.Length == 0)
        {
            return;
        }

        int backgroundIndex = Mathf.Clamp(level - 1, 0, _bgs.Length - 1);
        for (int i = 0; i < _bgs.Length; i++)
        {
            if (_bgs[i] != null)
            {
                _bgs[i].SetActive(i == backgroundIndex);
            }
        }
    }
}