---
name: unity-mcp-orchestrator
description: Orchestrate Unity Editor work through MCP tools for scene, prefab, asset, script, console, and Editor-state tasks in this Unity project. For Unity test authoring and test execution, use `$test-writing`.
---

# Unity MCP Orchestrator

## Generated Asset Cleanup

After generating Unity assets, delete only temporary generation scripts that this current agent explicitly created for the task once the assets are created and verified. Keep a generation script only when the user explicitly asked for a permanent reusable tool or the skill workflow requires a permanent MenuItem; in that case, state why it remains.

Never broaden cleanup from temporary tooling to project assets. Do not delete untracked or modified assets, `.meta` files, TMP/generated materials, localization tables, scenes, prefabs, package/import artifacts, or files from parallel chats unless the current task explicitly created those exact paths as temporary outputs and the user has not asked to keep them. Timing, generated-looking names, missing references from the target asset, or a clean reference search are not proof of ownership. If ownership is unclear, stop and ask before deleting or reverting anything.

## Unity MCP Routing

When this skill uses Unity MCP directly or through another skill, target the Unity Editor for the current project. If exactly one Unity Editor is running for the current workspace, do not block on a missing `mcpforunity://instances`; use the available typed Unity MCP call, omitting `unity_instance` when its schema has no such field.

For multi-project or multi-Editor work, resolve `mcpforunity://instances`, choose the exact `Name@hash` whose project matches the current workspace, and pass that value as `unity_instance` on every Unity MCP tool/resource call whose schema supports it, including custom-tool discovery/execution and every nested `batch_execute` command's `params`. Do not use `set_active_instance` or any global active-instance routing as a substitute. If a needed schema cannot accept `unity_instance`, stop and report that concurrent-safe routing is blocked; do not execute the project-scoped call.

## Core Rule

Use Unity MCP for Editor-owned state: scenes, prefabs, materials, ScriptableObjects, Addressables, imports, console, play mode, screenshots, and MenuItems. Use `$test-writing` for Unity test authoring and test execution workflow. Use local file tools for plain text only when Editor state is not involved.


Use plain asset, folder, and resource-path names. Do not create or rename files/folders with square brackets, quotes, punctuation, or other special symbols; use letters, digits, spaces, hyphens, and underscores.

For project-owned Unity asset and folder naming, read `.codex/docs/ProjectStructure.md`. In particular, do not choose `Resources`, Addressables, generated folders, scene companion folders, or package/vendor roots by convenience; each has a documented owner and naming rule.

Rename or move Unity-owned files and folders only through Unity MCP / Unity Editor APIs / `AssetDatabase`, so `.meta` GUIDs and serialized references are preserved. Do not use shell `Move-Item`, Explorer, raw `git mv`, or manual `.meta` movement for assets, scenes, prefabs, ScriptableObjects, materials, controllers, animations, render textures, or Unity folders. For same-folder renames, prefer `AssetDatabase.RenameAsset(path, newNameWithoutExtension)` or a typed Unity tool with the same semantics. For moves, use `AssetDatabase.MoveAsset(oldPath, fullNewPath)` with a full destination path. Do not guess whether a tool destination parameter is a basename, folder, or full path; confirm the schema or use explicit `AssetDatabase` calls. After any rename or move error, inspect both the filesystem and `AssetDatabase.GetAssetPath` before retrying, because Unity tooling can report failure after a partial move. Verify serialized references that depend on the asset still resolve to the expected path.

For ad-hoc MCP/Editor scans, do not probe, load, or generate code against Roslyn APIs such as `Microsoft.CodeAnalysis`, `CSharpCompilation`, or `SyntaxTree`. Use Unity-owned information surfaces first: typed MCP tools/resources, `AssetDatabase`, `MonoScript`, `TypeCache`, reflection, Unity docs/reflection tools, and local text search for plain source text. Do not create temporary `.cs` files in `Assets/` just to inspect code or types.

At the start of Unity-specific work, expose Unity MCP tools with one `tool_search` query for the needed tool family. If the exact required direct `mcp__unityMCP.*` tool is still missing or failing, check the Unity process before saying the Editor is closed.

If Unity Editor is running and the MCP HTTP server answers at the configured URL (for Outcasts this is usually `http://127.0.0.1:8080/mcp`) but typed Unity tools still do not appear in Codex, the required fix is a full Codex Desktop restart with Unity and the MCP HTTP server left running. After restart, run `tool_search` again. Do not treat this as a wrong Unity address until the HTTP endpoint itself fails.

Do not use a direct Streamable HTTP MCP client to perform Unity project work. Direct HTTP is allowed only for diagnostics such as confirming `initialize` or `tools/list`; asset, scene, prefab, script, console, and Editor mutations must use typed `mcp__unityMCP.*` tools. If typed tools remain unavailable after restart, report Unity MCP as unavailable and treat the missing routing or connection as a technical defect to fix, not as a project-task blocker; do not execute unrouted project-scoped Editor operations. Continue only safe read-only source or serialized-file inspection when Editor state is not required, and restore routed MCP before claiming Console, Play Mode, asset, scene, or prefab verification.

Generic MCP resources are optional. Empty or unrouted `list_mcp_resources` is not a Unity MCP outage when direct Unity MCP tools are available.

## Minimal Workflow

