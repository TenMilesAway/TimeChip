using UnityEngine;
using UnityEngine.UI;

public class CommonItemDetailView : UIBasePanel
{
    private const float RewardScaleDivisor = 10000f;
    private static readonly Color RareLevelColor = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EpicLevelColor = new Color(0.7f, 0.3f, 1f);
    private static readonly Color LegendaryLevelColor = new Color(1f, 0.75f, 0.1f);
    private static readonly Color MythicLevelColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField] private Text _txtName;      // 物品名称
    [SerializeField] private Text _txtLevel;     // 物品品质
    [SerializeField] private Text _txtDetail;    // 物品描述
    [SerializeField] private Image _imgIcon;     // 物品图标

    private int _iconRequestVersion;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!(param?.data is int itemId))
        {
            Debug.LogError("CommonItemDetailView 需要通过 OpenUIParam.data 传入道具 ID。", this);
            ClearData();
            return;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(itemId);
        if (itemConfig == null)
        {
            Debug.LogError($"道具详情配置不存在: [{itemId}]", this);
            ClearData();
            return;
        }

        SetData(itemConfig);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonItemDetailView;
    }

    private void SetData(cfg.Item itemConfig)
    {
        _txtName.text = itemConfig.Name;
        _txtDetail.text = itemConfig.Desc;
        SetLevel(itemConfig.Level);

        int requestVersion = ++_iconRequestVersion;
        SetIconAsync(itemConfig, requestVersion);
    }

    private async void SetIconAsync(cfg.Item itemConfig, int requestVersion)
    {
        _imgIcon.sprite = null;

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            GetInstanceID().ToString());
        if (requestVersion != _iconRequestVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"道具详情图标加载失败: [{itemConfig.Id}], [{itemConfig.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / RewardScaleDivisor);
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
        _txtName.text = string.Empty;
        _txtLevel.text = string.Empty;
        _txtDetail.text = string.Empty;
    }
}
