using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Text _txtTitle;        // 零工标题
    [SerializeField] private Text _txtDetail;       // 零工详情
    [SerializeField] private Text _txtCoin;         // 零工奖励模拟币
    [SerializeField] private Text _txtHealth;       // 零工消耗体力
    [SerializeField] private Text _txtTip;          // 零工提示: 与按钮互斥显示
    [SerializeField] private Image _imgIcon;        // 零工图标
    [SerializeField] private Button _btnGetAll;     // 零工真实领取按钮: 与提示互斥显示
    [SerializeField] private GameObject _btnGet;    // 零工领取按钮: 与提示互斥显示

    private cfg.Work _workConfig;
    private Action<cfg.Work> _workHandler;
    private bool _isUnlocked;
    private string _unlockTip;
    private int _iconRequestVersion;

    private void Awake()
    {
        _btnGetAll.onClick.AddListener(OnClickGet);
    }

    private void OnDestroy()
    {
        if (_btnGetAll != null)
        {
            _btnGetAll.onClick.RemoveListener(OnClickGet);
        }
    }

    public void SetData(
        cfg.Work workConfig,
        Action<cfg.Work> workHandler,
        bool isUnlocked,
        string unlockTip)
    {
        _workConfig = workConfig;
        _workHandler = workHandler;
        _isUnlocked = isUnlocked;
        _unlockTip = unlockTip;
        _iconRequestVersion++;

        if (workConfig == null)
        {
            _imgIcon.sprite = null;
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtTitle.text = workConfig.Name;
        _txtDetail.text = workConfig.Desc;
        _txtCoin.text = $"+{workConfig.CoinReward}";
        _txtHealth.text = $"-{workConfig.HealthCost}";
        SetIconAsync(workConfig, _iconRequestVersion);
        RefreshWorkState();
    }

    public void RefreshWorkState()
    {
        if (_workConfig == null)
        {
            return;
        }

        bool workedThisTurn = PlayerInfoManager.GetInstance().WorkedThisTurn;
        bool canAccept = _isUnlocked && !workedThisTurn;
        _txtTip.gameObject.SetActive(!canAccept);
        _btnGet.SetActive(canAccept);
        _btnGetAll.interactable = canAccept;
        _txtTip.text = workedThisTurn ? "本回合已工作" : _unlockTip;
    }

    private async void SetIconAsync(cfg.Work workConfig, int requestVersion)
    {
        _imgIcon.sprite = null;

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(workConfig.Icon);
        cfg.Scale scaleConfig = GetScale("work");
        if (itemConfig == null || scaleConfig == null)
        {
            Debug.LogError($"零工图标配置不存在: [{workConfig.Id}], [{workConfig.Icon}]", this);
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            GetInstanceID().ToString());
        if (requestVersion != _iconRequestVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"零工图标加载失败: [{workConfig.Id}], [{itemConfig.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / ScaleDivisor) *
            (scaleConfig.ScaleValue / ScaleDivisor);
    }

    private static cfg.Scale GetScale(string scaleName)
    {
        IReadOnlyList<cfg.Scale> scales = DataTableMananger.GetInstance()
            .Tables.ScaleTable.DataList;
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

    private void OnClickGet()
    {
        if (_workConfig == null)
        {
            return;
        }

        _workHandler?.Invoke(_workConfig);
    }
}