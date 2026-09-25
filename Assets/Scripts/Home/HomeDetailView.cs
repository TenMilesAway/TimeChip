using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeDetailView : UIBasePanel
{
    [SerializeField] private Text _txtSatisfaction; // 满意度文本：xx%
    [SerializeField] private Slider _sliderSatisfaction; // 满意度滑动条
    [SerializeField] private HomeDetailShowItem[] _showItems; // 展示的详细信息项

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        RefreshSatisfaction(PlayerInfoManager.GetInstance());
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= RefreshSatisfaction;
        playerInfoManager.PlayerInfoChanged += RefreshSatisfaction;
        RefreshSatisfaction(playerInfoManager);
    }

    protected override void CloseHandle()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        base.CloseHandle();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshSatisfaction;
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= RefreshSatisfaction;
        base.OnDestroy();
    }

    private void RefreshSatisfaction(PlayerInfoManager playerInfoManager)
    {
        if (_txtSatisfaction == null || _sliderSatisfaction == null)
        {
            Debug.LogError("HomeDetailView 的满意度文本或滑动条未在 Inspector 中配置", this);
            return;
        }

        BuffSystem.GetInstance().RefreshHomeSatisfactionBuff();

        float satisfaction = Mathf.Clamp(playerInfoManager.Satisfaction, 0f, 100f);
        _txtSatisfaction.text = $"{satisfaction:0.##}%";
        _sliderSatisfaction.value = satisfaction / 100f;
        RefreshSatisfactionBuffItems(satisfaction);
    }

    private void RefreshSatisfactionBuffItems(float satisfaction)
    {
        if (_showItems == null)
        {
            Debug.LogError("HomeDetailView 未配置满意度 BUFF 展示项", this);
            return;
        }

        IReadOnlyList<cfg.HomeSatisfactionBuff> tiers = DataTableMananger.GetInstance()
            .Tables.HomeSatisfactionBuffTable.DataList;
        if (_showItems.Length < tiers.Count)
        {
            Debug.LogError(
                $"HomeDetailView 的展示项数量不足: 需要 {tiers.Count} 个，当前 {_showItems.Length} 个",
                this);
        }

        int itemCount = Mathf.Min(_showItems.Length, tiers.Count);
        for (int i = 0; i < itemCount; i++)
        {
            cfg.HomeSatisfactionBuff tier = tiers[i];
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(tier.BuffId);
            if (_showItems[i] == null)
            {
                Debug.LogError($"HomeDetailView 的第 {i + 1} 个满意度 BUFF 展示项未配置", this);
                continue;
            }

            _showItems[i].SetData(tier, buffConfig, satisfaction >= tier.MinSatisfaction);
        }

        for (int i = itemCount; i < _showItems.Length; i++)
        {
            if (_showItems[i] != null)
            {
                _showItems[i].gameObject.SetActive(false);
            }
        }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.HomeDetailView;
    }
}
