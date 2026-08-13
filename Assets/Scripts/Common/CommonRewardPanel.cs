using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CommonRewardPanel : UIBasePanel
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _commonRewardItemParent;
    [SerializeField] private GameObject _textTip;
    [SerializeField, Min(0f)] private float _itemDisplayInterval = 0.35f;

    private readonly List<CommonRewardItem> _rewardItems = new List<CommonRewardItem>();

    private int _presentationVersion;

    private void Awake()
    {
        _closeButton.onClick.AddListener(ClosePanel);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        ResetPresentation();

        if (!(param?.data is List<CommonRewardItemData> rewardDataList))
        {
            Debug.LogError("CommonRewardPanel requires a List<CommonRewardItemData> in OpenUIParam.data.");
            FinishPresentation();
            return;
        }

        InitializeRewardsAsync(rewardDataList, _presentationVersion);
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();

        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonRewardPanel;
    }

    private async void InitializeRewardsAsync(List<CommonRewardItemData> rewardDataList, int presentationVersion)
    {
        string resourceTag = GetInstanceID().ToString();

        for (int i = 0; i < rewardDataList.Count; i++)
        {
            CommonRewardItemData rewardData = rewardDataList[i];
            cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(rewardData.itemId);

            if (itemConfig == null)
            {
                Debug.LogError($"Reward item config was not found: itemId[{rewardData.itemId}].");
                continue;
            }

            GameObject rewardItemObject = await UnityObjectPoolFactory.GetInstance().GetItem<GameObject>(GlobalDefine.CommonRewardItem, resourceTag);

            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                return;
            }

            rewardItemObject.SetActive(false);
            rewardItemObject.transform.SetParent(_commonRewardItemParent, false);

            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(itemConfig.Icon, resourceTag);

            if (!IsCurrentPresentation(presentationVersion))
            {
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                return;
            }

            if (icon == null)
            {
                Debug.LogError($"Reward icon could not be loaded: itemId[{rewardData.itemId}], key[{itemConfig.Icon}].");
                UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, rewardItemObject);
                continue;
            }

            CommonRewardItem rewardItem = rewardItemObject.GetComponent<CommonRewardItem>();
            rewardItem.SetData(icon, rewardData.itemCount, itemConfig.RewardScale);
            _rewardItems.Add(rewardItem);
        }

        if (IsCurrentPresentation(presentationVersion))
        {
            PlayRewardItemSequence(presentationVersion);
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
            CommonRewardItem rewardItem = _rewardItems[i];
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

    private void ResetPresentation()
    {
        _presentationVersion++;
        DOTween.Kill(this);
        ClearRewardItems();
        _textTip.SetActive(false);
        _closeButton.interactable = false;
    }

    private void FinishPresentation()
    {
        _textTip.SetActive(true);
        _closeButton.interactable = true;
    }

    private bool IsCurrentPresentation(int presentationVersion)
    {
        return presentationVersion == _presentationVersion && isActiveAndEnabled;
    }

    private void ClearRewardItems()
    {
        for (int i = 0; i < _rewardItems.Count; i++)
        {
            UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.CommonRewardItem, _rewardItems[i].gameObject);
        }

        _rewardItems.Clear();
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
