using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeDetailShowItem : MonoBehaviour
{
    [SerializeField] private Text _txtSatisfaction;
    [SerializeField] private Text _txtBuff;
    [SerializeField] private GameObject _goUnlockGroup;
    [SerializeField] private GameObject _goLockGroup;

    public bool SetData(cfg.HomeSatisfactionBuff tier, cfg.BuffConfig buffConfig, bool isUnlocked)
    {
        if (tier == null || buffConfig == null)
        {
            Debug.LogError("HomeDetailShowItem 需要有效的满意度档位与 BUFF 配置", this);
            gameObject.SetActive(false);
            return false;
        }

        if (_txtSatisfaction == null ||
            _txtBuff == null ||
            _goUnlockGroup == null ||
            _goLockGroup == null)
        {
            Debug.LogError("HomeDetailShowItem 的 UI 引用未在 Inspector 中完整配置", this);
            gameObject.SetActive(false);
            return false;
        }

        gameObject.SetActive(true);
        _txtSatisfaction.text = $"{tier.MinSatisfaction:0.##}%";
        _txtBuff.text = buffConfig.Desc;
        _goUnlockGroup.SetActive(isUnlocked);
        _goLockGroup.SetActive(!isUnlocked);
        return true;
    }
}
