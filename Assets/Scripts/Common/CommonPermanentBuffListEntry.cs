using UnityEngine;
using UnityEngine.UI;

public class CommonPermanentBuffListEntry : MonoBehaviour
{
    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtDetail;

    public void SetData(cfg.BuffConfig buffConfig)
    {
        if (buffConfig == null ||
            _txtName == null ||
            _txtDetail == null)
        {
            Debug.LogError("CommonPermanentBuffListEntry 需要有效的 BUFF 配置与 UI 引用。", this);
            gameObject.SetActive(false);
            return;
        }

        _txtName.text = buffConfig.Name;
        _txtDetail.text = buffConfig.Desc;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (_txtName != null)
        {
            _txtName.text = string.Empty;
        }

        if (_txtDetail != null)
        {
            _txtDetail.text = string.Empty;
        }

        gameObject.SetActive(false);
    }
}
