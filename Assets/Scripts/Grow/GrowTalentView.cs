using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrowTalentView : UIBasePanel
{
    private const string HiddenText = "???";

    [SerializeField] private Text _txtPoint;
    [SerializeField] private Button _btnBack;
    [SerializeField] private GrowItem2[] _items;

    [Header("详情弹窗")]
    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Text _txtNeedPoint;
    [SerializeField] private Button _btnLevelUp;
    [SerializeField] private GameObject _goDetail;

    [SerializeField] private float _unlockHoldDuration = 1.1f;
    [SerializeField] private ScrollRect _scrollRect;

    private Grow2TalentGraph _graph;
    private GrowView2TalentLines _talentLines;
    private readonly Dictionary<int, GrowItem2> _itemMap = new Dictionary<int, GrowItem2>();
    private GrowItem2 _selectedItem;
    private UIButtonLongPressHold _unlockHold;
    private bool _isUnlocking;

    private void Awake()
    {
        if (_items == null || _items.Length == 0)
        {
            _items = GetComponentsInChildren<GrowItem2>(true);
        }

        if (_btnBack != null)
        {
            _btnBack.onClick.AddListener(ClosePanel);
        }

        if (_btnLevelUp != null)
        {
            _unlockHold = _btnLevelUp.GetComponent<UIButtonLongPressHold>();
            if (_unlockHold == null)
            {
                _unlockHold = _btnLevelUp.gameObject.AddComponent<UIButtonLongPressHold>();
            }

            _unlockHold.SetHoldDuration(_unlockHoldDuration);
            _unlockHold.HoldProgressChanged += OnUnlockHoldProgress;
            _unlockHold.HoldCompleted += OnUnlockHoldCompleted;
            _unlockHold.HoldCancelled += OnUnlockHoldCancelled;
        }

        if (_goDetail != null)
        {
            _goDetail.SetActive(false);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        InitializeItems();
        HideDetail();
        RefreshAll();
    }

    protected override void ShowHandle()
    {
        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        globalInfoManager.GlobalInfoChanged -= OnGlobalInfoChanged;
        globalInfoManager.GlobalInfoChanged += OnGlobalInfoChanged;
        InitializeItems();
        RefreshAll();
    }

    protected override void HideHandle()
    {
        GlobalInfoManager.GetInstance().GlobalInfoChanged -= OnGlobalInfoChanged;
        CancelUnlockHold();
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
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(ClosePanel);
        }

        if (_unlockHold != null)
        {
            _unlockHold.HoldProgressChanged -= OnUnlockHoldProgress;
            _unlockHold.HoldCompleted -= OnUnlockHoldCompleted;
            _unlockHold.HoldCancelled -= OnUnlockHoldCancelled;
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.GrowView2;
    }

    private void InitializeItems()
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables?.Grow2Table == null)
        {
            Debug.LogError("Grow2Table 未加载", this);
            return;
        }

        _graph = Grow2TalentGraph.Build(tables.Grow2Table);
        _itemMap.Clear();
        for (int index = 0; index < _items.Length; index++)
        {
            GrowItem2 item = _items[index];
            if (item == null)
            {
                continue;
            }

            int growId = item.GrowId;
            if (!_graph.TryGetNode(growId, out _))
            {
                Debug.LogError($"GrowItem2 配置了表中不存在的 ID：{growId}", item);
                continue;
            }

            if (!_itemMap.TryAdd(growId, item))
            {
                Debug.LogError($"GrowItem2 存在重复 ID：{growId}", item);
                continue;
            }

            item.BindDetailClick(OnItemDetailClicked);
        }

        RebuildTalentLines();
    }

    private void RebuildTalentLines()
    {
        if (_graph == null || _itemMap.Count == 0)
        {
            return;
        }

        if (_scrollRect == null)
        {
            _scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        RectTransform content = _scrollRect != null ? _scrollRect.content : null;
        if (content == null)
        {
            return;
        }

        if (_talentLines == null)
        {
            _talentLines = content.GetComponent<GrowView2TalentLines>();
            if (_talentLines == null)
            {
                _talentLines = content.gameObject.AddComponent<GrowView2TalentLines>();
            }
        }

        _talentLines.Rebuild(_graph, _itemMap);
    }

    private void OnGlobalInfoChanged(GlobalInfoManager _)
    {
        RefreshAll();
        RefreshDetail();
    }

    private void RefreshAll()
    {
        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        if (_txtPoint != null)
        {
            _txtPoint.text = globalInfo.MemoryPoints.ToString();
        }

        for (int index = 0; index < _items.Length; index++)
        {
            GrowItem2 item = _items[index];
            if (item != null)
            {
                item.RefreshVisual(_graph, globalInfo);
            }
        }
    }

    private void OnItemDetailClicked(GrowItem2 item)
    {
        _selectedItem = item;
        if (_goDetail != null)
        {
            _goDetail.SetActive(true);
        }

        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selectedItem == null || _goDetail == null || !_goDetail.activeSelf)
        {
            return;
        }

        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        int growId = _selectedItem.GrowId;
        bool unlocked = globalInfo.IsGrow2TalentUnlocked(growId);
        bool prerequisiteMet = _graph.IsPrerequisiteSatisfied(growId, globalInfo.GetUnlockedGrow2TalentIdSet());
        bool canUnlock = globalInfo.CanUnlockGrow2Talent(growId, _graph);
        bool showHidden = !unlocked && !prerequisiteMet;

        if (!_graph.TryGetNode(growId, out cfg.Grow2 config))
        {
            return;
        }

        if (_txtName != null)
        {
            _txtName.text = showHidden ? HiddenText : config.Name;
        }

        if (_txtDetail != null)
        {
            _txtDetail.text = showHidden ? HiddenText : config.EffectDesc;
        }

        if (_txtNeedPoint != null)
        {
            _txtNeedPoint.text = showHidden ? HiddenText : config.MemoryPointCost.ToString();
        }

        if (_btnLevelUp != null)
        {
            _btnLevelUp.gameObject.SetActive(!unlocked);
            _btnLevelUp.interactable = canUnlock && !_isUnlocking;
        }
    }

    private void HideDetail()
    {
        _selectedItem = null;
        if (_goDetail != null)
        {
            _goDetail.SetActive(false);
        }
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void OnUnlockHoldProgress(float progress)
    {
        if (_selectedItem == null || _isUnlocking)
        {
            return;
        }

        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        if (!globalInfo.CanUnlockGrow2Talent(_selectedItem.GrowId, _graph))
        {
            return;
        }

        if (progress <= 0f)
        {
            _selectedItem.BeginUnlockHold();
        }

        _selectedItem.ApplyUnlockHoldProgress(progress);
    }

    private void OnUnlockHoldCompleted()
    {
        if (_selectedItem == null)
        {
            return;
        }

        GlobalInfoManager globalInfo = GlobalInfoManager.GetInstance();
        int growId = _selectedItem.GrowId;
        if (!globalInfo.CanUnlockGrow2Talent(growId, _graph))
        {
            _selectedItem.CancelUnlockHold();
            RefreshDetail();
            return;
        }

        _isUnlocking = true;
        _selectedItem.ApplyUnlockHoldProgress(1f);
        bool unlocked = globalInfo.TryUnlockGrow2Talent(growId, _graph);
        _isUnlocking = false;
        if (!unlocked)
        {
            _selectedItem.CancelUnlockHold();
            RefreshDetail();
            return;
        }

        _selectedItem.CompleteUnlockVisual();
        RefreshAll();
        RefreshDetail();
        _unlockHold?.ResetHoldState();
    }

    private void OnUnlockHoldCancelled()
    {
        CancelUnlockHold();
        RefreshDetail();
    }

    private void CancelUnlockHold()
    {
        _isUnlocking = false;
        _selectedItem?.CancelUnlockHold();
        _unlockHold?.ResetHoldState();
    }
}
