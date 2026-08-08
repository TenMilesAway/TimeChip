using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AutoRecycleData
{
    public string _itemName;
    public int _recycleTime;
}


/// <summary>
/// 此类可以不使用, 无本地配置需求
/// </summary>
public class AutoRecycleConfig : IDisposable
{
    private Dictionary<string, float> _recycleTimeDic = new Dictionary<string, float>();
    private bool _hasInit;
    private bool _disposed;

    #region 资源释放
    ~AutoRecycleConfig()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _recycleTimeDic.Clear();
            }

            _disposed = true;
            _hasInit = false;
        }
    }
    #endregion
}