using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : UIBasePanel
{
    private const int MinimumTrashCanHealthCost = 3;
    private const int MaximumTrashCanHealthCost = 5;
    private const float TrashCanReminderInterval = 5f;
    private const float TrashCanReminderPeakScaleMultiplier = 1.15f;
    private const float TrashCanReminderExpandDuration = 0.15f;
    private const float TrashCanReminderRestoreDuration = 0.2f;
    private const float NoticeReminderPeakScaleMultiplier = 1.12f;
    private const float NoticeReminderHalfCycleDuration = 0.5f;
    private static readonly int[] TrashCanLotteryPoolIds = { 5, 6, 7 };
    private readonly Dictionary<GameObject, Vector3> _trashCanOriginalScales = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Tween> _trashCanReminderTweens = new Dictionary<GameObject, Tween>();
    private Coroutine _trashCanReminderCoroutine;
    private Tween _noticeReminderTween;
    private Vector3 _noticeOriginalScale;
    private const string HomeStoreFunctionId = "HomeStore";
    private const string ConvenienceStoreFunctionId = "ConvenienceStore";
    private const string ClinicFunctionId = "Clinic";
    private const string CommunityCentreFunctionId = "CommunityCentre";
    private const string CommunityCentreNoticeFunctionId = "CommunityCentreNotice";
    private const string TrashCanFunctionId = "TrashCan";
    private const string SubwayFunctionId = "Subway";
    private const string VendingMachineFunctionId = "VendingMachine";
    private const string LostWalletFunctionId = "LostWallet";
    private const string StrayCatFoodBowlFunctionId = "StrayCatFoodBowl";
    private const string DonationFunctionId = "Donation";
    private const int CommunityCentreNeedCount = 3;
    private const int CommunityCentreRedundancyItemCategory = 4;
    private const int CommunitySupplyGiftBoxItemId = 3010;
    private const int BearTapeMissionId = 200;
    private const string BearConvenienceDialogueConfig = "MissionRole,2";
    private const int CommunityReportMissionId = 201;
    private const string CommunityEnterDialogueConfig = "MissionRole,4";
    private const int ClinicConsultationMissionId = 202;
    private const string ClinicEnterDialogueConfig = "MissionRole,6";
    private const int VendingMachineLotteryPoolId = 8;
    private const int VendingMachineCost = 50;
    private const int LostWalletLotteryPoolId = 9;
    private const int LostWalletCommunityExperienceCost = 20;
    private const int LostWalletCommunityExperienceReward = 20;
    private const int StrayCatFoodBowlLotteryPoolId = 10;
    private const int DriedFishItemId = 5005;
    private const int DonationLotteryPoolId = 11;
    private const int DonationCost = 30;

    [SerializeField] private Button _btnWork;             // 零工中心
    [SerializeField] private Button _btnHomeStore;        // 家具店
    [SerializeField] private Button _btnConvenienceStore; // 便利店
    [SerializeField] private Button _btnCilinic;          // 医务室
    [SerializeField] private Button _btnCommunityCentre;  // 社区中心
    [SerializeField] private Button _btnSubway;           // 地铁
    [SerializeField] private Button _btnCommunityCentreNotice; // 社区中心公告
    [SerializeField] private Button _btnVendingMachine;   // 自动售货机
    [SerializeField] private Button _btnDonation;         // 社区募捐箱

    [Space(10)]
    [SerializeField] private GameObject[] _goTrashCans;             // 垃圾桶点位
    [SerializeField] private GameObject[] _goVendingMachines;       // 自动售货机: 0 为有货, 1 为无货
    [SerializeField] private GameObject[] _goLostWallets;           // 遗失的钱包点位
    [SerializeField] private GameObject[] _goStaryCatFoodBowls;     // 流浪猫食物盆点位
    [SerializeField] private GameObject _goDonation;                // 社区募捐箱点位

    [Space(10)]
    [SerializeField] private GameObject _goWorkRedPoint;            // 零工中心红点
    [SerializeField] private GameObject _goClinicRedPoint;          // 医务室红点
    [SerializeField] private GameObject _goCommunityCentreRedPoint; // 社区中心红点
    [SerializeField] private GameObject _goHasNotice;               // 社区中心公告红点

    private void Awake()
    {
        BindTrashCanButtons();
        BindLostWalletButtons();
        BindStrayCatFoodBowlButtons();
        BindDonationButton();

        if (_btnConvenienceStore == null)
        {
            Debug.LogError("CommunityView 未绑定便利店按钮", this);
            return;
        }

        _btnConvenienceStore.onClick.AddListener(OnClickConvenienceStore);
        if (_btnSubway == null)
        {
            Debug.LogError("CommunityView 未绑定地铁按钮", this);
            return;
        }

        _btnSubway.onClick.AddListener(OnClickSubway);
        if (_btnVendingMachine != null)
        {
            _btnVendingMachine.onClick.AddListener(OnClickVendingMachine);
        }

        if (_btnCommunityCentreNotice != null)
        {
            _btnCommunityCentreNotice.onClick.AddListener(OnClickCommunityCentreNotice);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.TurnAdvanced -= RefreshTrashCanSpawns;
        playerInfoManager.TurnAdvanced += RefreshTrashCanSpawns;
        playerInfoManager.TurnAdvanced -= RefreshVendingMachineStock;
        playerInfoManager.TurnAdvanced += RefreshVendingMachineStock;
        playerInfoManager.TurnAdvanced -= RefreshLostWalletSpawns;
        playerInfoManager.TurnAdvanced += RefreshLostWalletSpawns;
        playerInfoManager.TurnAdvanced -= RefreshStrayCatFoodBowlSpawns;
        playerInfoManager.TurnAdvanced += RefreshStrayCatFoodBowlSpawns;
        playerInfoManager.TurnAdvanced -= RefreshDonationSpawn;
        playerInfoManager.TurnAdvanced += RefreshDonationSpawn;
        playerInfoManager.PlayerInfoChanged -= RefreshFunctionUnlocks;
        playerInfoManager.PlayerInfoChanged += RefreshFunctionUnlocks;
        RefreshFunctionUnlocks(playerInfoManager);
        RefreshMissionNotice();
        StartTrashCanReminder();
    }

    protected override void HideHandle()
    {
        StopTrashCanReminder();
        StopMissionNoticeReminder();
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshTrashCanSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshVendingMachineStock;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshLostWalletSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshStrayCatFoodBowlSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshDonationSpawn;
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshFunctionUnlocks;
        base.HideHandle();
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        StopTrashCanReminder();
        StopMissionNoticeReminder();
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshTrashCanSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshVendingMachineStock;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshLostWalletSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshStrayCatFoodBowlSpawns;
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshDonationSpawn;
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshFunctionUnlocks;

        if (_btnConvenienceStore != null)
        {
            _btnConvenienceStore.onClick.RemoveListener(OnClickConvenienceStore);
        }

        if (_btnSubway != null)
        {
            _btnSubway.onClick.RemoveListener(OnClickSubway);
        }

        if (_btnVendingMachine != null)
        {
            _btnVendingMachine.onClick.RemoveListener(OnClickVendingMachine);
        }

        if (_btnCommunityCentreNotice != null)
        {
            _btnCommunityCentreNotice.onClick.RemoveListener(OnClickCommunityCentreNotice);
        }

        if (_btnDonation != null)
        {
            _btnDonation.onClick.RemoveListener(OnClickDonation);
        }

        base.OnDestroy();
    }

    public void OnClickWork()
    {
        UiButtonAudio.PlayClick();
        UIManager.GetInstance().OpenPanel(GlobalDefine.WorkView);
    }

    public void OnClickHomeStore()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(HomeStoreFunctionId))
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.HomeStoreView);
    }

    public void OnClickConvenienceStore()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(ConvenienceStoreFunctionId))
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (MissionAPI.IsMissionInProgress(BearTapeMissionId) &&
            !playerInfoManager.HasPlayedBearConvenienceDialogue)
        {
            cfg.Mission missionConfig = DataTableMananger.GetInstance().Tables.MissionTable
                .GetOrDefault(BearTapeMissionId);
            if (missionConfig == null)
            {
                Debug.LogError($"便利店对话任务配置不存在: [{BearTapeMissionId}]", this);
            }
            else
            {
                _btnConvenienceStore.interactable = false;
                bool isQueued = MissionDialogueService.TryPlayOneOffDialogue(
                    missionConfig,
                    BearConvenienceDialogueConfig,
                    dialoguePlayed =>
                    {
                        if (this == null)
                        {
                            return;
                        }

                        _btnConvenienceStore.interactable = true;
                        if (dialoguePlayed)
                        {
                            playerInfoManager.TryMarkBearConvenienceDialoguePlayed();
                        }

                        UIManager.GetInstance().OpenPanel(GlobalDefine.ConvenienceStoreView);
                    });
                if (isQueued)
                {
                    return;
                }

                _btnConvenienceStore.interactable = true;
            }
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.ConvenienceStoreView);
    }

    public void OnClickClinic()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(ClinicFunctionId))
        {
            return;
        }

        if (MissionAPI.IsMissionInProgress(ClinicConsultationMissionId))
        {
            cfg.Mission missionConfig = DataTableMananger.GetInstance().Tables.MissionTable
                .GetOrDefault(ClinicConsultationMissionId);
            if (missionConfig == null)
            {
                Debug.LogError($"医务室对话任务配置不存在: [{ClinicConsultationMissionId}]", this);
            }
            else if (_btnCilinic != null)
            {
                _btnCilinic.interactable = false;
                bool isQueued = MissionDialogueService.TryPlayOneOffDialogue(
                    missionConfig,
                    ClinicEnterDialogueConfig,
                    _ =>
                    {
                        if (this == null)
                        {
                            return;
                        }

                        if (_btnCilinic != null)
                        {
                            _btnCilinic.interactable = true;
                        }

                        UIManager.GetInstance().OpenPanel(GlobalDefine.ClinicView);
                    });
                if (isQueued)
                {
                    return;
                }

                _btnCilinic.interactable = true;
            }
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.ClinicView);
    }

    public void OnClickCommunityCentre()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(CommunityCentreFunctionId))
        {
            return;
        }

        if (MissionAPI.IsMissionInProgress(CommunityReportMissionId))
        {
            cfg.Mission missionConfig = DataTableMananger.GetInstance().Tables.MissionTable
                .GetOrDefault(CommunityReportMissionId);
            if (missionConfig == null)
            {
                Debug.LogError($"社区中心对话任务配置不存在: [{CommunityReportMissionId}]", this);
            }
            else if (_btnCommunityCentre != null)
            {
                _btnCommunityCentre.interactable = false;
                bool isQueued = MissionDialogueService.TryPlayOneOffDialogue(
                    missionConfig,
                    CommunityEnterDialogueConfig,
                    _ =>
                    {
                        if (this == null)
                        {
                            return;
                        }

                        if (_btnCommunityCentre != null)
                        {
                            _btnCommunityCentre.interactable = true;
                        }

                        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityCentreView);
                    });
                if (isQueued)
                {
                    return;
                }

                _btnCommunityCentre.interactable = true;
            }
        }

        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityCentreView);
    }

    public void OnClickSubway()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(SubwayFunctionId))
        {
            return;
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.SubwayView);
    }

    private void OnClickVendingMachine()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(VendingMachineFunctionId))
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.EnsureCurrentTurnVendingMachineStock();
        if (!playerInfoManager.HasCurrentTurnVendingMachineStock())
        {
            CommonTipView.Show("自动售货机已售罄，请下回合再来");
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "自动售货机",
                    $"购买将花费 {VendingMachineCost} 模拟币",
                    () =>
                    {
                        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
                        TryPurchaseVendingMachine();
                    })
            });
    }

    private void BindDonationButton()
    {
        if (_goDonation == null)
        {
            Debug.LogError("CommunityView 未绑定社区募捐箱点位。", this);
            return;
        }

        if (_btnDonation == null)
        {
            _btnDonation = _goDonation.GetComponent<Button>();
        }

        if (_btnDonation == null)
        {
            Debug.LogError("CommunityView 未绑定社区募捐箱按钮。", this);
            return;
        }

        _btnDonation.onClick.AddListener(OnClickDonation);
        _goDonation.SetActive(false);
    }

    private void RefreshDonationSpawn()
    {
        if (_goDonation == null)
        {
            return;
        }

        if (!FunctionUnlockService.IsUnlocked(DonationFunctionId))
        {
            _goDonation.SetActive(false);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnDonationAvailable(out bool isAvailable))
        {
            isAvailable = TurnRandom.Chance(
                "Community.Donation.Spawn",
                GameProbability.Get("Community.Donation.Spawn"));
            playerInfoManager.SetCurrentTurnDonationAvailable(isAvailable);
        }

        _goDonation.SetActive(isAvailable);
    }

    private void OnClickDonation()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(DonationFunctionId))
        {
            return;
        }

        if (!PlayerInfoManager.GetInstance().TryGetCurrentTurnDonationAvailable(
                out bool isAvailable) ||
            !isAvailable)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "社区募捐箱",
                    $"将花费 {DonationCost} 模拟币捐赠于社区困难居民",
                    DonateToCommunity)
            });
    }

    private void DonateToCommunity()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.SimulationCoins < DonationCost)
        {
            CommonTipView.Show("模拟币不足");
            return;
        }

        if (!LotteryView.TryDrawReward(
                DonationLotteryPoolId,
                "Community.Donation.Reward",
                out CommonRewardItemData reward))
        {
            Debug.LogError($"社区募捐箱奖池配置无效: [{DonationLotteryPoolId}]", this);
            CommonTipView.Show("募捐奖励暂时无法获取");
            return;
        }

        if (!playerInfoManager.TryConsumeCurrentTurnDonation() ||
            !playerInfoManager.TrySpendSimulationCoins(DonationCost))
        {
            return;
        }

        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
        LotteryView.GrantAndPresentReward(reward);
    }

    private void OnClickCommunityCentreNotice()
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(CommunityCentreNoticeFunctionId))
        {
            return;
        }

        if (!MissionAPI.TryStartMonthlyRandomMission(
                out cfg.Mission missionConfig,
                out int targetItemId,
                out int targetItemCount))
        {
            return;
        }

        cfg.Item targetItem = DataTableMananger.GetInstance()
            .Tables
            .ItemTable
            .GetOrDefault(targetItemId);
        string targetName = targetItem == null ? targetItemId.ToString() : targetItem.Name;
        string missionDescription;
        if (missionConfig.Message == "GameStamp")
        {
            missionDescription = missionConfig.Desc;
        }
        else if (missionConfig.Message == "FishCatch")
        {
            if (targetItem == null || targetItemCount <= 0)
            {
                Debug.LogError($"社区鱼获任务目标配置无效: [{targetItemId}], [{targetItemCount}]", this);
                return;
            }

            missionDescription = string.Format(
                missionConfig.Desc,
                $"<color=#00FFFA>{targetName}</color>",
                targetItemCount);
        }
        else
        {
            missionDescription = string.Format(
                missionConfig.Desc,
                targetName,
                targetItemCount);
        }

        if (string.IsNullOrEmpty(missionConfig.DialogueStart))
        {
            UIManager.GetInstance().OpenPanel(
                GlobalDefine.CommonConfirmPanel,
                UILayer.System,
                new OpenUIParam
                {
                    data = new CommonConfirmData(
                        missionConfig.Name,
                        "任务已开启，需要完成任务目标：\n" + missionDescription)
                });
        }

        RefreshMissionNotice();
    }

    private void BindTrashCanButtons()
    {
        if (_goTrashCans == null || _goTrashCans.Length != 6)
        {
            Debug.LogError("CommunityView 必须配置 6 个垃圾桶点位。", this);
            return;
        }

        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            GameObject point = _goTrashCans[pointIndex];
            if (point == null)
            {
                Debug.LogError($"CommunityView 的第 {pointIndex + 1} 个垃圾桶点位无效。", this);
                continue;
            }

            for (int childIndex = 0; childIndex < point.transform.childCount; childIndex++)
            {
                GameObject trashCan = point.transform.GetChild(childIndex).gameObject;
                Button button = trashCan.GetComponent<Button>();
                if (button == null)
                {
                    button = trashCan.AddComponent<Button>();
                    button.targetGraphic = trashCan.GetComponent<Graphic>();
                }

                int capturedPointIndex = pointIndex;
                int capturedChildIndex = childIndex;
                button.onClick.AddListener(() =>
                    TrySearchTrashCan(capturedPointIndex, capturedChildIndex, trashCan));
                _trashCanOriginalScales[trashCan] = trashCan.transform.localScale;
                trashCan.SetActive(false);
            }
        }
    }

    private void RefreshTrashCanSpawns()
    {
        if (_goTrashCans == null || _goTrashCans.Length != 6)
        {
            return;
        }

        if (!FunctionUnlockService.IsUnlocked(TrashCanFunctionId))
        {
            HideTrashCans();
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnTrashCanChildIndices(
                _goTrashCans.Length,
                out List<int> childIndices))
        {
            childIndices = CreateTrashCanChildIndices();
            playerInfoManager.SetCurrentTurnTrashCanChildIndices(childIndices);
        }

        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            SetTrashCanPointVisibility(_goTrashCans[pointIndex], childIndices[pointIndex]);
        }
    }

    private void RefreshFunctionUnlocks(PlayerInfoManager playerInfoManager)
    {
        SetFunctionButtonVisibility(_btnHomeStore, HomeStoreFunctionId);
        SetFunctionButtonVisibility(_btnConvenienceStore, ConvenienceStoreFunctionId);
        SetFunctionButtonVisibility(_btnCilinic, ClinicFunctionId);
        SetFunctionButtonVisibility(_btnCommunityCentre, CommunityCentreFunctionId);
        SetFunctionButtonVisibility(
            _btnCommunityCentreNotice,
            CommunityCentreNoticeFunctionId);
        SetFunctionButtonVisibility(_btnSubway, SubwayFunctionId);
        SetFunctionButtonVisibility(_btnVendingMachine, VendingMachineFunctionId);
        RefreshRedPoints(playerInfoManager);
        RefreshTrashCanSpawns();
        RefreshVendingMachineStock();
        RefreshLostWalletSpawns();
        RefreshStrayCatFoodBowlSpawns();
        RefreshDonationSpawn();
        RefreshMissionNotice();
    }

    private void RefreshRedPoints(PlayerInfoManager playerInfoManager)
    {
        if (_goWorkRedPoint != null)
        {
            _goWorkRedPoint.SetActive(!playerInfoManager.WorkedThisTurn);
        }

        if (_goCommunityCentreRedPoint != null)
        {
            bool isCommunityCentreUnlocked =
                FunctionUnlockService.IsUnlocked(CommunityCentreFunctionId);
            _goCommunityCentreRedPoint.SetActive(
                isCommunityCentreUnlocked &&
                (HasSubmittableCommunityCentreMaterial(playerInfoManager) ||
                 playerInfoManager.CommunityCentreProposalChoiceCount > 0));
        }
    }

    private void RefreshMissionNotice()
    {
        bool hasOffer = FunctionUnlockService.IsUnlocked(CommunityCentreNoticeFunctionId) &&
            MissionAPI.HasMonthlyRandomMissionOffer();
        if (_goHasNotice != null)
        {
            _goHasNotice.SetActive(hasOffer);
        }

        if (hasOffer)
        {
            StartMissionNoticeReminder();
        }
        else
        {
            StopMissionNoticeReminder();
        }
    }

    private void StartMissionNoticeReminder()
    {
        if (_goHasNotice == null || _noticeReminderTween != null)
        {
            return;
        }

        _noticeOriginalScale = _goHasNotice.transform.localScale;
        _noticeReminderTween = _goHasNotice.transform
            .DOScale(
                _noticeOriginalScale * NoticeReminderPeakScaleMultiplier,
                NoticeReminderHalfCycleDuration)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopMissionNoticeReminder()
    {
        if (_noticeReminderTween != null)
        {
            _noticeReminderTween.Kill();
            _noticeReminderTween = null;
        }

        if (_goHasNotice != null)
        {
            _goHasNotice.transform.localScale = _noticeOriginalScale == Vector3.zero
                ? Vector3.one
                : _noticeOriginalScale;
        }
    }

    private static bool HasSubmittableCommunityCentreMaterial(
        PlayerInfoManager playerInfoManager)
    {
        if (!CommunityCentreView.EnsureMonthlyNeeds(out _))
        {
            return false;
        }

        if (playerInfoManager.HasMonthlyCommunityCentreNeeds(CommunityCentreNeedCount))
        {
            for (int i = 0; i < CommunityCentreNeedCount; i++)
            {
                if (playerInfoManager.TryGetMonthlyCommunityCentreNeedAt(
                        i,
                        out int itemId,
                        out bool submitted) &&
                    !submitted &&
                    playerInfoManager.GetItemCount(itemId) > 0)
                {
                    return true;
                }
            }
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            return false;
        }

        IReadOnlyList<cfg.Item> items = tables.ItemTable.DataList;
        for (int i = 0; i < items.Count; i++)
        {
            cfg.Item item = items[i];
            if (item.Category == CommunityCentreRedundancyItemCategory &&
                item.Id != CommunitySupplyGiftBoxItemId &&
                playerInfoManager.GetItemCount(item.Id) > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetFunctionButtonVisibility(Button button, string functionId)
    {
        if (button != null)
        {
            button.gameObject.SetActive(FunctionUnlockService.IsUnlocked(functionId));
        }
    }

    private void RefreshVendingMachineStock()
    {
        if (!FunctionUnlockService.IsUnlocked(VendingMachineFunctionId))
        {
            SetVendingMachineStockVisibility(false, false);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.EnsureCurrentTurnVendingMachineStock();
        SetVendingMachineStockVisibility(
            true,
            playerInfoManager.HasCurrentTurnVendingMachineStock());
    }

    private void SetVendingMachineStockVisibility(bool isUnlocked, bool hasStock)
    {
        if (_goVendingMachines == null || _goVendingMachines.Length != 2)
        {
            Debug.LogError("CommunityView 必须配置自动售货机的有货和无货状态。", this);
            return;
        }

        _goVendingMachines[0].SetActive(isUnlocked && hasStock);
        _goVendingMachines[1].SetActive(isUnlocked && !hasStock);
    }

    private void TryPurchaseVendingMachine()
    {
        if (!LotteryView.TryDrawReward(
                VendingMachineLotteryPoolId,
                "Community.VendingMachine.Reward",
                out CommonRewardItemData reward))
        {
            Debug.LogError($"自动售货机奖池配置无效: [{VendingMachineLotteryPoolId}]", this);
            CommonTipView.Show("自动售货机暂时无法购买");
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.SimulationCoins < VendingMachineCost)
        {
            CommonTipView.Show("模拟币不足");
            return;
        }

        if (!playerInfoManager.TryPurchaseCurrentTurnVendingMachine(VendingMachineCost))
        {
            CommonTipView.Show("自动售货机已售罄，请下回合再来");
            return;
        }

        LotteryView.GrantAndPresentReward(reward);
    }

    private void BindLostWalletButtons()
    {
        if (_goLostWallets == null || _goLostWallets.Length != 3)
        {
            Debug.LogError("CommunityView 必须配置 3 个遗失钱包点位。", this);
            return;
        }

        for (int pointIndex = 0; pointIndex < _goLostWallets.Length; pointIndex++)
        {
            GameObject lostWallet = _goLostWallets[pointIndex];
            if (lostWallet == null)
            {
                Debug.LogError($"CommunityView 的第 {pointIndex + 1} 个遗失钱包点位无效。", this);
                continue;
            }

            Button button = lostWallet.GetComponent<Button>();
            if (button == null)
            {
                button = lostWallet.AddComponent<Button>();
                button.targetGraphic = lostWallet.GetComponent<Graphic>();
            }

            int capturedPointIndex = pointIndex;
            button.onClick.AddListener(() => OnClickLostWallet(capturedPointIndex));
            lostWallet.SetActive(false);
        }
    }

    private void RefreshLostWalletSpawns()
    {
        if (_goLostWallets == null || _goLostWallets.Length != 3)
        {
            return;
        }

        if (!FunctionUnlockService.IsUnlocked(LostWalletFunctionId))
        {
            SetLostWalletVisibility(-1);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnLostWalletPointIndex(
                _goLostWallets.Length,
                out int visiblePointIndex))
        {
            visiblePointIndex = TurnRandom.Chance(
                "Community.LostWallet.Spawn",
                GameProbability.Get("Community.LostWallet.Spawn"))
                ? TurnRandom.Range(
                    "Community.LostWallet.Point",
                    0,
                    _goLostWallets.Length)
                : -1;
            playerInfoManager.SetCurrentTurnLostWalletPointIndex(
                visiblePointIndex,
                _goLostWallets.Length);
        }

        SetLostWalletVisibility(visiblePointIndex);
    }

    private void SetLostWalletVisibility(int visiblePointIndex)
    {
        for (int pointIndex = 0; pointIndex < _goLostWallets.Length; pointIndex++)
        {
            _goLostWallets[pointIndex].SetActive(pointIndex == visiblePointIndex);
        }
    }

    private void OnClickLostWallet(int pointIndex)
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(LostWalletFunctionId))
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnLostWalletPointIndex(
                _goLostWallets.Length,
                out int visiblePointIndex) ||
            visiblePointIndex != pointIndex)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "遗失的钱包",
                    $"不知道是谁遗失的钱包，你要如何处理?",
                    () => KeepLostWallet(pointIndex),
                    () => ReturnLostWallet(pointIndex),
                    "占为己有",
                    "拾金不昧")
            });
    }

    private void KeepLostWallet(int pointIndex)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!LotteryView.TryDrawReward(
                LostWalletLotteryPoolId,
                $"Community.LostWallet.{pointIndex}.Reward",
                out CommonRewardItemData reward))
        {
            Debug.LogError($"遗失钱包奖池配置无效: [{LostWalletLotteryPoolId}]", this);
            CommonTipView.Show("钱包中的<color=#00FFFA>物品</color>暂时无法获取");
            return;
        }

        if (!playerInfoManager.TryConsumeCurrentTurnLostWallet(
                pointIndex,
                _goLostWallets.Length))
        {
            return;
        }

        int experienceCost = Mathf.Min(
            playerInfoManager.CommunityCentreExperience,
            LostWalletCommunityExperienceCost);
        if (experienceCost > 0)
        {
            playerInfoManager.TrySpendCommunityCentreExperience(experienceCost);
        }

        CommonTipView.Show($"占为己有，扣除 {experienceCost} 点社区经验");
        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
        LotteryView.GrantAndPresentReward(reward);
    }

    private void ReturnLostWallet(int pointIndex)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryConsumeCurrentTurnLostWallet(
                pointIndex,
                _goLostWallets.Length))
        {
            return;
        }

        playerInfoManager.AddCommunityCentreExperience(LostWalletCommunityExperienceReward);
        CommonTipView.Show($"上交钱包，社区经验 +{LostWalletCommunityExperienceReward}");
    }

    private void BindStrayCatFoodBowlButtons()
    {
        if (_goStaryCatFoodBowls == null || _goStaryCatFoodBowls.Length == 0)
        {
            Debug.LogError("CommunityView 必须配置至少 1 个流浪猫食物盆点位。", this);
            return;
        }

        for (int pointIndex = 0; pointIndex < _goStaryCatFoodBowls.Length; pointIndex++)
        {
            GameObject foodBowl = _goStaryCatFoodBowls[pointIndex];
            if (foodBowl == null)
            {
                Debug.LogError($"CommunityView 的第 {pointIndex + 1} 个流浪猫食物盆点位无效。", this);
                continue;
            }

            Button button = foodBowl.GetComponent<Button>();
            if (button == null)
            {
                button = foodBowl.AddComponent<Button>();
                button.targetGraphic = foodBowl.GetComponent<Graphic>();
            }

            int capturedPointIndex = pointIndex;
            button.onClick.AddListener(() => OnClickStrayCatFoodBowl(capturedPointIndex));
            foodBowl.SetActive(false);
        }
    }

    private void RefreshStrayCatFoodBowlSpawns()
    {
        if (_goStaryCatFoodBowls == null || _goStaryCatFoodBowls.Length == 0)
        {
            return;
        }

        if (!FunctionUnlockService.IsUnlocked(StrayCatFoodBowlFunctionId))
        {
            SetStrayCatFoodBowlVisibility(-1);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnStrayCatFoodBowlPointIndex(
                _goStaryCatFoodBowls.Length,
                out int visiblePointIndex))
        {
            visiblePointIndex = TurnRandom.Chance(
                "Community.StrayCatFoodBowl.Spawn",
                GameProbability.Get("Community.StrayCatFoodBowl.Spawn"))
                ? TurnRandom.Range(
                    "Community.StrayCatFoodBowl.Point",
                    0,
                    _goStaryCatFoodBowls.Length)
                : -1;
            playerInfoManager.SetCurrentTurnStrayCatFoodBowlPointIndex(
                visiblePointIndex,
                _goStaryCatFoodBowls.Length);
        }

        SetStrayCatFoodBowlVisibility(visiblePointIndex);
    }

    private void SetStrayCatFoodBowlVisibility(int visiblePointIndex)
    {
        for (int pointIndex = 0; pointIndex < _goStaryCatFoodBowls.Length; pointIndex++)
        {
            _goStaryCatFoodBowls[pointIndex].SetActive(pointIndex == visiblePointIndex);
        }
    }

    private void OnClickStrayCatFoodBowl(int pointIndex)
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(StrayCatFoodBowlFunctionId))
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnStrayCatFoodBowlPointIndex(
                _goStaryCatFoodBowls.Length,
                out int visiblePointIndex) ||
            visiblePointIndex != pointIndex)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "流浪猫食物盆",
                    "喂养将花费1个小鱼干",
                    () => FeedStrayCat(pointIndex))
            });
    }

    private void FeedStrayCat(int pointIndex)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.GetItemCount(DriedFishItemId) < 1)
        {
            CommonTipView.Show("小鱼干不足");
            return;
        }

        if (!LotteryView.TryDrawReward(
                StrayCatFoodBowlLotteryPoolId,
                $"Community.StrayCatFoodBowl.{pointIndex}.Reward",
                out CommonRewardItemData reward))
        {
            Debug.LogError(
                $"流浪猫食物盆奖池配置无效: [{StrayCatFoodBowlLotteryPoolId}]",
                this);
            CommonTipView.Show("食物盆奖励暂时无法获取");
            return;
        }

        if (!playerInfoManager.TryConsumeCurrentTurnStrayCatFoodBowl(
                pointIndex,
                _goStaryCatFoodBowls.Length) ||
            !playerInfoManager.TryConsumeItem(DriedFishItemId))
        {
            return;
        }

        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
        LotteryView.GrantAndPresentReward(reward);
    }

    private void HideTrashCans()
    {
        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            SetTrashCanPointVisibility(_goTrashCans[pointIndex], -1);
        }
    }

    private List<int> CreateTrashCanChildIndices()
    {
        List<int> childIndices = new List<int>(_goTrashCans.Length);
        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            GameObject point = _goTrashCans[pointIndex];
            int childCount = point == null ? 0 : point.transform.childCount;
            bool shouldSpawn = childCount > 0 && TurnRandom.Chance(
                $"Community.TrashCan.{pointIndex}.Spawn",
                GameProbability.Get("Community.TrashCan.Spawn"));
            childIndices.Add(shouldSpawn
                ? TurnRandom.Range(
                    $"Community.TrashCan.{pointIndex}.Child",
                    0,
                    childCount)
                : -1);
        }

        return childIndices;
    }

    private void SetTrashCanPointVisibility(GameObject point, int visibleChildIndex)
    {
        if (point == null)
        {
            return;
        }

        for (int childIndex = 0; childIndex < point.transform.childCount; childIndex++)
        {
            GameObject trashCan = point.transform.GetChild(childIndex).gameObject;
            StopTrashCanReminderTween(trashCan);
            trashCan.SetActive(childIndex == visibleChildIndex);
        }
    }

    private void StartTrashCanReminder()
    {
        StopTrashCanReminder();
        _trashCanReminderCoroutine = StartCoroutine(PlayTrashCanReminderPeriodically());
    }

    private void StopTrashCanReminder()
    {
        if (_trashCanReminderCoroutine != null)
        {
            StopCoroutine(_trashCanReminderCoroutine);
            _trashCanReminderCoroutine = null;
        }

        foreach (GameObject trashCan in new List<GameObject>(_trashCanReminderTweens.Keys))
        {
            StopTrashCanReminderTween(trashCan);
        }
    }

    private IEnumerator PlayTrashCanReminderPeriodically()
    {
        WaitForSeconds wait = new WaitForSeconds(TrashCanReminderInterval);
        while (true)
        {
            yield return wait;
            PlayTrashCanReminder();
        }
    }

    private void PlayTrashCanReminder()
    {
        if (_goTrashCans == null)
        {
            return;
        }

        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            GameObject point = _goTrashCans[pointIndex];
            if (point == null)
            {
                continue;
            }

            for (int childIndex = 0; childIndex < point.transform.childCount; childIndex++)
            {
                GameObject trashCan = point.transform.GetChild(childIndex).gameObject;
                if (trashCan.activeInHierarchy)
                {
                    PlayTrashCanReminderTween(trashCan);
                }
            }
        }
    }

    private void PlayTrashCanReminderTween(GameObject trashCan)
    {
        StopTrashCanReminderTween(trashCan);

        if (!_trashCanOriginalScales.TryGetValue(trashCan, out Vector3 originalScale))
        {
            originalScale = trashCan.transform.localScale;
            _trashCanOriginalScales[trashCan] = originalScale;
        }

        _trashCanReminderTweens[trashCan] = DOTween.Sequence()
            .Append(trashCan.transform.DOScale(
                originalScale * TrashCanReminderPeakScaleMultiplier,
                TrashCanReminderExpandDuration))
            .Append(trashCan.transform.DOScale(originalScale, TrashCanReminderRestoreDuration));
    }

    private void StopTrashCanReminderTween(GameObject trashCan)
    {
        if (trashCan == null)
        {
            return;
        }

        if (_trashCanReminderTweens.TryGetValue(trashCan, out Tween tween))
        {
            tween.Kill();
            _trashCanReminderTweens.Remove(trashCan);
        }

        if (_trashCanOriginalScales.TryGetValue(trashCan, out Vector3 originalScale))
        {
            trashCan.transform.localScale = originalScale;
        }
    }

    private void TrySearchTrashCan(int pointIndex, int childIndex, GameObject trashCan)
    {
        UiButtonAudio.PlayClick();
        if (!FunctionUnlockService.IsUnlocked(TrashCanFunctionId))
        {
            return;
        }

        int healthCost = TurnRandom.Range(
            $"Community.TrashCan.{pointIndex}.{childIndex}.HealthCost",
            MinimumTrashCanHealthCost,
            MaximumTrashCanHealthCost + 1);
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.Health < healthCost)
        {
            CommonTipView.Show($"健康值不足，需要至少 {healthCost} 点健康值");
            return;
        }

        int poolId = TrashCanLotteryPoolIds[TurnRandom.Range(
            $"Community.TrashCan.{pointIndex}.{childIndex}.LotteryPool",
            0,
            TrashCanLotteryPoolIds.Length)];
        if (!LotteryView.TryDrawReward(
                poolId,
                $"Community.TrashCan.{pointIndex}.{childIndex}.Reward",
                out CommonRewardItemData reward))
        {
            Debug.LogError($"垃圾桶奖池配置无效: [{poolId}]", this);
            CommonTipView.Show("垃圾桶里什么也没有");
            return;
        }

        if (!playerInfoManager.TryConsumeCurrentTurnTrashCan(pointIndex, childIndex))
        {
            return;
        }

        playerInfoManager.ChangeHealth(-healthCost);
        CommonTipView.Show($"翻找垃圾桶消耗 {healthCost} 点健康值");
        trashCan.SetActive(false);
        LotteryView.GrantAndPresentReward(reward);
    }

    public bool TryGetWorkButton(out Button workButton)
    {
        if (_btnWork != null)
        {
            workButton = _btnWork;
            return true;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            for (int listenerIndex = 0; listenerIndex < button.onClick.GetPersistentEventCount(); listenerIndex++)
            {
                if (button.onClick.GetPersistentMethodName(listenerIndex) == nameof(OnClickWork))
                {
                    workButton = button;
                    return true;
                }
            }
        }

        workButton = null;
        Debug.LogError("CommunityView 未绑定零工中心按钮", this);
        return false;
    }

    public bool TryGetHomeStoreButton(out Button homeStoreButton)
    {
        if (_btnHomeStore != null && _btnHomeStore.gameObject.activeInHierarchy)
        {
            homeStoreButton = _btnHomeStore;
            return true;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            for (int listenerIndex = 0; listenerIndex < button.onClick.GetPersistentEventCount(); listenerIndex++)
            {
                if (button.onClick.GetPersistentMethodName(listenerIndex) == nameof(OnClickHomeStore))
                {
                    homeStoreButton = button;
                    return true;
                }
            }
        }

        homeStoreButton = null;
        Debug.LogError("CommunityView 未绑定家具店按钮", this);
        return false;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityView;
    }
}
