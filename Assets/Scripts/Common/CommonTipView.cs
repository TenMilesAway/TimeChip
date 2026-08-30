using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用提示容器, 通过 UIManager 打开并复用唯一的提示实例
/// </summary>
public class CommonTipView : UIBasePanel
{
    [SerializeField] private Transform _tipParent;

    private static readonly Queue<string> PendingMessages = new Queue<string>();

    private static CommonTipView _instance;
    private static bool _isOpening;

    private CommonTip _currentTip;
    private string _pendingMessage;
    private bool _isLoadingTip;

    private void Awake()
    {
        _instance = this;
        _isBlockingWindow = false;
    }

    /// <summary>
    /// 展示一条通用提示; 面板未打开时会由 UIManager 自动打开
    /// </summary>
    public static void Show(string message)
    {
        GameManager.Audio.Play(AudioDefine.SFXMessageOpen);

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        PendingMessages.Enqueue(message);
        if (_instance != null && _instance.gameObject.activeInHierarchy)
        {
            FlushPendingMessages();
            return;
        }

        if (_isOpening)
        {
            return;
        }

        _isOpening = true;
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonTipView,
            UILayer.System,
            action: FlushPendingMessages);
    }

    /// <summary>
    /// 处理等待展示的提示文本; 连续请求时只保留最后一次内容
    /// </summary>
    private static void FlushPendingMessages()
    {
        _isOpening = false;
        if (_instance == null)
        {
            return;
        }

        while (PendingMessages.Count > 0)
        {
            _instance.ShowInternal(PendingMessages.Dequeue());
        }
    }

    /// <summary>
    /// 清理当前显示的提示
    /// </summary>
    public void Clear()
    {
        _pendingMessage = null;
        if (_currentTip != null)
        {
            RecycleTip(_currentTip);
        }
    }

    /// <summary>
    /// 从对象池创建提示并加入容器
    /// </summary>
    private void ShowInternal(string message)
    {
        if (_tipParent == null)
        {
            Debug.LogError("CommonTipView 未绑定提示挂载节点", this);
            return;
        }

        _pendingMessage = message;
        if (_currentTip != null)
        {
            _currentTip.Play(_pendingMessage, RecycleTip);
            return;
        }

        if (_isLoadingTip)
        {
            return;
        }

        _isLoadingTip = true;
        CreateTip();
    }

    /// <summary>
    /// 从对象池异步取得一条提示实例
    /// </summary>
    private void CreateTip()
    {
        UnityObjectPoolFactory.GetInstance().GetItemAsync<GameObject>(
            GlobalDefine.CommonTip,
            GetInstanceID().ToString(),
            tipObject =>
            {
                _isLoadingTip = false;
                if (tipObject == null)
                {
                    Debug.LogError("CommonTip 预制体加载失败", this);
                    return;
                }

                CommonTip tip = tipObject.GetComponent<CommonTip>();
                if (tip == null)
                {
                    Debug.LogError("CommonTip 预制体缺少 CommonTip 组件", tipObject);
                    UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonTip, tipObject);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_pendingMessage))
                {
                    UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonTip, tipObject);
                    return;
                }

                tip.transform.SetParent(_tipParent, false);
                _currentTip = tip;
                tip.Play(_pendingMessage, RecycleTip);
            });
    }

    /// <summary>
    /// 回收结束的提示实例
    /// </summary>
    private void RecycleTip(CommonTip tip)
    {
        if (tip == null || _currentTip != tip)
        {
            return;
        }

        _currentTip = null;
        UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonTip, tip.gameObject);
    }

    /// <summary>
    /// 销毁面板时清理提示与静态引用
    /// </summary>
    protected override void OnDestroy()
    {
        Clear();
        if (_instance == this)
        {
            _instance = null;
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonTipView;
    }
}
