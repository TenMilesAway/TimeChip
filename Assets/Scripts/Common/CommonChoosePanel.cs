using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommonChooseData
{
    public string Title { get; }
    public string Detail { get; }
    public Action ConfirmAction { get; }
    public Action CancelAction { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }

    public CommonChooseData(
        string title,
        string detail,
        Action confirmAction,
        Action cancelAction = null,
        string confirmText = null,
        string cancelText = null)
    {
        Title = title;
        Detail = detail;
        ConfirmAction = confirmAction;
        CancelAction = cancelAction;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }
}

public class CommonChoosePanel : UIBasePanel
{
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Text _txtConfirm;
    [SerializeField] private Text _txtCancel;
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private Button _btnCancel;

    private string _defaultConfirmText;
    private string _defaultCancelText;

    private void Awake()
    {
        _defaultConfirmText = _txtConfirm.text;
        _defaultCancelText = _txtCancel.text;
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        if (!(param?.data is CommonChooseData data))
        {
            Debug.LogError("CommonChoosePanel 需要有效的 CommonChooseData", this);
            return;
        }

        _txtTitle.text = data.Title;
        _txtDetail.text = data.Detail;
        _txtConfirm.text = string.IsNullOrWhiteSpace(data.ConfirmText)
            ? _defaultConfirmText
            : data.ConfirmText;
        _txtCancel.text = string.IsNullOrWhiteSpace(data.CancelText)
            ? _defaultCancelText
            : data.CancelText;

        _btnConfirm.onClick.RemoveAllListeners();
        _btnConfirm.onClick.AddListener(() => data.ConfirmAction?.Invoke());

        _btnCancel.onClick.RemoveAllListeners();
        _btnCancel.onClick.AddListener(() =>
        {
            data.CancelAction?.Invoke();
            UIManager.GetInstance().ClosePanel(GetPanelName());
        });
    }

    protected override void OnDestroy()
    {
        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
        }

        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonChoosePanel;
    }
}
