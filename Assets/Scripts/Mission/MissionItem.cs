using System;
using RedSaw.MissionSystem;
using UnityEngine;
using UnityEngine.UI;

public class MissionItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Image _imgIcon;
    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtDetail;
    [SerializeField] private Text _txtProgress;
    [SerializeField] private Button _btnGet;
    [SerializeField] private Slider _sliderProgress;
    [SerializeField] private MissionRewardItem[] _rewardItems;

    private int _requestVersion;

    public void SetData(cfg.Mission missionConfig, Mission<MissionMessage> mission, Action claimAction)
    {
        _requestVersion++;
        gameObject.SetActive(false);
        _txtTitle.text = missionConfig.Name;
        _txtDetail.text = missionConfig.Desc;

        MissionProgress[] progresses = mission.Progresses;
        int target = Mathf.Max(1, int.TryParse(missionConfig.Target, out int value) ? value : 1);
        int current = progresses.Length == 0 ? 0 : Mathf.Clamp(progresses[0].currentCount, 0, target);
        _sliderProgress.minValue = 0f;
        _sliderProgress.maxValue = target;
        _sliderProgress.value = current;
        _txtProgress.text = $"{current} / {target}";

        _btnGet.onClick.RemoveAllListeners();
        _btnGet.interactable = mission.IsFinished;
        if (mission.IsFinished)
        {
            _btnGet.onClick.AddListener(() => claimAction());
        }

        SetIconAsync(missionConfig, _requestVersion);
        SetRewardsAsync(missionConfig, _requestVersion);
    }

    public void Clear()
    {
        _requestVersion++;
        _imgIcon.sprite = null;
        _btnGet.onClick.RemoveAllListeners();
        for (int i = 0; i < _rewardItems.Length; i++)
        {
            _rewardItems[i].Clear();
        }

        gameObject.SetActive(false);
    }

    private async void SetIconAsync(cfg.Mission missionConfig, int requestVersion)
    {
        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable.GetOrDefault(missionConfig.Icon);
        cfg.Scale scaleConfig = GetScale("missionIcon");
        if (itemConfig == null || scaleConfig == null)
        {
            _imgIcon.sprite = null;
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            GetInstanceID().ToString());
        if (requestVersion != _requestVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"任务图标加载失败: [{missionConfig.Id}], [{itemConfig.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / ScaleDivisor) *
            (scaleConfig.ScaleValue / ScaleDivisor);
        gameObject.SetActive(true);
    }

    private async void SetRewardsAsync(cfg.Mission missionConfig, int requestVersion)
    {
        for (int i = 0; i < _rewardItems.Length; i++)
        {
            _rewardItems[i].Clear();
        }

        cfg.Scale scaleConfig = GetScale("missionReward");
        if (scaleConfig == null || string.IsNullOrEmpty(missionConfig.Reward))
        {
            return;
        }

        string[] rewardValues = missionConfig.Reward.Split(',');
        int[] rewardIds =
        {
            BasePropertyId.SimulationCoin,
            BasePropertyId.TimeCoin,
            BasePropertyId.Health
        };

        int slotIndex = 0;
        for (int i = 0; i < rewardIds.Length && slotIndex < _rewardItems.Length; i++)
        {
            if (i >= rewardValues.Length ||
                !int.TryParse(rewardValues[i], out int amount) ||
                amount <= 0)
            {
                continue;
            }

            cfg.Base rewardConfig = DataTableMananger.GetInstance().Tables.BaseTable.GetOrDefault(rewardIds[i]);
            if (rewardConfig == null)
            {
                Debug.LogError($"任务奖励配置不存在: [{rewardIds[i]}]", this);
                continue;
            }

            Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
                rewardConfig.Icon,
                GetInstanceID().ToString());
            if (requestVersion != _requestVersion)
            {
                return;
            }

            if (icon == null)
            {
                Debug.LogError($"任务奖励图标加载失败: [{rewardIds[i]}], [{rewardConfig.Icon}]", this);
                continue;
            }

            _rewardItems[slotIndex].SetData(
                icon,
                amount,
                (rewardConfig.RewardScale / ScaleDivisor) *
                (scaleConfig.ScaleValue / ScaleDivisor));
            slotIndex++;
        }
    }

    private static cfg.Scale GetScale(string scaleName)
    {
        System.Collections.Generic.IReadOnlyList<cfg.Scale> scales =
            DataTableMananger.GetInstance().Tables.ScaleTable.DataList;
        for (int i = 0; i < scales.Count; i++)
        {
            if (scales[i].Name == scaleName)
            {
                return scales[i];
            }
        }

        Debug.LogError($"缩放配置不存在: [{scaleName}]");
        return null;
    }
}
