using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataTableMananger : Singleton<DataTableMananger>
{
    private cfg.Tables _tables;

    private const string TableDataDirectory = "TableDatas";

    #region Getter
    /// <summary>
    /// 所有配置表
    /// </summary>
    public cfg.Tables Tables => _tables;
    #endregion

    public void Init()
    {
        string tableDataPath = Path.Combine(Application.dataPath, TableDataDirectory);

        _tables = new cfg.Tables(fileName =>
        {
            string filePath = Path.Combine(tableDataPath, $"{fileName}.json");
            return JArray.Parse(File.ReadAllText(filePath));
        });
    }
}
