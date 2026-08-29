using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommonMessageData
{
    public string Message { get; }

    public CommonMessageData(string message)
    {
        Message = message;
    }
}

public class CommonMessageView : UIBasePanel
{
    [SerializeField] private Text _txtMessage;

    /// <summary>打开通用消息面板并显示指定文本</summary>
    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("消息内容不能为空", nameof(message));
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonMessageView,
            param: new OpenUIParam { data = new CommonMessageData(message) });
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (_txtMessage == null)
        {
            Debug.LogError("CommonMessageView 的消息文本未在 Inspector 中配置", this);
            return;
        }

        if (!(param?.data is CommonMessageData messageData) ||
            string.IsNullOrWhiteSpace(messageData.Message))
        {
            Debug.LogError("CommonMessageView 需要有效的 CommonMessageData", this);
            _txtMessage.text = string.Empty;
            return;
        }

        _txtMessage.text = messageData.Message;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonMessageView;
    }
}
