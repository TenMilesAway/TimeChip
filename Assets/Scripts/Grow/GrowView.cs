using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrowView : UIBasePanel
{
    private const int MaxStarLevel = 5;

    [SerializeField] private Text _txtPoint;              // 回忆点文本
    [SerializeField] private Text _txtCardName;           // 卡牌名称文本
    [SerializeField] private Text _txtBaseEffect;         // 基础效果文本
    [SerializeField] private Text _txtOneStarEffect;      // 一星效果文本
    [SerializeField] private Text _txtThreeStarEffect;    // 三星效果文本
    [SerializeField] private Text _txtFiveStarEffect;     // 五星效果文本
    [SerializeField] private Text _txtGrowNum;            // 升星数量文本
    [SerializeField] private Text _txtGrowUp;             // 默认：升星；满星时显示 "已满星"
    [SerializeField] private Button _btnClose;            // 关闭按钮
    [SerializeField] private Button _btnGrow;             // 升星按钮

    [SerializeField] private GameObject _goSelect;        // 选中卡牌时展示
    [SerializeField] private GameObject _goUnselect;      // 未选中卡牌时展示    
    [SerializeField] private GameObject[] _goUnlockStars; // 已解锁星级, 共 5 个
    [SerializeField] private GameObject[] _goLockStars;   // 未解锁星级, 共 5 个
    [SerializeField] private GrowItem[] _goGrowItems;     // 升星卡牌列表, 共 9 个

    private cfg.Grow _selectedGrow;

    private void Awake()
    {
        _btnClose.onClick.AddListener(ClosePanel);
        _btnGrow.onClick.AddListener(TryUpgradeSelectedGrow);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        RefreshGrowItems();
        ClearSelection();
    }

    protected override void ShowHandle()
    {
        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        globalInfoManager.GlobalInfoChanged -= OnGlobalInfoChanged;
        globalInfoManager.GlobalInfoChanged += OnGlobalInfoChanged;
        RefreshGrowItems();
        RefreshSelectedGrow();
    }

    protected override void HideHandle()
    {
        GlobalInfoManager.GetInstance().GlobalInfoChanged -= OnGlobalInfoChanged;
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();

        if (Launcher.Instance != null)
        {
            Launcher.Instance.ReturnToLauncherMenu();
        }
    }

    protected override void OnDestroy()
    {
        GlobalInfoManager.GetInstance().GlobalInfoChanged -= OnGlobalInfoChanged;
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        if (_btnGrow != null)
        {
            _btnGrow.onClick.RemoveListener(TryUpgradeSelectedGrow);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.GrowView;
    }

    private void RefreshGrowItems()
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("成长配置尚未初始化", this);
            return;
        }

        IReadOnlyList<cfg.Grow> grows = tables.GrowTable.DataList;
        if (grows.Count > _goGrowItems.Length)
        {
            Debug.LogError("GrowItem 数量不足，无法展示全部成长卡牌", this);
        }

        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        int itemCount = Mathf.Min(grows.Count, _goGrowItems.Length);
        for (int index = 0; index < itemCount; index++)
        {
            _goGrowItems[index].SetData(
                grows[index],
                globalInfoManager.GetGrowCard(grows[index].Id),
                SelectGrow);
        }

        for (int index = itemCount; index < _goGrowItems.Length; index++)
        {
            _goGrowItems[index].gameObject.SetActive(false);
        }
    }

    private void SelectGrow(int growId)
    {
        cfg.Grow grow = DataTableMananger.GetInstance().Tables.GrowTable.GetOrDefault(growId);
        if (grow == null)
        {
            Debug.LogError($"找不到成长卡牌配置：{growId}", this);
            return;
        }

        _selectedGrow = grow;
        RefreshSelectedGrow();
    }

    private void RefreshSelectedGrow()
    {
        if (_selectedGrow == null)
        {
            ClearSelection();
            return;
        }

        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        GrowCardData cardData = globalInfoManager.GetGrowCard(_selectedGrow.Id);
        if (cardData == null)
        {
            ClearSelection();
            return;
        }

        _goSelect.SetActive(true);
        _goUnselect.SetActive(false);
        _txtPoint.text = globalInfoManager.MemoryPoints.ToString();
        _txtCardName.text = _selectedGrow.Name;
        _txtBaseEffect.text =
            $"{_selectedGrow.BaseEffect} +{_selectedGrow.BaseEffectValue + _selectedGrow.UpgradeAdditionalValue * cardData.starLevel}";
        _txtOneStarEffect.text = _selectedGrow.OneStarExtraEffect;
        _txtThreeStarEffect.text = _selectedGrow.ThreeStarExtraEffect;
        _txtFiveStarEffect.text = _selectedGrow.FiveStarExtraEffect;

        RefreshStars(cardData.starLevel);
        bool hasNextStarCost = TryGetNextStarCost(
            _selectedGrow,
            cardData.starLevel,
            out int cost);
        _txtGrowNum.text = hasNextStarCost ? cost.ToString() : "∞";
        _txtGrowUp.text = hasNextStarCost ? "升星" : "已满星";
        _btnGrow.interactable = cardData.isUnlocked &&
            hasNextStarCost &&
            globalInfoManager.MemoryPoints >= cost;
    }

    private void RefreshStars(int starLevel)
    {
        int starCount = Mathf.Min(_goUnlockStars.Length, _goLockStars.Length);
        for (int index = 0; index < starCount; index++)
        {
            bool unlocked = index < starLevel;
            _goUnlockStars[index].SetActive(unlocked);
            _goLockStars[index].SetActive(!unlocked);
        }
    }

    private void TryUpgradeSelectedGrow()
    {
        if (_selectedGrow == null)
        {
            return;
        }

        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        GrowCardData cardData = globalInfoManager.GetGrowCard(_selectedGrow.Id);
        if (cardData == null ||
            !TryGetNextStarCost(_selectedGrow, cardData.starLevel, out int cost))
        {
            return;
        }

        globalInfoManager.TryUpgradeGrowCard(_selectedGrow.Id, cost);
        GameManager.Audio.Play(AudioDefine.SFXBuy);
    }

    private void OnGlobalInfoChanged(GlobalInfoManager globalInfoManager)
    {
        RefreshGrowItems();
        RefreshSelectedGrow();
    }

    private void ClearSelection()
    {
        _selectedGrow = null;
        _goSelect.SetActive(false);
        _goUnselect.SetActive(true);
        _txtPoint.text = GlobalInfoManager.GetInstance().MemoryPoints.ToString();
        _txtGrowUp.text = "升星";
        _btnGrow.interactable = false;
        RefreshStars(0);
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private static bool TryGetNextStarCost(cfg.Grow grow, int starLevel, out int cost)
    {
        cost = 0;
        if (starLevel < 0 || starLevel >= MaxStarLevel)
        {
            return false;
        }

        string[] costs = grow.StarUpMemoryCosts.Split(';');
        return costs.Length == MaxStarLevel &&
            int.TryParse(costs[starLevel], out cost) &&
            cost > 0;
    }
}
