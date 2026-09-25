using System;
using UnityEngine;
using UnityEngine.UI;

public class FishStoreItem : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtPrice;
    [SerializeField] private Text _txtCharge;
    [SerializeField] private Button _btnCharge;

    private int _presentationVersion;
    private cfg.FishStore _storeConfig;
    private Action<cfg.FishStore> _purchaseHandler;

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
        cfg.FishStore storeConfig,
        cfg.Item itemConfig,
        int remainingCount,
        Action<cfg.FishStore> purchaseHandler)
    {
        _presentationVersion++;
        _storeConfig = storeConfig;
        _purchaseHandler = purchaseHandler;
        if (storeConfig == null || itemConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtName.text = storeConfig.Name;
        _txtPrice.text = BuffSystem.GetInstance()
            .CalculateShopPrice(storeConfig.Price)
            .ToString();
        bool isSoldOut = remainingCount <= 0;
        _txtCharge.text = isSoldOut ? "购买上限" : "购买";
        _btnCharge.interactable = !isSoldOut;
        _imgIcon.sprite = null;
        LoadIconAsync(itemConfig.Icon, _presentationVersion);
    }

    private async void LoadIconAsync(string iconPath, int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (presentationVersion != _presentationVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"河边商店商品图标加载失败: [{_storeConfig.Id}], [{iconPath}]", this);
            return;
        }

        _imgIcon.sprite = icon;
    }

    private void OnClickPurchase()
    {
        if (_storeConfig == null || !_btnCharge.interactable)
        {
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
        _purchaseHandler?.Invoke(_storeConfig);
    }
}
