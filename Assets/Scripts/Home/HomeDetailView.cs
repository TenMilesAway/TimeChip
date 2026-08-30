using UnityEngine;
using UnityEngine.UI;

public class HomeDetailView : UIBasePanel
{
    [SerializeField] private Text _txtSatisfaction; // 满意度文本：xx%

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        RefreshSatisfaction(PlayerInfoManager.GetInstance());
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshSatisfaction;
        playerInfoManager.PlayerInfoChanged += RefreshSatisfaction;
        RefreshSatisfaction(playerInfoManager);
    }

    protected override void CloseHandle()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        base.CloseHandle();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshSatisfaction;
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshSatisfaction;
        base.OnDestroy();
    }

    private void RefreshSatisfaction(PlayerInfoManager playerInfoManager)
    {
        if (_txtSatisfaction == null)
        {
            Debug.LogError("HomeDetailView 的满意度文本未在 Inspector 中配置", this);
            return;
        }

        _txtSatisfaction.text = $"{playerInfoManager.Satisfaction:0.##}%";
    }

    public override string GetPanelName()
    {
        return GlobalDefine.HomeDetailView;
    }
}
