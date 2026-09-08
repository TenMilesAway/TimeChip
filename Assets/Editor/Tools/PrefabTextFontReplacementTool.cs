using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PrefabTextFontReplacementTool : EditorWindow
{
    private Font _targetFont;
    private int _prefabCount;
    private int _textCount;
    private int _replaceCount;
    private bool _hasPreview;

    [MenuItem("Tools/字体/替换所有预制体 Text 字体")]
    public static void Open()
    {
        GetWindow<PrefabTextFontReplacementTool>("替换 Text 字体");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("批量替换预制体 Text 字体", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "扫描 Assets 下所有预制体（包含未激活对象），将旧版 Unity UI Text 组件的 Font 替换为所选字体。TextMeshPro 组件不受影响。",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _targetFont = (Font)EditorGUILayout.ObjectField("目标字体", _targetFont, typeof(Font), false);
        if (EditorGUI.EndChangeCheck())
        {
            _hasPreview = false;
        }

        using (new EditorGUI.DisabledScope(_targetFont == null))
        {
            if (GUILayout.Button("统计影响范围"))
            {
                CollectPreview();
            }

            if (GUILayout.Button("替换所有预制体中的 Text 字体", GUILayout.Height(30)))
            {
                ReplaceFonts();
            }
        }

        if (_hasPreview)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"扫描到 {_prefabCount} 个预制体、{_textCount} 个 Text 组件。");
            EditorGUILayout.LabelField($"将替换 {_replaceCount} 个 Text 组件的字体。");
        }
    }

    private void CollectPreview()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        _prefabCount = prefabGuids.Length;
        _textCount = 0;
        _replaceCount = 0;

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            Text[] texts = prefab.GetComponentsInChildren<Text>(true);
            _textCount += texts.Length;
            foreach (Text text in texts)
            {
                if (text.font != _targetFont)
                {
                    _replaceCount++;
                }
            }
        }

        _hasPreview = true;
    }

    private void ReplaceFonts()
    {
        CollectPreview();
        if (_replaceCount == 0)
        {
            EditorUtility.DisplayDialog("无需替换", "所有预制体中的 Text 组件已经使用所选字体。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "确认替换字体",
                $"将修改 {_prefabCount} 个预制体中的 {_replaceCount} 个 Text 组件。\n此操作会直接保存预制体资源。",
                "确认替换",
                "取消"))
        {
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int modifiedPrefabCount = 0;
        int modifiedTextCount = 0;

        try
        {
            for (int index = 0; index < prefabGuids.Length; index++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);
                EditorUtility.DisplayProgressBar(
                    "替换预制体 Text 字体",
                    prefabPath,
                    (float)index / prefabGuids.Length);

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    bool isModified = false;
                    foreach (Text text in prefabRoot.GetComponentsInChildren<Text>(true))
                    {
                        if (text.font == _targetFont)
                        {
                            continue;
                        }

                        text.font = _targetFont;
                        modifiedTextCount++;
                        isModified = true;
                    }

                    if (isModified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                        modifiedPrefabCount++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _textCount = modifiedTextCount;
        _replaceCount = 0;
        _hasPreview = true;

        Debug.Log($"Text 字体替换完成：修改 {modifiedPrefabCount} 个预制体、{modifiedTextCount} 个 Text 组件。");
        EditorUtility.DisplayDialog(
            "替换完成",
            $"已修改 {modifiedPrefabCount} 个预制体中的 {modifiedTextCount} 个 Text 组件。",
            "确定");
    }
}