1. If exactly one Unity Editor is running for the current workspace, use the available typed Unity MCP tools directly and do not require `mcpforunity://instances` or `unity_instance`.
2. For multiple Unity projects or Editors, resolve `mcpforunity://instances` and use the exact `Name@hash` whose project root matches the current workspace as the routing token.
3. In multi-Editor work, pass that token explicitly as `unity_instance` on every project-scoped Unity MCP tool/resource call whose schema supports it. This includes validation, console, MenuItem, asset, scene, prefab, custom-tool, and batch calls.
4. For `batch_execute` in multi-Editor work, add `unity_instance` inside each nested command's `params`; do not rely on a top-level batch route or active global instance.
5. Never call `mcp__unityMCP.set_active_instance` or `set_active_instance` as a workaround for missing `unity_instance`. If concurrent-safe routing cannot be expressed by the schema, stop only the multi-Editor project-scoped operation and report the blocker.
6. Inspect only the target area: scene/object/asset/script/package state needed for the task.
7. Modify through Unity MCP tools for Editor state; use `$apply-patch` for project text files.
8. After C# or `.asmdef` edits, run `$mcp-unity-validate-script`, then `$unity-recompile-menuitem`, then read console errors.
9. For visual scene/UI changes, verify with a bounded screenshot only when visual correctness matters.

## Concurrent Chat Ownership

When multiple Codex chats or agents may be active against the same Unity project, treat console errors, validation failures, test failures, dirty files, untracked files, Unity import side effects, and AssetDatabase changes as shared state. Fix or delete only state that is clearly caused by the current request or by files explicitly edited/created in the current chat. Do not repair, remove, revert, or clean unrelated changes from other chats; report them as external/blocking with the affected files or messages and continue only when the current task can still be verified honestly.

For existing scene work, preserve the current multi-scene Editor layout. Open the target scene additively, make that loaded scene the active scene before creating or moving scene objects, and save only the scene intentionally edited by the current task. Do not close, unload, reload, or replace other loaded scenes unless the user explicitly asks, because they may belong to another active chat. If multiple Unity Editors are active and the available typed MCP scene tool cannot load additively or set the active scene with per-call `unity_instance` routing, stop and report that concurrent-safe scene editing is blocked. With one Unity Editor, use the available typed schema directly without instance routing.

## Modal Dialogs

Scene reloads or scene switches can open a Unity modal confirmation dialog that locks the Editor and blocks MCP progress. When MCP calls stop making progress after a scene reload/switch, assume a user-only dialog may be open; report that the user must click it manually and wait. Do not try to bypass it with shell automation, repeated MCP calls, or global instance switching.

## Recovery after a current-turn Unity mutation

If the current turn broke an unsaved scene, reopen that scene without saving and discard the broken in-memory state. Do not save the damaged scene first.

If the current turn already saved a broken scene or prefab, verify the exact path and `git diff`/`git status`, then revert only that exact file with Git. Never use a broad reset/checkout and never touch unrelated dirty or untracked changes. If the editor has multiple scenes loaded, recover only the scene owned by the current turn.

## Script Rules

Before creating or substantially editing Unity C# scripts, load `$code-style`. Keep exactly one top-level C# type per `.cs` file. Do not add migration scripts, conversion tools, package-cache edits, or future-facing public API unless the user asks or existing code requires it.

Do not edit package source, embedded packages, `Packages/`, or `Library/PackageCache/`. Read packages or docs only when project code and reflected APIs are insufficient.

## TEMP/TLS Allocator Safety

Generic `manage_gameobject.find`, `get_components`, `get_component`, and reflection-based component-property operations can serialize Transform vectors through Unity native `Matrix4x4` code and leave `ALLOC_TEMP_TLS` allocations in the Editor. Treat these calls as high-risk diagnostics: prefer narrow typed tools, `read_resource`, project text search, or a purpose-built Editor API. Do not use generic component serialization for routine inspection. Avoid `manage_gameobject.modify` with `component_properties` or `set_component_property` for scene/prefab authoring when a typed Unity Editor/API route exists.

If Console shows `TLS Allocator ALLOC_TEMP_TLS` or `ALLOC_TEMP_MAIN` after an MCP GameObject/Component operation, stop all further GameObject/Component MCP calls and Play/Save attempts. Clearing Console is not a fix; report the exact operation and require a Unity Editor restart before continuing. Do not edit `Library/PackageCache` to work around it.

## Direct-Only MCP Tools

Call these directly, never through `batch_execute`:

- `mcp__unityMCP.find_in_file`
- `mcp__unityMCP.validate_script`

Use `batch_execute` only for tools known to route correctly, such as grouped scene/object/component operations.

## Payload Discipline

Keep discovery bounded:

- Prefer narrow paths, names, components, layers, and page sizes. Do not search, create, assign, or filter by Unity GameObject tags.
- Read the first page or summary first; follow `next_cursor` only when the task needs the complete set.
- Keep console reads focused on recent errors unless warnings are part of the task.
- Use screenshots at 256-512 px unless higher detail is necessary.
- Use `include_image=True` only when image analysis is needed.

## API Verification

Prefer this order when API details are uncertain:

1. Existing project code/assets.
2. Live reflection through `unity_reflect` when available.
3. Official Unity/package docs through `unity_docs` or package docs.
4. Large reference files in this skill, read by relevant section only.

Do not run docs/reflection lookups for routine Unity APIs already clear from nearby project code.

## References

Read only the relevant reference index, then open the one topic file it points to:

- `references/tools-reference.md`: index for exact MCP tool parameters and uncommon tool actions.
- `references/workflows.md`: index for extended workflow examples. For testing workflow, use `$test-writing` first and consult MCP references only for exact tool parameters.
