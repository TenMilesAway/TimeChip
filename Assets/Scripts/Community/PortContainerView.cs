using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PortContainerView : UIBasePanel
{
    [SerializeField] private Button _btnClose;
    [SerializeField] private Transform _gridParent;
    [SerializeField] private GameObject _goTxtClose;
    [SerializeField, Min(0f)] private float _itemDisplayInterval = 0.35f;
    [SerializeField, Min(1)] private int _simulationCoinIconCount = 8;

    private readonly List<PortContainerItem> _rewardItems =
        new List<PortContainerItem>();
    private int _presentationVersion;
    private int? _simulationCoinDisplayStart;

    private void Awake()
    {
        _btnClose.onClick.AddListener(ClosePanel);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        GameManager.Audio.Play(AudioDefine.SFXGetReward);
        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
        _btnClose.enabled = false;
        _goTxtClose.SetActive(false);
        _simulationCoinDisplayStart = param?.simulationCoinDisplayStart;

        if (!(param?.data is List<CommonRewardItemData> rewards) ||
            rewards.Count == 0)
        {
            Debug.LogError("PortContainerView 需要至少一个有效奖励。", this);
            return;
        }

        InitializeRewardsAsync(rewards, _presentationVersion);
    }

    protected override void CloseHandle()
    {
        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.PortContainerView;
    }

    private async void InitializeRewardsAsync(
        List<CommonRewardItemData> rewards,
        int presentationVersion)
    {
        string resourceTag = GetInstanceID().ToString();
        for (int i = 0; i < rewards.Count; i++)
        {
            GameObject rewardObject = await UnityObjectPoolFactory.GetInstance()
                .GetItem<GameObject>(GlobalDefine.PortContainerItem, resourceTag);
            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.PortContainerItem,
                    rewardObject);
                return;
            }

            rewardObject.SetActive(false);
            rewardObject.transform.SetParent(_gridParent, false);

            PortContainerItem rewardItem = rewardObject.GetComponent<PortContainerItem>();
            if (rewardItem == null ||
                !await rewardItem.SetDataAsync(rewards[i], resourceTag))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.PortContainerItem,
                    rewardObject);
                continue;
            }

            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(
                    GlobalDefine.PortContainerItem,
                    rewardObject);
                return;
            }

            _rewardItems.Add(rewardItem);
        }

        if (IsCurrentPresentation(presentationVersion))
        {
            PlayRewardItemSequence(presentationVersion);
        }
    }

    private void ClosePanel()
    {
        if (_btnClose.enabled)
        {
            PlaySimulationCoinFlyAnimations();
            UIManager.GetInstance().ClosePanel(GetPanelName());
        }
    }

    private void PlayRewardItemSequence(int presentationVersion)
    {
        if (_rewardItems.Count == 0)
        {
            FinishPresentation();
            return;
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            PortContainerItem rewardItem = _rewardItems[i];
            sequence.AppendCallback(() => rewardItem.gameObject.SetActive(true));
            sequence.AppendInterval(_itemDisplayInterval);
        }

        sequence.OnComplete(() =>
        {
            if (IsCurrentPresentation(presentationVersion))
            {
                FinishPresentation();
            }
        });
    }

    private void FinishPresentation()
    {
        _btnClose.enabled = true;
        _goTxtClose.SetActive(true);
    }

    private void PlaySimulationCoinFlyAnimations()
    {
        if (!_simulationCoinDisplayStart.HasValue)
        {
            return;
        }

        MainMenuView mainMenuView = UIManager.GetInstance()
            .GetOpeningPanel(GlobalDefine.MainMenuView) as MainMenuView;
        if (mainMenuView == null)
        {
            return;
        }

        bool hasStartedCountAnimation = false;
        bool hasFlyAnimation = false;
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            PortContainerItem rewardItem = _rewardItems[i];
            if (rewardItem.ItemId != BasePropertyId.SimulationCoin)
            {
                continue;
            }

            hasFlyAnimation |= CurrencyFlyAnimation.Play(
                rewardItem.IconTransform,
                mainMenuView.SimulationCoinIcon.rectTransform,
                mainMenuView.SimulationCoinIcon.sprite,
                _simulationCoinIconCount,
                true,
                () =>
                {
                    if (hasStartedCountAnimation)
                    {
                        return;
                    }

                    hasStartedCountAnimation = true;
                    mainMenuView.PlaySimulationCoinCountAnimation(
                        _simulationCoinDisplayStart.Value,
                        PlayerInfoManager.GetInstance().SimulationCoins);
                });
        }

        if (!hasFlyAnimation)
        {
            mainMenuView.SetSimulationCoinDisplay(
                PlayerInfoManager.GetInstance().SimulationCoins);
        }
    }

    private bool IsCurrentPresentation(int presentationVersion)
    {
        return presentationVersion == _presentationVersion && isActiveAndEnabled;
    }

    private void ClearRewardItems()
    {
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            PortContainerItem rewardItem = _rewardItems[i];
            if (rewardItem == null)
            {
                continue;
            }

            rewardItem.Clear();
            UnityObjectPoolFactory.GetInstance().PutItem(
                GlobalDefine.PortContainerItem,
                rewardItem.gameObject);
        }

        _rewardItems.Clear();
    }
}
