using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FishView : UIBasePanel
{
    private const string FishMovementSteady = "steady";
    private const string FishMovementDart = "dart";
    private const string FishMovementSinker = "sinker";
    private const string FishMovementFloater = "floater";
    private const string FishMovementJumper = "jumper";
    private const string FishMovementErratic = "erratic";

    [Header("钓鱼与捕鱼游戏参数")]
    private const float BiteWaitMinSeconds = 1f;             // 钓鱼: 最短等待时间
    private const float BiteWaitMaxSeconds = 2f;            // 钓鱼: 最长等待时间
    private const float CatchBoundaryBounceDamping = 0.55f;    // 捕鱼: 边界反弹速度衰减
    private const float ClickPressDuration = 0.06f;           // 捕鱼游戏: 点击按钮压缩时长
    private const float ClickBounceDuration = 0.14f;          // 捕鱼游戏: 点击按钮回弹时长
    private const float ClickSettleDuration = 0.1f;           // 捕鱼游戏: 点击按钮复位时长
    private const float FishButtonCooldownSeconds = 1f;       // 捕鱼游戏结束后再次钓鱼的等待时间
    private const float FishCatchCelebrationDuration = 0.24f;  // 捕鱼成功反馈时长
    private const float TreasureSpawnDelaySeconds = 1f;       // 捕鱼开始后宝藏出现等待时间
    private const float TreasureScaleUpDuration = 0.2f;        // 宝藏出现放大时长
    private const float TreasureScaleSettleDuration = 0.12f;   // 宝藏出现回弹时长
    private const float TreasureCatchFeedbackDuration = 0.22f; // 宝藏钓起反馈时长
    private const float InitialFishProgress = 0.3f;           // 捕鱼游戏: 初始进度
    private const int FishHookCategory = 7;
    private const int FishBaitCategory = 8;
    private const int FishItemCategory = 6;
    private const int TreasureItemId = 3030;
    private const string FishDegreeEffectType = "FishDegree";

    [Header("UI")]
    [SerializeField] private Text _txtFishStoreCoin;    // 河边商店: 模拟币数量
    [SerializeField] private Text _txtEquipedHookName;  // 钓鱼: 已装备的鱼钩名称
    [SerializeField] private Text _txtEquipedBaitName;  // 钓鱼: 已装备的鱼饵名称
    [SerializeField] private Image _imgFishProgress;    // 捕鱼游戏: 鱼进度条
    private Image _imgFish;
    [SerializeField] private Image _imgEquipedHookIcon;  // 钓鱼: 已装备的鱼钩图标
    [SerializeField] private Image _imgEquipedBaitIcon;  // 钓鱼: 已装备的鱼饵图标
    [SerializeField] private Image _imgTreasure;         // 捕鱼游戏: 宝藏图标
    [SerializeField] private Button _btnFish;           // 钓鱼按钮
    [SerializeField] private Button _btnChangeBait;     // 钓鱼: 更换鱼饵按钮
    [SerializeField] private Button _btnChangeHook;     // 钓鱼: 更换鱼钩按钮
    [SerializeField] private Button _btnCloseChangePanel;  // 钓鱼: 关闭更换装备面板按钮
    [SerializeField] private Button _btnClick;          // 捕鱼游戏: 与鱼互动按钮
    [SerializeField] private Button _btnBack;           // 返回社区
    [SerializeField] private Button _btnFishStore;      // 河边商店
    [SerializeField] private Button _btnFishStoreClose; // 河边商店: 关闭按钮
    [SerializeField] private Button _btnPageHook;       // 河边商店: 鱼钩页签
    [SerializeField] private Button _btnPageBait;       // 河边商店: 鱼饵页签
    [SerializeField] private Button _btnInventory;      // 钓鱼背包
    [SerializeField] private Animator _animatorFishRod; // 鱼竿动画

    [Header("GO")]
    [SerializeField] private GameObject _goFishCatch;   // 捕鱼游戏
    [SerializeField] private GameObject _goEquipedBait; // 钓鱼: 已装备的鱼饵
    [SerializeField] private GameObject _goChangePanel; // 钓鱼: 更换装备面板
    [SerializeField] private GameObject _goStore;       // 河边商店
    [SerializeField] private GameObject _goStoreHooks;  // 河边商店: 鱼钩页签
    [SerializeField] private GameObject _goStoreBaits;  // 河边商店: 鱼饵页签
    [SerializeField] private RectTransform _rectFishBg; // 捕鱼游戏: 鱼背景位置
    [SerializeField] private RectTransform _rectFish;   // 捕鱼游戏: 鱼位置
    [SerializeField] private RectTransform _rectCatch;  // 捕鱼游戏: 捕捉位置
    [SerializeField] private RectTransform _rectTreasure;  // 捕鱼游戏: 宝藏位置
    [SerializeField] private FishStoreItem[] _itemHooks;  // 河边商店: 鱼钩页签的鱼钩道具, 共 5 个
    [SerializeField] private FishStoreItem[] _itemBaits;  // 河边商店: 鱼饵页签的鱼饵道具, 共 6 个
    [SerializeField] private FishChangeItem[] fishChangeItems; // 钓鱼: 更换装备面板的道具, 共 6 个
    [Header("捕鱼测试")]
    [SerializeField] private bool _enableFishCatchTest;   // 捕鱼测试: 强制选择指定鱼种
    [SerializeField] private int _testFishId;             // 捕鱼测试: 指定鱼种 ID

    private const string AnimationEmpty = "Empty";   // 空闲动画
    private const string AnimationStart = "Start";   // 开始钓鱼动画
    private const string AnimationIdle = "Idle";     // 钓鱼中动画
    private const string AnimationEnd = "End";       // 上钩动画

    private Coroutine _fishingCoroutine;
    private Coroutine _fishButtonCooldownCoroutine;
    private bool _isCatchActive;
    private float _fishTargetY;
    private float _fishMoveSpeed;
    private int _fishMovementRandomIndex;
    private float _fishPauseTimer;
    private float _fishMinY;
    private float _fishMaxY;
    private float _catchMinY;
    private float _catchMaxY;
    private float _catchVelocity;
    private float _treasureSpawnTimer;
    private bool _treasureSpawnPending;
    private bool _treasureSpawned;
    private bool _wasFishCaught;
    private bool _wasTreasureCovered;
    private bool _wasTreasureCaught;
    private cfg.FishRod _activeFishRod;
    private cfg.FishBait _activeFishBait;
    private cfg.Fish _activeFish;
    private Coroutine _fishCatchCelebrationCoroutine;
    private float _initialCatchHeight;
    private bool _isCatchButtonHeld;
    private int _catchButtonPointerId;
    private Vector2 _initialFishPosition;
    private Vector2 _initialCatchPosition;
    private Vector3 _initialFishScale;
    private Vector3 _initialFishProgressScale;
    private Vector3 _initialTreasureScale;
    private Color _initialTreasureColor;
    private Vector3 _initialClickButtonScale;
    private int _equipmentPresentationVersion;
    private int _fishingAttemptIndex;
    private int _activeFishSizeCm;
    private Sprite _initialFishSprite;

    private void Awake()
    {
        _imgFish = _rectFish == null ? null : _rectFish.GetComponent<Image>();
        if (!HasValidUiReferences())
        {
            enabled = false;
            return;
        }

        _initialFishPosition = _rectFish.anchoredPosition;
        _initialCatchPosition = _rectCatch.anchoredPosition;
        _initialFishScale = _rectFish.localScale;
        _initialFishProgressScale = _imgFishProgress.transform.localScale;
        _initialTreasureScale = _rectTreasure.localScale;
        _initialTreasureColor = _imgTreasure.color;
        _initialCatchHeight = _rectCatch.rect.height;
        _initialFishSprite = _imgFish.sprite;
        HideTreasure();
        _initialClickButtonScale = _btnClick.transform.localScale;
        EventTrigger clickButtonEventTrigger = _btnClick.GetComponent<EventTrigger>();
        if (clickButtonEventTrigger == null)
        {
            clickButtonEventTrigger = _btnClick.gameObject.AddComponent<EventTrigger>();
        }

        AddPointerEventListener(
            clickButtonEventTrigger,
            EventTriggerType.PointerDown,
            OnCatchButtonPointerDown);
        AddPointerEventListener(
            clickButtonEventTrigger,
            EventTriggerType.PointerUp,
            OnCatchButtonPointerUp);
        _btnFish.onClick.AddListener(OnClickFish);
        _btnChangeBait.onClick.AddListener(OnClickChangeBait);
        _btnChangeHook.onClick.AddListener(OnClickChangeHook);
        _btnCloseChangePanel.onClick.AddListener(OnClickCloseChangePanel);
        _btnClick.onClick.AddListener(OnClickCatch);
        _btnBack.onClick.AddListener(OnClickBack);
        _btnFishStore.onClick.AddListener(OnClickFishStore);
        _btnFishStoreClose.onClick.AddListener(OnClickFishStoreClose);
        _btnPageHook.onClick.AddListener(OnClickHookPage);
        _btnPageBait.onClick.AddListener(OnClickBaitPage);
        _btnInventory.onClick.AddListener(OnClickInventory);
    }

    protected override void ShowHandle()
    {
        ResetFishingState();
        UIManager.GetInstance().SetMainMenuNavigationVisible(false);
        PlayerInfoManager.GetInstance().PlayerInfoChanged += RefreshStore;
        RefreshStore(PlayerInfoManager.GetInstance());
        SetStorePage(true);
        _goStore.SetActive(false);
        _goChangePanel.SetActive(false);
    }

    protected override void HideHandle()
    {
        ResetFishingState();
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshStore;
        UIManager.GetInstance().SetMainMenuNavigationVisible(true);
    }

    protected override void OnDestroy()
    {
        StopFishing();
        UIManager.GetInstance().SetMainMenuNavigationVisible(true);
        if (_btnFish != null)
        {
            _btnFish.onClick.RemoveListener(OnClickFish);
        }

        if (_btnChangeBait != null)
        {
            _btnChangeBait.onClick.RemoveListener(OnClickChangeBait);
        }

        if (_btnChangeHook != null)
        {
            _btnChangeHook.onClick.RemoveListener(OnClickChangeHook);
        }

        if (_btnCloseChangePanel != null)
        {
            _btnCloseChangePanel.onClick.RemoveListener(OnClickCloseChangePanel);
        }

        if (_btnClick != null)
        {
            _btnClick.onClick.RemoveListener(OnClickCatch);
        }

        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveListener(OnClickBack);
        }

        if (_btnFishStore != null)
        {
            _btnFishStore.onClick.RemoveListener(OnClickFishStore);
        }

        if (_btnFishStoreClose != null)
        {
            _btnFishStoreClose.onClick.RemoveListener(OnClickFishStoreClose);
        }

        if (_btnPageHook != null)
        {
            _btnPageHook.onClick.RemoveListener(OnClickHookPage);
        }

        if (_btnPageBait != null)
        {
            _btnPageBait.onClick.RemoveListener(OnClickBaitPage);
        }

        if (_btnInventory != null)
        {
            _btnInventory.onClick.RemoveListener(OnClickInventory);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshStore;
        base.OnDestroy();
    }

    private void Update()
    {
        if (!_isCatchActive)
        {
            return;
        }

        MoveFish();
        MoveCatch();
        UpdateTreasureSpawn();
        UpdateTreasureProgress();
        UpdateFishProgress();
    }

    private void OnCatchButtonPointerDown(BaseEventData baseEventData)
    {
        if (!_isCatchActive || !(baseEventData is PointerEventData eventData))
        {
            return;
        }

        _isCatchButtonHeld = true;
        _catchButtonPointerId = eventData.pointerId;
        PlayCatchClickFeedback();
    }

    private void OnCatchButtonPointerUp(BaseEventData baseEventData)
    {
        if (!(baseEventData is PointerEventData eventData))
        {
            return;
        }

        if (_isCatchButtonHeld && eventData.pointerId == _catchButtonPointerId)
        {
            _isCatchButtonHeld = false;
        }
    }

    private bool TrySelectFish(
        IReadOnlyList<cfg.Fish> fishConfigs,
        int forcedFishId,
        float currentFishDegree,
        out cfg.Fish selectedFish)
    {
        selectedFish = null;
        if (fishConfigs == null || fishConfigs.Count == 0)
        {
            return false;
        }

        long totalWeight = 0;
        for (int i = 0; i < fishConfigs.Count; i++)
        {
            if (fishConfigs[i].Weight <= 0 ||
                fishConfigs[i].Rarity <= 0 ||
                fishConfigs[i].MoveDurationMinSeconds <= 0f ||
                fishConfigs[i].MoveDurationMaxSeconds <
                fishConfigs[i].MoveDurationMinSeconds ||
                fishConfigs[i].MoveSpeedMultiplier <= 0f ||
                fishConfigs[i].SuddenMoveProbability < 0f ||
                fishConfigs[i].SuddenMoveProbability > 1f ||
                fishConfigs[i].SuddenMoveSpeedMultiplier < 1f ||
                !IsValidFishMovementConfig(fishConfigs[i]) ||
                fishConfigs[i].FishDegree < 0 ||
                fishConfigs[i].SizeMinCm <= 0 ||
                fishConfigs[i].SizeMaxCm < fishConfigs[i].SizeMinCm)
            {
                Debug.LogError($"鱼类配置无效: [{fishConfigs[i].Id}]", this);
                return false;
            }

            if (fishConfigs[i].FishDegree <= currentFishDegree)
            {
                totalWeight += fishConfigs[i].Weight;
            }
        }

        if (forcedFishId > 0)
        {
            for (int i = 0; i < fishConfigs.Count; i++)
            {
                if (fishConfigs[i].Id == forcedFishId)
                {
                    selectedFish = fishConfigs[i];
                    return true;
                }
            }

            return false;
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        long roll = TurnRandom.Range($"Fish.Species.{_fishingAttemptIndex}", totalWeight);
        for (int i = 0; i < fishConfigs.Count; i++)
        {
            if (fishConfigs[i].FishDegree > currentFishDegree)
            {
                continue;
            }

            roll -= fishConfigs[i].Weight;
            if (roll < 0)
            {
                selectedFish = fishConfigs[i];
                return true;
            }
        }

        return false;
    }

    private static void AddPointerEventListener(
        EventTrigger eventTrigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(callback);
        eventTrigger.triggers.Add(entry);
    }

    private void OnClickFish()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_fishingCoroutine != null || _fishButtonCooldownCoroutine != null)
        {
            return;
        }

        _activeFishRod = null;
        _activeFishBait = null;
        _activeFish = null;
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.EquippedFishHookItemId <= 0 ||
            playerInfoManager.GetItemCount(playerInfoManager.EquippedFishHookItemId) <= 0)
        {
            CommonTipView.Show("请先装备鱼钩");
            return;
        }

        if (playerInfoManager.EquippedFishBaitItemId <= 0 ||
            playerInfoManager.GetItemCount(playerInfoManager.EquippedFishBaitItemId) <= 0)
        {
            CommonTipView.Show("请先装备鱼饵");
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("数据表未初始化，无法开始捕鱼", this);
            return;
        }

        _activeFishRod = tables.FishRodTable.GetOrDefault(
            playerInfoManager.EquippedFishHookItemId);
        if (_activeFishRod == null)
        {
            Debug.LogError(
                $"缺少鱼钩能力配置: [{playerInfoManager.EquippedFishHookItemId}]",
                this);
            return;
        }

        if (_activeFishRod.TreasureProbability < 0f ||
            _activeFishRod.TreasureProbability > 1f ||
            _activeFishRod.TreasureProgressRate <= 0f ||
            _activeFishRod.FishDegree < 0)
        {
            Debug.LogError(
                $"鱼钩宝藏配置无效: [{_activeFishRod.Id}]",
                this);
            _activeFishRod = null;
            return;
        }

        _activeFishBait = tables.FishBaitTable.GetOrDefault(
            playerInfoManager.EquippedFishBaitItemId);
        if (!IsValidFishBaitConfig(_activeFishBait))
        {
            Debug.LogError(
                $"缺少或无效的鱼饵效果配置: [{playerInfoManager.EquippedFishBaitItemId}]",
                this);
            _activeFishRod = null;
            _activeFishBait = null;
            return;
        }

        if (_enableFishCatchTest && _testFishId <= 0)
        {
            Debug.LogError($"捕鱼测试鱼种 ID 无效: [{_testFishId}]", this);
            _activeFishRod = null;
            _activeFishBait = null;
            return;
        }

        float currentFishDegree = Mathf.Max(
            0f,
            BuffSystem.GetInstance().GetEffectValue(FishDegreeEffectType)) +
            _activeFishRod.FishDegree;
        if (!_enableFishCatchTest &&
            _activeFishBait.ExtraFishDegree > 0 &&
            TurnRandom.Chance(
                $"Fish.BaitDegree.{_fishingAttemptIndex + 1}",
                _activeFishBait.ExtraFishDegreeProbability))
        {
            currentFishDegree += _activeFishBait.ExtraFishDegree;
        }

        if (!TrySelectFish(
            tables.FishTable.DataList,
            _enableFishCatchTest ? _testFishId : 0,
            currentFishDegree,
            out _activeFish))
        {
            Debug.LogError(
                _enableFishCatchTest
                    ? $"捕鱼测试鱼种 ID 无效: [{_testFishId}]"
                    : "鱼类配置为空或无有效出现权重，无法开始捕鱼",
                this);
            _activeFishRod = null;
            _activeFishBait = null;
            return;
        }

        cfg.Item rewardItem = tables.ItemTable.GetOrDefault(_activeFish.RewardItemId);
        if (rewardItem == null || rewardItem.Category != FishItemCategory ||
            _activeFish.RewardItemCount <= 0)
        {
            Debug.LogError($"鱼类奖励配置无效: [{_activeFish.Id}]", this);
            _activeFish = null;
            _activeFishRod = null;
            _activeFishBait = null;
            return;
        }

        _fishingAttemptIndex++;
        _activeFishSizeCm = TurnRandom.Range(
            $"Fish.Size.{_activeFish.Id}.{_fishingAttemptIndex}",
            _activeFish.SizeMinCm,
            _activeFish.SizeMaxCm + 1);

        // TODO: 根据已装备鱼饵的类别和品质筛选可钓鱼种。
        _btnFish.interactable = false;
        _fishingCoroutine = StartCoroutine(WaitForFishBite());
    }

    private void OnClickCatch()
    {
        if (!_isCatchActive)
        {
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void OnClickBack()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
    }

    private void OnClickInventory()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(GlobalDefine.FishInventoryView);
    }

    private void OnClickFishStore()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        RefreshStore(PlayerInfoManager.GetInstance());
        SetStorePage(true);
        _goStore.SetActive(true);
    }

    private void OnClickFishStoreClose()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        _goStore.SetActive(false);
    }

    private void OnClickChangeBait()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        OpenChangePanel(FishBaitCategory);
    }

    private void OnClickChangeHook()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        OpenChangePanel(FishHookCategory);
    }

    private void OnClickCloseChangePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        _goChangePanel.SetActive(false);
    }

    private void OnClickHookPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        SetStorePage(true);
    }

    private void OnClickBaitPage()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        SetStorePage(false);
    }

    private void SetStorePage(bool showHooks)
    {
        _goStoreHooks.SetActive(showHooks);
        _goStoreBaits.SetActive(!showHooks);
        _btnPageHook.interactable = !showHooks;
        _btnPageBait.interactable = showHooks;
    }

    private void RefreshStore(PlayerInfoManager playerInfoManager)
    {
        _txtFishStoreCoin.text = playerInfoManager.SimulationCoins.ToString();
        RefreshEquippedFishingItems(playerInfoManager);

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("河边商店数据表尚未初始化", this);
            return;
        }

        IReadOnlyList<cfg.FishStore> storeConfigs = tables.FishStoreTable.DataList;
        if (storeConfigs.Count != _itemHooks.Length + _itemBaits.Length)
        {
            Debug.LogError("河边商店配置数量与商品栏数量不一致", this);
            return;
        }

        playerInfoManager.RefreshFishStoreOffers(storeConfigs);
        InitializeStoreItems(_itemHooks, 1, storeConfigs, playerInfoManager);
        InitializeStoreItems(_itemBaits, 6, storeConfigs, playerInfoManager);
    }

    private void OpenChangePanel(int itemCategory)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("数据表尚未初始化，无法打开更换装备面板", this);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int slotIndex = 0;
        IReadOnlyList<cfg.Item> itemConfigs = tables.ItemTable.DataList;
        for (int i = 0; i < itemConfigs.Count && slotIndex < fishChangeItems.Length; i++)
        {
            cfg.Item itemConfig = itemConfigs[i];
            int itemCount = playerInfoManager.GetItemCount(itemConfig.Id);
            if (itemConfig.Category != itemCategory || itemCount <= 0)
            {
                continue;
            }

            fishChangeItems[slotIndex].SetData(
                itemConfig,
                itemCount,
                itemCategory == FishBaitCategory,
                OnClickChangeItem);
            slotIndex++;
        }

        for (; slotIndex < fishChangeItems.Length; slotIndex++)
        {
            fishChangeItems[slotIndex].SetData(null, 0, false, null);
        }

        _goChangePanel.SetActive(true);
    }

    private void OnClickChangeItem(cfg.Item itemConfig)
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (!PlayerInfoManager.GetInstance().TryEquipFishingItem(itemConfig))
        {
            Debug.LogError($"装备钓鱼道具失败: [{itemConfig.Id}]", this);
            return;
        }

        _goChangePanel.SetActive(false);
    }

    private void RefreshEquippedFishingItems(PlayerInfoManager playerInfoManager)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            return;
        }

        _equipmentPresentationVersion++;
        int presentationVersion = _equipmentPresentationVersion;
        cfg.Item hookConfig = tables.ItemTable
            .GetOrDefault(playerInfoManager.EquippedFishHookItemId);
        if (hookConfig == null ||
            hookConfig.Category != FishHookCategory ||
            playerInfoManager.GetItemCount(hookConfig.Id) <= 0)
        {
            hookConfig = null;
            IReadOnlyList<cfg.Item> itemConfigs = tables.ItemTable.DataList;
            for (int i = 0; i < itemConfigs.Count; i++)
            {
                cfg.Item itemConfig = itemConfigs[i];
                if (itemConfig.Category != FishHookCategory ||
                    playerInfoManager.GetItemCount(itemConfig.Id) <= 0)
                {
                    continue;
                }

                if (playerInfoManager.TryEquipFishingItem(itemConfig))
                {
                    return;
                }

                Debug.LogError($"自动装备鱼钩失败: [{itemConfig.Id}]", this);
                break;
            }
        }

        bool hasEquippedHook = hookConfig != null &&
            hookConfig.Category == FishHookCategory &&
            playerInfoManager.GetItemCount(hookConfig.Id) > 0;
        _imgEquipedHookIcon.gameObject.SetActive(hasEquippedHook);
        if (!hasEquippedHook)
        {
            _txtEquipedHookName.text = "未装备鱼钩";
            _imgEquipedHookIcon.sprite = null;
        }
        else
        {
            _txtEquipedHookName.text = hookConfig.Name;
            _imgEquipedHookIcon.sprite = null;
            LoadEquippedIconAsync(_imgEquipedHookIcon, hookConfig, presentationVersion);
        }

        cfg.Item baitConfig = tables.ItemTable
            .GetOrDefault(playerInfoManager.EquippedFishBaitItemId);
        bool hasEquippedBait = baitConfig != null &&
            baitConfig.Category == FishBaitCategory &&
            playerInfoManager.GetItemCount(baitConfig.Id) > 0;
        _goEquipedBait.SetActive(hasEquippedBait);
        if (!hasEquippedBait)
        {
            return;
        }

        _txtEquipedBaitName.text = baitConfig.Name;
        _imgEquipedBaitIcon.sprite = null;
        LoadEquippedIconAsync(_imgEquipedBaitIcon, baitConfig, presentationVersion);
    }

    private async void LoadEquippedIconAsync(
        Image icon,
        cfg.Item itemConfig,
        int presentationVersion)
    {
        Sprite loadedIcon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            GetInstanceID().ToString());
        if (presentationVersion != _equipmentPresentationVersion)
        {
            return;
        }

        if (loadedIcon == null)
        {
            Debug.LogError($"已装备钓鱼道具图标加载失败: [{itemConfig.Id}]", this);
            return;
        }

        icon.sprite = loadedIcon;
    }

    private void InitializeStoreItems(
        FishStoreItem[] items,
        int firstStoreId,
        IReadOnlyList<cfg.FishStore> storeConfigs,
        PlayerInfoManager playerInfoManager)
    {
        for (int i = 0; i < items.Length; i++)
        {
            cfg.FishStore storeConfig = FindStoreConfig(storeConfigs, firstStoreId + i);
            cfg.Item itemConfig = storeConfig == null
                ? null
                : DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(storeConfig.ItemId);
            if (storeConfig == null || itemConfig == null)
            {
                Debug.LogError($"河边商店商品配置无效: [{firstStoreId + i}]", this);
            }

            items[i].SetData(
                storeConfig,
                itemConfig,
                storeConfig == null
                    ? 0
                    : playerInfoManager.GetFishStoreOfferRemainingCount(storeConfig.Id),
                TryPurchaseStoreItem);
        }
    }

    private void TryPurchaseStoreItem(cfg.FishStore storeConfig)
    {
        FishStorePurchaseResult purchaseResult = PlayerInfoManager.GetInstance()
            .TryPurchaseFishStoreOffer(storeConfig);
        switch (purchaseResult)
        {
            case FishStorePurchaseResult.Success:
                GameManager.Audio.Play(AudioDefine.SFXBuy);
                CommonTipView.Show($"购买{storeConfig.Name}成功");
                break;
            case FishStorePurchaseResult.SoldOut:
                CommonTipView.Show("购买次数已达上限");
                break;
            case FishStorePurchaseResult.InsufficientCoins:
                CommonTipView.Show("模拟币不足");
                break;
            default:
                Debug.LogError($"河边商店商品购买失败: [{storeConfig.Id}]", this);
                break;
        }
    }

    private static cfg.FishStore FindStoreConfig(
        IReadOnlyList<cfg.FishStore> storeConfigs,
        int storeId)
    {
        for (int i = 0; i < storeConfigs.Count; i++)
        {
            if (storeConfigs[i].Id == storeId)
            {
                return storeConfigs[i];
            }
        }

        return null;
    }

    private IEnumerator WaitForFishBite()
    {
        yield return PlayAnimationOnce(AnimationStart);
        _animatorFishRod.Play(AnimationIdle, 0, 0f);
        yield return new WaitForSeconds(TurnRandom.Range(
            $"Fish.BiteWait.{PlayerInfoManager.GetInstance().EquippedFishBaitItemId}",
            BiteWaitMinSeconds,
            BiteWaitMaxSeconds));
        yield return PlayAnimationOnce(AnimationEnd);

        int equippedBaitItemId = PlayerInfoManager.GetInstance().EquippedFishBaitItemId;
        if (!PlayerInfoManager.GetInstance().TryConsumeItem(equippedBaitItemId))
        {
            _fishingCoroutine = null;
            ResetFishingState();
            CommonTipView.Show("鱼饵不足");
            yield break;
        }

        StartCatchGame();
        _fishingCoroutine = null;
    }

    private IEnumerator PlayAnimationOnce(string animationName)
    {
        int animationHash = Animator.StringToHash(animationName);
        _animatorFishRod.Play(animationHash, 0, 0f);
        yield return null;

        while (_animatorFishRod.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = _animatorFishRod.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == animationHash &&
                stateInfo.normalizedTime >= 1f &&
                !_animatorFishRod.IsInTransition(0))
            {
                yield break;
            }

            yield return null;
        }
    }

    private void StartCatchGame()
    {
        _rectCatch.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            _activeFishRod.CatchAreaHeight + _activeFishBait.CatchAreaHeightBonus);
        _goFishCatch.SetActive(true);
        _imgFishProgress.fillAmount = InitialFishProgress;
        HideTreasure();
        _treasureSpawnTimer = TreasureSpawnDelaySeconds;
        _treasureSpawnPending = TurnRandom.Chance(
            $"Fish.Treasure.{_fishingAttemptIndex}",
            Mathf.Min(
                _activeFishRod.TreasureProbability +
                _activeFishBait.TreasureProbabilityBonus,
                1f));
        _treasureSpawned = false;
        CalculateMovementBounds();
        SetAnchoredPositionY(_rectFish, _fishMinY);
        SetAnchoredPositionY(_rectCatch, _catchMinY);
        _catchVelocity = 0f;
        _wasFishCaught = false;
        _wasTreasureCovered = false;
        _wasTreasureCaught = false;
        _isCatchActive = true;
        _btnClick.interactable = true;
        SelectNextFishTarget();
    }

    private void MoveFish()
    {
        if (_fishPauseTimer > 0f)
        {
            _fishPauseTimer = Mathf.Max(_fishPauseTimer - Time.deltaTime, 0f);
            return;
        }

        Vector2 position = _rectFish.anchoredPosition;
        position.y = Mathf.MoveTowards(
            position.y,
            _fishTargetY,
            _fishMoveSpeed * Time.deltaTime);
        _rectFish.anchoredPosition = position;

        if (Mathf.Approximately(position.y, _fishTargetY))
        {
            SelectNextFishTarget();
        }
    }

    private void MoveCatch()
    {
        Vector2 position = _rectCatch.anchoredPosition;
        if (_isCatchButtonHeld)
        {
            _catchVelocity = Mathf.Min(
                _catchVelocity + _activeFishRod.RiseAcceleration * Time.deltaTime,
                _activeFishRod.MaxRiseSpeed);
        }
        else
        {
            _catchVelocity = Mathf.Max(
                _catchVelocity - _activeFishRod.FallAcceleration * Time.deltaTime,
                -_activeFishRod.MaxFallSpeed);
        }

        position.y = Mathf.Clamp(
            position.y + _catchVelocity * Time.deltaTime,
            _catchMinY,
            _catchMaxY);
        if (position.y <= _catchMinY && _catchVelocity < 0f)
        {
            _catchVelocity = -_catchVelocity * CatchBoundaryBounceDamping;
        }
        else if (position.y >= _catchMaxY && _catchVelocity > 0f)
        {
            _catchVelocity = -_catchVelocity * CatchBoundaryBounceDamping;
        }

        _rectCatch.anchoredPosition = position;
    }

    private void UpdateFishProgress()
    {
        bool isFishCaught = _rectFish.anchoredPosition.y >=
            _rectCatch.anchoredPosition.y - _rectCatch.rect.height * _rectCatch.pivot.y &&
            _rectFish.anchoredPosition.y <=
            _rectCatch.anchoredPosition.y +
            _rectCatch.rect.height * (1f - _rectCatch.pivot.y);
        if (isFishCaught && !_wasFishCaught)
        {
            PlayFishCatchContactFeedback();
        }
        _wasFishCaught = isFishCaught;

        float targetFillAmount = isFishCaught ? 1f : 0f;
        float changeSpeed = isFishCaught
            ? _activeFishRod.ProgressIncreaseRate * BuffSystem.GetInstance()
                .GetEffectMultiplier("FishCatchProgressMultiplier")
            : _activeFishRod.ProgressDecreaseRate;
        _imgFishProgress.fillAmount = Mathf.MoveTowards(
            _imgFishProgress.fillAmount,
            targetFillAmount,
            changeSpeed * Time.deltaTime);

        if (_imgFishProgress.fillAmount >= 1f)
        {
            CompleteFishCatch();
            return;
        }

        if (_imgFishProgress.fillAmount <= 0f)
        {
            EndCatchGameWithCooldown();
        }
    }

    private void UpdateTreasureSpawn()
    {
        if (!_treasureSpawnPending || _treasureSpawned)
        {
            return;
        }

        _treasureSpawnTimer -= Time.deltaTime;
        if (_treasureSpawnTimer > 0f)
        {
            return;
        }

        GetVerticalBounds(_rectTreasure, out float minY, out float maxY);
        Vector2 position = _rectTreasure.anchoredPosition;
        position.y = TurnRandom.Range(
            $"Fish.TreasureY.{_fishingAttemptIndex}",
            minY,
            maxY);
        _rectTreasure.anchoredPosition = position;
        _imgTreasure.fillAmount = 0f;
        Color treasureColor = _initialTreasureColor;
        treasureColor.a = 0f;
        _imgTreasure.color = treasureColor;
        _rectTreasure.localScale = Vector3.zero;
        _rectTreasure.gameObject.SetActive(true);
        _imgTreasure.DOFade(_initialTreasureColor.a, 0.14f)
            .SetEase(Ease.OutQuad)
            .SetTarget(_imgTreasure);
        DOTween.Sequence()
            .Append(_rectTreasure.DOScale(Vector3.one * 1.15f, TreasureScaleUpDuration)
                .SetEase(Ease.OutBack))
            .Append(_rectTreasure.DOScale(Vector3.one, TreasureScaleSettleDuration)
                .SetEase(Ease.OutQuad))
            .SetTarget(_rectTreasure);
        _treasureSpawned = true;
        _treasureSpawnPending = false;
    }

    private void UpdateTreasureProgress()
    {
        if (!_treasureSpawned || !_rectTreasure.gameObject.activeSelf)
        {
            return;
        }

        bool isTreasureCovered = _rectTreasure.anchoredPosition.y >=
            _rectCatch.anchoredPosition.y - _rectCatch.rect.height * _rectCatch.pivot.y &&
            _rectTreasure.anchoredPosition.y <=
            _rectCatch.anchoredPosition.y +
            _rectCatch.rect.height * (1f - _rectCatch.pivot.y);
        if (isTreasureCovered && !_wasTreasureCovered)
        {
            DOTween.Kill(_imgTreasure.transform);
            _imgTreasure.transform.localScale = Vector3.one;
            _imgTreasure.transform
                .DOPunchScale(Vector3.one * 0.12f, 0.16f, 4, 0.6f)
                .SetTarget(_imgTreasure.transform);
        }
        _wasTreasureCovered = isTreasureCovered;

        _imgTreasure.fillAmount = Mathf.MoveTowards(
            _imgTreasure.fillAmount,
            isTreasureCovered ? 1f : 0f,
            (isTreasureCovered
                ? _activeFishRod.TreasureProgressRate
                : _activeFishRod.ProgressDecreaseRate) * Time.deltaTime);
        if (_imgTreasure.fillAmount < 1f)
        {
            return;
        }

        _treasureSpawned = false;
        _wasTreasureCaught = true;
        GameManager.Audio.Play(AudioDefine.SFXGetReward);
        CommonTipView.Show(
            $"钓起<color=#00FFFA>宝藏</color>，成功捕获鱼后将一并结算！");
        DOTween.Kill(_rectTreasure);
        DOTween.Kill(_imgTreasure);
        DOTween.Sequence()
            .Append(_rectTreasure.DOScale(_initialTreasureScale * 1.2f, 0.1f)
                .SetEase(Ease.OutBack))
            .Append(_rectTreasure.DOScale(Vector3.zero, TreasureCatchFeedbackDuration))
            .Join(_imgTreasure.DOFade(0f, TreasureCatchFeedbackDuration))
            .AppendCallback(() => _rectTreasure.gameObject.SetActive(false))
            .SetTarget(_rectTreasure);
    }

    private void PlayFishCatchContactFeedback()
    {
        DOTween.Kill(_rectFish);
        _rectFish.localScale = _initialFishScale;
        _rectFish.DOPunchScale(_initialFishScale * 0.08f, 0.18f, 5, 0.65f)
            .SetTarget(_rectFish);

        DOTween.Kill(_imgFishProgress.transform);
        _imgFishProgress.transform.localScale = _initialFishProgressScale;
        _imgFishProgress.transform
            .DOScale(_initialFishProgressScale * 1.1f, 0.09f)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo)
            .SetTarget(_imgFishProgress.transform);
    }

    private void HideTreasure()
    {
        DOTween.Kill(_rectTreasure);
        DOTween.Kill(_imgTreasure);
        DOTween.Kill(_imgTreasure.transform);
        _rectTreasure.localScale = _initialTreasureScale;
        _imgTreasure.transform.localScale = Vector3.one;
        _imgTreasure.fillAmount = 0f;
        _imgTreasure.color = _initialTreasureColor;
        _rectTreasure.gameObject.SetActive(false);
    }

    private void CompleteFishCatch()
    {
        cfg.Fish caughtFish = _activeFish;
        int fishSizeCm = _activeFishSizeCm;
        List<CommonRewardItemData> rewards = new List<CommonRewardItemData>
        {
            new CommonRewardItemData
            {
                itemId = caughtFish.RewardItemId,
                itemCount = caughtFish.RewardItemCount
            }
        };
        if (_wasTreasureCaught)
        {
            rewards.Add(new CommonRewardItemData
            {
                itemId = TreasureItemId,
                itemCount = 1
            });
        }

        _isCatchActive = false;
        _isCatchButtonHeld = false;
        _btnClick.interactable = false;
        DOTween.Kill(_imgFishProgress.transform);
        _imgFishProgress.transform.localScale = _initialFishProgressScale;
        _imgFishProgress.transform
            .DOScale(_initialFishProgressScale * 1.2f, 0.1f)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo)
            .SetTarget(_imgFishProgress.transform);
        GameManager.Audio.Play(AudioDefine.SFXGetReward);
        MissionAPI.Broadcast(new MissionMessage(
            MissionEventType.FishCatch,
            1,
            caughtFish.Id.ToString()));
        CommonTipView.Show(
            $"成功捕捉到<color=#00FFFA>{caughtFish.Name}</color>，尺寸 " +
            $"<color=#00FFFA>{fishSizeCm} cm</color> x{caughtFish.RewardItemCount}！");
        _fishCatchCelebrationCoroutine = StartCoroutine(
            EndCatchGameAfterCelebration(rewards));
    }

    private IEnumerator EndCatchGameAfterCelebration(
        List<CommonRewardItemData> rewards)
    {
        yield return new WaitForSeconds(FishCatchCelebrationDuration);
        _fishCatchCelebrationCoroutine = null;
        EndCatchGameWithCooldown();
        UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
        {
            data = rewards
        });
    }

    private void EndCatchGameWithCooldown()
    {
        ResetFishingState();
        _btnFish.interactable = false;
        _fishButtonCooldownCoroutine = StartCoroutine(EnableFishButtonAfterCooldown());
    }

    private IEnumerator EnableFishButtonAfterCooldown()
    {
        yield return new WaitForSeconds(FishButtonCooldownSeconds);
        _fishButtonCooldownCoroutine = null;
        _btnFish.interactable = true;
    }

    private void PlayCatchClickFeedback()
    {
        DOTween.Kill(_btnClick);
        _btnClick.transform.localScale = _initialClickButtonScale;

        Vector3 pressedScale = Vector3.Scale(
            _initialClickButtonScale,
            new Vector3(1.08f, 0.92f, 1f));
        Vector3 bouncedScale = Vector3.Scale(
            _initialClickButtonScale,
            new Vector3(0.95f, 1.1f, 1f));

        DOTween.Sequence()
            .Append(_btnClick.transform.DOScale(pressedScale, ClickPressDuration))
            .Append(
                _btnClick.transform.DOScale(bouncedScale, ClickBounceDuration)
                    .SetEase(Ease.OutBack))
            .Append(
                _btnClick.transform.DOScale(
                    _initialClickButtonScale,
                    ClickSettleDuration)
                    .SetEase(Ease.OutQuad))
            .SetTarget(_btnClick);
    }

    private void StopCatchClickFeedback()
    {
        if (_btnClick == null)
        {
            return;
        }

        DOTween.Kill(_btnClick);
        _btnClick.transform.localScale = _initialClickButtonScale;
    }

    private void SelectNextFishTarget()
    {
        float currentY = _rectFish.anchoredPosition.y;
        int movementIndex = _fishMovementRandomIndex++;
        float movementRange = _fishMaxY - _fishMinY;
        float targetMinY = _fishMinY +
            movementRange * _activeFish.MoveRangeMinRatio;
        float targetMaxY = _fishMinY +
            movementRange * _activeFish.MoveRangeMaxRatio;
        if (_activeFish.MovementType == FishMovementJumper)
        {
            _fishTargetY = movementIndex % 2 == 0 ? targetMaxY : targetMinY;
        }
        else
        {
            if (_activeFish.MovementType == FishMovementErratic)
            {
                int targetStep = TurnRandom.Range(
                    $"Fish.TargetStep.{_activeFish.Id}.{movementIndex}",
                    0,
                    3);
                _fishTargetY = Mathf.Lerp(
                    targetMinY,
                    targetMaxY,
                    targetStep * 0.5f);
            }
            else
            {
                _fishTargetY = TurnRandom.Range(
                    $"Fish.TargetY.{_activeFish.Id}.{movementIndex}",
                    targetMinY,
                    targetMaxY);
            }

            if (Mathf.Abs(_fishTargetY - currentY) < 1f)
            {
                _fishTargetY = currentY < (targetMinY + targetMaxY) * 0.5f
                    ? targetMaxY
                    : targetMinY;
            }
        }

        float duration = TurnRandom.Range(
            $"Fish.MoveDuration.{_activeFish.Id}.{movementIndex}",
            _activeFish.MoveDurationMinSeconds,
            _activeFish.MoveDurationMaxSeconds);
        _fishMoveSpeed = Mathf.Abs(_fishTargetY - currentY) / duration *
            _activeFish.MoveSpeedMultiplier *
            _activeFishBait.FishMoveSpeedMultiplier;
        if (TurnRandom.Chance(
            $"Fish.SuddenMove.{_activeFish.Id}.{movementIndex}",
            _activeFish.SuddenMoveProbability))
        {
            _fishMoveSpeed *= _activeFish.SuddenMoveSpeedMultiplier;
        }

        if (TurnRandom.Chance(
            $"Fish.Pause.{_activeFish.Id}.{movementIndex}",
            _activeFish.PauseProbability))
        {
            _fishPauseTimer = TurnRandom.Range(
                $"Fish.PauseDuration.{_activeFish.Id}.{movementIndex}",
                _activeFish.PauseDurationMinSeconds,
                _activeFish.PauseDurationMaxSeconds);
        }
    }

    private static bool IsValidFishMovementConfig(cfg.Fish fish)
    {
        if (fish.MoveRangeMinRatio < 0f ||
            fish.MoveRangeMaxRatio > 1f ||
            fish.MoveRangeMaxRatio <= fish.MoveRangeMinRatio ||
            fish.PauseProbability < 0f ||
            fish.PauseProbability > 1f ||
            fish.PauseDurationMinSeconds < 0f ||
            fish.PauseDurationMaxSeconds < fish.PauseDurationMinSeconds)
        {
            return false;
        }

        switch (fish.MovementType)
        {
            case FishMovementSteady:
            case FishMovementDart:
            case FishMovementSinker:
            case FishMovementFloater:
            case FishMovementJumper:
            case FishMovementErratic:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidFishBaitConfig(cfg.FishBait fishBait)
    {
        return fishBait != null &&
            fishBait.ExtraFishDegree >= 0 &&
            fishBait.ExtraFishDegreeProbability >= 0f &&
            fishBait.ExtraFishDegreeProbability <= 1f &&
            fishBait.TreasureProbabilityBonus >= 0f &&
            fishBait.TreasureProbabilityBonus <= 1f &&
            fishBait.CatchAreaHeightBonus >= 0f &&
            fishBait.FishMoveSpeedMultiplier > 0f &&
            fishBait.FishMoveSpeedMultiplier <= 1f;
    }

    private void CalculateMovementBounds()
    {
        GetVerticalBounds(_rectFish, out _fishMinY, out _fishMaxY);
        GetVerticalBounds(_rectCatch, out _catchMinY, out _catchMaxY);
    }

    private static void SetAnchoredPositionY(RectTransform rectTransform, float y)
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.y = y;
        rectTransform.anchoredPosition = position;
    }

    private void GetVerticalBounds(
        RectTransform movingRect,
        out float minY,
        out float maxY)
    {
        float backgroundBottom = -_rectFishBg.rect.height * _rectFishBg.pivot.y;
        float backgroundTop = _rectFishBg.rect.height * (1f - _rectFishBg.pivot.y);
        minY = backgroundBottom + movingRect.rect.height * movingRect.pivot.y;
        maxY = backgroundTop - movingRect.rect.height * (1f - movingRect.pivot.y);
    }

    private void ResetFishingState()
    {
        StopFishing();
        _goFishCatch.SetActive(false);
        _imgFishProgress.fillAmount = 0f;
        HideTreasure();
        _treasureSpawnPending = false;
        _treasureSpawned = false;
        _wasTreasureCaught = false;
        _treasureSpawnTimer = 0f;
        _rectFish.anchoredPosition = _initialFishPosition;
        DOTween.Kill(_rectFish);
        DOTween.Kill(_imgFishProgress.transform);
        _rectFish.localScale = _initialFishScale;
        _imgFishProgress.transform.localScale = _initialFishProgressScale;
        _rectCatch.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            _initialCatchHeight);
        _rectCatch.anchoredPosition = _initialCatchPosition;
        _catchVelocity = 0f;
        _fishPauseTimer = 0f;
        _activeFishRod = null;
        _activeFishBait = null;
        _activeFish = null;
        _activeFishSizeCm = 0;
        if (_imgFish != null)
        {
            _imgFish.sprite = _initialFishSprite;
        }
        _isCatchButtonHeld = false;
        _btnFish.interactable = true;
        _btnClick.interactable = false;
        _animatorFishRod.Play(AnimationEmpty, 0, 0f);
    }

    private void StopFishing()
    {
        if (_fishCatchCelebrationCoroutine != null)
        {
            StopCoroutine(_fishCatchCelebrationCoroutine);
            _fishCatchCelebrationCoroutine = null;
        }

        if (_fishingCoroutine != null)
        {
            StopCoroutine(_fishingCoroutine);
            _fishingCoroutine = null;
        }

        if (_fishButtonCooldownCoroutine != null)
        {
            StopCoroutine(_fishButtonCooldownCoroutine);
            _fishButtonCooldownCoroutine = null;
        }

        _isCatchActive = false;
        StopCatchClickFeedback();
        if (_animatorFishRod != null)
        {
            _animatorFishRod.Play(AnimationEmpty, 0, 0f);
        }
    }

    private bool HasValidUiReferences()
    {
        if (_imgFishProgress != null &&
        _imgTreasure != null &&
            _imgFish != null &&
            _txtFishStoreCoin != null &&
            _txtEquipedHookName != null &&
            _txtEquipedBaitName != null &&
            _imgEquipedHookIcon != null &&
            _imgEquipedBaitIcon != null &&
            _btnFish != null &&
            _btnChangeBait != null &&
            _btnChangeHook != null &&
            _btnCloseChangePanel != null &&
            _btnClick != null &&
            _btnBack != null &&
            _btnFishStore != null &&
            _btnFishStoreClose != null &&
            _btnPageHook != null &&
            _btnPageBait != null &&
            _btnInventory != null &&
            _animatorFishRod != null &&
            _goFishCatch != null &&
            _goEquipedBait != null &&
            _goChangePanel != null &&
            _goStore != null &&
            _goStoreHooks != null &&
            _goStoreBaits != null &&
            _rectFishBg != null &&
            _rectFish != null &&
            _rectCatch != null &&
            _rectTreasure != null &&
            HasValidStoreItems(_itemHooks, 5) &&
            HasValidStoreItems(_itemBaits, 6) &&
            HasValidChangeItems(fishChangeItems, 6))
        {
            return true;
        }

        Debug.LogError("FishView 存在未绑定的 UI 引用", this);
        return false;
    }

    private static bool HasValidStoreItems(FishStoreItem[] items, int expectedCount)
    {
        if (items == null || items.Length != expectedCount)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasValidChangeItems(FishChangeItem[] items, int expectedCount)
    {
        if (items == null || items.Length != expectedCount)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.FishView;
    }
}
