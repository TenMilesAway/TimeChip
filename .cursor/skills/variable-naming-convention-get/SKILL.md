---
name: variable-naming-convention-get
description: Apply the project's C# variable naming convention whenever creating or modifying scripts.
---

# Variable Naming Convention / Get

# C# Variable Naming Convention

When creating or modifying scripts in this project, prefix every non-constant, non-static declared variable with an underscore. This applies to instance fields, local variables, and method parameters. Use camelCase after the underscore (for example, `_playerName`).

Do not add an underscore to constants or static values. Keep their existing naming appropriate to their declaration and project usage.

Externally callable getters must start with an uppercase letter. Use PascalCase for public getter properties or getter methods (for example, `PlayerName` or `GetPlayerName`); do not prefix these getters with an underscore.

Preserve public API compatibility: before renaming an existing externally referenced member, update every call site or retain a compatible public getter. Do not use bulk text replacement for renames; make syntax-aware, targeted changes.

## How to Call

```bash
unity-mcp-cli run-tool variable-naming-convention-get --input '{}'
```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

This tool takes no input parameters.

### Input JSON Schema

```json
{
  "type": "object",
  "additionalProperties": false
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

