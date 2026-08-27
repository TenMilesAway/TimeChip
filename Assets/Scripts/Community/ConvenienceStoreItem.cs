using UnityEngine;
using System;
using UnityEngine.UI;

public class ConvenienceStoreItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Text _txtName;     // 商品名称
    [SerializeField] private Text _txtTag;      // 商品标签
    [SerializeField] private Text _txtPrice;    // 商品价格
    [SerializeField] private Text _txtNum;      // 商品剩余数量：剩余   {0}/{1}
    [SerializeField] private Button _btnCharge; // 购买按钮
    [SerializeField] private Image _imgIcon;    // 商品图标

    private int _presentationVersion;
    private cfg.Convenience _convenienceConfig;
    private Action<cfg.Convenience> _purchaseHandler;

    private void Awake()
    {
        _btnCharge.onClick.AddListener(OnClickPurchase);
    }

    private void OnDestroy()
    {
        if (_btnCharge != null)
        {
            _btnCharge.onClick.RemoveListener(OnClickPurchase);
        }
    }

    public void SetData(
        cfg.Convenience convenienceConfig,
        cfg.Item itemConfig,
        float scaleMultiplier,
        int remainingCount,
        Action<cfg.Convenience> purchaseHandler)
    {
        _presentationVersion++;
        _convenienceConfig = convenienceConfig;
        _purchaseHandler = purchaseHandler;
        if (convenienceConfig == null || itemConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtName.text = convenienceConfig.Name;
        _txtTag.text = $"限购{convenienceConfig.Num}份";
        _txtPrice.text = convenienceConfig.Price.ToString();
        _txtNum.text = $"剩余   {remainingCount}/{convenienceConfig.Num}";
        _imgIcon.sprite = null;
        LoadIconAsync(itemConfig, scaleMultiplier, _presentationVersion);
    }

    private async void LoadIconAsync(cfg.Item itemConfig, float scaleMultiplier, int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            itemConfig.Icon,
            GetInstanceID().ToString());
        if (presentationVersion != _presentationVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"便利店商品图标加载失败: [{itemConfig.Id}], [{itemConfig.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / ScaleDivisor) * scaleMultiplier;
    }

    private void OnClickPurchase()
    {
        if (_convenienceConfig != null)
        {
            _purchaseHandler?.Invoke(_convenienceConfig);
        }
    }
}
