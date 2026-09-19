using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FishView : UIBasePanel
{
    [Header("钓鱼与捕鱼游戏参数")]
    private const float BiteWaitMinSeconds = 1f;             // 钓鱼: 最短等待时间
    private const float BiteWaitMaxSeconds = 2f;            // 钓鱼: 最长等待时间
    private const float FishMoveDurationMinSeconds = 0.25f;  // 捕鱼: 鱼移动最短时间
    private const float FishMoveDurationMaxSeconds = 1.2f;   // 捕鱼: 鱼移动最长时间
    private const float CatchRiseDistance = 110f;            // 捕鱼: 捕捉上升距离
    private const float CatchRiseSpeed = 1100f;               // 捕鱼: 捕捉上升速度
    private const float CatchFallSpeed = 220f;                // 捕鱼: 捕捉下降速度
    private const float ClickPressDuration = 0.06f;           // 捕鱼游戏: 点击按钮压缩时长
    private const float ClickBounceDuration = 0.14f;          // 捕鱼游戏: 点击按钮回弹时长
    private const float ClickSettleDuration = 0.1f;           // 捕鱼游戏: 点击按钮复位时长
    private const float FishButtonCooldownSeconds = 1f;       // 捕鱼游戏结束后再次钓鱼的等待时间
    private const float InitialFishProgress = 0.5f;           // 捕鱼游戏: 初始进度
    private const float ProgressIncreasePerSecond = 0.25f;   // 捕鱼游戏: 进度条增加速度
    private const float ProgressDecreasePerSecond = 0.12f;   // 捕鱼游戏: 进度条减少速度
    private const int FishHookCategory = 7;
    private const int FishBaitCategory = 8;

    [Header("UI")]
    [SerializeField] private Text _txtFishStoreCoin;    // 河边商店: 模拟币数量
    [SerializeField] private Text _txtEquipedHookName;  // 钓鱼: 已装备的鱼钩名称
    [SerializeField] private Text _txtEquipedBaitName;  // 钓鱼: 已装备的鱼饵名称
    [SerializeField] private Image _imgFishProgress;    // 捕鱼游戏: 鱼进度条
    [SerializeField] private Image _imgEquipedHookIcon;  // 钓鱼: 已装备的鱼钩图标
    [SerializeField] private Image _imgEquipedBaitIcon;  // 钓鱼: 已装备的鱼饵图标
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
    [SerializeField] private FishStoreItem[] _itemHooks;  // 河边商店: 鱼钩页签的鱼钩道具, 共 5 个
    [SerializeField] private FishStoreItem[] _itemBaits;  // 河边商店: 鱼饵页签的鱼饵道具, 共 6 个
    [SerializeField] private FishChangeItem[] fishChangeItems; // 钓鱼: 更换装备面板的道具, 共 6 个

    private const string AnimationEmpty = "Empty";   // 空闲动画
    private const string AnimationStart = "Start";   // 开始钓鱼动画
    private const string AnimationIdle = "Idle";     // 钓鱼中动画
    private const string AnimationEnd = "End";       // 上钩动画

    private Coroutine _fishingCoroutine;
    private Coroutine _fishButtonCooldownCoroutine;
    private bool _isCatchActive;
    private float _fishTargetY;
    private float _fishMoveSpeed;
    private float _fishMinY;
    private float _fishMaxY;
    private float _catchMinY;
    private float _catchMaxY;
    private float _catchTargetY;
    private Vector2 _initialFishPosition;
    private Vector2 _initialCatchPosition;
    private Vector3 _initialClickButtonScale;
    private int _equipmentPresentationVersion;

    private void Awake()
    {
        if (!HasValidUiReferences())
        {
            enabled = false;
            return;
        }

        _initialFishPosition = _rectFish.anchoredPosition;
        _initialCatchPosition = _rectCatch.anchoredPosition;
        _initialClickButtonScale = _btnClick.transform.localScale;
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
        UpdateFishProgress();
    }

    private void OnClickFish()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_fishingCoroutine != null || _fishButtonCooldownCoroutine != null)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.EquippedFishBaitItemId <= 0 ||
            playerInfoManager.GetItemCount(playerInfoManager.EquippedFishBaitItemId) <= 0)
        {
            CommonTipView.Show("请先装备鱼饵");
            return;
        }

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
        _catchTargetY = Mathf.Min(
            Mathf.Max(_catchTargetY, _rectCatch.anchoredPosition.y) + CatchRiseDistance,
            _catchMaxY);
        PlayCatchClickFeedback();
    }

    private void OnClickBack()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
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
        if (hookConfig == null || hookConfig.Category != FishHookCategory)
        {
            Debug.LogError("当前装备的鱼钩配置无效", this);
            return;
        }

        _txtEquipedHookName.text = hookConfig.Name;
        _imgEquipedHookIcon.sprite = null;
        LoadEquippedIconAsync(_imgEquipedHookIcon, hookConfig, presentationVersion);

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
        yield return new WaitForSeconds(Random.Range(BiteWaitMinSeconds, BiteWaitMaxSeconds));
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
        _goFishCatch.SetActive(true);
        _imgFishProgress.fillAmount = InitialFishProgress;
        CalculateMovementBounds();
        _isCatchActive = true;
        _btnClick.interactable = true;
        SelectNextFishTarget();
    }

    private void MoveFish()
    {
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
        if (position.y < _catchTargetY)
        {
            position.y = Mathf.MoveTowards(
                position.y,
                _catchTargetY,
                CatchRiseSpeed * Time.deltaTime);
            _rectCatch.anchoredPosition = position;
            return;
        }

        position.y = Mathf.Max(position.y - CatchFallSpeed * Time.deltaTime, _catchMinY);
        _rectCatch.anchoredPosition = position;
        _catchTargetY = position.y;
    }

    private void UpdateFishProgress()
    {
        bool isFishCaught = _rectFish.anchoredPosition.y >=
            _rectCatch.anchoredPosition.y - _rectCatch.rect.height * _rectCatch.pivot.y &&
            _rectFish.anchoredPosition.y <=
            _rectCatch.anchoredPosition.y +
            _rectCatch.rect.height * (1f - _rectCatch.pivot.y);
        float targetFillAmount = isFishCaught ? 1f : 0f;
        float changeSpeed = isFishCaught
            ? ProgressIncreasePerSecond
            : ProgressDecreasePerSecond;
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

    private void CompleteFishCatch()
    {
        EndCatchGameWithCooldown();
        CommonTipView.Show("成功捕捉到鱼！");
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
        _fishTargetY = Random.Range(_fishMinY, _fishMaxY);
        if (Mathf.Abs(_fishTargetY - currentY) < 1f)
        {
            _fishTargetY = currentY < (_fishMinY + _fishMaxY) * 0.5f
                ? _fishMaxY
                : _fishMinY;
        }

        float duration = Random.Range(
            FishMoveDurationMinSeconds,
            FishMoveDurationMaxSeconds);
        _fishMoveSpeed = Mathf.Abs(_fishTargetY - currentY) / duration;
    }

    private void CalculateMovementBounds()
    {
        GetVerticalBounds(_rectFish, out _fishMinY, out _fishMaxY);
        GetVerticalBounds(_rectCatch, out _catchMinY, out _catchMaxY);
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
        _rectFish.anchoredPosition = _initialFishPosition;
        _rectCatch.anchoredPosition = _initialCatchPosition;
        _catchTargetY = _initialCatchPosition.y;
        _btnFish.interactable = true;
        _btnClick.interactable = false;
        _animatorFishRod.Play(AnimationEmpty, 0, 0f);
    }

    private void StopFishing()
    {
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
