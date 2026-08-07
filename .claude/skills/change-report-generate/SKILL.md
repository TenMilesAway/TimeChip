---
name: change-report-generate
description: Generate a Markdown change report after completing a modification task. Records completed work, changed files, and usage instructions. The filename uses the current date plus a concise task summary.
---

# Change Report / Generate

Generate a Markdown modification report after completing any code, asset, scene, configuration, or documentation change. Call this tool after the implementation and validation are complete. Provide a concise task name, an accurate summary of the completed work, usage instructions for the resulting feature, and all files changed by the task.

## Inputs

- `taskName` — concise task summary used in the document title and filename.
- `workSummary` — Markdown describing what was implemented and important design decisions.
- `usageInstructions` — Markdown explaining how to use the changed feature.
- `changedFiles` — project-relative paths changed by this task.
- `outputDirectory` — optional project-relative report folder; defaults to `Design/修改文档`.

## Output

Writes a UTF-8 Markdown file named `yyyy-MM-dd_任务名称总结.md`. If that name already exists, a numeric suffix is appended so previous reports are preserved. Returns the project-relative path.

## Rules

Only report work actually completed in the current task. Keep usage instructions concrete and include code examples when useful. Do not include unrelated pre-existing working-tree changes.

## How to Call

```bash
unity-mcp-cli run-tool change-report-generate --input '{
  "taskName": "string_value",
  "workSummary": "string_value",
  "usageInstructions": "string_value",
  "changedFiles": "string_value",
  "outputDirectory": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool change-report-generate --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool change-report-generate --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `taskName` | `string` | Yes | Concise task summary used in the report title and filename. |
| `workSummary` | `string` | Yes | Markdown summary of the work completed. |
| `usageInstructions` | `string` | Yes | Markdown instructions explaining how to use the result. |
| `changedFiles` | `any` | No | Project-relative paths changed by this task. |
| `outputDirectory` | `string` | No | Project-relative output directory. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "taskName": {
      "type": "string"
    },
    "workSummary": {
      "type": "string"
    },
    "usageInstructions": {
      "type": "string"
    },
    "changedFiles": {
      "$ref": "#/$defs/System.String-1"
    },
    "outputDirectory": {
      "type": "string"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "taskName",
    "workSummary",
    "usageInstructions"
  ]
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

