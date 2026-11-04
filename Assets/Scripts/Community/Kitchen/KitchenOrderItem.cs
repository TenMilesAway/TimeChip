using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KitchenOrderItem : MonoBehaviour
{
    [SerializeField] private Text _txtOrderName;        // 订单需求
    [SerializeField] private Text _txtProgress;         // 订单进度, 例如: 1 / 3
    [SerializeField] private Image _imgAvatar;          // 顾客头像
    [SerializeField] private Image _imgFood;            // 订单图像
    [SerializeField] private Button _btnComplete;       // 提交订单

    [SerializeField] private GameObject _goOrder;       // 订单 GO
    [SerializeField] private GameObject _goCompleted;   // 订单完成 GO
}
