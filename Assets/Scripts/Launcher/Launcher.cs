using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 启动器
/// </summary>
public class Launcher : SingletonMono<Launcher>
{
    private LauncherProcess process = LauncherProcess.None;

    private void Start()
    {
        process = LauncherProcess.PreloadBegin;
    }

    private void Update()
    {
        switch (process)
        {
            case LauncherProcess.PreloadBegin:
                {
                    process = LauncherProcess.PreloadIng;
                }
                break;
            case LauncherProcess.PreloadIng:
                {

                }
                break;
            case LauncherProcess.PreloadEnd:
                {
                    
                }
                break;
            case LauncherProcess.ConnectBegin:
                {
                    process = LauncherProcess.ConnectIng;
                }
                break;
            case LauncherProcess.ConnectIng:
                {
                        
                }
                break;
            case LauncherProcess.ConnectEnd:
                {

                }
                break;
            case LauncherProcess.InitProgressBegin:
                {
                    process = LauncherProcess.InitProgressIng;
                }
                break;
            case LauncherProcess.InitProgressIng:
                {
                    
                }
                break;
            case LauncherProcess.InitProgressEnd:
                {
                    
                }
                break;
            case LauncherProcess.InitDataBegin:
                {
                    process = LauncherProcess.InitDataIng;

                }
                break;
            case LauncherProcess.InitDataIng:
                {
                    
                }
                break;
            case LauncherProcess.InitDataEnd:
                {
                    
                }
                break;
            case LauncherProcess.SwitchSceneBegin:
                {
                    process = LauncherProcess.SwitchSceneIng;
                }
                break;
            case LauncherProcess.SwitchSceneIng:
                {
                    
                }
                break;
            case LauncherProcess.SwitchSceneEnd:
                {
                    process = LauncherProcess.None;
                }
                break;
            default:
                {
                        
                }
                break;
        }
    }

    #region 主要方法
    /// <summary>
    /// 修改 Launcher 的状态
    /// </summary>
    private void SetProcessState(LauncherProcess state)
    {
        process = state;
    }
    #endregion

}

/// <summary>
/// 启动状态枚举类
/// </summary>
public enum LauncherProcess
{
    None,

    // 预加载：一些配置、资源、道具配置表等
    PreloadBegin,
    PreloadIng,
    PreloadEnd,

    // 连接服务器
    ConnectBegin,
    ConnectIng,
    ConnectEnd,

    // 进度界面
    InitProgressBegin,
    InitProgressIng,
    InitProgressEnd,

    // 初始化数据
    InitDataBegin,
    InitDataIng,
    InitDataEnd,

    // 切换地图
    SwitchSceneBegin,
    SwitchSceneIng,
    SwitchSceneEnd,
}