using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardLotteryItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _num;

    [SerializeField] private GameObject _goCardBg;     // 未翻牌时的 GO
    [SerializeField] private GameObject _goSelectedBg; // 翻牌后的 GO

    private int _presentationVersion;
    private Vector3 _defaultRotation;

    private void Awake()
    {
        _defaultRotation = transform.localEulerAngles;
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public void SetUnopened()
    {
        _presentationVersion++;
        DOTween.Kill(this);
        transform.localEulerAngles = _defaultRotation;
        _icon.sprite = null;
        _num.text = string.Empty;
        _goCardBg.SetActive(true);
        _goSelectedBg.SetActive(false);
    }

    public void SetOpened(CommonRewardItemData reward)
    {
        _presentationVersion++;
        DOTween.Kill(this);
        transform.localEulerAngles = _defaultRotation;
        _goCardBg.SetActive(false);
        _goSelectedBg.SetActive(true);
        SetRewardPresentation(reward, _presentationVersion);
    }

    public void PlayRevealAnimation(CommonRewardItemData reward)
    {
        _presentationVersion++;
        int presentationVersion = _presentationVersion;
        DOTween.Kill(this);
        _icon.sprite = null;
        _num.text = string.Empty;
        _goCardBg.SetActive(true);
        _goSelectedBg.SetActive(false);
        transform.localEulerAngles = _defaultRotation;

        DOTween.Sequence()
            .Append(transform.DOLocalRotate(
                _defaultRotation + new Vector3(0f, 90f, 0f),
                0.18f,
                RotateMode.FastBeyond360))
            .AppendCallback(() =>
            {
                _goCardBg.SetActive(false);
                _goSelectedBg.SetActive(true);
                SetRewardPresentation(reward, presentationVersion);
            })
            .Append(transform.DOLocalRotate(
                _defaultRotation,
                0.18f,
                RotateMode.Fast))
            .SetUpdate(true)
            .SetTarget(this);
    }

    private async void SetRewardPresentation(
        CommonRewardItemData reward,
        int presentationVersion)
    {
        _icon.sprite = null;
        _num.text = reward.itemCount.ToString();

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(reward.itemId);
        cfg.Item itemConfig = baseConfig == null ? tables.ItemTable.GetOrDefault(reward.itemId) : null;
        if (baseConfig == null && itemConfig == null)
        {
            Debug.LogError($"时光星阵奖励配置不存在: [{reward.itemId}]", this);
            return;
        }

        string iconPath = baseConfig == null ? itemConfig.Icon : baseConfig.Icon;
        int rewardScale = baseConfig == null ? itemConfig.RewardScale : baseConfig.RewardScale;
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            $"{GetInstanceID()}_{presentationVersion}");
        if (presentationVersion != _presentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"时光星阵奖励图标加载失败: [{reward.itemId}], [{iconPath}]", this);
            return;
        }

        _icon.sprite = icon;
        _icon.SetNativeSize();
        _icon.rectTransform.localScale = Vector3.one * (rewardScale / ScaleDivisor);
    }
}
