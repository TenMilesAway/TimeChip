using UnityEngine;
using UnityEngine.UI;

public class CommonOverPanel : UIBasePanel
{
    private System.Action _returnToLauncher;

    protected override void InitHandle(OpenUIParam param)
    {
        _returnToLauncher = param?.callback;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonOverPanel;
    }

    public void ReturnToLauncher()
    {
        _returnToLauncher?.Invoke();
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
