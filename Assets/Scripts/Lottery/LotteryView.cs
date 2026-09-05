using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LotteryView : UIBasePanel
{
    [Header("抽奖界面")]
    [SerializeField] private GameObject _normal;  // 普通抽奖
    [SerializeField] private GameObject _mys;     // 神秘转盘

    [Header("普通抽奖")]
    [FormerlySerializedAs("lotteryBox")]
    [SerializeField] private RectTransform _lotteryBox;     // 抽奖盒子
    [FormerlySerializedAs("bubbles")]
    [SerializeField] private RectTransform[] _bubbles;      // 气泡
    [FormerlySerializedAs("lotteryButton")]
    [SerializeField] private Button _lotteryButton;         // 抽奖按钮
    [SerializeField] private Text _timeCoinText;            // 剩余时间币文本

    [Header("神秘转盘")]
    [SerializeField] private MysLotteryItem[] _mysLotteryItems; // 神秘转盘奖品列表, 共 12 个
    [SerializeField] private Button _mysButton;             // 抽奖按钮
    [SerializeField] private Text _wheelCoinText;           // 剩余转盘币文本

    private Vector3 _lotteryBoxScale;                       // 奖池盒子缩放
    private Vector3 _lotteryButtonScale;                    // 抽奖按钮缩放
    private Vector3 _mysButtonScale;                        // 转盘按钮缩放
    private Vector2[] _bubblePositions;                     // 气泡位置
    private Vector3[] _bubbleScales;                        // 气泡缩放
    private float _lotteryBoxRotation;                      // 奖池盒子旋转
    private bool _hasCachedAnimationState;                  // 是否缓存动画状态
    private bool _hasRegisteredButtonListener;              // 是否注册按钮监听
    private bool _isLotteryInProgress;                      // 是否正在抽奖
    private readonly List<CommonRewardItemData> _mysteryWheelRewards =
        new List<CommonRewardItemData>(MysteryWheelRewardCount);

    private const int LotteryPoolId = 1;                    // 奖池 ID
    private const int MysteryWheelPoolId = 2;               // 神秘转盘奖池 ID
    private const float LotteryDuration = 1f;               // 奖池抽奖间隔
    private const int MysteryWheelRewardCount = 12;         // 转盘奖品数量
    private const int MysteryWheelMinimumSteps = 24;        // 至少转两圈
    private const float MysteryWheelSlowInterval = 0.16f;   // 转盘最慢间隔
    private const float MysteryWheelFastInterval = 0.035f;  // 转盘最快间隔
    private const float IdleButtonScaleMultiplier = 1.035f; // 待机按钮最大缩放
    private const float IdleButtonBreathDuration = 0.25f;   // 待机按钮单程时长
    private const float IdleButtonInterval = 1.5f;          // 待机按钮呼吸间隔
    private const float IdleDecorationScaleMultiplier = 1.025f; // 待机装饰最大缩放
    private const float LotteryRevealDuration = 0.38f;      // 开箱揭晓演出时长
    private const float MysteryWheelRevealDuration = 0.38f; // 转盘结果揭晓时长

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        CacheAnimationState();
        RestoreAnimationState();
        RegisterButtonListener();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshTimeCoins;
        playerInfoManager.PlayerInfoChanged += RefreshTimeCoins;
        playerInfoManager.TurnAdvanced -= RefreshMysteryWheelRewards;
        playerInfoManager.TurnAdvanced += RefreshMysteryWheelRewards;
        RefreshTimeCoins(playerInfoManager);
        RefreshMysteryWheelRewards();
        PlayIdleAnimations();
    }

    protected override void HideHandle()
    {
        base.HideHandle();

        _isLotteryInProgress = false;
        DOTween.Kill(this);
        RestoreAnimationState();
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshTimeCoins;
        playerInfoManager.TurnAdvanced -= RefreshMysteryWheelRewards;

        if (_lotteryButton != null)
        {
            _lotteryButton.interactable = true;
        }

        if (_mysButton != null)
        {
            _mysButton.interactable = true;
        }

        ClearMysteryWheelHighlights();
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);

        if (_hasRegisteredButtonListener)
        {
            _lotteryButton.onClick.RemoveListener(StartLottery);
            _mysButton.onClick.RemoveListener(StartMysteryWheelLottery);
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshTimeCoins;
        playerInfoManager.TurnAdvanced -= RefreshMysteryWheelRewards;
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.LotteryView;
    }

    private void RegisterButtonListener()
    {
        if (_hasRegisteredButtonListener) return;

        _lotteryButton.onClick.AddListener(StartLottery);
        _mysButton.onClick.AddListener(StartMysteryWheelLottery);
        _hasRegisteredButtonListener = true;
    }

    private void StartLottery()
    {
        if (_isLotteryInProgress) return;

        if (!TrySpendLotteryCost(LotteryPoolId))
        {
            return;
        }

        _isLotteryInProgress = true;
        _lotteryButton.interactable = false;

        DOTween.Kill(this);
        PlayLotteryButtonClickAnimation();
        PlayLotteryBoxAnimation();
        PlayBubbleAnimations();
        PlayLotteryHapticFeedback();

        DOVirtual.DelayedCall(LotteryDuration, CompleteLottery)
            .SetUpdate(true)
            .SetTarget(this);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void CompleteLottery()
    {
        if (!_isLotteryInProgress) return;

        DOTween.Kill(this);
        RestoreAnimationState();
        PlayLotteryRevealAnimation();

        DOVirtual.DelayedCall(LotteryRevealDuration, CompleteLotteryReward)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void CompleteLotteryReward()
    {
        if (!_isLotteryInProgress) return;

        _isLotteryInProgress = false;
        _lotteryButton.interactable = true;
        PlayIdleAnimations();

        if (!TryDrawReward(LotteryPoolId, out CommonRewardItemData reward)) return;

        ApplyReward(reward);

        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonRewardPanel, param: new OpenUIParam
        {
            data = new List<CommonRewardItemData> { reward }
        });
    }

    private static void ApplyReward(CommonRewardItemData reward)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        switch (reward.itemId)
        {
            case BasePropertyId.SimulationCoin:
            case BasePropertyId.TimeCoin:
            case BasePropertyId.Health:
            case BasePropertyId.WheelCoin:
                break;
            default:
                playerInfoManager.AddItem(reward.itemId, reward.itemCount);
                break;
        }
    }

    /// <summary>
    /// 将玩家当前持有的时间币数量显示在抽奖界面
    /// </summary>
    /// <param name="playerInfoManager">提供最新玩家数据的管理器</param>
    private void RefreshTimeCoins(PlayerInfoManager playerInfoManager)
    {
        _timeCoinText.text = $"{playerInfoManager.TimeCoins}";
        _wheelCoinText.text = $"{playerInfoManager.WheelCoins}";
    }

    private bool TryDrawReward(int lotteryPoolId, out CommonRewardItemData reward)
    {
        reward = null;

        cfg.Lottery lotteryConfig = DataTableMananger.GetInstance().Tables.LotteryTable
            .GetOrDefault(lotteryPoolId);
        if (lotteryConfig == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(lotteryConfig.Rewards))
        {
            return false;
        }

        List<LotteryReward> rewards = ParseRewards(lotteryConfig.Rewards);
        if (rewards.Count == 0)
        {
            return false;
        }

        long totalWeight = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            totalWeight += rewards[i].Weight;
        }

        double randomValue = UnityEngine.Random.value * totalWeight;
        long accumulatedWeight = 0;

        for (int i = 0; i < rewards.Count; i++)
        {
            LotteryReward lotteryReward = rewards[i];
            accumulatedWeight += lotteryReward.Weight;

            if (randomValue < accumulatedWeight || i == rewards.Count - 1)
            {
                reward = new CommonRewardItemData
                {
                    itemId = lotteryReward.ItemId,
                    itemCount = lotteryReward.ItemCount
                };
                return true;
            }
        }

        return false;
    }

    private bool TrySpendLotteryCost(int lotteryPoolId)
    {
        cfg.Lottery lotteryConfig = DataTableMananger.GetInstance().Tables.LotteryTable
            .GetOrDefault(lotteryPoolId);
        if (lotteryConfig == null || !TryParseCosts(lotteryConfig.UseCoin, out List<LotteryCost> costs))
        {
            Debug.LogError($"抽奖消耗配置无效: [{lotteryPoolId}]", this);
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = 0; i < costs.Count; i++)
        {
            LotteryCost cost = costs[i];
            if (playerInfoManager.GetConsumableCount(cost.ItemId) < cost.Amount)
            {
                CommonTipView.Show("抽奖币不足");
                GameManager.Audio.Play(AudioDefine.SFXClickFail);
                return false;
            }
        }

        for (int i = 0; i < costs.Count; i++)
        {
            LotteryCost cost = costs[i];
            if (!playerInfoManager.TrySpendConsumable(cost.ItemId, cost.Amount))
            {
                Debug.LogError($"抽奖消耗失败: [{cost.ItemId}], [{cost.Amount}]", this);
                return false;
            }
        }

        return true;
    }

    private static bool TryParseCosts(string costConfig, out List<LotteryCost> costs)
    {
        costs = new List<LotteryCost>();
        if (string.IsNullOrWhiteSpace(costConfig))
        {
            return false;
        }

        Dictionary<int, int> costsByItemId = new Dictionary<int, int>();
        string[] entries = costConfig.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            string[] values = entries[i].Split(',');
            if (values.Length != 2 ||
                !int.TryParse(values[0], out int itemId) ||
                !int.TryParse(values[1], out int amount) ||
                itemId <= 0 || amount <= 0)
            {
                return false;
            }

            if (costsByItemId.TryGetValue(itemId, out int existingAmount))
            {
                if (existingAmount > int.MaxValue - amount)
                {
                    return false;
                }

                costsByItemId[itemId] = existingAmount + amount;
            }
            else
            {
                costsByItemId.Add(itemId, amount);
            }
        }

        foreach (KeyValuePair<int, int> entry in costsByItemId)
        {
            costs.Add(new LotteryCost(entry.Key, entry.Value));
        }

        return costs.Count > 0;
    }

    private List<LotteryReward> ParseRewards(string rewardConfig)
    {
        List<LotteryReward> rewards = new List<LotteryReward>();
        string[] rewardEntries = rewardConfig.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < rewardEntries.Length; i++)
        {
            string[] values = rewardEntries[i].Split(',');

            if (values.Length != 3 ||
                !int.TryParse(values[0], out int itemId) ||
                !int.TryParse(values[1], out int itemCount) ||
                !int.TryParse(values[2], out int weight) ||
                itemId <= 0 || itemCount <= 0 || weight <= 0)
            {
                continue;
            }

            rewards.Add(new LotteryReward(itemId, itemCount, weight));
        }

        return rewards;
    }

    private void RefreshMysteryWheelRewards()
    {
        if (_mysLotteryItems == null || _mysLotteryItems.Length != MysteryWheelRewardCount)
        {
            Debug.LogError($"神秘转盘必须配置 {MysteryWheelRewardCount} 个奖品格。", this);
            _mysButton.interactable = false;
            return;
        }

        if (!TryLoadMonthlyMysteryWheelRewards())
        {
            _mysButton.interactable = false;
            return;
        }

        for (int i = 0; i < _mysLotteryItems.Length; i++)
        {
            _mysLotteryItems[i].SetData(_mysteryWheelRewards[i]);
        }

        ClearMysteryWheelHighlights();
        _mysButton.interactable = true;
    }

    private bool TryLoadMonthlyMysteryWheelRewards()
    {
        cfg.Lottery lotteryConfig = DataTableMananger.GetInstance().Tables.LotteryTable
            .GetOrDefault(MysteryWheelPoolId);
        if (lotteryConfig == null)
        {
            Debug.LogError($"神秘转盘配置不存在: [{MysteryWheelPoolId}]", this);
            return false;
        }

        List<LotteryReward> candidates = ParseRewards(lotteryConfig.Rewards);
        candidates.RemoveAll(reward => !HasRewardPresentation(reward.ItemId));
        RemoveDuplicateRewardItems(candidates);
        if (candidates.Count < MysteryWheelRewardCount)
        {
            Debug.LogError($"神秘转盘可用奖励少于 {MysteryWheelRewardCount} 个。", this);
            return false;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        List<CommonRewardItemData> rewards = new List<CommonRewardItemData>(MysteryWheelRewardCount);
        if (playerInfoManager.HasMonthlyMysteryWheelRewards(MysteryWheelRewardCount))
        {
            for (int i = 0; i < MysteryWheelRewardCount; i++)
            {
                if (!playerInfoManager.TryGetMonthlyMysteryWheelRewardAt(
                        i, out int itemId, out int itemCount) ||
                    !HasRewardPresentation(itemId))
                {
                    rewards.Clear();
                    break;
                }

                rewards.Add(new CommonRewardItemData { itemId = itemId, itemCount = itemCount });
            }
        }

        if (rewards.Count != MysteryWheelRewardCount)
        {
            rewards = SelectMysteryWheelRewards(candidates);
            playerInfoManager.SetMonthlyMysteryWheelRewards(rewards);
        }

        _mysteryWheelRewards.Clear();
        _mysteryWheelRewards.AddRange(rewards);
        return true;
    }

    private static void RemoveDuplicateRewardItems(List<LotteryReward> rewards)
    {
        HashSet<int> itemIds = new HashSet<int>();
        rewards.RemoveAll(reward => !itemIds.Add(reward.ItemId));
    }

    private static List<CommonRewardItemData> SelectMysteryWheelRewards(
        List<LotteryReward> candidates)
    {
        List<LotteryReward> pool = new List<LotteryReward>(candidates);
        List<CommonRewardItemData> selectedRewards =
            new List<CommonRewardItemData>(MysteryWheelRewardCount);
        for (int i = 0; i < MysteryWheelRewardCount; i++)
        {
            long totalWeight = 0;
            for (int j = 0; j < pool.Count; j++)
            {
                totalWeight += pool[j].Weight;
            }

            long randomValue = (long)(UnityEngine.Random.value * totalWeight);
            long accumulatedWeight = 0;
            int selectedIndex = pool.Count - 1;
            for (int j = 0; j < pool.Count; j++)
            {
                accumulatedWeight += pool[j].Weight;
                if (randomValue < accumulatedWeight)
                {
                    selectedIndex = j;
                    break;
                }
            }

            LotteryReward selectedReward = pool[selectedIndex];
            selectedRewards.Add(new CommonRewardItemData
            {
                itemId = selectedReward.ItemId,
                itemCount = selectedReward.ItemCount
            });
            pool.RemoveAt(selectedIndex);
        }

        return selectedRewards;
    }

    private static bool HasRewardPresentation(int itemId)
    {
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        return tables.BaseTable.GetOrDefault(itemId) != null ||
            tables.ItemTable.GetOrDefault(itemId) != null;
    }

    private void StartMysteryWheelLottery()
    {
        if (_isLotteryInProgress || _mysteryWheelRewards.Count != MysteryWheelRewardCount)
        {
            return;
        }

        if (!TrySpendLotteryCost(MysteryWheelPoolId))
        {
            return;
        }

        _isLotteryInProgress = true;
        _mysButton.interactable = false;
        DOTween.Kill(this);
        ClearMysteryWheelHighlights();
        PlayMysteryWheelButtonClickAnimation();
        GameManager.Audio.Vibrate();

        int selectedIndex = UnityEngine.Random.Range(0, MysteryWheelRewardCount);
        PlayMysteryWheelAnimation(selectedIndex);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void PlayMysteryWheelAnimation(int selectedIndex)
    {
        int stepCount = MysteryWheelMinimumSteps + selectedIndex + 1;
        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        for (int i = 0; i < stepCount; i++)
        {
            int currentIndex = i % MysteryWheelRewardCount;
            int stepIndex = i;
            float progress = stepCount <= 1 ? 1f : (float)i / (stepCount - 1);
            float speedMultiplier = Mathf.Sin(progress * Mathf.PI);
            float interval = Mathf.Lerp(
                MysteryWheelSlowInterval,
                MysteryWheelFastInterval,
                speedMultiplier);

            sequence.AppendCallback(() =>
            {
                HighlightMysteryWheelItem(currentIndex);
                PlayMysteryWheelStepHaptic(stepIndex, stepCount);
            });
            sequence.AppendInterval(interval);
        }

        sequence.OnComplete(() => CompleteMysteryWheelLottery(selectedIndex));
    }

    private void CompleteMysteryWheelLottery(int selectedIndex)
    {
        if (!_isLotteryInProgress)
        {
            return;
        }

        _mysLotteryItems[selectedIndex].PlayRewardRevealAnimation();
        GameManager.Audio.Vibrate();

        DOVirtual.DelayedCall(
                MysteryWheelRevealDuration,
                () => CompleteMysteryWheelReward(selectedIndex))
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void CompleteMysteryWheelReward(int selectedIndex)
    {
        if (!_isLotteryInProgress)
        {
            return;
        }

        _isLotteryInProgress = false;
        _mysButton.interactable = true;
        CommonRewardItemData reward = _mysteryWheelRewards[selectedIndex];
        ApplyReward(reward);
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonRewardPanel, param: new OpenUIParam
        {
            data = new List<CommonRewardItemData> { reward }
        });
    }

    private void HighlightMysteryWheelItem(int selectedIndex)
    {
        for (int i = 0; i < _mysLotteryItems.Length; i++)
        {
            _mysLotteryItems[i].SetHighlighted(i == selectedIndex);
        }
    }

    private static void PlayMysteryWheelStepHaptic(int stepIndex, int stepCount)
    {
        bool isFinalThreeSteps = stepIndex >= stepCount - 3;
        if (stepIndex == 0 || isFinalThreeSteps || stepIndex % 4 == 0)
        {
            GameManager.Audio.Vibrate();
        }
    }

    private void ClearMysteryWheelHighlights()
    {
        if (_mysLotteryItems == null)
        {
            return;
        }

        for (int i = 0; i < _mysLotteryItems.Length; i++)
        {
            if (_mysLotteryItems[i] != null)
            {
                _mysLotteryItems[i].SetHighlighted(false);
            }
        }
    }

    private void CacheAnimationState()
    {
        if (_hasCachedAnimationState) return;

        _lotteryBoxScale = _lotteryBox.localScale;
        _lotteryButtonScale = _lotteryButton.transform.localScale;
        _mysButtonScale = _mysButton.transform.localScale;
        _lotteryBoxRotation = _lotteryBox.localEulerAngles.z;

        _bubblePositions = new Vector2[_bubbles.Length];
        _bubbleScales = new Vector3[_bubbles.Length];

        for (int i = 0; i < _bubbles.Length; i++)
        {
            _bubblePositions[i] = _bubbles[i].anchoredPosition;
            _bubbleScales[i] = _bubbles[i].localScale;
        }

        _hasCachedAnimationState = true;
    }

    private void PlayIdleAnimations()
    {
        if (_normal != null && !_normal.activeInHierarchy)
        {
            return;
        }

        DOTween.Sequence()
            .Append(_lotteryButton.transform.DOScale(
                _lotteryButtonScale * IdleButtonScaleMultiplier,
                IdleButtonBreathDuration))
            .Append(_lotteryButton.transform.DOScale(_lotteryButtonScale, IdleButtonBreathDuration))
            .AppendInterval(IdleButtonInterval)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(this);

        PlayIdleDecorationAnimation(
            _lotteryBox,
            _lotteryBoxScale,
            UnityEngine.Random.Range(2.5f, 5f));
    }

    private void PlayIdleDecorationAnimation(
        RectTransform decoration,
        Vector3 originalScale,
        float interval)
    {
        const float expandDuration = 0.35f;
        const float restoreDuration = 0.45f;

        DOTween.Sequence()
            .AppendInterval(UnityEngine.Random.Range(0.5f, interval))
            .Append(decoration.DOScale(
                originalScale * IdleDecorationScaleMultiplier,
                expandDuration))
            .Append(decoration.DOScale(originalScale, restoreDuration))
            .AppendInterval(interval)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayLotteryBoxAnimation()
    {
        const float shakeAngle = 8f;
        const float animationDuration = 0.1f;

        DOTween.Sequence()
            .Append(_lotteryBox.DOScale(_lotteryBoxScale * 0.96f, animationDuration))
            .Append(_lotteryBox.DORotate(
                new Vector3(0f, 0f, _lotteryBoxRotation + shakeAngle), animationDuration))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 1.04f, animationDuration))
            .Append(_lotteryBox.DORotate(
                new Vector3(0f, 0f, _lotteryBoxRotation - shakeAngle), animationDuration * 1.2f))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 0.98f, animationDuration * 1.2f))
            .Append(_lotteryBox.DORotate(
                new Vector3(0f, 0f, _lotteryBoxRotation + shakeAngle), animationDuration * 0.9f))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 1.06f, animationDuration * 0.9f))
            .Append(_lotteryBox.DORotate(
                new Vector3(0f, 0f, _lotteryBoxRotation - shakeAngle), animationDuration * 0.8f))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 0.97f, animationDuration * 0.8f))
            .Append(_lotteryBox.DORotate(
                new Vector3(0f, 0f, _lotteryBoxRotation), animationDuration))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale, animationDuration))
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayBubbleAnimations()
    {
        for (int i = 0; i < _bubbles.Length; i++)
        {
            RectTransform bubble = _bubbles[i];
            Vector2 movement = (_bubblePositions[i] - (Vector2)_lotteryBox.anchoredPosition)
                .normalized * 16f;

            DOTween.Sequence()
                .AppendInterval(0.08f + i * 0.06f)
                .Append(bubble.DOAnchorPos(_bubblePositions[i] + movement, 0.16f))
                .Join(bubble.DOScale(_bubbleScales[i] * 1.12f, 0.16f))
                .Append(bubble.DOAnchorPos(_bubblePositions[i], 0.24f))
                .Join(bubble.DOScale(_bubbleScales[i], 0.24f))
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetTarget(this);
        }
    }

    private void PlayLotteryButtonClickAnimation()
    {
        DOTween.Sequence()
            .Append(_lotteryButton.transform.DOScale(_lotteryButtonScale * 0.94f, 0.06f))
            .Append(_lotteryButton.transform.DOScale(_lotteryButtonScale * 1.06f, 0.1f))
            .Append(_lotteryButton.transform.DOScale(_lotteryButtonScale, 0.12f))
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayMysteryWheelButtonClickAnimation()
    {
        DOTween.Sequence()
            .Append(_mysButton.transform.DOScale(_mysButtonScale * 0.94f, 0.06f))
            .Append(_mysButton.transform.DOScale(_mysButtonScale * 1.06f, 0.1f))
            .Append(_mysButton.transform.DOScale(_mysButtonScale, 0.12f))
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayLotteryRevealAnimation()
    {
        GameManager.Audio.Vibrate();

        DOTween.Sequence()
            .Append(_lotteryBox.DOScale(_lotteryBoxScale * 0.9f, 0.08f))
            .Append(_lotteryBox.DOScale(_lotteryBoxScale * 1.15f, 0.18f).SetEase(Ease.OutBack))
            .Append(_lotteryBox.DOScale(_lotteryBoxScale, 0.12f))
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayLotteryHapticFeedback()
    {
        GameManager.Audio.Vibrate();

        DOVirtual.DelayedCall(0.26f, () => GameManager.Audio.Vibrate())
            .SetUpdate(true)
            .SetTarget(this);
        DOVirtual.DelayedCall(0.52f, () => GameManager.Audio.Vibrate())
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void RestoreAnimationState()
    {
        if (!_hasCachedAnimationState) return;

        _lotteryBox.localRotation = Quaternion.Euler(0f, 0f, _lotteryBoxRotation);
        _lotteryBox.localScale = _lotteryBoxScale;
        _lotteryButton.transform.localScale = _lotteryButtonScale;
        _mysButton.transform.localScale = _mysButtonScale;

        for (int i = 0; i < _bubbles.Length; i++)
        {
            _bubbles[i].anchoredPosition = _bubblePositions[i];
            _bubbles[i].localScale = _bubbleScales[i];
        }
    }

    private readonly struct LotteryReward
    {
        public readonly int ItemId;
        public readonly int ItemCount;
        public readonly int Weight;

        public LotteryReward(int itemId, int itemCount, int weight)
        {
            ItemId = itemId;
            ItemCount = itemCount;
            Weight = weight;
        }

    }

    private readonly struct LotteryCost
    {
        public readonly int ItemId;
        public readonly int Amount;

        public LotteryCost(int itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }
}
