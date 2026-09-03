using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WorkView : UIBasePanel
{
    private const int ItemsPerPage = 3;

    private static readonly string[] WorkTypes =
    {
        "基础劳务",
        "设计",
        "程序",
        "维修",
        "写作",
        "机遇"
    };

    [SerializeField] private Text _txtLevel;          // 当前零工等级: LV.{x}
    [SerializeField] private Text _txtWorkName;       // 当前零工名称
    [SerializeField] private Text _txtProgress;       // 当前零工经验进度:  {当前经验} / {升级所需经验}
    [SerializeField] private Text _txtCurrentPage;
    [SerializeField] private Text _txtMaxPage;
    [SerializeField] private Text _txtIsWork;         // 当前回合是否工作: 未工作或已工作
    [SerializeField] private Slider _sliderProgress;  // 当前零工经验进度条
    [SerializeField] private Button _btnPrevious;
    [SerializeField] private Button _btnNext;
    [SerializeField] private Button[] _btnTags;       // 左侧标签按钮，共 6 个
    [SerializeField] private GameObject[] _goIcons;   // 经验条显示对应 Icon，共 6 个
    [SerializeField] private WorkItem[] _workItems;   // 3 个

    private readonly List<cfg.Work> _filteredWorks = new List<cfg.Work>();
    private readonly List<UnityAction> _tagClickHandlers = new List<UnityAction>();

    private int _selectedWorkTypeIndex;
    private int _currentPage = 1;
    private bool _isUiReady;

    private void Awake()
    {
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < _btnTags.Length; i++)
        {
            int workTypeIndex = i;
            UnityAction clickHandler = () => SelectWorkType(workTypeIndex);
            _tagClickHandlers.Add(clickHandler);
            _btnTags[i].onClick.AddListener(clickHandler);
        }

        _btnPrevious.onClick.AddListener(ShowPreviousPage);
        _btnNext.onClick.AddListener(ShowNextPage);
        PlayerInfoManager.GetInstance().PlayerInfoChanged += OnPlayerInfoChanged;
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        if (_isUiReady)
        {
            SelectWorkType(0);
        }
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (_isUiReady)
        {
            RefreshPage();
        }
    }

    protected override void OnDestroy()
    {
        for (int i = 0; i < _tagClickHandlers.Count; i++)
        {
            _btnTags[i].onClick.RemoveListener(_tagClickHandlers[i]);
        }

        if (_btnPrevious != null)
        {
            _btnPrevious.onClick.RemoveListener(ShowPreviousPage);
        }

        if (_btnNext != null)
        {
            _btnNext.onClick.RemoveListener(ShowNextPage);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        base.OnDestroy();
    }

    private void SelectWorkType(int workTypeIndex)
    {
        if (workTypeIndex < 0 || workTypeIndex >= WorkTypes.Length)
        {
            return;
        }

        _selectedWorkTypeIndex = workTypeIndex;
        _currentPage = 1;
        _filteredWorks.Clear();

        IReadOnlyList<cfg.Work> works = DataTableMananger.GetInstance().Tables.WorkTable.DataList;
        string selectedWorkType = WorkTypes[_selectedWorkTypeIndex];
        for (int i = 0; i < works.Count; i++)
        {
            cfg.Work work = works[i];
            if (work.WorkType == selectedWorkType)
            {
                _filteredWorks.Add(work);
            }
        }

        UpdateTagSelection();
        RefreshPage();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void ShowPreviousPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;
        RefreshPage();
    }

    private void ShowNextPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_currentPage >= GetMaxPage())
        {
            return;
        }

        _currentPage++;
        RefreshPage();
    }

    private void RefreshPage()
    {
        int maxPage = GetMaxPage();
        _currentPage = Mathf.Clamp(_currentPage, 1, maxPage);
        int firstItemIndex = (_currentPage - 1) * ItemsPerPage;

        for (int i = 0; i < _workItems.Length; i++)
        {
            int workIndex = firstItemIndex + i;
            cfg.Work workConfig = workIndex < _filteredWorks.Count ? _filteredWorks[workIndex] : null;
            _workItems[i].SetData(
                workConfig,
                TryCompleteWork,
                IsWorkUnlocked(workConfig, out string unlockTip),
                unlockTip);
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        string selectedWorkType = WorkTypes[_selectedWorkTypeIndex];
        int workLevel = playerInfoManager.GetWorkLevel(selectedWorkType);
        int workExperience = playerInfoManager.GetWorkExperience(selectedWorkType);
        int requiredExperience = PlayerInfoManager.GetWorkExperienceRequired(workLevel);
        _txtLevel.text = $"LV.{workLevel}";
        _txtWorkName.text = selectedWorkType;
        _txtProgress.text = requiredExperience == 0 ? "∞" : $"{workExperience} / {requiredExperience}";
        _sliderProgress.minValue = 0f;
        _sliderProgress.maxValue = requiredExperience == 0 ? 1f : requiredExperience;
        _sliderProgress.value = requiredExperience == 0 ? 1f : workExperience;
        _txtCurrentPage.text = _currentPage.ToString();
        _txtMaxPage.text = maxPage.ToString();
        _txtIsWork.text = playerInfoManager.WorkedThisTurn ? "已工作" : "未工作";
        _btnPrevious.interactable = _currentPage > 1;
        _btnNext.interactable = _currentPage < maxPage;

        for (int i = 0; i < _goIcons.Length; i++)
        {
            _goIcons[i].SetActive(i == _selectedWorkTypeIndex);
        }
    }

    private void TryCompleteWork(cfg.Work workConfig)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();

        GameManager.Audio.Play(AudioDefine.SFXClick);

        if (playerInfoManager.WorkedThisTurn)
        {
            CommonTipView.Show("本回合已完成零工");
            return;
        }

        if (!IsWorkUnlocked(workConfig, out string unlockTip))
        {
            CommonTipView.Show(unlockTip);
            return;
        }

        WorkBuffResult workResult = BuffSystem.GetInstance().CalculateWorkResult(workConfig);
        if (playerInfoManager.Health < workResult.HealthCost)
        {
            CommonTipView.Show("体力不足，无法完成零工");
            return;
        }

        if (workConfig.IsUseItem > 0 &&
            !playerInfoManager.TryConsumeItem(workConfig.IsUseItem))
        {
            Debug.LogError($"零工消耗道具失败: [{workConfig.Id}], [{workConfig.IsUseItem}]", this);
            CommonTipView.Show("所需道具不足，无法完成零工");
            return;
        }

        if (!playerInfoManager.TryMarkWorkedThisTurn())
        {
            CommonTipView.Show("本回合已完成零工");
            return;
        }

        playerInfoManager.ChangeHealth(-workResult.HealthCost);
        int coinReward = CalculateCoinReward(workResult.CoinReward, workConfig.CoinRewardSection);
        int workExperience = Mathf.Max(0, workConfig.Exp);
        playerInfoManager.AddSimulationCoins(coinReward);
        if (workExperience > 0)
        {
            playerInfoManager.AddWorkExperience(workConfig.WorkType, workExperience);
        }

        CommonTipView.Show($"完成{workConfig.Name}，获得{coinReward}模拟币和{workExperience}经验");
        GameManager.Audio.Play(AudioDefine.SFXWork);
    }

    private static bool IsWorkUnlocked(cfg.Work workConfig, out string unlockTip)
    {
        unlockTip = string.Empty;
        if (workConfig == null)
        {
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        List<string> requirements = new List<string>();
        if (playerInfoManager.GetWorkLevel(workConfig.WorkType) < workConfig.UnlockLevel)
        {
            requirements.Add($"需要LV.{workConfig.UnlockLevel}");
        }

        if (workConfig.UnlockItemId > 0 &&
            playerInfoManager.GetItemCount(workConfig.UnlockItemId) <= 0)
        {
            requirements.Add($"需要【{GetItemName(workConfig.UnlockItemId)}】");
        }

        if (workConfig.IsUseItem > 0 &&
            playerInfoManager.GetItemCount(workConfig.IsUseItem) <= 0)
        {
            requirements.Add($"需要并消耗【{GetItemName(workConfig.IsUseItem)}】");
        }

        if (requirements.Count == 0)
        {
            return true;
        }

        unlockTip = string.Join("及", requirements);
        return false;
    }

    private static int CalculateCoinReward(int baseCoinReward, int rewardSection)
    {
        int variationPercent = UnityEngine.Random.Range(
            -Mathf.Max(0, rewardSection),
            Mathf.Max(0, rewardSection) + 1);
        return Mathf.Max(0, Mathf.RoundToInt(baseCoinReward * (100 + variationPercent) / 100f));
    }

    private static string GetItemName(int itemId)
    {
        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(itemId);
        return itemConfig == null ? $"道具{itemId}" : itemConfig.Name;
    }

    private void UpdateTagSelection()
    {
        for (int i = 0; i < _btnTags.Length; i++)
        {
            _btnTags[i].interactable = i != _selectedWorkTypeIndex;
        }
    }

    private int GetMaxPage()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)_filteredWorks.Count / ItemsPerPage));
    }

    private bool HasValidUiReferences()
    {
        if (_txtLevel == null ||
            _txtWorkName == null ||
            _txtProgress == null ||
            _txtCurrentPage == null ||
            _txtMaxPage == null ||
            _txtIsWork == null ||
            _sliderProgress == null ||
            _btnPrevious == null ||
            _btnNext == null ||
            _btnTags == null ||
            _btnTags.Length != WorkTypes.Length ||
            _goIcons == null ||
            _goIcons.Length != WorkTypes.Length ||
            _workItems == null ||
            _workItems.Length != ItemsPerPage)
        {
            Debug.LogError("WorkView 的 UI 引用未在 Inspector 中完整配置。", this);
            return false;
        }

        for (int i = 0; i < ItemsPerPage; i++)
        {
            if (_workItems[i] == null)
            {
                Debug.LogError($"WorkView 的第 {i + 1} 个 WorkItem 未在 Inspector 中配置。", this);
                return false;
            }
        }

        return true;
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        if (_isUiReady && gameObject.activeInHierarchy)
        {
            RefreshPage();
        }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.WorkView;
    }
}
