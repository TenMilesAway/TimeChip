using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityCentreLevelUpItem : MonoBehaviour
{
    [SerializeField] private Text _txtName;        // 提案名称
    [SerializeField] private Text _txtDescription; // 提案增益描述
    [SerializeField] private Image _imgIcon;       // 提案图标
    [SerializeField] private Button _btnLevelUp;   // 选择按钮

    private int _presentationVersion;
    private cfg.CommunityCentre _proposalConfig;
    private System.Action<cfg.CommunityCentre> _selectHandler;

    private void Awake()
    {
        _btnLevelUp.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        if (_btnLevelUp != null)
        {
            _btnLevelUp.onClick.RemoveListener(Select);
        }
    }

    public void SetData(
        cfg.CommunityCentre proposalConfig,
        cfg.BuffConfig buffConfig,
        System.Action<cfg.CommunityCentre> selectHandler)
    {
        _presentationVersion++;
        _proposalConfig = proposalConfig;
        _selectHandler = selectHandler;
        if (proposalConfig == null || buffConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _txtName.text = buffConfig.Name;
        _txtDescription.text = buffConfig.Desc;
        _imgIcon.sprite = null;
        _btnLevelUp.interactable = true;
        LoadIconAsync(proposalConfig.Icon, _presentationVersion);
    }

    private async void LoadIconAsync(string iconPath, int presentationVersion)
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
            Debug.LogError($"社区提案图标加载失败: [{_proposalConfig.Id}], [{iconPath}]", this);
            return;
        }

        _imgIcon.sprite = icon;
    }

    private void Select()
    {
        if (_proposalConfig != null)
        {
            _selectHandler?.Invoke(_proposalConfig);
        }
    }
}
