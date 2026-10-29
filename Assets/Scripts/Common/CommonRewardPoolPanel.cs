using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommonRewardPoolEntry
{
    public int ItemId { get; }
    public int ItemCount { get; }
    public double ProbabilityPercent { get; }
    public bool IsGrandPrize { get; }

    public CommonRewardPoolEntry(
        int itemId,
        int itemCount,
        double probabilityPercent,
        bool isGrandPrize = false)
    {
        ItemId = itemId;
        ItemCount = itemCount;
        ProbabilityPercent = probabilityPercent;
        IsGrandPrize = isGrandPrize;
    }

    public string GetProbabilityDisplayText()
    {
        return ProbabilityPercent.ToString("F2") + "%";
    }
}

public sealed class CommonRewardPoolPanelData
{
    public string Title { get; }
    public IReadOnlyList<CommonRewardPoolEntry> Entries { get; }

    public CommonRewardPoolPanelData(string title, IReadOnlyList<CommonRewardPoolEntry> entries)
    {
        Title = title ?? string.Empty;
        Entries = entries ?? Array.Empty<CommonRewardPoolEntry>();
    }
}

public class CommonRewardPoolPanel : UIBasePanel
{
    private const float RewardScaleDivisor = 10000f;

    [SerializeField] private Text _txtTitle;
    [SerializeField] private Transform _itemParent;
    [SerializeField] private Button _btnClose;

    private readonly List<CommonRewardPoolItem> _poolItems = new List<CommonRewardPoolItem>();

    private int _initVersion;
    private string _resourceTag;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            UiButtonAudio.Bind(_btnClose, ClosePanel);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!(param?.data is CommonRewardPoolPanelData panelData) ||
            panelData.Entries == null ||
            panelData.Entries.Count == 0)
        {
            Debug.LogError("CommonRewardPoolPanel 初始化参数无效。", this);
            ClosePanel();
            return;
        }

        _txtTitle.text = panelData.Title;
        ClearPoolItems();
        InitializePoolItemsAsync(panelData.Entries, ++_initVersion);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
        ClearPoolItems();
        GameManager.Audio.Play(AudioDefine.SFXClose);
    }

    protected override void OnDestroy()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonRewardPoolPanel;
    }

    private async void InitializePoolItemsAsync(
        IReadOnlyList<CommonRewardPoolEntry> entries,
        int initVersion)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;

        for (int i = 0; i < entries.Count; i++)
        {
            CommonRewardPoolEntry entry = entries[i];
            cfg.Item itemConfig = tables.ItemTable.GetOrDefault(entry.ItemId);
            cfg.Base baseConfig = itemConfig == null
                ? tables.BaseTable.GetOrDefault(entry.ItemId)
                : null;
            if (itemConfig == null && baseConfig == null)
            {
                Debug.LogError($"CommonRewardPoolPanel 奖池道具不存在: [{entry.ItemId}]", this);
                continue;
            }

            string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
            int rewardScale = itemConfig != null
                ? itemConfig.RewardScale
                : baseConfig.RewardScale;

            GameObject itemObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.CommonRewardPoolItem, _resourceTag);
            if (initVersion != _initVersion || itemObject == null)
            {
                if (itemObject != null)
                {
                    UnityObjectPoolFactory.GetInstance()
                        .PutItem(GlobalDefine.CommonRewardPoolItem, itemObject);
                }

                return;
            }

            itemObject.transform.SetParent(_itemParent, false);
            CommonRewardPoolItem poolItem = itemObject.GetComponent<CommonRewardPoolItem>();
            if (poolItem == null)
            {
                Debug.LogError("CommonRewardPoolItem 预制体缺少组件。", this);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonRewardPoolItem, itemObject);
                continue;
            }

            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(iconPath, _resourceTag);
            if (initVersion != _initVersion)
            {
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonRewardPoolItem, itemObject);
                return;
            }

            if (icon == null)
            {
                Debug.LogError(
                    $"CommonRewardPoolPanel 图标加载失败: [{entry.ItemId}], [{iconPath}]",
                    this);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonRewardPoolItem, itemObject);
                continue;
            }

            float iconScale = rewardScale / RewardScaleDivisor;
            poolItem.Configure(
                entry.ItemId,
                entry.ItemCount,
                icon,
                iconScale,
                entry.GetProbabilityDisplayText(),
                entry.IsGrandPrize);
            _poolItems.Add(poolItem);
        }
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void ClearPoolItems()
    {
        _initVersion++;
        for (int i = 0; i < _poolItems.Count; i++)
        {
            CommonRewardPoolItem poolItem = _poolItems[i];
            if (poolItem == null)
            {
                continue;
            }

            UnityObjectPoolFactory.GetInstance()
                .PutItem(GlobalDefine.CommonRewardPoolItem, poolItem.gameObject);
        }

        _poolItems.Clear();
    }
}
