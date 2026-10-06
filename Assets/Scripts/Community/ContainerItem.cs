using System;
using UnityEngine;
using UnityEngine.UI;

public class ContainerItem : MonoBehaviour
{
    [SerializeField] private Image _imgContainer;
    [SerializeField] private Image _imgAvatar;
    [SerializeField] private Text _txtCoin;
    [SerializeField] private GameObject _goGroupBuy;
    [SerializeField] private GameObject _goGroupNoBuy;

    private Button _btnBuy;
    private int _iconRequestVersion;

    private void Awake()
    {
        _btnBuy = GetComponentInChildren<Button>(true);
        if (_btnBuy == null)
        {
            Debug.LogError("ContainerItem 未找到购买按钮。", this);
        }
    }

    public void SetData(
        PlayerPortContainerOffer offer,
        string iconPath,
        Action buyAction)
    {
        if (offer == null)
        {
            Debug.LogError("港口集装箱数据为空。", this);
            return;
        }

        _iconRequestVersion++;
        _txtCoin.text = offer.price.ToString();
        _goGroupBuy.SetActive(!offer.purchased);
        _goGroupNoBuy.SetActive(offer.purchased);

        if (_btnBuy != null)
        {
            _btnBuy.onClick.RemoveAllListeners();
            _btnBuy.interactable = !offer.purchased;
            if (!offer.purchased)
            {
                _btnBuy.onClick.AddListener(() => buyAction?.Invoke());
            }
        }

        LoadContainerIconAsync(iconPath, _iconRequestVersion);
    }

    public void Clear()
    {
        _iconRequestVersion++;
        if (_btnBuy != null)
        {
            _btnBuy.onClick.RemoveAllListeners();
        }
    }

    private async void LoadContainerIconAsync(string iconPath, int requestVersion)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (requestVersion != _iconRequestVersion || icon == null)
        {
            return;
        }

        _imgContainer.sprite = icon;
    }
}
