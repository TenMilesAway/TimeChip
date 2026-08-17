using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : UIBasePanel
{
    [SerializeField] private Button _btnWork;  // 零工中心

    public void OnClickWork()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.WorkView);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityView;
    }
}
