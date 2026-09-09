using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrefabTextFontReplacementTool : EditorWindow
{
    private Font _targetFont;
    private int _prefabCount;
    private int _sceneCount;
    private int _prefabTextCount;
    private int _sceneTextCount;
    private int _prefabReplaceCount;
    private int _sceneReplaceCount;
    private bool _hasPreview;

    [MenuItem("Tools/字体/替换所有预制体 Text 字体")]
    public static void Open()
    {
        GetWindow<PrefabTextFontReplacementTool>("替换 Text 字体");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("批量替换 Text 字体", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "扫描 Assets 下所有预制体和场景（包含未激活对象），将旧版 Unity UI Text 组件的 Font 替换为所选字体。TextMeshPro 组件不受影响。",
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

            if (GUILayout.Button("替换所有预制体和场景中的 Text 字体", GUILayout.Height(30)))
            {
                ReplaceFonts();
            }
        }

        if (_hasPreview)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"扫描到 {_prefabCount} 个预制体、{_sceneCount} 个场景。");
            EditorGUILayout.LabelField($"预制体中有 {_prefabTextCount} 个 Text，场景中有 {_sceneTextCount} 个 Text。");
            EditorGUILayout.LabelField($"将替换 {_prefabReplaceCount} 个预制体 Text、{_sceneReplaceCount} 个场景 Text 的字体。");
        }
    }

    private void CollectPreview()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        _prefabCount = prefabGuids.Length;
        _prefabTextCount = 0;
        _prefabReplaceCount = 0;

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            Text[] texts = prefab.GetComponentsInChildren<Text>(true);
            _prefabTextCount += texts.Length;
            foreach (Text text in texts)
            {
                if (text.font != _targetFont)
                {
                    _prefabReplaceCount++;
                }
            }
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        _sceneCount = sceneGuids.Length;
        _sceneTextCount = 0;
        _sceneReplaceCount = 0;

        try
        {
            foreach (string sceneGuid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                bool wasOpenedByTool;
                Scene scene = GetOrOpenScene(scenePath, out wasOpenedByTool);

                try
                {
                    CollectScenePreview(scene);
                }
                finally
                {
                    if (wasOpenedByTool)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        _hasPreview = true;
    }

    private void ReplaceFonts()
    {
        CollectPreview();
        if (_prefabReplaceCount == 0 && _sceneReplaceCount == 0)
        {
            EditorUtility.DisplayDialog("无需替换", "所有预制体和场景中的 Text 组件已经使用所选字体。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "确认替换字体",
                $"将修改 {_prefabReplaceCount} 个预制体 Text 和 {_sceneReplaceCount} 个场景 Text。\n此操作会直接保存对应的预制体和场景文件。",
                "确认替换",
                "取消"))
        {
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int modifiedPrefabCount = 0;
        int modifiedTextCount = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        int modifiedSceneCount = 0;
        int modifiedSceneTextCount = 0;

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

            for (int index = 0; index < sceneGuids.Length; index++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[index]);
                EditorUtility.DisplayProgressBar(
                    "替换场景 Text 字体",
                    scenePath,
                    (float)index / sceneGuids.Length);

                bool wasOpenedByTool;
                Scene scene = GetOrOpenScene(scenePath, out wasOpenedByTool);
                try
                {
                    int sceneTextChanges = ReplaceSceneFonts(scene);
                    if (sceneTextChanges > 0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        modifiedSceneCount++;
                        modifiedSceneTextCount += sceneTextChanges;
                    }
                }
                finally
                {
                    if (wasOpenedByTool)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CollectPreview();

        Debug.Log(
            $"Text 字体替换完成：修改 {modifiedPrefabCount} 个预制体中的 {modifiedTextCount} 个 Text 组件，"
            + $"{modifiedSceneCount} 个场景中的 {modifiedSceneTextCount} 个 Text 组件。");
        EditorUtility.DisplayDialog(
            "替换完成",
            $"已修改 {modifiedPrefabCount} 个预制体中的 {modifiedTextCount} 个 Text 组件，"
            + $"{modifiedSceneCount} 个场景中的 {modifiedSceneTextCount} 个 Text 组件。",
            "确定");
    }

    private void CollectScenePreview(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            _sceneTextCount += texts.Length;

            foreach (Text text in texts)
            {
                if (text.font != _targetFont)
                {
                    _sceneReplaceCount++;
                }
            }
        }
    }

    private int ReplaceSceneFonts(Scene scene)
    {
        int changes = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font == _targetFont)
                {
                    continue;
                }

                text.font = _targetFont;
                changes++;
            }
        }

        return changes;
    }

    private static Scene GetOrOpenScene(string scenePath, out bool wasOpenedByTool)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        wasOpenedByTool = !scene.isLoaded;
        return wasOpenedByTool
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
            : scene;
    }
}
