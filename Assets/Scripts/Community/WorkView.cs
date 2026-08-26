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
    }

    private void ShowPreviousPage()
    {
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;
        RefreshPage();
    }

    private void ShowNextPage()
    {
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
            _workItems[i].SetData(
                workIndex < _filteredWorks.Count ? _filteredWorks[workIndex] : null,
                TryCompleteWork);
        }

        _txtLevel.text = "LV.1";
        _txtWorkName.text = WorkTypes[_selectedWorkTypeIndex];
        _txtProgress.text = "0 / 0";
        _sliderProgress.value = 0f;
        _txtCurrentPage.text = _currentPage.ToString();
        _txtMaxPage.text = maxPage.ToString();
        _txtIsWork.text = PlayerInfoManager.GetInstance().WorkedThisTurn ? "已工作" : "未工作";
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
        if (playerInfoManager.WorkedThisTurn)
        {
            CommonTipView.Show("本回合已完成零工");
            return;
        }

        if (playerInfoManager.Health < workConfig.HealthCost)
        {
            CommonTipView.Show("体力不足，无法完成零工");
            return;
        }

        if (!playerInfoManager.TryMarkWorkedThisTurn())
        {
            CommonTipView.Show("本回合已完成零工");
            return;
        }

        playerInfoManager.ChangeHealth(-workConfig.HealthCost);
        playerInfoManager.AddSimulationCoins(workConfig.CoinReward);
        CommonTipView.Show($"完成{workConfig.Name}，获得{workConfig.CoinReward}模拟币");
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
