using UnityEngine;
using UnityEngine.UI;

public class CommonOverPanel : UIBasePanel
{
    private System.Action _returnToLauncher;

    protected override void InitHandle(OpenUIParam param)
    {
        _returnToLauncher = param?.callback;

        GameManager.Audio.Play(AudioDefine.SFXFailure);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonOverPanel;
    }

    public void ReturnToLauncher()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        _returnToLauncher?.Invoke();
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
