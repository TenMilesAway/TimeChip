using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MysLotteryItem : MonoBehaviour
{
    [SerializeField] private Image _bg;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _num;

    private Color _defaultBackgroundColor;
    private int _presentationVersion;

    private void Awake()
    {
        _defaultBackgroundColor = _bg.color;
    }

    public async void SetData(CommonRewardItemData reward)
    {
        _presentationVersion++;
        int presentationVersion = _presentationVersion;
        SetHighlighted(false);
        _icon.sprite = null;
        _num.text = reward.itemCount.ToString();

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(reward.itemId);
        cfg.Item itemConfig = baseConfig == null ? tables.ItemTable.GetOrDefault(reward.itemId) : null;
        if (baseConfig == null && itemConfig == null)
        {
            Debug.LogError($"神秘转盘奖励配置不存在: [{reward.itemId}]", this);
            return;
        }

        string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            $"{GetInstanceID()}_{presentationVersion}");
        if (presentationVersion != _presentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"神秘转盘奖励图标加载失败: [{reward.itemId}], [{iconPath}]", this);
            return;
        }

        _icon.sprite = icon;
    }

    public void SetHighlighted(bool highlighted)
    {
        _bg.color = highlighted ? Color.yellow : _defaultBackgroundColor;
    }
}
