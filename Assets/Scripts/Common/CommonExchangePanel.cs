using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonExchangePanel : UIBasePanel
{
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Transform _groupParent;
    [SerializeField] private Button _btnClose;

    private readonly List<GameObject> _activeGroupObjects = new List<GameObject>();
    private int _exchangeGroupId;
    private string _resourceTag;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(ClosePanel);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        _exchangeGroupId = 0;
        if (param?.data is CommonExchangeOpenParam openParam)
        {
            _exchangeGroupId = openParam.exchangeGroupId;
        }

        if (_exchangeGroupId <= 0)
        {
            Debug.LogError("CommonExchangePanel 需要有效的 exchangeGroupId", this);
            return;
        }

        cfg.ExchangeGroup exchangeGroup = DataTableMananger.GetInstance()
            .Tables
            .ExchangeGroupTable
            .GetOrDefault(_exchangeGroupId);
        if (exchangeGroup == null)
        {
            Debug.LogError($"兑换组配置不存在: [{_exchangeGroupId}]", this);
            return;
        }

        if (_txtTitle != null)
        {
            _txtTitle.text = exchangeGroup.Name;
        }

        BuildExchangeGroupsAsync();
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        ClearExchangeGroups();
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        ClearExchangeGroups();
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonExchangePanel;
    }

    private async void BuildExchangeGroupsAsync()
    {
        ClearExchangeGroups();
        if (_groupParent == null)
        {
            Debug.LogError("CommonExchangePanel 未配置 _groupParent", this);
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        for (int i = 0; i < tables.ExchangeTable.DataList.Count; i++)
        {
            cfg.Exchange exchangeConfig = tables.ExchangeTable.DataList[i];
            if (exchangeConfig == null || exchangeConfig.GroupId != _exchangeGroupId)
            {
                continue;
            }

            GameObject groupObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.CommonExchangeGroup, _resourceTag);
            if (groupObject == null)
            {
                continue;
            }

            groupObject.transform.SetParent(_groupParent, false);
            CommonExchangeGroup exchangeGroup = groupObject.GetComponent<CommonExchangeGroup>();
            if (exchangeGroup == null)
            {
                Debug.LogError("CommonExchangeGroup 预制体缺少组件。", this);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonExchangeGroup, groupObject);
                continue;
            }

            exchangeGroup.Setup(exchangeConfig, RefreshAllGroups);
            _activeGroupObjects.Add(groupObject);
        }
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        RefreshAllGroups();
    }

    private void RefreshAllGroups()
    {
        for (int i = 0; i < _activeGroupObjects.Count; i++)
        {
            GameObject groupObject = _activeGroupObjects[i];
            if (groupObject == null)
            {
                continue;
            }

            CommonExchangeGroup exchangeGroup = groupObject.GetComponent<CommonExchangeGroup>();
            if (exchangeGroup != null)
            {
                exchangeGroup.RefreshPresentation();
            }
        }
    }

    private void ClearExchangeGroups()
    {
        for (int i = 0; i < _activeGroupObjects.Count; i++)
        {
            GameObject groupObject = _activeGroupObjects[i];
            if (groupObject != null)
            {
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonExchangeGroup, groupObject);
            }
        }

        _activeGroupObjects.Clear();
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
