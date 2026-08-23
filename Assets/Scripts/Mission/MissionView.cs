using System.Collections.Generic;
using RedSaw.MissionSystem;
using UnityEngine;
using UnityEngine.UI;

public class MissionView : UIBasePanel
{
    [SerializeField] private Text _txtCurrentPage;
    [SerializeField] private Text _txtMaxPage;
    [SerializeField] private Button _btnPrevious;
    [SerializeField] private Button _btnNext;
    [SerializeField] private MissionItem[] _missionItems;

    private readonly List<Mission<MissionMessage>> _missions = new List<Mission<MissionMessage>>();

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

        _btnPrevious.onClick.AddListener(ShowPreviousPage);
        _btnNext.onClick.AddListener(ShowNextPage);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        RefreshMissions();
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (!_isUiReady)
        {
            return;
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        PlayerInfoManager.GetInstance().PlayerInfoChanged += OnPlayerInfoChanged;
        RefreshMissions();
    }

    protected override void HideHandle()
    {
        base.HideHandle();
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
    }

    protected override void OnDestroy()
    {
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

    public override string GetPanelName()
    {
        return GlobalDefine.MissionView;
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        RefreshMissions();
    }

    private void RefreshMissions()
    {
        if (!_isUiReady)
        {
            return;
        }

        _missions.Clear();
        _missions.AddRange(MissionAPI.GetActiveMissions());
        _missions.Sort((left, right) => int.Parse(left.id).CompareTo(int.Parse(right.id)));
        _currentPage = Mathf.Clamp(_currentPage, 1, GetMaxPage());
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
        int firstMissionIndex = (_currentPage - 1) * _missionItems.Length;
        for (int i = 0; i < _missionItems.Length; i++)
        {
            int missionIndex = firstMissionIndex + i;
            if (missionIndex >= _missions.Count ||
                !int.TryParse(_missions[missionIndex].id, out int missionId))
            {
                _missionItems[i].Clear();
                continue;
            }

            cfg.Mission missionConfig = DataTableMananger.GetInstance().Tables.MissionTable.GetOrDefault(missionId);
            if (missionConfig == null)
            {
                Debug.LogError($"任务配置不存在: [{missionId}]", this);
                _missionItems[i].Clear();
                continue;
            }

            string claimMissionId = _missions[missionIndex].id;
            _missionItems[i].SetData(
                missionConfig,
                _missions[missionIndex],
                () => ClaimMission(claimMissionId));
        }

        _txtCurrentPage.text = _currentPage.ToString();
        _txtMaxPage.text = GetMaxPage().ToString();
        _btnPrevious.interactable = _currentPage > 1;
        _btnNext.interactable = _currentPage < GetMaxPage();
    }

    private void ClaimMission(string missionId)
    {
        if (!MissionAPI.TryClaimMission(missionId))
        {
            Debug.LogWarning($"任务不可领取: [{missionId}]", this);
        }
    }

    private int GetMaxPage()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)_missions.Count / _missionItems.Length));
    }

    private bool HasValidUiReferences()
    {
        if (_txtCurrentPage == null ||
            _txtMaxPage == null ||
            _btnPrevious == null ||
            _btnNext == null ||
            _missionItems == null ||
            _missionItems.Length == 0)
        {
            Debug.LogError("MissionView 的 UI 引用未完整配置。", this);
            return false;
        }

        for (int i = 0; i < _missionItems.Length; i++)
        {
            if (_missionItems[i] == null)
            {
                Debug.LogError($"MissionView 的第 {i + 1} 个任务格子未配置。", this);
                return false;
            }
        }

        return true;
    }
}
