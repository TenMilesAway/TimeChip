using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommonConfirmData
{
    public string Title { get; }
    public string Detail { get; }

    public CommonConfirmData(string title, string detail)
    {
        Title = title;
        Detail = detail;
    }
}

public class CommonConfirmPanel : UIBasePanel
{
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Button _btnConfirm;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        if (!(param?.data is CommonConfirmData data))
        {
            Debug.LogError("CommonConfirmPanel 需要有效的 CommonConfirmData", this);
            return;
        }

        _txtTitle.text = data.Title;
        _txtDetail.text = data.Detail;
        _btnConfirm.onClick.RemoveAllListeners();
        _btnConfirm.onClick.AddListener(() =>
            UIManager.GetInstance().ClosePanel(GetPanelName()));
    }

    protected override void OnDestroy()
    {
        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonConfirmPanel;
    }
}
