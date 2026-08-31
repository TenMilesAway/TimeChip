using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrowView : UIBasePanel
{
    [SerializeField] private Text _txtPoint;              // 回忆点文本
    [SerializeField] private Text _txtCardName;           // 卡牌名称文本
    [SerializeField] private Text _txtBaseEffect;         // 基础效果文本
    [SerializeField] private Text _txtOneStarEffect;      // 一星效果文本
    [SerializeField] private Text _txtThreeStarEffect;    // 三星效果文本
    [SerializeField] private Text _txtFiveStarEffect;     // 五星效果文本
    [SerializeField] private Text _txtGrowNum;            // 升星数量文本
    [SerializeField] private Button _btnClose;            // 关闭按钮
    [SerializeField] private Button _btnGrow;             // 升星按钮

    [SerializeField] private GameObject _goSelect;        // 选中卡牌时展示
    [SerializeField] private GameObject _goUnselect;      // 未选中卡牌时展示
    [SerializeField] private GameObject[] _goUnlockStars; // 已解锁星级, 共 5 个
    [SerializeField] private GameObject[] _goLockStars;   // 未解锁星级, 共 5 个
    [SerializeField] private GrowItem[] _goGrowItems;     // 升星卡牌列表, 共 9 个

    public override string GetPanelName()
    {
        return GlobalDefine.GrowView;
    }
}
