using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClinicView : UIBasePanel
{
    [SerializeField] private Text _txtCoin;           // 模拟币文本
    [SerializeField] private Text _txtHealth;         // 健康值文本
    [SerializeField] private Text _txtCureService1;   // 治疗服务1价格
    [SerializeField] private Text _txtCureService2;   // 治疗服务2价格
    [SerializeField] private Text _txtCureItem1;      // 治疗物品1价格, 500
    [SerializeField] private Text _txtCureItem2;      // 治疗物品2价格, 998
    [SerializeField] private Text _txtCureItem3;      // 治疗物品3价格, 1488

    [SerializeField] private Button _btnExamination;  // 体检按钮
    [SerializeField] private Button _btnCure1;        // 治疗服务1按钮
    [SerializeField] private Button _btnCure2;        // 治疗服务2按钮
    [SerializeField] private Button _btnCureItem1;    // 治疗物品1购买按钮
    [SerializeField] private Button _btnCureItem2;    // 治疗物品2购买按钮
    [SerializeField] private Button _btnCureItem3;    // 治疗物品3购买按钮

    [SerializeField] private GameObject[] _playerIcons; // 玩家头像, 90以上显示第0个，80以上显示第1个，70以上显示第2个，60以上显示第3个，60以下显示第4个

    private const int CureItem1Id = 1050;
    private const int CureItem2Id = 1051;
    private const int CureItem3Id = 1052;
    private const int CureItem1Price = 500;
    private const int CureItem2Price = 998;
    private const int CureItem3Price = 1488;

    private bool _isUiReady;

    private void Awake()
    {
        FindPlayerIconsWhenNeeded();
        _isUiReady = HasValidUiReferences();
        if (!_isUiReady)
        {
            enabled = false;
            return;
        }

        _btnExamination.onClick.AddListener(TryExamination);
        _btnCure1.onClick.AddListener(TryTreatment1);
        _btnCure2.onClick.AddListener(TryTreatment2);
        _btnCureItem1.onClick.AddListener(TryPurchaseItem1);
        _btnCureItem2.onClick.AddListener(TryPurchaseItem2);
        _btnCureItem3.onClick.AddListener(TryPurchaseItem3);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        if (!_isUiReady)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged += RefreshPlayerInfo;
        RefreshPlayerInfo(playerInfoManager);
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        if (_btnExamination != null)
        {
            _btnExamination.onClick.RemoveListener(TryExamination);
        }

        if (_btnCure1 != null)
        {
            _btnCure1.onClick.RemoveListener(TryTreatment1);
        }

        if (_btnCure2 != null)
        {
            _btnCure2.onClick.RemoveListener(TryTreatment2);
        }

        if (_btnCureItem1 != null)
        {
            _btnCureItem1.onClick.RemoveListener(TryPurchaseItem1);
        }

        if (_btnCureItem2 != null)
        {
            _btnCureItem2.onClick.RemoveListener(TryPurchaseItem2);
        }

        if (_btnCureItem3 != null)
        {
            _btnCureItem3.onClick.RemoveListener(TryPurchaseItem3);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshPlayerInfo;
        base.OnDestroy();
    }

    private void TryExamination()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.ExaminedThisTurn)
        {
            CommonTipView.Show("本回合已完成体检");
            return;
        }

        List<cfg.BuffConfig> availableBuffs = GetAvailableExaminationBuffs(playerInfoManager);
        if (availableBuffs.Count == 0)
        {
            Debug.LogError("体检没有可用的 BUFF 配置。", this);
            CommonTipView.Show("当前没有可获得的 BUFF");
            return;
        }

        cfg.BuffConfig selectedBuff = availableBuffs[Random.Range(0, availableBuffs.Count)];
        if (!BuffSystem.GetInstance().TryAddBuff(selectedBuff.Id))
        {
            Debug.LogError($"体检激活 BUFF 失败: [{selectedBuff.Id}]", this);
            CommonTipView.Show("BUFF 激活失败");
            return;
        }

        if (playerInfoManager.TryUseClinicExamination(selectedBuff.Id) ==
            ClinicExaminationResult.Success)
        {
            CommonTipView.Show($"体检成功，获得【{selectedBuff.Name}】");
        }
    }

    private void TryTreatment(int serviceId)
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int healthBefore = playerInfoManager.Health;
        ClinicTreatmentResult result = playerInfoManager.TryUseClinicTreatment(serviceId);
        switch (result)
        {
            case ClinicTreatmentResult.Success:
                CommonTipView.Show($"治疗成功，健康值 +{playerInfoManager.Health - healthBefore}");
                break;
            case ClinicTreatmentResult.AlreadyTreated:
                CommonTipView.Show("本回合已使用治疗服务");
                break;
            case ClinicTreatmentResult.InsufficientCoins:
                CommonTipView.Show("模拟币不足");
                break;
            case ClinicTreatmentResult.HealthFull:
                CommonTipView.Show("健康值已满");
                break;
            default:
                Debug.LogError($"治疗服务配置无效: [{serviceId}]", this);
                break;
        }
    }

    private void TryTreatment1()
    {
        TryTreatment(1);
    }

    private void TryTreatment2()
    {
        TryTreatment(2);
    }

    private void TryPurchaseItem(int itemId, int price)
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        ClinicItemPurchaseResult result = PlayerInfoManager.GetInstance()
            .TryPurchaseClinicItem(itemId, price);
        switch (result)
        {
            case ClinicItemPurchaseResult.Success:
                cfg.Item item = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(itemId);
                CommonTipView.Show($"购买成功，获得【{item.Name}】");
                break;
            case ClinicItemPurchaseResult.AlreadyPurchased:
                CommonTipView.Show("本回合已购买治疗物品");
                break;
            case ClinicItemPurchaseResult.InsufficientCoins:
                CommonTipView.Show("模拟币不足");
                break;
            default:
                Debug.LogError($"治疗物品配置无效: [{itemId}]", this);
                break;
        }
    }

    private void TryPurchaseItem1()
    {
        TryPurchaseItem(CureItem1Id, CureItem1Price);
    }

    private void TryPurchaseItem2()
    {
        TryPurchaseItem(CureItem2Id, CureItem2Price);
    }

    private void TryPurchaseItem3()
    {
        TryPurchaseItem(CureItem3Id, CureItem3Price);
    }

    private void RefreshPlayerInfo(PlayerInfoManager playerInfoManager)
    {
        _txtCoin.text = playerInfoManager.SimulationCoins.ToString();
        _txtHealth.text = playerInfoManager.Health.ToString();
        _txtCureService1.text = playerInfoManager.CureService1Price.ToString();
        _txtCureService2.text = playerInfoManager.CureService2Price.ToString();
        _txtCureItem1.text = CureItem1Price.ToString();
        _txtCureItem2.text = CureItem2Price.ToString();
        _txtCureItem3.text = CureItem3Price.ToString();

        _btnExamination.interactable = !playerInfoManager.ExaminedThisTurn;
        bool canTreat = !playerInfoManager.TreatedThisTurn;
        _btnCure1.interactable = canTreat;
        _btnCure2.interactable = canTreat;
        bool canPurchase = !playerInfoManager.PurchasedClinicItemThisTurn;
        _btnCureItem1.interactable = canPurchase;
        _btnCureItem2.interactable = canPurchase;
        _btnCureItem3.interactable = canPurchase;

        int iconIndex = playerInfoManager.Health >= 90 ? 0 :
            playerInfoManager.Health >= 80 ? 1 :
            playerInfoManager.Health >= 70 ? 2 :
            playerInfoManager.Health >= 60 ? 3 : 4;
        for (int i = 0; i < _playerIcons.Length; i++)
        {
            _playerIcons[i].SetActive(i == iconIndex);
        }
    }

    private static List<cfg.BuffConfig> GetAvailableExaminationBuffs(
        PlayerInfoManager playerInfoManager)
    {
        IReadOnlyList<cfg.BuffConfig> configs = DataTableMananger.GetInstance()
            .Tables.BuffConfigTable.DataList;
        List<cfg.BuffConfig> availableBuffs = new List<cfg.BuffConfig>();
        for (int i = 0; i < configs.Count; i++)
        {
            cfg.BuffConfig config = configs[i];
            if (config.ActivationType == "Manual" &&
                playerInfoManager.Satisfaction >= config.MinSatisfaction)
            {
                availableBuffs.Add(config);
            }
        }

        return availableBuffs;
    }

    private void FindPlayerIconsWhenNeeded()
    {
        if (_playerIcons != null && _playerIcons.Length == 5)
        {
            return;
        }

        _playerIcons = new GameObject[5];
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            switch (transforms[i].name)
            {
                case "Player Icon Health 90+":
                    _playerIcons[0] = transforms[i].gameObject;
                    break;
                case "Player Icon Health 80+":
                    _playerIcons[1] = transforms[i].gameObject;
                    break;
                case "Player Icon Health 70+":
                    _playerIcons[2] = transforms[i].gameObject;
                    break;
                case "Player Icon Health 60+":
                    _playerIcons[3] = transforms[i].gameObject;
                    break;
                case "Player Icon Health 60-":
                    _playerIcons[4] = transforms[i].gameObject;
                    break;
            }
        }
    }

    private bool HasValidUiReferences()
    {
        if (_txtCoin == null ||
            _txtHealth == null ||
            _txtCureService1 == null ||
            _txtCureService2 == null ||
            _txtCureItem1 == null ||
            _txtCureItem2 == null ||
            _txtCureItem3 == null ||
            _btnExamination == null ||
            _btnCure1 == null ||
            _btnCure2 == null ||
            _btnCureItem1 == null ||
            _btnCureItem2 == null ||
            _btnCureItem3 == null ||
            _playerIcons == null ||
            _playerIcons.Length != 5)
        {
            Debug.LogError("ClinicView 的 UI 引用未在 Inspector 中完整配置。", this);
            return false;
        }

        for (int i = 0; i < _playerIcons.Length; i++)
        {
            if (_playerIcons[i] == null)
            {
                Debug.LogError($"ClinicView 的第 {i + 1} 个健康头像未配置。", this);
                return false;
            }
        }

        return true;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.ClinicView;
    }
}
