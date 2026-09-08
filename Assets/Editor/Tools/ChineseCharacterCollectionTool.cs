using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ChineseCharacterCollectionTool : EditorWindow
{
    private const string DefaultOutputPath = "Assets/Fonts/ChineseCharacters.txt";
    private const int PrintableAsciiFirst = 0x20;
    private const int PrintableAsciiLast = 0x7E;
    private static readonly HashSet<string> TextFileExtensions = new HashSet<string>
    {
        ".asset",
        ".bytes",
        ".csv",
        ".cs",
        ".json",
        ".prefab",
        ".txt",
        ".unity",
        ".xml",
        ".yaml",
        ".yml"
    };

    private string _outputPath = DefaultOutputPath;
    private int _characterCount;
    private Vector2 _scrollPosition;
    private string _resultPreview = string.Empty;

    [MenuItem("Tools/字体/收集项目中文字符")]
    public static void Open()
    {
        GetWindow<ChineseCharacterCollectionTool>("中文字符收集");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("中文字体子集字符收集", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "扫描 Assets 下的文本资源，提取中文字符；同时固定包含大小写英文、数字、空格和常用英文标点，排序去重后生成 UTF-8 无 BOM 的 txt 文件。",
            MessageType.Info);

        _outputPath = EditorGUILayout.TextField("输出路径", _outputPath);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_outputPath)))
        {
            if (GUILayout.Button("收集并生成 txt", GUILayout.Height(30)))
            {
                CollectChineseCharacters();
            }
        }

        if (_characterCount > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"已收集 {_characterCount} 个去重中文字符。");
            EditorGUILayout.LabelField("预览");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(100));
            EditorGUILayout.SelectableLabel(_resultPreview, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void CollectChineseCharacters()
    {
        if (!IsValidOutputPath(_outputPath))
        {
            EditorUtility.DisplayDialog("输出路径无效", "输出路径必须位于 Assets 文件夹中，并以 .txt 结尾。", "确定");
            return;
        }

        SortedSet<int> characters = new SortedSet<int>();
        AddPrintableAsciiCharacters(characters);
        string normalizedOutputPath = _outputPath.Replace('\\', '/');
        string fullOutputPath = Path.GetFullPath(normalizedOutputPath);

        foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
        {
            if (!ShouldScan(assetPath, normalizedOutputPath))
            {
                continue;
            }

            CollectCharactersFromFile(assetPath, characters);
        }

        StringBuilder contentBuilder = new StringBuilder();
        foreach (int character in characters)
        {
            contentBuilder.Append(char.ConvertFromUtf32(character));
        }

        string content = contentBuilder.ToString();
        string outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(fullOutputPath, content, new UTF8Encoding(false));
        AssetDatabase.Refresh();

        _characterCount = characters.Count;
        _resultPreview = content;
        Debug.Log($"中文字符收集完成：共 {_characterCount} 个去重字符，已保存至 {normalizedOutputPath}");
        EditorUtility.DisplayDialog("收集完成", $"共收集 {_characterCount} 个去重中文字符。\n文件已生成：{normalizedOutputPath}", "确定");
    }

    private static bool IsValidOutputPath(string outputPath)
    {
        string normalizedPath = outputPath.Replace('\\', '/');
        return normalizedPath.StartsWith("Assets/") && normalizedPath.EndsWith(".txt");
    }

    private static void AddPrintableAsciiCharacters(ISet<int> characters)
    {
        for (int character = PrintableAsciiFirst; character <= PrintableAsciiLast; character++)
        {
            characters.Add(character);
        }
    }

    private static bool ShouldScan(string assetPath, string outputPath)
    {
        if (!assetPath.StartsWith("Assets/") || assetPath == outputPath)
        {
            return false;
        }

        string extension = Path.GetExtension(assetPath).ToLowerInvariant();
        return TextFileExtensions.Contains(extension);
    }

    private static void CollectCharactersFromFile(string assetPath, ISet<int> characters)
    {
        try
        {
            using (StreamReader reader = new StreamReader(assetPath, Encoding.UTF8, true))
            {
                string content = reader.ReadToEnd();
                CollectUnicodeEscapedCharacters(content, characters);
                for (int index = 0; index < content.Length; index++)
                {
                    int character = char.ConvertToUtf32(content, index);
                    if (character > char.MaxValue)
                    {
                        index++;
                    }

                    if (IsChineseCharacter(character))
                    {
                        characters.Add(character);
                    }
                }
            }
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"无法读取文件，已跳过：{assetPath}\n{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"没有读取权限，已跳过：{assetPath}\n{exception.Message}");
        }
    }

    private static void CollectUnicodeEscapedCharacters(string content, ISet<int> characters)
    {
        for (int index = 0; index < content.Length; index++)
        {
            if (!TryGetUnicodeEscape(content, index, out int character))
            {
                continue;
            }

            if (char.IsHighSurrogate((char)character)
                && TryGetUnicodeEscape(content, index + 6, out int lowSurrogate)
                && char.IsLowSurrogate((char)lowSurrogate))
            {
                character = char.ConvertToUtf32((char)character, (char)lowSurrogate);
                index += 6;
            }

            if (IsChineseCharacter(character))
            {
                characters.Add(character);
            }

            index += 5;
        }
    }

    private static bool TryGetUnicodeEscape(string content, int index, out int character)
    {
        character = 0;
        if (index + 5 >= content.Length || content[index] != '\\' || content[index + 1] != 'u')
        {
            return false;
        }

        for (int offset = 2; offset < 6; offset++)
        {
            int digit = GetHexValue(content[index + offset]);
            if (digit < 0)
            {
                return false;
            }

            character = character * 16 + digit;
        }

        return true;
    }

    private static int GetHexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    private static bool IsChineseCharacter(int character)
    {
        return character >= 0x3400 && character <= 0x4DBF
            || character >= 0x4E00 && character <= 0x9FFF
            || character >= 0xF900 && character <= 0xFAFF
            || character >= 0x20000 && character <= 0x2FA1F;
    }
}
