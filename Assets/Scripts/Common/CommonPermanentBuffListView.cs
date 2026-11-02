using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonPermanentBuffListView : UIBasePanel
{
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Transform _entryParent;
    [SerializeField] private Button _btnClose;

    private readonly List<CommonPermanentBuffListEntry> _activeEntries = new List<CommonPermanentBuffListEntry>();
    private string _resourceTag;
    private int _buildVersion;

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
        if (_txtTitle != null)
        {
            _txtTitle.text = "增益";
        }

        BuildEntriesAsync();
    }

    protected override void HideHandle()
    {
        ClearEntries();
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        ClearEntries();
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonPermanentBuffListView;
    }

    private async void BuildEntriesAsync()
    {
        int buildVersion = ++_buildVersion;
        ClearEntries();
        if (_entryParent == null)
        {
            Debug.LogError("CommonPermanentBuffListView 未配置 _entryParent。", this);
            return;
        }

        List<ActiveBuffData> permanentBuffs = CollectPermanentActiveBuffs();
        for (int i = 0; i < permanentBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = permanentBuffs[i];
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(activeBuff.buffId);
            if (buffConfig == null)
            {
                Debug.LogError($"永久 BUFF 配置不存在: [{activeBuff.buffId}]", this);
                continue;
            }

            GameObject entryObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.CommonPermanentBuffListEntry, _resourceTag);
            if (buildVersion != _buildVersion || entryObject == null)
            {
                if (entryObject != null)
                {
                    UnityObjectPoolFactory.GetInstance()
                        .PutItem(GlobalDefine.CommonPermanentBuffListEntry, entryObject);
                }

                return;
            }

            entryObject.transform.SetParent(_entryParent, false);
            CommonPermanentBuffListEntry entry = entryObject.GetComponent<CommonPermanentBuffListEntry>();
            if (entry == null)
            {
                Debug.LogError("CommonPermanentBuffListEntry 预制体缺少组件。", entryObject);
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.CommonPermanentBuffListEntry, entryObject);
                continue;
            }

            entry.SetData(buffConfig);
            _activeEntries.Add(entry);
        }
    }

    private static List<ActiveBuffData> CollectPermanentActiveBuffs()
    {
        List<ActiveBuffData> result = new List<ActiveBuffData>();
        List<ActiveBuffData> activeBuffs = PlayerInfoManager.GetInstance().GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(activeBuff.buffId);
            if (!BuffSystem.IsPermanentActiveBuff(activeBuff, buffConfig))
            {
                continue;
            }

            result.Add(activeBuff);
        }

        result.Sort((left, right) => left.buffId.CompareTo(right.buffId));
        return result;
    }

    private void ClearEntries()
    {
        _buildVersion++;
        for (int i = 0; i < _activeEntries.Count; i++)
        {
            CommonPermanentBuffListEntry entry = _activeEntries[i];
            if (entry == null)
            {
                continue;
            }

            entry.Clear();
            UnityObjectPoolFactory.GetInstance()
                .PutItem(GlobalDefine.CommonPermanentBuffListEntry, entry.gameObject);
        }

        _activeEntries.Clear();
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
