using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static readonly List<BaseComponent> _Components = new List<BaseComponent>();

    /// <summary>
    /// 网络
    /// </summary>
    //public static NetworkComponent Network { get; private set; }

    /// <summary>
    /// 事件
    /// </summary>
    public static EventComponent Event { get; private set; }

    /// <summary>
    /// 时间
    /// </summary>
    public static TimerComponent Timer { get; private set; }

    /// <summary>
    /// 延迟任务
    /// </summary>
    public static DelayedTaskComponent DelayedTask { get; private set; }

    /// <summary>
    /// 配置表
    /// </summary>
    //public static DataTableComponent DataTable { get; private set; }

    /// <summary>
    /// 控制台输出
    /// </summary>
    //public static ConsoleComponent Console { get; private set; }

    /// <summary>
    /// 资源加载
    /// </summary>
    public static ResourceComponent Resource { get; private set; }

    /// <summary>
    /// 音频
    /// </summary>
    public static AudioComponent Audio { get; private set; }

    /// <summary>
    /// 控制流程的有限状态机
    /// </summary>
    public static FsmComponent Fsm { get; private set; }

    /// <summary>
    /// 存储部分全局数据
    /// </summary>
    public static GlobalDataComponent GlobalData { get; private set; }

    private void Start()
    {
        InitComponents();
    }

    /// <summary>
    /// 注册组件
    /// </summary>
    public static void RegisterComponent(BaseComponent component)
    {
        if (component == null)
        {
            Debug.LogError("Game Manager's component is invalid");
            return;
        }

        Type type = component.GetType();

        foreach (BaseComponent current in _Components)
        {
            if (current != null && current.GetType() == type)
            {
                Debug.LogErrorFormat("Game Mananger's component type '{0}' is already exist.", type.FullName);
                return;
            }
        }

        _Components.Add(component);
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private static void InitComponents()
    {
        //Network     = GetTargetComponent<NetworkComponent>();
        Event       = GetTargetComponent<EventComponent>();
        Timer       = GetTargetComponent<TimerComponent>();
        DelayedTask = GetTargetComponent<DelayedTaskComponent>();
        //DataTable   = GetTargetComponent<DataTableComponent>();
        //Console     = GetTargetComponent<ConsoleComponent>();
        Resource    = GetTargetComponent<ResourceComponent>();
        Audio       = GetTargetComponent<AudioComponent>();
        Fsm         = GetTargetComponent<FsmComponent>();
        GlobalData  = GetTargetComponent<GlobalDataComponent>();

        Audio.Init();
    }

    /// <summary>
    /// 获得指定类型的 Component
    /// </summary>
    private static T GetTargetComponent<T>() where T : BaseComponent
    {
        return (T)GetTargetComponent(typeof(T));
    }

    private static BaseComponent GetTargetComponent(Type type)
    {
        foreach (BaseComponent current in _Components)
        {
            if (current != null && current.GetType() == type)
            {
                return current;
            }
        }

        return null;
    }
}
