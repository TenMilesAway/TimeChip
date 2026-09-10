using System;
using UnityEngine;
using UnityEngine.UI;

public class CommunityRedundancyItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _txtNum;      // 数量, 例: "冗余：1"
    [SerializeField] private Button _btnSelect; // 选择按钮, 选择后回退到提交界面

    private Vector3 _defaultIconScale;
    private cfg.Item _itemConfig;
    private Action<cfg.Item> _selectHandler;
    private int _presentationVersion;

    private void Awake()
    {
        _defaultIconScale = Vector3.one;
        _btnSelect.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        if (_btnSelect != null)
        {
            _btnSelect.onClick.RemoveListener(Select);
        }
    }

    public void SetData(
        cfg.Item itemConfig,
        int ownedCount,
        float communityCentreScale,
        Action<cfg.Item> selectHandler)
    {
        _presentationVersion++;
        _itemConfig = itemConfig;
        _selectHandler = selectHandler;
        if (itemConfig == null || ownedCount <= 1)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtNum.text = $"冗余：{ownedCount - 1}";
        _icon.sprite = null;
        LoadIconAsync(itemConfig.Icon, communityCentreScale, _presentationVersion);
    }

    private async void LoadIconAsync(
        string iconPath,
        float communityCentreScale,
        int presentationVersion)
    {
        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (presentationVersion != _presentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"社区冗余道具图标加载失败: [{_itemConfig.Id}], [{iconPath}]", this);
            return;
        }

        _icon.sprite = icon;
        _icon.SetNativeSize();
        _icon.rectTransform.localScale = _defaultIconScale *
            (_itemConfig.RewardScale / ScaleDivisor) * communityCentreScale;
    }

    private void Select()
    {
        if (_itemConfig != null)
        {
            _selectHandler?.Invoke(_itemConfig);
        }
    }
}
