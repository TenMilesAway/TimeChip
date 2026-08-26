using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AudioDefineGenTool
{
    private static string AudioDataSOPath = "Assets/Audios/AudioDataSO.asset";
    private static string AudioDefinePath = "Assets/Scripts/Utility/Audio/AudioDefine.cs";

    [MenuItem("Assets/生成音频配置")]
    public static void GenAudioDefine()
    {
        if (Selection.objects.Length == 0) return;

        Debug.LogFormat("当前选中文件夹: {0}", Selection.objects[0].name);

        Dictionary<string, AudioData> audioDataDic = new Dictionary<string, AudioData>();

        // 判断是否选中文件夹
        if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.objects[0])))
        {
            string rootPath = AssetDatabase.GetAssetPath(Selection.objects[0]);

            // 获取文件夹中的所有音频文件
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new string[] { rootPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                AudioData data = new AudioData();
                if (audioDataDic.ContainsKey(clip.name))
                {
                    Debug.LogWarningFormat("音频名称重复: 名称[{0}] 路径[{1}]", clip.name, path);
                    continue;
                }

                string relativePath = path.Replace(rootPath + "/", "");
                relativePath = relativePath.Substring(0, relativePath.LastIndexOf("/"));
                data.key = clip.name;
                data.path = path;
                data.loop = clip.name.StartsWith("Music");
                data.mixerName = relativePath;
                audioDataDic.Add(data.key, data);
            }

            bool isNewCreate = false;

            // 生成 SO
            AudioDataSO dataSO = AssetDatabase.LoadAssetAtPath<AudioDataSO>(AudioDataSOPath);
            if (dataSO == null)
            {
                dataSO = ScriptableObject.CreateInstance(typeof(AudioDataSO)) as AudioDataSO;
                isNewCreate = true;
            }
            dataSO.conf = new List<AudioData>();
            foreach (KeyValuePair<string, AudioData> conf in audioDataDic)
            {
                dataSO.conf.Add(conf.Value);
            }

            // 如果是新创建
            if (isNewCreate) AssetDatabase.CreateAsset(dataSO, AudioDataSOPath);
            // 刷新数据
            else EditorUtility.SetDirty(dataSO);

            // 生成 CSharp
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
            File.WriteAllText(AudioDefinePath, contentSB.ToString());
            AssetDatabase.Refresh();

            Debug.LogFormat("生成完成");
        }
    }
}
