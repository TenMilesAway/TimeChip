using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// UI 层级
/// </summary>
public enum UILayer
{ 
    Bot,
    Mid,
    Top,
    System,
}

/// <summary>
/// UI 管理器
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private Dictionary<string, UIBasePanel> _panelDic = new Dictionary<string, UIBasePanel>();        // 当前打开的面板
    private Dictionary<string, UIBasePanel> _blockingWindows = new Dictionary<string, UIBasePanel>(); // 阻断交互的面板
    private List<string> _loadingPanels = new List<string>();                                         // 正在加载的面板

    private const string _canvasPath = "Assets/ArtRes/Canvas/Prefabs/Canvas.prefab";
    private const string _eventSystemPath = "Assets/ArtRes/Canvas/Prefabs/EventSystem.prefab";
    private const float _waitDestoryTime = 20f;

    private Transform _bot;
    private Transform _mid;
    private Transform _top;
    private Transform _system;

    private GameObject _canvasPrefab;
    private GameObject _eventSystemPrefab;

    public RectTransform _canvas;

    /// <summary>
    /// 初始化 Canvas 和 EventSystem
    /// </summary>
    public async Task Init()
    {
        // 初始化面板
        AsyncOperationHandle canvasHandle = Addressables.LoadAssetAsync<GameObject>(_canvasPath);
        await canvasHandle.Task;
        _canvasPrefab = canvasHandle.Result as GameObject;
        GameObject _canvasGO = GameObject.Instantiate(_canvasPrefab);
        _canvas = _canvasGO.transform as RectTransform;
        GameObject.DontDestroyOnLoad(_canvasGO);
            
        // 初始化事件系统
        AsyncOperationHandle eventSystemHandle = Addressables.LoadAssetAsync<GameObject>(_eventSystemPath);
        await eventSystemHandle.Task;
        _eventSystemPrefab = eventSystemHandle.Result as GameObject;
        GameObject _eventSystemGO = GameObject.Instantiate(_eventSystemPrefab);
        GameObject.DontDestroyOnLoad(_eventSystemGO);

        // 各层
        _bot = _canvas.Find("Bot");
        _mid = _canvas.Find("Mid");
        _top = _canvas.Find("Top");
        _system = _canvas.Find("System");
    }

    /// <summary>
    /// 打开 UI 面板 (目前未走定时逻辑, 后续修改)
    /// </summary>
    /// <param name="panelName">AA 路径</param>
    /// <param name="layer">UI 层级</param>
    /// <param name="param">透传参数</param>
    /// <param name="action">回调函数</param>
    /// <returns></returns>
    public async void OpenPanel(string panelName, UILayer layer = UILayer.Mid, OpenUIParam param = null, Action action = null)
    {
        // 如果此面板正在加载
        if (_loadingPanels.Contains(panelName)) return;

        _loadingPanels.Add(panelName);

        // 如果字典中存在此面板
        if (_panelDic.ContainsKey(panelName))
        {
            UIBasePanel panel = _panelDic[panelName];

            GetPanelCompletedLogic(panelName, panel, param, action);

            return;
        }

        GameObject panelGO = await UnityObjectPoolFactory.GetInstance().GetItem<GameObject>(panelName, GetInstance().ToString());

        // 设置父对象, 设置相对位置和大小
        switch (layer)
        {
            case UILayer.Bot:
                panelGO.transform.SetParent(_bot);
                break;
            case UILayer.Mid:
                panelGO.transform.SetParent(_mid);
                break;
            case UILayer.Top:
                panelGO.transform.SetParent(_top);
                break;
            case UILayer.System:
                panelGO.transform.SetParent(_system);
                break;
        }
        panelGO.transform.localPosition = Vector3.zero;
        panelGO.transform.localScale = Vector3.one;
        (panelGO.transform as RectTransform).offsetMax = Vector2.zero;
        (panelGO.transform as RectTransform).offsetMin = Vector2.zero;

        UIBasePanel panelComponent = panelGO.GetComponent<UIBasePanel>();

        GetPanelCompletedLogic(panelName, panelComponent, param, action);

        _panelDic.Add(panelName, panelComponent);
    }

    /// <summary>
    /// 处理获取面板后的打开逻辑
    /// </summary>
    private void GetPanelCompletedLogic(string panelName, UIBasePanel panel, OpenUIParam param, Action action)
    {
        panel.OnInit(param);

        if (panel._isBlockingWindow && !_blockingWindows.ContainsKey(panelName)) _blockingWindows.Add(panelName, panel);

        panel.OnShow();

        if (action != null) action();

        _loadingPanels.Remove(panelName);
    }

    public UIBasePanel GetOpeningPanel(string panelName)
    {
        _panelDic.TryGetValue(panelName, out UIBasePanel panel);

        return panel;
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
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

    /// <summary>
    /// 关闭并销毁面板
    /// </summary>
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

    /// <summary>
    /// 控件添加自定义事件监听
    /// </summary>
    /// <param name="control">控件对象</param>
    /// <param name="type">事件类型</param>
    /// <param name="callback">事件的响应函数</param>
    public void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null) trigger = control.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callback);

        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 控件删除自定义事件监听
    /// </summary>
    /// <param name="control">控件对象</param>
    /// <param name="type">事件类型</param>
    /// <param name="callback">事件的响应函数</param>
    public void RemoveCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null || trigger.triggers == null) return;

        // 遍历查找指定类型的事件
        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry entry = trigger.triggers[i];
            if (entry.eventID == type)
            {
                // 移除指定回调
                entry.callback.RemoveListener(callback);

                if (entry.callback.GetPersistentEventCount() == 0)
                {
                    trigger.triggers.RemoveAt(i);
                }
            }
        }

        // 如果没有事件了，移除组件
        if (trigger.triggers.Count == 0)
        {
            // 未来需要考虑性能消耗
            GameObject.Destroy(trigger);
        }
    }

    public bool hasBlockingWindow()
    {
        if (_blockingWindows.Count != 0) return true;
        else return false;
    }
}