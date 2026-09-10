using System;
using UnityEngine;
using UnityEngine.UI;

public class CommunityCentreNeedItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Text _txtName;     // 物品名称
    [SerializeField] private Text _txtNum;      // 数量, 例: 10 / <color=white>3</color>
    [SerializeField] private Image _imgIcon;    // 物品图标
    [SerializeField] private Button _btnSubmit; // 提交按钮

    [SerializeField] private GameObject _goUnSubmit;  // 未提交时显示
    [SerializeField] private GameObject _goSubmit;    // 提交后显示

    private int _presentationVersion;
    private cfg.Item _itemConfig;
    private Action _submitHandler;

    private void Awake()
    {
        _btnSubmit.onClick.AddListener(Submit);
    }

    private void OnDestroy()
    {
        if (_btnSubmit != null)
        {
            _btnSubmit.onClick.RemoveListener(Submit);
        }
    }

    public void SetData(
        cfg.Item itemConfig,
        bool submitted,
        int ownedCount,
        float communityCentreScale,
        Action submitHandler)
    {
        _presentationVersion++;
        _itemConfig = itemConfig;
        _submitHandler = submitHandler;
        if (itemConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtName.text = itemConfig.Name;
        _txtNum.text = $"拥有 {ownedCount}/1";
        _goUnSubmit.SetActive(!submitted);
        _goSubmit.SetActive(submitted);
        _btnSubmit.interactable = !submitted;
        _imgIcon.sprite = null;
        LoadIconAsync(itemConfig, communityCentreScale, _presentationVersion);
    }

    private async void LoadIconAsync(
        cfg.Item itemConfig,
        float communityCentreScale,
        int presentationVersion)
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
            Debug.LogError($"社区需求道具图标加载失败: [{itemConfig.Id}], [{itemConfig.Icon}]", this);
            return;
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / ScaleDivisor) * communityCentreScale;
    }

    private void Submit()
    {
        _submitHandler?.Invoke();
    }
}