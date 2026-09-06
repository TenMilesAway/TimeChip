using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : UIBasePanel
{
    [SerializeField] private Button _btnWork;       // 零工中心
    [SerializeField] private Button _btnHomeStore;  // 家具店
    [SerializeField] private Button _btnConvenienceStore; // 便利店
    [SerializeField] private Button _btnCilinic;    // 医务室

    private void Awake()
    {
        if (_btnConvenienceStore == null)
        {
            Debug.LogError("CommunityView 未绑定便利店按钮。", this);
            return;
        }

        _btnConvenienceStore.onClick.AddListener(OnClickConvenienceStore);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        if (_btnConvenienceStore != null)
        {
            _btnConvenienceStore.onClick.RemoveListener(OnClickConvenienceStore);
        }

        base.OnDestroy();
    }

    public void OnClickWork()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.WorkView);
    }

    public void OnClickHomeStore()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.HomeStoreView);
    }

    public void OnClickConvenienceStore()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.ConvenienceStoreView);
    }

    public void OnClickClinic()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.ClinicView);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityView;
    }
}
