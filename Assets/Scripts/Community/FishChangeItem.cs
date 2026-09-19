using System;
using UnityEngine;
using UnityEngine.UI;

public class FishChangeItem : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _num;
    [SerializeField] private Button _btnChange;  // 更换装备

    private int _presentationVersion;
    private cfg.Item _itemConfig;
    private Action<cfg.Item> _changeHandler;

    private void Awake()
    {
        _btnChange.onClick.AddListener(OnClickChange);
    }

    private void OnDestroy()
    {
        if (_btnChange != null)
        {
            _btnChange.onClick.RemoveListener(OnClickChange);
        }
    }

    public void SetData(
        cfg.Item itemConfig,
        int itemCount,
        bool showCount,
        Action<cfg.Item> changeHandler)
    {
        _presentationVersion++;
        _itemConfig = itemConfig;
        _changeHandler = changeHandler;
        bool hasItem = itemConfig != null;
        _icon.sprite = null;
        _icon.enabled = hasItem;
        _num.gameObject.SetActive(hasItem && showCount);
        _btnChange.interactable = hasItem;
        if (!hasItem)
        {
            return;
        }

        if (showCount)
        {
            _num.text = itemCount.ToString();
        }

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
            Debug.LogError($"更换装备道具图标加载失败: [{_itemConfig.Id}], [{iconPath}]", this);
            return;
        }

        _icon.sprite = icon;
    }

    private void OnClickChange()
    {
        if (_itemConfig != null)
        {
            _changeHandler?.Invoke(_itemConfig);
        }
    }
}
