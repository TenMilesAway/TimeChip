using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : UIBasePanel
{
    [SerializeField] private Button _btnWork;       // 零工中心
    [SerializeField] private Button _btnHomeStore;  // 家具店

    public void OnClickWork()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.WorkView);
    }

    public void OnClickHomeStore()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.HomeStoreView);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityView;
    }
}
