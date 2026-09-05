using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MysLotteryItem : MonoBehaviour
{
    [SerializeField] private Image _bg;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _num;

    private Color _defaultBackgroundColor;
    private Vector3 _defaultScale;
    private int _presentationVersion;

    private const float RewardScaleDivisor = 10000f;
    private const int MysIconScaleId = 7;

    private void Awake()
    {
        _defaultBackgroundColor = _bg.color;
        _defaultScale = transform.localScale;
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
        int rewardScale = baseConfig == null ? itemConfig.RewardScale : baseConfig.RewardScale;
        cfg.Scale mysIconScaleConfig = tables.ScaleTable.GetOrDefault(MysIconScaleId);
        if (mysIconScaleConfig == null)
        {
            Debug.LogError($"神秘转盘图标缩放配置不存在: [{MysIconScaleId}]", this);
            return;
        }

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
        _icon.SetNativeSize();
        float scaleMultiplier = mysIconScaleConfig.ScaleValue / RewardScaleDivisor;
        _icon.rectTransform.localScale =
            Vector3.one * (rewardScale / RewardScaleDivisor) * scaleMultiplier;
    }

    public void SetHighlighted(bool highlighted)
    {
        DOTween.Kill(this);
        _bg.DOColor(highlighted ? Color.yellow : _defaultBackgroundColor, 0.08f)
            .SetTarget(this);
        transform.DOScale(_defaultScale * (highlighted ? 1.08f : 1f), 0.08f)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);
    }

    public void PlayRewardRevealAnimation()
    {
        DOTween.Kill(this);
        _bg.color = Color.yellow;

        DOTween.Sequence()
            .Append(transform.DOScale(_defaultScale * 0.92f, 0.06f))
            .Append(transform.DOScale(_defaultScale * 1.18f, 0.18f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(_defaultScale * 1.08f, 0.14f))
            .SetUpdate(true)
            .SetTarget(this);
    }
}
