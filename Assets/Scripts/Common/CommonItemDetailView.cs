using UnityEngine;
using UnityEngine.UI;

public sealed class CommonBuffDetailData
{
    public cfg.BuffConfig BuffConfig { get; }
    public int RemainingTurns { get; }
    public int Stacks { get; }

    public CommonBuffDetailData(
        cfg.BuffConfig buffConfig,
        int remainingTurns,
        int stacks)
    {
        BuffConfig = buffConfig;
        RemainingTurns = remainingTurns;
        Stacks = stacks;
    }
}

public class CommonItemDetailView : UIBasePanel
{
    private const float RewardScaleDivisor = 10000f;
    private static readonly Color RareLevelColor = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EpicLevelColor = new Color(0.7f, 0.3f, 1f);
    private static readonly Color LegendaryLevelColor = new Color(1f, 0.75f, 0.1f);
    private static readonly Color MythicLevelColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField] private Text _txtTitle;     // 标题
    [SerializeField] private Text _txtName;      // 物品名称
    [SerializeField] private Text _txtLevel;     // 物品品质
    [SerializeField] private Text _txtDetail;    // 物品描述
    [SerializeField] private Text _txtType;      // 物品类型
    [SerializeField] private Image _imgIcon;     // 物品图标
    [SerializeField] private GameObject _goType;

    private int _iconRequestVersion;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (param?.data is CommonBuffDetailData buffDetailData &&
            buffDetailData.BuffConfig != null)
        {
            SetData(buffDetailData);
            return;
        }

        if (!(param?.data is int itemId))
        {
            Debug.LogError(
                "CommonItemDetailView 需要通过 OpenUIParam.data 传入道具 ID 或 Buff 详情数据。",
                this);
            ClearData();
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig != null)
        {
            SetData(itemConfig);
            return;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        if (baseConfig == null)
        {
            Debug.LogError($"道具详情配置不存在: [{itemId}]", this);
            ClearData();
            return;
        }

        SetData(baseConfig);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonItemDetailView;
    }

    private void SetData(cfg.Item itemConfig)
    {
        _txtTitle.text = "物品详情";
        _txtLevel.gameObject.SetActive(true);
        _txtName.text = itemConfig.Name;
        _txtDetail.text = itemConfig.Desc;
        _txtType.text = itemConfig.Type;
        _goType.SetActive(true);
        SetLevel(itemConfig.Level);

        int requestVersion = ++_iconRequestVersion;
        SetIconAsync(
            itemConfig.Id,
            itemConfig.Icon,
            itemConfig.RewardScale,
            requestVersion);
    }

    private void SetData(cfg.Base baseConfig)
    {
        _txtTitle.text = "物品详情";
        _txtLevel.gameObject.SetActive(true);
        _txtName.text = baseConfig.Name;
        _txtDetail.text = baseConfig.Desc;
        _txtType.text = string.Empty;
        _goType.SetActive(false);
        SetLevel(1);

        int requestVersion = ++_iconRequestVersion;
        SetIconAsync(
            baseConfig.Id,
            baseConfig.Icon,
            baseConfig.RewardScale,
            requestVersion);
    }

    private void SetData(CommonBuffDetailData detailData)
    {
        cfg.Item iconItemConfig = DataTableMananger.GetInstance()
            .Tables
            .ItemTable
            .GetOrDefault(detailData.BuffConfig.Icon);
        if (iconItemConfig == null)
        {
            Debug.LogError(
                $"BUFF 详情图标配置不存在: [{detailData.BuffConfig.Id}], [{detailData.BuffConfig.Icon}]",
                this);
            ClearData();
            return;
        }

        _txtTitle.text = "BUFF详情";
        _txtName.text = detailData.BuffConfig.Name;
        _txtDetail.text = detailData.BuffConfig.Desc;
        _txtName.color = Color.white;
        _txtLevel.gameObject.SetActive(false);
        _goType.SetActive(false);

        int requestVersion = ++_iconRequestVersion;
        SetIconAsync(
            detailData.BuffConfig.Id,
            iconItemConfig.Icon,
            iconItemConfig.RewardScale,
            requestVersion);
    }

    private async void SetIconAsync(
        int itemId,
        string iconPath,
        int rewardScale,
        int requestVersion)
    {
        _imgIcon.sprite = null;

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (requestVersion != _iconRequestVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"道具详情图标加载失败: [{itemId}], [{iconPath}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (rewardScale / RewardScaleDivisor);
    }

    private void SetLevel(int level)
    {
        Color levelColor;
        switch (level)
        {
            case 2:
                _txtLevel.text = "稀有";
                levelColor = RareLevelColor;
                break;
            case 3:
                _txtLevel.text = "史诗";
                levelColor = EpicLevelColor;
                break;
            case 4:
                _txtLevel.text = "传说";
                levelColor = LegendaryLevelColor;
                break;
            case 5:
                _txtLevel.text = "神话";
                levelColor = MythicLevelColor;
                break;
            default:
                _txtLevel.text = "普通";
                levelColor = Color.white;
                break;
        }

        _txtName.color = levelColor;
        _txtLevel.color = levelColor;
    }

    private void ClearData()
    {
        _iconRequestVersion++;
        _imgIcon.sprite = null;
        _txtTitle.text = string.Empty;
        _txtName.text = string.Empty;
        _txtLevel.text = string.Empty;
        _txtDetail.text = string.Empty;
        _txtType.text = string.Empty;
        _goType.SetActive(false);
    }
}
