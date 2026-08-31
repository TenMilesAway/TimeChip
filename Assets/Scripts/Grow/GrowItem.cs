using System;
using UnityEngine;
using UnityEngine.UI;

public class GrowItem : MonoBehaviour
{
    [SerializeField] private Button _btnUnlock;
    [SerializeField] private GameObject _goLock;

    private int _growId;
    private Action<int> _selectAction;

    private void Awake()
    {
        _btnUnlock.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        if (_btnUnlock != null)
        {
            _btnUnlock.onClick.RemoveListener(Select);
        }
    }

    /// <summary>绑定顺序对应的成长配置与卡牌状态。</summary>
    public void SetData(cfg.Grow growConfig, GrowCardData cardData, Action<int> selectAction)
    {
        if (growConfig == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _growId = growConfig.Id;
        _selectAction = selectAction;
        _goLock.SetActive(cardData == null || !cardData.isUnlocked);
        gameObject.SetActive(true);
    }

    private void Select()
    {
        _selectAction?.Invoke(_growId);
    }
}
