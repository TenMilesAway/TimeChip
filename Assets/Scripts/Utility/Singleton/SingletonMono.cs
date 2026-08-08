using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 继承 Mono 的单例类
/// 适用: 需要使用 Unity 生命周期函数的类
/// </summary>
public class SingletonMono<T> : MonoBehaviour where T: MonoBehaviour
{
    private static T instance;
    private static object mutex = new object();

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            lock (mutex)
            {
                if (instance == null)
                {
                    instance = this as T;
                }
            }
        }
    }
	
}
