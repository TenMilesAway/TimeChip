using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_VariableNamingConvention
    {
        public const string VariableNamingConventionToolId = "variable-naming-convention-get";

        [AiTool(VariableNamingConventionToolId, Title = "Variable Naming Convention / Get")]
        [AiSkillDescription("Apply the project's C# variable naming convention whenever creating or modifying scripts.")]
        [AiSkillBody("# C# Variable Naming Convention\n\nWhen creating or modifying scripts in this project, prefix every non-constant, non-static declared variable with an underscore. This applies to instance fields, local variables, and method parameters. Use camelCase after the underscore (for example, `_playerName`).\n\nDo not add an underscore to constants or static values. Keep their existing naming appropriate to their declaration and project usage.\n\nExternally callable getters must start with an uppercase letter. Use PascalCase for public getter properties or getter methods (for example, `PlayerName` or `GetPlayerName`); do not prefix these getters with an underscore.\n\nPreserve public API compatibility: before renaming an existing externally referenced member, update every call site or retain a compatible public getter. Do not use bulk text replacement for renames; make syntax-aware, targeted changes.")]
        [Description("Returns the required C# variable and external getter naming convention for this project.")]
        public string GetConvention()
        {
            return "Non-constant, non-static declared variables use _camelCase. Constants and static values are exempt. Externally callable getters use PascalCase without an underscore.";
        }
    }
}
