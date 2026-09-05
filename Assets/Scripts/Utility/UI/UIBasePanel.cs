using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 打开 UI 时传递的参数
/// </summary>
public class OpenUIParam
{
    public object data;
    public Action callback;
    public bool rewardsAlreadyGranted; // 奖励是否已在打开面板前结算
}

/// <summary>
/// UI 面板基类
/// </summary>
public abstract class UIBasePanel : MonoBehaviour
{
    [HideInInspector] public bool _isBlockingWindow = true;

    /// <summary>
    /// 初始化
    /// </summary>
    public void OnInit(OpenUIParam param)
    {
        InitHandle(param);
    }

    protected virtual void InitHandle(OpenUIParam param)
    {
            
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void OnClose()
    {
        OnHide();
        CloseHandle();
    }

    protected virtual void CloseHandle()
    {
        
    }

    /// <summary>
    /// 显示
    /// </summary>
    public void OnShow()
    {
        gameObject.SetActive(true);
        ShowHandle();
    }

    protected virtual void ShowHandle()
    {
        
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void OnHide()
    {
        gameObject.SetActive(false);
        HideHandle();
    }

    protected virtual void HideHandle()
    {

    }

    protected virtual void OnDestroy()
    {
        GameManager.Resource.Release(GetInstanceID().ToString());
    }

    public abstract string GetPanelName();
}