using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单例类
/// 适用: 管理器类 / 数据类等不需要挂载在 GameObject 上的类
/// </summary>
public class Singleton<T> where T:new()
{
    private static T _instance;
    private static object mutex = new object();

    // 双重锁检查
    public static T GetInstance()
    {
        if (_instance == null)
        {
            lock (mutex)
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
            }
        }
            
        return _instance;
    }
}
