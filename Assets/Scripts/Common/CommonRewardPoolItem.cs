using UnityEngine;
using UnityEngine.UI;

public class CommonRewardPoolItem : MonoBehaviour
{
    [SerializeField] private Text _txtProb;
    [SerializeField] private Text _txtNum;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Button _btnDetail;
    [SerializeField] private GameObject _goBigReward;

    private int _itemId;

    private void Awake()
    {
        UiButtonAudio.Bind(_btnDetail, OpenItemDetail);
    }

    private void OnDestroy()
    {
        if (_btnDetail != null)
        {
            _btnDetail.onClick.RemoveListener(OpenItemDetail);
        }
    }

    public void Configure(
        int itemId,
        int itemCount,
        Sprite icon,
        float iconScale,
        string probabilityText,
        bool isGrandPrize)
    {
        _itemId = itemId;
        if (_txtNum != null)
        {
            _txtNum.text = itemCount.ToString();
        }

        if (_goBigReward != null)
        {
            _goBigReward.SetActive(isGrandPrize);
        }

        if (_txtProb != null)
        {
            _txtProb.gameObject.SetActive(!isGrandPrize);
            if (!isGrandPrize)
            {
                _txtProb.text = probabilityText;
            }
        }

        _imgIcon.sprite = icon;
        _imgIcon.SetNativeSize();
        _imgIcon.rectTransform.localScale = Vector3.one * iconScale;

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        _btnDetail.interactable = tables.ItemTable.GetOrDefault(itemId) != null ||
            tables.BaseTable.GetOrDefault(itemId) != null;
    }

    private void OpenItemDetail()
    {
        if (_itemId <= 0)
        {
            Debug.LogError($"奖池道具详情配置不存在: [{_itemId}]", this);
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables.ItemTable.GetOrDefault(_itemId) == null &&
            tables.BaseTable.GetOrDefault(_itemId) == null)
        {
            Debug.LogError($"奖池道具详情配置不存在: [{_itemId}]", this);
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonItemDetailView,
            param: new OpenUIParam { data = _itemId });
    }
}
