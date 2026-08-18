using UnityEngine;
using UnityEngine.UI;

public class HomeItem : MonoBehaviour
{
    [SerializeField] private GameObject _loadComponent;    // 加载组合
    [SerializeField] private GameObject _itemComponent;    // Item 组合
    [SerializeField] private Image _icon;                  // Item 图片
    [SerializeField] private Text _satisfactionText;       // 满意度文本
    [SerializeField] private Text _priceText;              // 价格文本

    private int _presentationVersion;                      // 加载版本

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(cfg.Home homeConfig, string resourceTag)
    {
        _presentationVersion++;

        if (!HasValidUiReferences())
        {
            gameObject.SetActive(false);
            return;
        }

        if (homeConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetLoadingState(true);

        if (_satisfactionText != null) _satisfactionText.text = $"+{homeConfig.Satisfaction:0.##}";
        if (_priceText != null) _priceText.text = $"-{homeConfig.Price}";

        LoadIconAsync(homeConfig, resourceTag, _presentationVersion);
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        _presentationVersion++;

        if (HasValidUiReferences())
        {
            SetLoadingState(true);
            _icon.sprite = null;
            _satisfactionText.text = string.Empty;
            _priceText.text = string.Empty;
        }
    }

    /// <summary>
    /// 异步加载图片
    /// </summary>
    private async void LoadIconAsync(cfg.Home homeConfig, string resourceTag, int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(homeConfig.Sprite, resourceTag);

        if (presentationVersion != _presentationVersion) return;

        if (icon == null)
        {
            Debug.LogError($"家具图标加载失败: [{homeConfig.Id}], [{homeConfig.Sprite}].");
            return;
        }

        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.SetNativeSize();
            _icon.rectTransform.localScale = Vector3.one * (homeConfig.HomeStoreScale / 10000f);
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// UI 引用是否有效
    /// </summary>
    private bool HasValidUiReferences()
    {
        if (_loadComponent != null &&
            _itemComponent != null &&
            _icon != null &&
            _satisfactionText != null &&
            _priceText != null)
        {
            return true;
        }

        Debug.LogError("HomeItem 的 UI 引用未在 Inspector 中完整配置");
        return false;
    }

    /// <summary>
    /// 设置加载状态
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        _loadComponent.SetActive(isLoading);
        _itemComponent.SetActive(!isLoading);
    }

}
