using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LotteryView : UIBasePanel
{
    [Header("Animation")]
    [FormerlySerializedAs("lotteryBox")]
    [SerializeField] private RectTransform _lotteryBox;     // 抽奖盒子
    [FormerlySerializedAs("bubbles")]
    [SerializeField] private RectTransform[] _bubbles;      // 气泡
    [FormerlySerializedAs("lotteryButton")]
    [SerializeField] private Button _lotteryButton;         // 抽奖按钮
    [SerializeField] private Text _timeCoinText;            // 剩余时间币文本

    private Vector3 _lotteryBoxScale;                       // 奖池盒子缩放
    private Vector2[] _bubblePositions;                     // 气泡位置
    private Vector3[] _bubbleScales;                        // 气泡缩放
    private float _lotteryBoxRotation;                      // 奖池盒子旋转
    private bool _hasCachedAnimationState;                  // 是否缓存动画状态
    private bool _hasRegisteredButtonListener;              // 是否注册按钮监听
    private bool _isLotteryInProgress;                      // 是否正在抽奖

    private const int LotteryPoolId = 1;                    // 奖池 ID
    private const float LotteryDuration = 1f;               // 奖池抽奖间隔
    private const int LotteryTimeCoinCost = 1;              // 单次抽奖消耗的时间币
    private const int SmallSimulationCoinItemId = 1000;     // 兑换 100 模拟币的奖励 ID
    private const int LargeSimulationCoinItemId = 1001;     // 兑换 1000 模拟币的奖励 ID
    private const int SmallSimulationCoinValue = 100;       // 小额兑换值
    private const int LargeSimulationCoinValue = 1000;      // 大额兑换值

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
        RefreshTimeCoins(playerInfoManager);
    }

    protected override void HideHandle()
    {
        base.HideHandle();

        _isLotteryInProgress = false;
        DOTween.Kill(this);
        RestoreAnimationState();
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshTimeCoins;

        if (_lotteryButton != null)
        {
            _lotteryButton.interactable = true;
        }
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);

        if (_hasRegisteredButtonListener)
        {
            _lotteryButton.onClick.RemoveListener(StartLottery);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshTimeCoins;
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
        _hasRegisteredButtonListener = true;
    }

    private void StartLottery()
    {
        if (_isLotteryInProgress) return;

        if (!PlayerInfoManager.GetInstance().TrySpendTimeCoins(LotteryTimeCoinCost))
        {
            Debug.LogWarning("时间币不足, 无法抽奖", this);
            CommonTipView.Show("时间币不足");
            GameManager.Audio.Play(AudioDefine.SFXClickFail);
            return;
        }

        _isLotteryInProgress = true;
        _lotteryButton.interactable = false;

        DOTween.Kill(this);
        PlayLotteryBoxAnimation();
        PlayBubbleAnimations();

        DOVirtual.DelayedCall(LotteryDuration, CompleteLottery)
            .SetUpdate(true)
            .SetTarget(this);
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    private void CompleteLottery()
    {
        if (!_isLotteryInProgress) return;

        _isLotteryInProgress = false;
        DOTween.Kill(this);
        RestoreAnimationState();
        _lotteryButton.interactable = true;

        if (!TryDrawReward(out CommonRewardItemData reward)) return;

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
            case SmallSimulationCoinItemId:
            case LargeSimulationCoinItemId:
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
    }

    private bool TryDrawReward(out CommonRewardItemData reward)
    {
        reward = null;

        cfg.Lottery lotteryConfig = DataTableMananger.GetInstance().Tables.LotteryTable.GetOrDefault(LotteryPoolId);
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

    private void CacheAnimationState()
    {
        if (_hasCachedAnimationState) return;

        _lotteryBoxScale = _lotteryBox.localScale;
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

    private void PlayLotteryBoxAnimation()
    {
        const float shakeAngle = 7f;
        const float animationDuration = 0.16f;

        DOTween.Sequence()
            .Append(_lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation + shakeAngle), animationDuration))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 1.08f, animationDuration))
            .Append(_lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation - shakeAngle), animationDuration * 2f))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale * 0.92f, animationDuration * 2f))
            .Append(_lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation), animationDuration))
            .Join(_lotteryBox.DOScale(_lotteryBoxScale, animationDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayBubbleAnimations()
    {
        for (int i = 0; i < _bubbles.Length; i++)
        {
            RectTransform bubble = _bubbles[i];
            float duration = 1.2f + i * 0.1f;
            Vector2 movement = new Vector2(i % 2 == 0 ? 12f : -12f, 16f);

            DOTween.Sequence()
                .Append(bubble.DOAnchorPos(_bubblePositions[i] + movement, duration))
                .Join(bubble.DOScale(_bubbleScales[i] * 1.06f, duration))
                .Append(bubble.DOAnchorPos(_bubblePositions[i] - movement, duration * 2f))
                .Join(bubble.DOScale(_bubbleScales[i] * 0.94f, duration * 2f))
                .Append(bubble.DOAnchorPos(_bubblePositions[i], duration))
                .Join(bubble.DOScale(_bubbleScales[i], duration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetTarget(this);
        }
    }

    private void RestoreAnimationState()
    {
        if (!_hasCachedAnimationState) return;

        _lotteryBox.localRotation = Quaternion.Euler(0f, 0f, _lotteryBoxRotation);
        _lotteryBox.localScale = _lotteryBoxScale;

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
}
