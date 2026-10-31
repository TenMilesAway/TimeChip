using UnityEngine;
using UnityEngine.UI;

public sealed class CommonOverPanelData
{
    public int MemoryPoints { get; }
    public bool IsVoluntarySettlement { get; }

    public CommonOverPanelData(int memoryPoints, bool isVoluntarySettlement)
    {
        MemoryPoints = memoryPoints;
        IsVoluntarySettlement = isVoluntarySettlement;
    }
}

public class CommonOverPanel : UIBasePanel
{
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Text _txtMemoryPoints;

    private System.Action _returnToLauncher;

    protected override void InitHandle(OpenUIParam param)
    {
        _returnToLauncher = param?.callback;

        if (param?.data is CommonOverPanelData panelData)
        {
            if (_txtMemoryPoints != null)
            {
                _txtMemoryPoints.text = panelData.MemoryPoints.ToString();
            }

            if (panelData.IsVoluntarySettlement)
            {
                if (_txtTitle != null)
                {
                    _txtTitle.text = "主动结档";
                }

                if (_txtDetail != null)
                {
                    _txtDetail.text = "本局已结束，当前存档将被删除。";
                }

                GameManager.Audio.Play(AudioDefine.SFXClick);
            }
            else
            {
                GameManager.Audio.Play(AudioDefine.SFXFailure);
            }
        }
        else
        {
            GameManager.Audio.Play(AudioDefine.SFXFailure);
        }
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
