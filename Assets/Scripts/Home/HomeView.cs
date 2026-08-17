using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeView : UIBasePanel
{
    // 基础倍率：500 - 1%
    // 一些家中重要的物品会有 0% - 20% 的降价 (500 ~ 400)
    // 一些装饰性物品会有 0% - 20% 的涨价     (500 ~ 600)

    [Header("玄关")]
    [SerializeField] private GameObject _hallwayChairs;          // 500     1%       玄关椅子
    [SerializeField] private GameObject _hallwayTable;           // 1000    2%       玄关柜子

    [Header("游戏屋")]
    [SerializeField] private GameObject _gameComputer;           // 1600    4%       游戏屋电脑
    [SerializeField] private GameObject _gameComputerChair;      // 520     1%       游戏屋椅子
    [SerializeField] private GameObject _gameComputerLight;      // 280     0.5%     游戏屋电脑旁灯
    [Space(10)]
    [SerializeField] private GameObject _gameCarpet;             // 300     0.5%     游戏屋地毯
    [Space(10)]
    [SerializeField] private GameObject _gameMachine;            // 1125    2.5%     游戏屋游戏机
    [SerializeField] private GameObject _gameTable1;             // 800     1.5%     游戏屋游戏机旁柜子
    [SerializeField] private GameObject _gameBed;                // 500     1%       游戏屋游戏机旁小窝
    [Space(10)]
    [SerializeField] private GameObject _gameTable2;             // 1000    2%       游戏屋柜子
    [SerializeField] private GameObject _gameTable2Light;        // 280     0.5%     游戏屋柜子旁灯
    [SerializeField] private GameObject _gameTable2StorageBox;   // 800     1.5%     游戏屋柜子旁储物箱

    [Header("洗手间")]
    [SerializeField] private GameObject _washCarpet;             // 550     1%       洗手间地毯
    [SerializeField] private GameObject _washPlant;              // 300     0.5%     洗手间植物
    [Space(10)]
    [SerializeField] private GameObject _washToilet;             // 1300    3%       洗手间马桶
    [SerializeField] private GameObject _washBathtub;            // 1100    2.5%     洗手间浴缸
    [SerializeField] private GameObject _washBathtubDesk;        // 800     1.5%     洗手间浴缸旁桌子
    [SerializeField] private GameObject _washClotheBusket;       // 600     1%       洗手间脏衣篓
    [Space(10)]
    [SerializeField] private GameObject _washBasin;              // 1200    3%       洗手间洗手台
    [SerializeField] private GameObject _washTowel;              // 500     1%       洗手间毛巾架
    [SerializeField] private GameObject _washBasinDesk;          // 600     1%       洗手间洗手台旁桌子

    [Header("厨房")]
    [SerializeField] private GameObject _kitchenCarpet;          // 600     1%       厨房地毯
    [Space(10)]
    [SerializeField] private GameObject _kitchenFridge;          // 1200    3%       厨房冰箱
    [SerializeField] private GameObject _kitchenTables;          // 800     1.5%     厨房橱柜
    [SerializeField] private GameObject _kitchenTableware;       // 1100    2%       厨房厨具
    [Space(10)]
    [SerializeField] private GameObject _kitchenHearth;          // 1200    3%       厨房灶台
    [SerializeField] private GameObject _kitchenBasin;           // 800     2%       厨房水槽

    [Header("客厅")]
    [SerializeField] private GameObject _livingCarpet1;          // 550     1%       客厅地毯1
    [SerializeField] private GameObject _livingCarpet2;          // 1180    2%       客厅地毯2
    [Space(10)]
    [SerializeField] private GameObject _livingSofa;             // 1250    2.5%     客厅沙发
    [SerializeField] private GameObject _livingSofaLight;        // 280     0.5%     客厅沙发旁灯
    [SerializeField] private GameObject _livingSofaLittleDesk;   // 850     1.5%     客厅沙发旁小桌子
    [SerializeField] private GameObject _livingSofaDesk;         // 1280    3%       客厅沙发旁桌子
    [SerializeField] private GameObject _livingSofaTable;        // 1800    3%       客厅沙发旁收纳柜
    [SerializeField] private GameObject _livingTV;               // 1280    3%       客厅电视
    [SerializeField] private GameObject _livingTVLight;          // 280     0.5%     客厅电视旁灯
    [Space(10)]
    [SerializeField] private GameObject _livingDiningTable;      // 900     2%       客厅餐桌
    [SerializeField] private GameObject _livingDiningTablePlant; // 300     0.5%     客厅餐桌植物
    [Space(10)]
    [SerializeField] private GameObject _livingPlant;            // 1100    2%       客厅植物

    [Header("卧室1")]
    [SerializeField] private GameObject _bedroomPlant;           // 300     0.5%     卧室植物
    [Space(10)]
    [SerializeField] private GameObject _bedroomBed;             // 1000    2.5%     卧室床
    [SerializeField] private GameObject _bedroomBesideTables;    // 1000    2%       卧室床头柜
    [SerializeField] private GameObject _bedroomBesideLights;    // 560     1%       卧室床头灯
    [Space(10)]
    [SerializeField] private GameObject _bedroomCloset;          // 675     1.5%     卧室衣柜
    [SerializeField] private GameObject _bedroomClosetTable;     // 600     1%       卧室衣柜旁桌子
    [Space(10)]
    [SerializeField] private GameObject _bedroomDressingTable;   // 1050    2%       卧室梳妆台
    [SerializeField] private GameObject _bedroomDressingLight;   // 550     1%       卧室梳妆台旁灯
    [SerializeField] private GameObject _bedroomDesk;            // 1200    2%       卧室梳妆台旁桌子

    [Header("卧室2")]
    [SerializeField] private GameObject _bedroom2Bed;            // 800     2%       卧室2床
    [SerializeField] private GameObject _bedroom2BesideTable;    // 500     1%       卧室2床头柜
    [SerializeField] private GameObject _bedroom2BesideLight;    // 480     1%       卧室2床头灯
    [Space(10)]
    [SerializeField] private GameObject _bedroom2Closet;         // 675     1.5%     卧室2衣柜
    [Space(10)]
    [SerializeField] private GameObject _bedroom2Sofa;           // 520     1%       卧室2椅子
    [SerializeField] private GameObject _bedroom2StorageBox1;    // 300     0.5%     卧室2储物箱1
    [SerializeField] private GameObject _bedroom2StorageBox2;    // 275     0.5%     卧室2储物箱2
    [Space(10)]
    [SerializeField] private GameObject _bedroom2Desk;           // 750     1.5%     卧室2桌子

    [Header("阳台")]
    [SerializeField] private GameObject _balconyCarpet;          // 550     1%       阳台地毯
    [Space(10)]
    [SerializeField] private GameObject _balconyKettle;          // 200     0.5%     阳台水壶
    [SerializeField] private GameObject _balconyPlant1;          // 480     1%       阳台植物1
    [SerializeField] private GameObject _balconyPlant2;          // 500     1%       阳台植物2
    [SerializeField] private GameObject _balconyPlant3;          // 550     1%       阳台植物3
    [SerializeField] private GameObject _balconyPlant4;          // 300     0.5%     阳台植物4
    [Space(10)]
    [SerializeField] private GameObject _balconyClotheHanger1;   // 200     0.5%     阳台晾衣架1
    [SerializeField] private GameObject _balconyClotheHanger2;   // 495     1%       阳台晾衣架2
    [SerializeField] private GameObject _balconyStorageBox;      // 500     1%       阳台储物箱
    [Space(10)]
    [SerializeField] private GameObject _balconyLeftDesk;        // 250     0.5%     阳台左侧桌子
    [SerializeField] private GameObject _balconyChair1;          // 600     1%       阳台左侧椅子1
    [SerializeField] private GameObject _balconyChair2;          // 550     1%       阳台左侧椅子2
    [SerializeField] private GameObject _balconyDesk;            // 550     1%       阳台桌子

    public override string GetPanelName()
    {
        return GlobalDefine.HomeView;
    }
}
