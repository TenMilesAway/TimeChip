using UnityEngine;
using UnityEngine.UI;

public class BuffItem : MonoBehaviour
{
    private const float ScaleDivisor = 10000f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtRemain;
    [SerializeField] private Text _txtMulti;

    private int _presentationVersion;

    public void SetData(cfg.BuffConfig buffConfig, ActiveBuffData activeBuff, string resourceTag)
    {
        _presentationVersion++;
        if (buffConfig == null || activeBuff == null || !HasValidUiReferences())
        {
            gameObject.SetActive(false);
            return;
        }

        _txtName.text = buffConfig.Name;
        _txtRemain.text = activeBuff.remainingTurns < 0
            ? "永久"
            : $"剩余{activeBuff.remainingTurns}月";
        _txtMulti.text = $"×{activeBuff.stacks}";
        SetIconAsync(buffConfig, resourceTag, _presentationVersion);
    }

    public void Clear()
    {
        _presentationVersion++;
        if (_icon != null)
        {
            _icon.sprite = null;
        }

        if (_txtName != null)
        {
            _txtName.text = string.Empty;
        }

        if (_txtRemain != null)
        {
            _txtRemain.text = string.Empty;
        }

        if (_txtMulti != null)
        {
            _txtMulti.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    private async void SetIconAsync(cfg.BuffConfig buffConfig, string resourceTag, int presentationVersion)
    {
        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(buffConfig.Icon);
        cfg.Scale scaleConfig = GetScale("buff");
        if (itemConfig == null || scaleConfig == null)
        {
            Debug.LogError($"BUFF 图标配置不存在: [{buffConfig.Id}], [{buffConfig.Icon}]", this);
            Clear();
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(itemConfig.Icon, resourceTag);
        if (presentationVersion != _presentationVersion)
        {
            return;
        }

        if (icon == null)
        {
            Debug.LogError($"BUFF 图标加载失败: [{buffConfig.Id}], [{itemConfig.Icon}]", this);
            Clear();
            return;
        }

        _icon.sprite = icon;
        _icon.SetNativeSize();
        _icon.rectTransform.localScale = Vector3.one *
            (itemConfig.RewardScale / ScaleDivisor) *
            (scaleConfig.ScaleValue / ScaleDivisor);
        gameObject.SetActive(true);
    }

    private bool HasValidUiReferences()
    {
        if (_icon != null && _txtName != null && _txtRemain != null && _txtMulti != null)
        {
            return true;
        }

        Debug.LogError("BuffItem 的 UI 引用未在 Inspector 中完整配置。", this);
        return false;
    }

    private static cfg.Scale GetScale(string scaleName)
    {
        System.Collections.Generic.IReadOnlyList<cfg.Scale> scales = DataTableMananger.GetInstance()
            .Tables.ScaleTable.DataList;
        for (int i = 0; i < scales.Count; i++)
        {
            if (scales[i].Name == scaleName)
            {
                return scales[i];
            }
        }

        Debug.LogError($"缩放配置不存在: [{scaleName}]");
        return null;
    }
}
