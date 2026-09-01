using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DataTableMananger : Singleton<DataTableMananger>
{
    private cfg.Tables _tables;
    private readonly Dictionary<string, JArray> _tableJsonCache = new Dictionary<string, JArray>();
    private Task _initTask;

    private const string TableAssetDirectory = "Assets/TableDatas";
    private const string TableAssetExtension = ".json";
    private const string ResourceTag = "DataTableManager";
    private static readonly string[] TableNames =
    {
        "item",
        "lottery",
        "home",
        "mission",
        "base",
        "scale",
        "work",
        "convenience",
        "buffConfig",
        "grow"
    };

    #region Getter
    /// <summary>
    /// 所有配置表
    /// </summary>
    public cfg.Tables Tables => _tables;
    #endregion

    public Task Init()
    {
        if (_tables != null)
        {
            return Task.CompletedTask;
        }

        if (_initTask == null)
        {
            _initTask = InitInternalAsync();
        }

        return _initTask;
    }

    private async Task InitInternalAsync()
    {
        if (GameManager.Resource == null)
        {
            Debug.LogError("ResourceComponent 未初始化，无法加载数据表");
            _initTask = null;
            return;
        }

        _tableJsonCache.Clear();
        for (int index = 0; index < TableNames.Length; index++)
        {
            string tableName = TableNames[index];
            string key = $"{TableAssetDirectory}/{tableName}{TableAssetExtension}";
            TextAsset tableAsset = await GameManager.Resource.LoadResource<TextAsset>(key, ResourceTag);
            if (tableAsset == null)
            {
                Debug.LogError($"数据表加载失败，未找到 Addressables 资源：{key}");
                continue;
            }

            try
            {
                _tableJsonCache[tableName] = JArray.Parse(tableAsset.text);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"数据表解析失败：{key}，异常：{exception}");
            }
        }

        _tables = new cfg.Tables(fileName =>
        {
            if (_tableJsonCache.TryGetValue(fileName, out JArray tableJson))
            {
                return tableJson;
            }

            Debug.LogError($"数据表未预加载：{fileName}");
            return new JArray();
        });
    }
}
