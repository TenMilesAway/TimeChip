using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KitchenView : UIBasePanel
{
    [Header("上方订单")]
    [SerializeField] private KitchenOrderItem[] _orders;           // 订单, 共 4 个
                                                                  
    [Header("下方厨房")]                                          
    [SerializeField] private Button _btnInventory;                 // 冰箱按钮
    [SerializeField] private Button _btnHearth;                    // 烹饪按钮
    [SerializeField] private Button _btnCutting;                   // 砧板按钮
    [SerializeField] private Button _btnMenu;                      // 菜谱按钮
    [SerializeField] private Slider _sliderCutting;                // 切菜进度
    [SerializeField] private Slider _sliderHearth;                 // 烹饪进度
    [SerializeField] private GameObject _txtCutting;               // 切菜中文本
    [SerializeField] private GameObject _txtHearth;                // 烹饪中文本
    [SerializeField] private GameObject _inventoryView;            // 冰箱界面
    [SerializeField] private GameObject _hearthView;               // 烹饪界面
    [SerializeField] private GameObject _cuttingView;              // 砧板界面
    [SerializeField] private GameObject _menuView;                 // 菜谱界面
                                                                  
    [Header("冰箱")]                                              
    [SerializeField] private Button _btnLeft;                      // 上一页
    [SerializeField] private Button _btnRight;                     // 下一页
    [SerializeField] private KitchenInventoryItem[] _items;        // 冰箱物品 Item, 共 10 个

    [Header("砧板")]
    [SerializeField] private Image _imgMatLeft;                    // 左侧材料 Icon
    [SerializeField] private Image _imgMatRight;                   // 右侧材料 Icon
    [SerializeField] private Image _imgProgress;                   // 进度图片
    [SerializeField] private Button _btnMatLeft;                   // 左侧材料
    [SerializeField] private Button _btnMatRight;                  // 右侧材料
    [SerializeField] private Button _btnCut;                       // 切菜按钮
    [SerializeField] private Button _btnCuttingLeft;               // 上一页
    [SerializeField] private Button _btnCuttingRight;              // 下一页
    [SerializeField] private KitchenInventoryItem[] _cuttingItems; // 冰箱物品 Item, 共 10 个

    [SerializeField] private Button _btnBack;                      // 返回按钮 

    private void Awake()
    {
        if (_btnBack != null)
        {
            _btnBack.onClick.AddListener(OnClickBack);
        }
    }

    protected override void OnDestroy()
    {
        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        base.OnDestroy();
    }

    private void OnClickBack()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    public override string GetPanelName()
    {
        return GlobalDefine.KitchenView;
    }
}
