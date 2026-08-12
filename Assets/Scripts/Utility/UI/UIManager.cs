using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 为兼容 OpenPanel 接口而保留的 UI 层级枚举
/// </summary>
public enum UILayer
{
    Bot,
    Mid,
    Top,
    System,
}

/// <summary>
/// UI 管理器，所有打开的面板均挂载在 Launcher/UI Root 下
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private const string UIRootPath = "Canvas/UI Root";

    private readonly Dictionary<string, UIBasePanel> _panelDic = new Dictionary<string, UIBasePanel>();
    private readonly Dictionary<string, UIBasePanel> _blockingWindows = new Dictionary<string, UIBasePanel>();
    private readonly List<string> _loadingPanels = new List<string>();

    private RectTransform _uiRoot;

    /// <summary>
    /// 初始化 Launcher UI Root 引用
    /// </summary>
    public Task Init()
    {
        TryGetUIRoot();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在 Launcher/UI Root 下打开 UI 面板，layer 参数仅为兼容现有接口而保留
    /// </summary>
    public async void OpenPanel(string panelName, UILayer layer = UILayer.Mid, OpenUIParam param = null, Action action = null)
    {
        if (_loadingPanels.Contains(panelName) || !TryGetUIRoot())
        {
            return;
        }

        _loadingPanels.Add(panelName);

        if (_panelDic.TryGetValue(panelName, out UIBasePanel panel))
        {
            GetPanelCompletedLogic(panelName, panel, param, action);
            return;
        }

        GameObject panelGO = await UnityObjectPoolFactory.GetInstance().GetItem<GameObject>(panelName, GetInstance().ToString());

        if (!TryGetUIRoot())
        {
            _loadingPanels.Remove(panelName);
            UnityObjectPoolFactory.GetInstance().PutItem(panelName, panelGO);
            return;
        }

        panelGO.transform.SetParent(_uiRoot, false);
        RectTransform panelTransform = panelGO.transform as RectTransform;
        panelTransform.offsetMax = Vector2.zero;
        panelTransform.offsetMin = Vector2.zero;

        UIBasePanel panelComponent = panelGO.GetComponent<UIBasePanel>();
        GetPanelCompletedLogic(panelName, panelComponent, param, action);
        _panelDic.Add(panelName, panelComponent);
    }

    private bool TryGetUIRoot()
    {
        if (_uiRoot != null) return true;

        GameObject uiRootObject = GameObject.Find(UIRootPath);
        if (uiRootObject == null)
        {
            Debug.LogError($"UI Root was not found at '{UIRootPath}'.");
            return false;
        }

        _uiRoot = uiRootObject.transform as RectTransform;
        return _uiRoot != null;
    }

    private void GetPanelCompletedLogic(string panelName, UIBasePanel panel, OpenUIParam param, Action action)
    {
        panel.OnInit(param);

        if (panel._isBlockingWindow && !_blockingWindows.ContainsKey(panelName))
        {
            _blockingWindows.Add(panelName, panel);
        }

        panel.OnShow();
        action?.Invoke();
        _loadingPanels.Remove(panelName);
    }

    public UIBasePanel GetOpeningPanel(string panelName)
    {
        _panelDic.TryGetValue(panelName, out UIBasePanel panel);
        return panel;
    }

    public void ClosePanel(string panelName)
    {
        if (_blockingWindows.ContainsKey(panelName))
        {
            _blockingWindows.Remove(panelName);
        }

        if (_panelDic.ContainsKey(panelName))
        {
            _panelDic[panelName].OnClose();
            UnityObjectPoolFactory.GetInstance().PutItem(panelName, _panelDic[panelName].gameObject);
            _panelDic.Remove(panelName);
        }
    }

    public void ClosePanelAndDestory(string panelName)
    {
        if (_blockingWindows.ContainsKey(panelName))
        {
            _blockingWindows.Remove(panelName);
        }

        if (_panelDic.ContainsKey(panelName))
        {
            _panelDic[panelName].OnClose();
            GameObject.Destroy(_panelDic[panelName].gameObject);
            _panelDic.Remove(panelName);
        }
    }

    public void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = control.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    public void RemoveCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null || trigger.triggers == null)
        {
            return;
        }

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry entry = trigger.triggers[i];
            if (entry.eventID != type)
            {
                continue;
            }

            entry.callback.RemoveListener(callback);
            if (entry.callback.GetPersistentEventCount() == 0)
            {
                trigger.triggers.RemoveAt(i);
            }
        }

        if (trigger.triggers.Count == 0)
        {
            GameObject.Destroy(trigger);
        }
    }

    public bool hasBlockingWindow()
    {
        return _blockingWindows.Count != 0;
    }
}
