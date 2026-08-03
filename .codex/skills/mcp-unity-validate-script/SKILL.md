---
name: mcp-unity-validate-script
description: "Validate changed Unity C# scripts with direct sequential `mcp__unityMCP.validate_script` calls before recompilation; never via `batch_execute`."
---

# MCP Unity Validate Script


## Rule

Before forcing Unity script recompilation, validate changed Unity C# scripts under `Assets/` with direct sequential `mcp__unityMCP.validate_script` calls. Never call `validate_script` through `mcp__unityMCP.batch_execute`. After every validate-script pass, load and apply `$code-style` before deciding the result is acceptable.

## Workflow

1. Collect changed `.cs` scripts under `Assets/`, prioritizing files Codex edited directly.
2. Call one validation per file:
   ```text
   mcp__unityMCP.validate_script(uri="Assets/Scripts/Foo.cs", level="standard", include_diagnostics=true)
   ```
   With one Unity Editor, omit `unity_instance`; with multiple Unity Editors or projects, add it only when the exposed schema supports per-call routing.
3. Fix syntax or semantic errors before recompiling.
4. Load `$code-style` and review the changed scripts against project style, ownership, DI, localization, lifecycle, and public API rules. Treat code-style violations as validation failures even when `validate_script` reports no diagnostics.
5. When validation and `$code-style` review pass, or only known external-reference errors remain, run `$unity-recompile-menuitem` and read Unity console errors.

If many scripts changed, validate directly edited files first, then nearby dependent scripts only when diagnostics point there.

When multiple Codex chats or agents may be active, treat validation diagnostics outside the current chat's edited files as external unless diagnostics prove they are caused by the current changes. Do not fix unrelated script errors from other chats; report them with file paths/messages and continue only if the current files can still be validated honestly.

## Availability

If `mcp__unityMCP.validate_script` is not exposed, run one targeted `tool_search` for `Unity MCP validate_script read_console`. If it is still unavailable and Unity MCP validation matters, say that validation is unavailable, check the Unity process before claiming the Editor is closed, and ask the user to reconnect/enable MCP or approve weaker fallback verification. Do not use `set_active_instance` as a fallback.

Validation is not a replacement for Unity compilation; it is a fast per-file gate before the full Editor compile pass.
