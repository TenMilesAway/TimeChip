using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HomeStoreView : UIBasePanel
{
    private const int AllCategory = 0;    // 全部种类的序号
    private const int ItemsPerPage = 9;   // 每页的 Item 数量

    [SerializeField] private GameObject _itemBg;             // Item 的挂载父物体
    [SerializeField] private Button[] _tagButtons;           // 标签按钮
    [SerializeField] private int[] _tagCategories;           // 标签种类
    [SerializeField] private Button _previousPageButton;     // 上一页按钮
    [SerializeField] private Button _nextPageButton;         // 下一页按钮
    [SerializeField] private Text _currentPageText;          // 当前页
    [SerializeField] private Text _maxPageText;              // 最大页

    private readonly List<HomeItem> _homeItems = new List<HomeItem>(ItemsPerPage);   // HomeItem
    private readonly List<UnityAction> _tagClickHandlers = new List<UnityAction>();  // 标签点击事件
    private readonly List<cfg.Home> _filteredHomes = new List<cfg.Home>();           // Home 配置

    private int _selectedCategory = AllCategory;  // 进入默认选择全部种类
    private int _currentPage = 1;                 // 进入默认页数
    private int _itemRequestVersion;              // Item 加载版本
    private bool _isUiReady;                      // UI 是否准备完毕

    private void Awake()
    {
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        // 初始化监听
        for (int i = 0; i < _tagButtons.Length; i++)
        {
            int category = _tagCategories[i];
            UnityAction clickHandler = () => SelectCategory(category);
            _tagClickHandlers.Add(clickHandler);
            _tagButtons[i].onClick.AddListener(clickHandler);
        }

        _previousPageButton.onClick.AddListener(ShowPreviousPage);
        _nextPageButton.onClick.AddListener(ShowNextPage);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!_isUiReady) return;

        SelectCategory(AllCategory);
        LoadHomeItemsAsync(++_itemRequestVersion);
    }

    protected override void CloseHandle()
    {
        _itemRequestVersion++;
        ClearHomeItems();
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        for (int i = 0; i < _tagClickHandlers.Count; i++)
        {
            _tagButtons[i].onClick.RemoveListener(_tagClickHandlers[i]);
        }

        if (_previousPageButton != null)
        {
            _previousPageButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (_nextPageButton != null)
        {
            _nextPageButton.onClick.RemoveListener(ShowNextPage);
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 选择商品种类
    /// </summary>
    private void SelectCategory(int category)
    {
        _selectedCategory = category;
        _currentPage = 1;
        _filteredHomes.Clear();

        IReadOnlyList<cfg.Home> homes = DataTableMananger.GetInstance().Tables.HomeTable.DataList;
        for (int i = 0; i < homes.Count; i++)
        {
            cfg.Home home = homes[i];
            if (category == AllCategory || home.Id / 1000 == category)
            {
                _filteredHomes.Add(home);
            }
        }

        UpdateTagSelection();
        RefreshPage();
    }

    /// <summary>
    /// 上一页
    /// </summary>
    private void ShowPreviousPage()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            RefreshPage();
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    private void ShowNextPage()
    {
        if (_currentPage < GetMaxPage())
        {
            _currentPage++;
            RefreshPage();
        }
    }

    /// <summary>
    /// 刷新页面
    /// </summary>
    private void RefreshPage()
    {
        int maxPage = GetMaxPage();
        _currentPage = Mathf.Clamp(_currentPage, 1, maxPage);

        int firstItemIndex = (_currentPage - 1) * ItemsPerPage;
        string resourceTag = GetInstanceID().ToString();
        for (int i = 0; i < _homeItems.Count; i++)
        {
            int homeIndex = firstItemIndex + i;
            if (homeIndex < _filteredHomes.Count)
            {
                _homeItems[i].SetData(_filteredHomes[homeIndex], resourceTag);
            }
            else
            {
                _homeItems[i].SetData(null, resourceTag);
            }
        }

        _currentPageText.text = _currentPage.ToString();
        _maxPageText.text = maxPage.ToString();
        _previousPageButton.interactable = _currentPage > 1;
        _nextPageButton.interactable = _currentPage < maxPage;
    }

    /// <summary>
    /// 获得最大页数
    /// </summary>
    private int GetMaxPage()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)_filteredHomes.Count / ItemsPerPage));
    }

    /// <summary>
    /// 更新标签可交互性
    /// </summary>
    private void UpdateTagSelection()
    {
        for (int i = 0; i < _tagButtons.Length; i++)
        {
            _tagButtons[i].interactable = _tagCategories[i] != _selectedCategory;
        }
    }

    /// <summary>
    /// UI 是否准备完毕
    /// </summary>
    private bool HasValidUiReferences()
    {
        if (_itemBg == null ||
            _previousPageButton == null ||
            _nextPageButton == null ||
            _currentPageText == null ||
            _maxPageText == null ||
            _tagButtons == null ||
            _tagCategories == null ||
            _tagButtons.Length == 0 ||
            _tagButtons.Length != _tagCategories.Length)
        {
            Debug.LogError("HomeStoreView 的 UI 引用或标签分类未在 Inspector 中完整配置");
            return false;
        }

        for (int i = 0; i < _tagButtons.Length; i++)
        {
            if (_tagButtons[i] == null)
            {
                Debug.LogError($"HomeStoreView 的第 {i + 1} 个标签按钮未在 Inspector 中配置。");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 加载 HomeItem
    /// </summary>
    /// <param name="requestVersion"></param>
    private async void LoadHomeItemsAsync(int requestVersion)
    {
        ClearHomeItems();

        string resourceTag = GetInstanceID().ToString();
        for (int i = 0; i < ItemsPerPage; i++)
        {
            GameObject homeItemObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.HomeItem, resourceTag);

            if (requestVersion != _itemRequestVersion)
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.HomeItem, homeItemObject);
                return;
            }

            homeItemObject.transform.SetParent(_itemBg.transform, false);
            HomeItem homeItem = homeItemObject.GetComponent<HomeItem>();
            if (homeItem == null)
            {
                Debug.LogError("HomeItem 预制体缺少 HomeItem 组件");
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.HomeItem, homeItemObject);
                continue;
            }

            _homeItems.Add(homeItem);
            RefreshPage();
        }
    }

    /// <summary>
    /// 清除 HomeItem
    /// </summary>
    private void ClearHomeItems()
    {
        for (int i = 0; i < _homeItems.Count; i++)
        {
            _homeItems[i].ResetData();
            UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.HomeItem, _homeItems[i].gameObject);
        }

        _homeItems.Clear();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.HomeStoreView;
    }
}
