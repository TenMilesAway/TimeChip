#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_ChangeReport
    {
        public const string ChangeReportGenerateToolId = "change-report-generate";

        [AiTool(ChangeReportGenerateToolId, Title = "Change Report / Generate")]
        [AiSkillDescription("Generate a Markdown change report after completing a modification task. " +
            "Records completed work, changed files, and usage instructions. " +
            "The filename uses the current date plus a concise task summary.")]
        [AiSkillBody("Generate a Markdown modification report after completing any code, asset, scene, configuration, or documentation change. " +
            "Call this tool after the implementation and validation are complete. Provide a concise task name, an accurate summary of the completed work, " +
            "usage instructions for the resulting feature, and all files changed by the task.\n\n" +
            "## Inputs\n\n" +
            "- `taskName` — concise task summary used in the document title and filename.\n" +
            "- `workSummary` — Markdown describing what was implemented and important design decisions.\n" +
            "- `usageInstructions` — Markdown explaining how to use the changed feature.\n" +
            "- `changedFiles` — project-relative paths changed by this task.\n" +
            "- `outputDirectory` — optional project-relative report folder; defaults to `Design/修改文档`.\n\n" +
            "## Output\n\n" +
            "Writes a UTF-8 Markdown file named `yyyy-MM-dd_任务名称总结.md`. If that name already exists, " +
            "a numeric suffix is appended so previous reports are preserved. Returns the project-relative path.\n\n" +
            "## Rules\n\n" +
            "Only report work actually completed in the current task. Keep usage instructions concrete and include code examples when useful. " +
            "Do not include unrelated pre-existing working-tree changes.")]
        [Description("Generate a Markdown report describing a completed modification and how to use it.")]
        public string Generate
        (
            [Description("Concise task summary used in the report title and filename.")]
            string taskName,
            [Description("Markdown summary of the work completed.")]
            string workSummary,
            [Description("Markdown instructions explaining how to use the result.")]
            string usageInstructions,
            [Description("Project-relative paths changed by this task.")]
            string[]? changedFiles = null,
            [Description("Project-relative output directory.")]
            string outputDirectory = "AgentLogs"
        )
        {
            ValidateRequired(taskName, nameof(taskName));
            ValidateRequired(workSummary, nameof(workSummary));
            ValidateRequired(usageInstructions, nameof(usageInstructions));
            ValidateRequired(outputDirectory, nameof(outputDirectory));

            return MainThread.Instance.Run(() =>
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string outputPath = ResolveProjectPath(projectRoot, outputDirectory);
                Directory.CreateDirectory(outputPath);

                string normalizedTaskName = NormalizeSingleLine(taskName);
                string safeTaskName = SanitizeFileName(normalizedTaskName);
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                string reportPath = GetAvailableReportPath(outputPath, $"{date}_{safeTaskName}");

                var markdown = new StringBuilder();
                markdown.AppendLine($"# {normalizedTaskName}");
                markdown.AppendLine();
                markdown.AppendLine($"- 日期：{date}");
                markdown.AppendLine($"- 任务：{normalizedTaskName}");
                markdown.AppendLine();
                markdown.AppendLine("## 完成的工作");
                markdown.AppendLine();
                markdown.AppendLine(workSummary.Trim());
                markdown.AppendLine();
                markdown.AppendLine("## 修改文件");
                markdown.AppendLine();

                string[] files = changedFiles?
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => path.Trim().Replace('\\', '/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

                if (files.Length == 0)
                {
                    markdown.AppendLine("- 无文件路径记录");
                }
                else
                {
                    foreach (string file in files)
                    {
                        markdown.AppendLine($"- `{file.Replace("`", "\\`")}`");
                    }
                }

                markdown.AppendLine();
                markdown.AppendLine("## 使用说明");
                markdown.AppendLine();
                markdown.AppendLine(usageInstructions.Trim());

                File.WriteAllText(reportPath, markdown.ToString(), new UTF8Encoding(false));

                if (IsInsideAssets(projectRoot, reportPath))
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    EditorUtils.RepaintAllEditorWindows();
                }

                return Path.GetRelativePath(projectRoot, reportPath).Replace('\\', '/');
            });
        }

        private static void ValidateRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
            }
        }

        private static string ResolveProjectPath(string projectRoot, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("Output directory must be project-relative.", nameof(relativePath));
            }

            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            string rootWithSeparator = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Output directory must stay inside the Unity project.", nameof(relativePath));
            }

            return fullPath;
        }

        private static string GetAvailableReportPath(string outputPath, string baseName)
        {
            string path = Path.Combine(outputPath, baseName + ".md");
            int suffix = 2;

            while (File.Exists(path))
            {
                path = Path.Combine(outputPath, $"{baseName}_{suffix}.md");
                suffix++;
            }

            return path;
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            var result = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (invalidChars.Contains(character))
                {
                    result.Append('-');
                }
                else if (char.IsWhiteSpace(character))
                {
                    result.Append('-');
                }
                else
                {
                    result.Append(character);
                }
            }

            string fileName = result.ToString().Trim('-', '.');
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Task name does not contain any valid filename characters.", nameof(value));
            }

            return fileName;
        }

        private static string NormalizeSingleLine(string value)
        {
            return string.Join(" ", value
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0));
        }

        private static bool IsInsideAssets(string projectRoot, string path)
        {
            string assetsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Assets"))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return path.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}