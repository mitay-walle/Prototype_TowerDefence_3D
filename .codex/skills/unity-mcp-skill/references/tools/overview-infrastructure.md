# Unity-MCP Tools Reference

Complete reference for all MCP tools. Each tool includes parameters, types, and usage examples.

> **Template warning:** Examples in this file are skill templates and may be inaccurate for some Unity versions, packages, or project setups. Validate parameters and payload shapes against your active tool schema and runtime behavior.
>
> **Outcasts routing requirement:** Every executable Unity MCP call copied from this reference must include `unity_instance="<Outcasts Name@hash>"` when the exposed schema supports it. For `batch_execute`, include `unity_instance` inside each nested command's `params`. Treat examples without that argument as abbreviated documentation, not ready-to-run commands.

## Table of Contents

- [Infrastructure Tools](#infrastructure-tools)
- [Scene Tools](#scene-tools)
- [GameObject Tools](#gameobject-tools)
- [Script Tools](#script-tools)
- [Asset Tools](#asset-tools)
- [Material & Shader Tools](#material--shader-tools)
- [UI Tools](#ui-tools)
- [Editor Control Tools](#editor-control-tools)
- [Testing Tools](#testing-tools)
- [Camera Tools](#camera-tools)
- [Graphics Tools](#graphics-tools)
- [Package Tools](#package-tools)
- [Physics Tools](#physics-tools)
- [ProBuilder Tools](#probuilder-tools)
- [Profiler Tools](#profiler-tools)
- [Docs Tools](#docs-tools)

---

## Project Info Resource

Read `mcpforunity://project/info` to detect project capabilities before making assumptions about UI, input, or rendering setup.

**Returned fields:**

| Field | Type | Description |
|-------|------|-------------|
| `projectRoot` | string | Absolute path to project root |
| `projectName` | string | Project folder name |
| `unityVersion` | string | e.g. `"2022.3.20f1"` |
| `platform` | string | Active build target e.g. `"StandaloneWindows64"` |
| `assetsPath` | string | Absolute path to Assets folder |
| `renderPipeline` | string | `"BuiltIn"`, `"Universal"`, `"HighDefinition"`, or `"Custom"` |
| `activeInputHandler` | string | `"Old"`, `"New"`, or `"Both"` |
| `packages.ugui` | bool | `com.unity.ugui` installed (Canvas, Image, Button, etc.) |
| `packages.textmeshpro` | bool | `com.unity.textmeshpro` installed (TMP_Text, TMP_InputField) |
| `packages.inputsystem` | bool | `com.unity.inputsystem` installed (InputAction, PlayerInput) |
| `packages.uiToolkit` | bool | Always `true` for Unity 2021.3+ (UIDocument, VisualElement, UXML/USS) |
| `packages.screenCapture` | bool | `com.unity.modules.screencapture` enabled (ScreenCapture API for screenshots) |

**Key decision points:**

- **UI system**: If `packages.uiToolkit` is true (always for Unity 2021+), use `manage_ui` for UI Toolkit workflows (UXML/USS). If `packages.ugui` is true, use Canvas + uGUI components via `batch_execute`. UI Toolkit is preferred for new UI — it uses a frontend-like workflow (UXML for structure, USS for styling).
- **Text**: If `packages.textmeshpro` is true, use `TextMeshProUGUI` instead of legacy `Text`.
- **Input**: Use `activeInputHandler` to decide EventSystem module — `StandaloneInputModule` (Old) vs `InputSystemUIInputModule` (New). See [workflows.md — Input System](workflows.md#input-system-old-vs-new).
- **Shaders**: Use `renderPipeline` to pick correct shader names — `Standard` (BuiltIn) vs `Universal Render Pipeline/Lit` (URP) vs `HDRP/Lit` (HDRP).

---

## Infrastructure Tools

### batch_execute

Execute multiple MCP commands in a single batch (10-100x faster).

```python
batch_execute(
    commands=[                    # list[dict], required, max 25
        {"tool": "tool_name", "params": {...}},
        ...
    ],
    parallel=False,              # bool, optional - advisory only (Unity may still run sequentially)
    fail_fast=False,             # bool, optional - stop on first failure
    max_parallelism=None         # int, optional - max parallel workers
)
```

`batch_execute` is not transactional: earlier commands are not rolled back if a later command fails.

Do not route direct-only tools through `batch_execute`:
- `find_in_file`: batch routing does not dispatch it reliably. Call `mcp__unityMCP.find_in_file` directly once per file/pattern.
- `validate_script`: batch routing does not support it. Call `mcp__unityMCP.validate_script` sequentially once per script.

### set_active_instance

Global session routing command. For Outcasts project work, do not call this tool and do not use it as a fallback when a resource or tool cannot accept `unity_instance`.

Use per-call `unity_instance="Outcasts@..."` routing instead. If a project-scoped call cannot accept `unity_instance`, stop and report MCP routing is blocked; do not use global active-instance routing as a fallback.

```python
# Do not use for Outcasts project work.
# Instead, pass unity_instance="Outcasts@abc123" to every routed MCP call.
```

### refresh_unity

Refresh asset database and trigger script compilation.

```python
refresh_unity(
    mode="if_dirty",             # "if_dirty" | "force"
    scope="all",                 # "assets" | "scripts" | "all"
    compile="none",              # "none" | "request"
    wait_for_ready=True          # bool - wait until editor ready
)
```

---
