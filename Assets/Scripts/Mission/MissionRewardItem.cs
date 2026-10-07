using UnityEngine;
using UnityEngine.UI;

public class MissionRewardItem : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Text _txtNum;
    [SerializeField] private Button _btnDetail;

    private int _itemId;

    private void Awake()
    {
        _btnDetail.onClick.AddListener(OpenItemDetail);
    }

    private void OnDestroy()
    {
        if (_btnDetail != null)
        {
            _btnDetail.onClick.RemoveListener(OpenItemDetail);
        }
    }

    public void SetData(int itemId, Sprite icon, int amount, float scale)
    {
        _itemId = itemId;
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        _btnDetail.interactable = tables.ItemTable.GetOrDefault(itemId) != null ||
            tables.BaseTable.GetOrDefault(itemId) != null;
        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * scale;
        _txtNum.text = amount.ToString();
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        _itemId = 0;
        _imgIcon.sprite = null;
        _txtNum.text = string.Empty;
        _btnDetail.interactable = false;
        gameObject.SetActive(false);
    }

    private void OpenItemDetail()
    {
        if (_itemId <= 0)
        {
            Debug.LogError($"任务奖励详情配置不存在: [{_itemId}]", this);
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables.ItemTable.GetOrDefault(_itemId) == null &&
            tables.BaseTable.GetOrDefault(_itemId) == null)
        {
            Debug.LogError($"任务奖励详情配置不存在: [{_itemId}]", this);
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            param: new OpenUIParam { data = _itemId });
    }
}
