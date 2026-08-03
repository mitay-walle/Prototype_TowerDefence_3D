---
name: unity-recompile-menuitem
description: Force Unity script recompilation through the `MCP/Force Recompile Scripts` MenuItem, then read console errors.
---

# Unity Recompile MenuItem


## Rule

After editing Unity C# or `.asmdef` files, verify through the Unity Editor by default. IDE `.csproj` compilation is not enough.

Order:

1. Validate changed C# scripts with `$mcp-unity-validate-script`.
2. Execute `MCP/Force Recompile Scripts` through `mcp__unityMCP.execute_menu_item`.
3. Read recent Unity console errors with `mcp__unityMCP.read_console`.
4. Fix compile errors and repeat only as needed.

## MenuItem

Default path:

```text
MCP/Force Recompile Scripts
```

Expected script:

```text
Assets/Editor/CodexForceRecompileScripts.cs
```

If missing or outdated, load `$code-style`, then create/update it from `assets/CodexForceRecompileScripts.cs` through `$apply-patch`. Keep it as a single editor-only top-level type directly under `Assets/Editor/` unless the project already has a better shared Editor folder.

The MenuItem should refresh the AssetDatabase before requesting compilation and log `[Codex] Script compilation started/finished` so console polling can stop quickly.

## MCP Availability

If the needed MCP tool is not exposed, run one targeted `tool_search` for `Unity MCP execute_menu_item read_console validate_script`. If it remains unavailable, check the Unity process before claiming the Editor is closed, then ask the user to reconnect/enable MCP or approve weaker log/file verification. Do not use `set_active_instance` as a substitute for the typed call.

Do not silently replace this workflow with Editor log inspection. Use log-only fallback only after user approval.

## Safety

- Keep tooling under an `Editor` folder.
- Do not introduce runtime references to `UnityEditor`.
- Do not clear the console unless the user asks.
- Do not add broad formatting or unrelated project changes.
- Write changed project text files through `$apply-patch` so EOL, UTF-8 BOM, final newline, and Cyrillic text remain intact.
- When multiple Codex chats or agents may be active, do not fix unrelated console errors from files outside the current task. Attribute them as external/shared-state failures unless there is evidence the current changes caused them.
