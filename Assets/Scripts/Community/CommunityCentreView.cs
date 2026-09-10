using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CommunityCentreView : UIBasePanel
{
    private const int MonthlyNeedCount = 3;
    private const int CommunityNeedItemCategory = 5;
    private const int RedundancyItemCategory = 4;
    private const int CommunitySupplyGiftBoxItemId = 3001;
    private const int CommunityCentreScaleId = 8;
    private const int RedundancyItemsPerPage = 4;
    private const float ExperienceAnimationDuration = 0.45f;

    [SerializeField] private Text _txtLevel;    // 社区等级, 例: LV.3
    [SerializeField] private Text _txtExp;      // 社区经验
    [SerializeField] private Slider _sliderExp; // 社区经验进度条

    [Header("提交冗余道具")]
    [SerializeField] private Text _txtSelectName;       // 提交道具模块: 选择物品名称
    [SerializeField] private Text _txtSelectNum;        // 提交道具模块: 选择物品数量, 例: 数量 10
    [SerializeField] private Image _imgSelectIcon;      // 提交道具模块: 选择物品图标
    [SerializeField] private Button _btnSelect;         // 提交道具模块: 选择按钮
    [SerializeField] private Button _btnSubmit;         // 提交道具模块: 提交按钮
    [SerializeField] private Button _btnLeftPage;       // 选择提交道具: 前一页
    [SerializeField] private Button _btnRightPage;      // 选择提交道具: 后一页
    [SerializeField] private Button _btnBackSubmit;     // 选择提交道具: 返回提交界面
    [SerializeField] private GameObject _goNeedItemSelected;    // 选择道具后显示
    [SerializeField] private GameObject _goNeedItemUnSelected;  // 未选择道具时显示
    [SerializeField] private GameObject _goSubmit;      // 提交界面
    [SerializeField] private GameObject _goSelect;      // 选择冗余道具界面


    [Header("社区提案")]
    [SerializeField] private Text _txtLevelUpNum;          // 社区升级模块: 剩余选择次数
    [SerializeField] private GameObject _goHasLevelUpNum;  // 有剩余次数时显示
    [SerializeField] private GameObject _goNoneLevelUpNum; // 无剩余次数时显示

    [Space(10)]
    [SerializeField] private CommunityCentreNeedItem[] _needItems;       // 社区需求物品列表, 共 3 个, 每月刷新
    [SerializeField] private CommunityCentreLevelUpItem[] _levelUpItems; // 社区升级提案列表, 共 3 个
    [SerializeField] private CommunityRedundancyItem[] _redundancyItems; // 冗余道具列表, 共 4 个

    private bool _isSubmittingNeed;
    private bool _isSubmittingRedundancyItem;
    private bool _isSelectingProposal;
    private readonly List<cfg.Item> _redundancyItemConfigs = new List<cfg.Item>();
    private cfg.Item _selectedRedundancyItem;
    private int _redundancyItemPage;
    private int _selectedItemPresentationVersion;

    private void Awake()
    {
        _btnSelect.onClick.AddListener(OpenRedundancySelection);
        _btnSubmit.onClick.AddListener(SubmitSelectedRedundancyItem);
        _btnLeftPage.onClick.AddListener(ShowPreviousRedundancyPage);
        _btnRightPage.onClick.AddListener(ShowNextRedundancyPage);
        _btnBackSubmit.onClick.AddListener(ShowSubmitPage);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        RefreshCommunityProgress(PlayerInfoManager.GetInstance());
        RefreshMonthlyNeeds();
        RefreshProposals();
        ClearSelectedRedundancyItem();
        ShowSubmitPage();
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.PlayerInfoChanged += OnPlayerInfoChanged;
        playerInfoManager.TurnAdvanced -= RefreshMonthlyNeeds;
        playerInfoManager.TurnAdvanced += RefreshMonthlyNeeds;
        RefreshCommunityProgress(playerInfoManager);
        RefreshMonthlyNeeds();
        RefreshProposals();
        RefreshSelectedRedundancyItem();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.TurnAdvanced -= RefreshMonthlyNeeds;
        DOTween.Kill(_sliderExp);
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= OnPlayerInfoChanged;
        playerInfoManager.TurnAdvanced -= RefreshMonthlyNeeds;
        DOTween.Kill(_sliderExp);
        if (_btnSelect != null)
        {
            _btnSelect.onClick.RemoveListener(OpenRedundancySelection);
        }

        if (_btnSubmit != null)
        {
            _btnSubmit.onClick.RemoveListener(SubmitSelectedRedundancyItem);
        }

        if (_btnLeftPage != null)
        {
            _btnLeftPage.onClick.RemoveListener(ShowPreviousRedundancyPage);
        }

        if (_btnRightPage != null)
        {
            _btnRightPage.onClick.RemoveListener(ShowNextRedundancyPage);
        }

        if (_btnBackSubmit != null)
        {
            _btnBackSubmit.onClick.RemoveListener(ShowSubmitPage);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityCentreView;
    }

    private void RefreshCommunityProgress(PlayerInfoManager playerInfoManager)
    {
        DOTween.Kill(_sliderExp);
        RefreshCommunityProgressText(playerInfoManager);
        _sliderExp.value = GetCommunityExperienceProgress(playerInfoManager);
    }

    private void RefreshMonthlyNeeds()
    {
        if (_needItems == null || _needItems.Length != MonthlyNeedCount)
        {
            Debug.LogError($"社区中心必须配置 {MonthlyNeedCount} 个需求栏。", this);
            return;
        }

        if (!TryLoadMonthlyNeeds(out List<cfg.Item> needs))
        {
            return;
        }

        cfg.Scale scaleConfig = DataTableMananger.GetInstance().Tables.ScaleTable
            .GetOrDefault(CommunityCentreScaleId);
        if (scaleConfig == null)
        {
            Debug.LogError($"社区中心道具缩放配置不存在: [{CommunityCentreScaleId}]", this);
            return;
        }

        float communityCentreScale = scaleConfig.ScaleValue / 10000f;
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < _needItems.Length; i++)
        {
            if (!playerInfoManager.TryGetMonthlyCommunityCentreNeedAt(
                    i,
                    out int itemId,
                    out bool submitted) ||
                itemId != needs[i].Id)
            {
                Debug.LogError($"社区需求存档无效: [{i}]", this);
                return;
            }

            int needIndex = i;
            _needItems[i].SetData(
                needs[i],
                submitted,
                playerInfoManager.GetItemCount(itemId),
                communityCentreScale,
                () => TrySubmitNeed(needIndex));
        }
    }

    private bool TryLoadMonthlyNeeds(out List<cfg.Item> needs)
    {
        needs = new List<cfg.Item>(MonthlyNeedCount);
        IReadOnlyList<cfg.Item> configurations = DataTableMananger.GetInstance()
            .Tables.ItemTable.DataList;
        List<cfg.Item> candidates = new List<cfg.Item>();
        for (int i = 0; i < configurations.Count; i++)
        {
            if (configurations[i].Category == CommunityNeedItemCategory)
            {
                candidates.Add(configurations[i]);
            }
        }

        if (candidates.Count < MonthlyNeedCount)
        {
            Debug.LogError(
                $"第 {CommunityNeedItemCategory} 类道具少于 {MonthlyNeedCount} 个，无法生成社区需求。",
                this);
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.HasMonthlyCommunityCentreNeeds(MonthlyNeedCount))
        {
            for (int i = 0; i < MonthlyNeedCount; i++)
            {
                if (!playerInfoManager.TryGetMonthlyCommunityCentreNeedAt(
                        i,
                        out int itemId,
                        out _) ||
                    !TryFindCandidate(candidates, itemId, out cfg.Item itemConfig))
                {
                    needs.Clear();
                    break;
                }

                needs.Add(itemConfig);
            }
        }

        if (needs.Count != MonthlyNeedCount)
        {
            needs = SelectMonthlyNeeds(candidates);
            List<int> itemIds = new List<int>(MonthlyNeedCount);
            for (int i = 0; i < needs.Count; i++)
            {
                itemIds.Add(needs[i].Id);
            }

            playerInfoManager.SetMonthlyCommunityCentreNeeds(itemIds);
        }

        return true;
    }

    private void TrySubmitNeed(int needIndex)
    {
        if (_isSubmittingNeed)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int previousLevel = playerInfoManager.CommunityCentreLevel;
        float previousProgress = GetCommunityExperienceProgress(playerInfoManager);

        _isSubmittingNeed = true;
        CommunityCentreNeedSubmitResult result =
            playerInfoManager.TrySubmitCommunityCentreNeed(
                needIndex,
                out int experienceGained,
                out int bonusExperienceGained);
        _isSubmittingNeed = false;

        switch (result)
        {
            case CommunityCentreNeedSubmitResult.Success:
                RefreshMonthlyNeeds();
                RefreshProposals();
                PlayCommunityExperienceAnimation(
                    playerInfoManager,
                    previousLevel,
                    previousProgress);
                CommonTipView.Show(bonusExperienceGained > 0
                    ? $"提交成功，社区经验 +{experienceGained}（额外经验 +{bonusExperienceGained}）"
                    : $"提交成功，社区经验 +{experienceGained}");
                GameManager.Audio.Play(AudioDefine.SFXBuy);
                break;
            case CommunityCentreNeedSubmitResult.InsufficientItem:
                CommonTipView.Show("所需道具不足");
                break;
            case CommunityCentreNeedSubmitResult.AlreadySubmitted:
                CommonTipView.Show("该社区需求已提交");
                break;
            default:
                Debug.LogError($"社区需求提交失败: [{needIndex}]", this);
                break;
        }
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        if (_isSubmittingNeed || _isSubmittingRedundancyItem || _isSelectingProposal)
        {
            return;
        }

        RefreshCommunityProgress(playerInfoManager);
        RefreshMonthlyNeeds();
        RefreshProposals();
        RefreshSelectedRedundancyItem();
    }

    private void RefreshCommunityProgressText(PlayerInfoManager playerInfoManager)
    {
        int requiredExperience = playerInfoManager.GetCommunityCentreExperienceRequired();
        bool isMaxLevel = requiredExperience == 0;

        _txtLevel.text = $"LV.{playerInfoManager.CommunityCentreLevel}";
        _txtExp.text = isMaxLevel
            ? "MAX"
            : $"{playerInfoManager.CommunityCentreExperience}/{requiredExperience}";
        _sliderExp.value = isMaxLevel
            ? 1f
            : (float)playerInfoManager.CommunityCentreExperience / requiredExperience;

        int proposalChoiceCount = playerInfoManager.CommunityCentreProposalChoiceCount;
        _txtLevelUpNum.text = proposalChoiceCount.ToString();
        _goHasLevelUpNum.SetActive(proposalChoiceCount > 0);
        _goNoneLevelUpNum.SetActive(proposalChoiceCount == 0);
    }

    private void RefreshProposals()
    {
        if (_levelUpItems == null || _levelUpItems.Length == 0)
        {
            Debug.LogError("社区中心未配置提案栏。", this);
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.CommunityCentreProposalChoiceCount <= 0)
        {
            SetProposalItems(null);
            return;
        }

        List<cfg.CommunityCentre> eligibleProposals = GetEligibleProposals();
        int offerCount = Mathf.Min(_levelUpItems.Length, eligibleProposals.Count);
        if (offerCount == 0)
        {
            SetProposalItems(null);
            Debug.LogError("没有可选择的社区提案配置。", this);
            return;
        }

        List<cfg.CommunityCentre> proposals;
        List<int> savedOfferIds = playerInfoManager.GetCommunityCentreProposalOfferIds();
        if (!TryGetSavedProposals(savedOfferIds, eligibleProposals, offerCount, out proposals))
        {
            proposals = SelectRandomProposals(eligibleProposals, offerCount);
            List<int> proposalIds = new List<int>(proposals.Count);
            for (int i = 0; i < proposals.Count; i++)
            {
                proposalIds.Add(proposals[i].Id);
            }

            playerInfoManager.SetCommunityCentreProposalOfferIds(proposalIds);
        }

        SetProposalItems(proposals);
    }

    private void SetProposalItems(List<cfg.CommunityCentre> proposals)
    {
        for (int i = 0; i < _levelUpItems.Length; i++)
        {
            cfg.CommunityCentre proposal = proposals != null && i < proposals.Count
                ? proposals[i]
                : null;
            cfg.BuffConfig buff = proposal == null
                ? null
                : DataTableMananger.GetInstance().Tables.BuffConfigTable.GetOrDefault(proposal.BuffId);
            _levelUpItems[i].SetData(proposal, buff, SelectProposal);
        }
    }

    private static List<cfg.CommunityCentre> GetEligibleProposals()
    {
        IReadOnlyList<cfg.CommunityCentre> configurations = DataTableMananger.GetInstance()
            .Tables.CommunityCentreTable.DataList;
        List<ActiveBuffData> activeBuffs = PlayerInfoManager.GetInstance().GetActiveBuffs();
        HashSet<int> activeBuffIds = new HashSet<int>();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            activeBuffIds.Add(activeBuffs[i].buffId);
        }

        Dictionary<string, cfg.CommunityCentre> nextProposalByCategory =
            new Dictionary<string, cfg.CommunityCentre>();
        for (int i = 0; i < configurations.Count; i++)
        {
            cfg.CommunityCentre proposal = configurations[i];
            cfg.BuffConfig buff = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(proposal.BuffId);
            if (proposal.Tier <= 0 || string.IsNullOrEmpty(proposal.Category) ||
                buff == null || buff.DurationType != BuffDurationType.Permanent.ToString() ||
                activeBuffIds.Contains(proposal.BuffId))
            {
                continue;
            }

            if (!nextProposalByCategory.TryGetValue(proposal.Category, out cfg.CommunityCentre next) ||
                proposal.Tier < next.Tier)
            {
                nextProposalByCategory[proposal.Category] = proposal;
            }
        }

        return new List<cfg.CommunityCentre>(nextProposalByCategory.Values);
    }

    private static bool TryGetSavedProposals(
        List<int> savedOfferIds,
        List<cfg.CommunityCentre> eligibleProposals,
        int expectedCount,
        out List<cfg.CommunityCentre> proposals)
    {
        proposals = new List<cfg.CommunityCentre>(expectedCount);
        if (savedOfferIds.Count != expectedCount)
        {
            return false;
        }

        HashSet<int> proposalIds = new HashSet<int>();
        for (int i = 0; i < savedOfferIds.Count; i++)
        {
            cfg.CommunityCentre proposal = FindProposalById(eligibleProposals, savedOfferIds[i]);
            if (proposal == null || !proposalIds.Add(proposal.Id))
            {
                proposals.Clear();
                return false;
            }

            proposals.Add(proposal);
        }

        return true;
    }

    private static cfg.CommunityCentre FindProposalById(
        List<cfg.CommunityCentre> proposals,
        int proposalId)
    {
        for (int i = 0; i < proposals.Count; i++)
        {
            if (proposals[i].Id == proposalId)
            {
                return proposals[i];
            }
        }

        return null;
    }

    private static List<cfg.CommunityCentre> SelectRandomProposals(
        List<cfg.CommunityCentre> candidates,
        int count)
    {
        List<cfg.CommunityCentre> pool = new List<cfg.CommunityCentre>(candidates);
        List<cfg.CommunityCentre> proposals = new List<cfg.CommunityCentre>(count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(i, pool.Count);
            cfg.CommunityCentre selected = pool[randomIndex];
            pool[randomIndex] = pool[i];
            pool[i] = selected;
            proposals.Add(selected);
        }

        return proposals;
    }

    private void SelectProposal(cfg.CommunityCentre proposal)
    {
        if (_isSelectingProposal ||
            PlayerInfoManager.GetInstance().CommunityCentreProposalChoiceCount <= 0 ||
            proposal == null ||
            !PlayerInfoManager.GetInstance().GetCommunityCentreProposalOfferIds().Contains(proposal.Id))
        {
            return;
        }

        cfg.BuffConfig buff = DataTableMananger.GetInstance().Tables.BuffConfigTable
            .GetOrDefault(proposal.BuffId);
        if (buff == null || buff.DurationType != BuffDurationType.Permanent.ToString())
        {
            Debug.LogError($"社区提案 BUFF 配置无效: [{proposal.Id}], [{proposal.BuffId}]", this);
            return;
        }

        _isSelectingProposal = true;
        bool isBuffAdded = BuffSystem.GetInstance().TryAddBuff(
            proposal.BuffId,
            proposal.Id,
            GetCategoryBuffIds(proposal.Category));
        bool isChoiceConsumed = isBuffAdded &&
            PlayerInfoManager.GetInstance().TryConsumeCommunityCentreProposalChoice();
        _isSelectingProposal = false;

        if (!isChoiceConsumed)
        {
            Debug.LogError($"社区提案选择失败: [{proposal.Id}]", this);
            return;
        }

        RefreshCommunityProgress(PlayerInfoManager.GetInstance());
        RefreshProposals();
        GameManager.Audio.Play(AudioDefine.SFXBuy);
    }

    private static HashSet<int> GetCategoryBuffIds(string category)
    {
        IReadOnlyList<cfg.CommunityCentre> configurations = DataTableMananger.GetInstance()
            .Tables.CommunityCentreTable.DataList;
        HashSet<int> buffIds = new HashSet<int>();
        for (int i = 0; i < configurations.Count; i++)
        {
            if (configurations[i].Category == category)
            {
                buffIds.Add(configurations[i].BuffId);
            }
        }

        return buffIds;
    }

    private void PlayCommunityExperienceAnimation(
        PlayerInfoManager playerInfoManager,
        int previousLevel,
        float previousProgress)
    {
        DOTween.Kill(_sliderExp);
        RefreshCommunityProgressText(playerInfoManager);
        _sliderExp.value = previousProgress;

        float currentProgress = GetCommunityExperienceProgress(playerInfoManager);
        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(_sliderExp);
        if (previousLevel < playerInfoManager.CommunityCentreLevel)
        {
            sequence.Append(_sliderExp.DOValue(1f, ExperienceAnimationDuration * 0.45f));
            if (playerInfoManager.GetCommunityCentreExperienceRequired() > 0)
            {
                sequence.AppendCallback(() => _sliderExp.value = 0f);
                sequence.Append(_sliderExp.DOValue(currentProgress, ExperienceAnimationDuration)
                    .SetEase(Ease.OutQuad));
            }

            return;
        }

        sequence.Append(_sliderExp.DOValue(currentProgress, ExperienceAnimationDuration)
            .SetEase(Ease.OutQuad));
    }

    private static List<cfg.Item> SelectMonthlyNeeds(List<cfg.Item> candidates)
    {
        List<cfg.Item> pool = new List<cfg.Item>(candidates);
        List<cfg.Item> needs = new List<cfg.Item>(MonthlyNeedCount);
        for (int i = 0; i < MonthlyNeedCount; i++)
        {
            int randomIndex = Random.Range(i, pool.Count);
            cfg.Item selected = pool[randomIndex];
            pool[randomIndex] = pool[i];
            pool[i] = selected;
            needs.Add(selected);
        }

        return needs;
    }

    private static bool TryFindCandidate(
        List<cfg.Item> candidates,
        int itemId,
        out cfg.Item itemConfig)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Id == itemId)
            {
                itemConfig = candidates[i];
                return true;
            }
        }

        itemConfig = null;
        return false;
    }

    private static float GetCommunityExperienceProgress(PlayerInfoManager playerInfoManager)
    {
        int requiredExperience = playerInfoManager.GetCommunityCentreExperienceRequired();
        return requiredExperience == 0
            ? 1f
            : (float)playerInfoManager.CommunityCentreExperience / requiredExperience;
    }

    private void OpenRedundancySelection()
    {
        _redundancyItemPage = 0;
        _goSubmit.SetActive(false);
        _goSelect.SetActive(true);
        RefreshRedundancyItems();
        if (_redundancyItemConfigs.Count == 0)
        {
            _goSubmit.SetActive(true);
            _goSelect.SetActive(false);
            CommonTipView.Show("背包中没有符合条件的物品");
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void ShowSubmitPage()
    {
        _goSubmit.SetActive(true);
        _goSelect.SetActive(false);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void ShowPreviousRedundancyPage()
    {
        if (_redundancyItemPage <= 0)
        {
            return;
        }

        _redundancyItemPage--;
        RefreshRedundancyItems();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void ShowNextRedundancyPage()
    {
        if (_redundancyItemPage >= GetMaxRedundancyItemPage() - 1)
        {
            return;
        }

        _redundancyItemPage++;
        RefreshRedundancyItems();
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void RefreshRedundancyItems()
    {
        if (_redundancyItems == null || _redundancyItems.Length != RedundancyItemsPerPage)
        {
            Debug.LogError(
                $"社区中心必须配置 {RedundancyItemsPerPage} 个冗余道具栏。",
                this);
            return;
        }

        cfg.Scale scaleConfig = DataTableMananger.GetInstance().Tables.ScaleTable
            .GetOrDefault(CommunityCentreScaleId);
        if (scaleConfig == null)
        {
            Debug.LogError($"社区中心道具缩放配置不存在: [{CommunityCentreScaleId}]", this);
            return;
        }

        _redundancyItemConfigs.Clear();
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        IReadOnlyList<cfg.Item> items = DataTableMananger.GetInstance().Tables.ItemTable.DataList;
        for (int i = 0; i < items.Count; i++)
        {
            cfg.Item itemConfig = items[i];
            if (itemConfig.Category == RedundancyItemCategory &&
                itemConfig.Id != CommunitySupplyGiftBoxItemId &&
                playerInfoManager.GetItemCount(itemConfig.Id) > 1)
            {
                _redundancyItemConfigs.Add(itemConfig);
            }
        }

        int maxPage = GetMaxRedundancyItemPage();
        _redundancyItemPage = Mathf.Clamp(_redundancyItemPage, 0, maxPage - 1);
        int firstItemIndex = _redundancyItemPage * RedundancyItemsPerPage;
        float communityCentreScale = scaleConfig.ScaleValue / 10000f;
        for (int i = 0; i < _redundancyItems.Length; i++)
        {
            int itemIndex = firstItemIndex + i;
            cfg.Item itemConfig = itemIndex < _redundancyItemConfigs.Count
                ? _redundancyItemConfigs[itemIndex]
                : null;
            int ownedCount = itemConfig == null
                ? 0
                : playerInfoManager.GetItemCount(itemConfig.Id);
            _redundancyItems[i].SetData(
                itemConfig,
                ownedCount,
                communityCentreScale,
                SelectRedundancyItem);
        }

        _btnLeftPage.interactable = _redundancyItemPage > 0;
        _btnRightPage.interactable = _redundancyItemPage < maxPage - 1;
    }

    private int GetMaxRedundancyItemPage()
    {
        return Mathf.Max(
            1,
            Mathf.CeilToInt(
                (float)_redundancyItemConfigs.Count / RedundancyItemsPerPage));
    }

    private void SelectRedundancyItem(cfg.Item itemConfig)
    {
        _selectedRedundancyItem = itemConfig;
        RefreshSelectedRedundancyItem();
        ShowSubmitPage();
    }

    private void RefreshSelectedRedundancyItem()
    {
        bool hasSelectedItem = _selectedRedundancyItem != null &&
            PlayerInfoManager.GetInstance().GetItemCount(_selectedRedundancyItem.Id) > 1;
        _goNeedItemSelected.SetActive(hasSelectedItem);
        _goNeedItemUnSelected.SetActive(!hasSelectedItem);
        _btnSubmit.interactable = hasSelectedItem;
        if (!hasSelectedItem)
        {
            _selectedRedundancyItem = null;
            return;
        }

        _txtSelectName.text = _selectedRedundancyItem.Name;
        _txtSelectNum.text = $"提交 1 个（冗余：{PlayerInfoManager.GetInstance().GetItemCount(_selectedRedundancyItem.Id) - 1}）";
        _imgSelectIcon.sprite = null;
        LoadSelectedItemIconAsync(_selectedRedundancyItem.Icon, ++_selectedItemPresentationVersion);
    }

    private void ClearSelectedRedundancyItem()
    {
        _selectedRedundancyItem = null;
        _selectedItemPresentationVersion++;
        _goNeedItemSelected.SetActive(false);
        _goNeedItemUnSelected.SetActive(true);
        _btnSubmit.interactable = false;
        _imgSelectIcon.sprite = null;
    }

    private async void LoadSelectedItemIconAsync(string iconPath, int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (presentationVersion != _selectedItemPresentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"社区已选冗余道具图标加载失败: [{iconPath}]", this);
            return;
        }

        _imgSelectIcon.sprite = icon;
    }

    private void SubmitSelectedRedundancyItem()
    {
        if (_isSubmittingRedundancyItem || _selectedRedundancyItem == null)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int previousLevel = playerInfoManager.CommunityCentreLevel;
        float previousProgress = GetCommunityExperienceProgress(playerInfoManager);

        _isSubmittingRedundancyItem = true;
        CommunityCentreRedundancySubmitResult result =
            playerInfoManager.TrySubmitCommunityCentreRedundancyItem(
                _selectedRedundancyItem.Id);
        _isSubmittingRedundancyItem = false;

        switch (result)
        {
            case CommunityCentreRedundancySubmitResult.Success:
                RefreshRedundancyItems();
                RefreshSelectedRedundancyItem();
                RefreshProposals();
                PlayCommunityExperienceAnimation(
                    playerInfoManager,
                    previousLevel,
                    previousProgress);
                CommonTipView.Show("提交成功，获得【社区物资礼盒】×1，社区经验 +10");
                GameManager.Audio.Play(AudioDefine.SFXBuy);
                break;
            case CommunityCentreRedundancySubmitResult.InsufficientRedundancy:
                ClearSelectedRedundancyItem();
                CommonTipView.Show("没有可提交的冗余道具");
                break;
            default:
                Debug.LogError(
                    $"提交的冗余道具无效: [{_selectedRedundancyItem.Id}]",
                    this);
                break;
        }
    }
}
