using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AudioDefineGenTool
{
    private static readonly string AudioDataSOPath = "Assets/Audio/AudioDataSO.asset";
    private static readonly string AudioDefinePath = "Assets/Scripts/Utility/Audio/AudioDefine.cs";

    [MenuItem("Assets/生成音频配置")]
    public static void GenAudioDefine()
    {
        if (Selection.objects.Length == 0)
        {
            return;
        }

        Debug.LogFormat("当前选择文件夹: {0}", Selection.objects[0].name);

        Dictionary<string, AudioData> audioDataDic = new Dictionary<string, AudioData>();

        if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.objects[0])))
        {
            string rootPath = AssetDatabase.GetAssetPath(Selection.objects[0]);
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { rootPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                if (audioDataDic.ContainsKey(clip.name))
                {
                    Debug.LogWarningFormat("音频名称重复: 名称[{0}] 路径[{1}]", clip.name, path);
                    continue;
                }

                string relativePath = path.Replace(rootPath + "/", "");
                relativePath = relativePath.Substring(0, relativePath.LastIndexOf("/"));

                AudioData data = new AudioData
                {
                    key = clip.name,
                    path = path,
                    loop = relativePath == "Music" || relativePath.StartsWith("Music/"),
                    mixerName = relativePath
                };
                audioDataDic.Add(data.key, data);
            }

            AudioDataSO dataSO = AssetDatabase.LoadAssetAtPath<AudioDataSO>(AudioDataSOPath);
            bool isNewCreate = dataSO == null;
            if (isNewCreate)
            {
                dataSO = ScriptableObject.CreateInstance<AudioDataSO>();
            }

            dataSO.conf = new List<AudioData>(audioDataDic.Values);

            if (isNewCreate)
            {
                AssetDatabase.CreateAsset(dataSO, AudioDataSOPath);
            }
            else
            {
                EditorUtility.SetDirty(dataSO);
            }

            StringBuilder contentSB = new StringBuilder();
            contentSB.Append("// 此文件是自动生成的，请勿手动修改\n");
            contentSB.Append("public static class AudioDefine\n");
            contentSB.Append("{\n");
            foreach (KeyValuePair<string, AudioData> conf in audioDataDic)
            {
                string name = conf.Key;
                contentSB.Append("    public static string " + name + " = \"" + name + "\";\n");
            }
            contentSB.Append("}\n");
            File.WriteAllText(AudioDefinePath, contentSB.ToString(), new UTF8Encoding(false));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("生成完成");
        }
    }
}
