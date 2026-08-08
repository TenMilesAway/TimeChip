using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂载在 GameObject 上的单例类
/// 适用: 需要使用 Unity 生命周期函数的类, 保证唯一
/// </summary>
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static object mutex = new object();

    public static T GetInstance()
    {
        if (instance == null)
        {
            lock (mutex)
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).ToString();
                    DontDestroyOnLoad(obj);
                    instance = obj.AddComponent<T>();
                }
            }
        }
        return instance;
    }

}
