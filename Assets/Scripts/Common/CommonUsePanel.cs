using System;
using UnityEngine;
using UnityEngine.UI;

public class CommonUsePanel : UIBasePanel
{
    [SerializeField] private Text _txtItemName;   // 使用物品名称
    [SerializeField] private Text _txtItemNum;    // 使用物品数量
    [SerializeField] private Button _btnLeft;     // 减少 1 个
    [SerializeField] private Button _btnLeftMax;  // 减少至 1 个
    [SerializeField] private Button _btnRight;    // 增加 1 个
    [SerializeField] private Button _btnRightMax; // 增加至最大数量
    [SerializeField] private Button _btnUse;      // 使用物品
    [SerializeField] private Button _btnCancel;   // 取消使用

    private int _maxAmount;
    private int _amount;
    private Action<int> _useCallback;

    private void Awake()
    {
        _btnLeft.onClick.AddListener(() => SetAmount(_amount - 1));
        _btnLeftMax.onClick.AddListener(() => SetAmount(1));
        _btnRight.onClick.AddListener(() => SetAmount(_amount + 1));
        _btnRightMax.onClick.AddListener(() => SetAmount(_maxAmount));
        _btnUse.onClick.AddListener(UseItems);
        _btnCancel.onClick.AddListener(ClosePanel);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        if (!(param?.data is CommonUsePanelData data) ||
            data.maxAmount < 1 ||
            string.IsNullOrWhiteSpace(data.itemName) ||
            data.useCallback == null)
        {
            Debug.LogError("CommonUsePanel 初始化参数无效。", this);
            ClosePanel();
            return;
        }

        _txtItemName.text = data.itemName;
        _maxAmount = data.maxAmount;
        _useCallback = data.useCallback;
        SetAmount(_maxAmount);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
        _useCallback = null;
        GameManager.Audio.Play(AudioDefine.SFXClose);
    }

    protected override void OnDestroy()
    {
        _btnUse.onClick.RemoveListener(UseItems);
        _btnCancel.onClick.RemoveListener(ClosePanel);
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonUsePanel;
    }

    private void SetAmount(int amount)
    {
        _amount = Mathf.Clamp(amount, 1, Mathf.Max(1, _maxAmount));
        _txtItemNum.text = _amount.ToString();
        _btnLeft.interactable = _amount > 1;
        _btnLeftMax.interactable = _amount > 1;
        _btnRight.interactable = _amount < _maxAmount;
        _btnRightMax.interactable = _amount < _maxAmount;
    }

    private void UseItems()
    {
        Action<int> callback = _useCallback;
        if (callback == null || _amount < 1 || _amount > _maxAmount)
        {
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
        _useCallback = null;
        UIManager.GetInstance().ClosePanel(GetPanelName());
        callback(_amount);
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}

public sealed class CommonUsePanelData
{
    public string itemName;
    public int maxAmount;
    public Action<int> useCallback;
}
