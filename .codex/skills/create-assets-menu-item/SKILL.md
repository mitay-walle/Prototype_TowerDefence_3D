---
name: create-assets-menu-item
description: Create a Unity Editor MenuItem for repeatable asset generation, recompile, execute it via MCP, and verify results.
---

# Create Assets Menu Item


## Overview

Add an Editor MenuItem that creates assets, then ensure the script compiles, trigger a recompile, and execute the menu item via MCP.

## Workflow

### 1) Gather requirements

Confirm:
- MenuItem path (e.g., `MCP/Create Assets`)
- Asset type(s) and template defaults
- Output folder(s) and naming scheme
- Overwrite policy (skip, replace, or unique name)
- Whether to use selection as input

Folder rule:
- Do not create a dedicated folder solely for one generated file when the folder and file repeat the same concept, such as `Assets/Editor/Foo/Foo.cs` or `Assets/Resources/MyAsset/MyAsset.asset`.
- Prefer the nearest existing appropriate folder, or a shared category folder such as `Assets/Editor`, `Assets/Resources`, `Assets/Materials`, or `Assets/Prefabs`.
- Create a new folder only when multiple related files/assets will live there, Unity requires that folder category, or the user explicitly asks for that layout.

### 2) Implement the Editor script

Before creating or updating any C# `.cs` script, load and apply `$code-style`. Treat `$code-style` as a required gate for file naming, type layout, dependencies, runtime/editor boundaries, and implementation details.

Create a static class under `Assets/Editor/`. Keep the MenuItem path as a constant.

Write new or changed scripts through `$apply-patch` `write`/`replace` so the file is created in the correct convention immediately:
- For a new Editor C# file, copy EOL/BOM/final-newline style from the nearest existing `Assets/**/Editor/*.cs`.
- If no local example exists, use LF, UTF-8 without BOM, no final newline, and 4-space indentation.
- Keep exactly one top-level C# type per `.cs` file. If the MenuItem needs helper classes, structs, enums, or list wrappers, create separate files named after each type; do not put multiple types in one plural/helper file such as `AuthoringLists.cs`, `Helpers.cs`, or `Types.cs`.
- Do not use PowerShell `Set-Content`/`Out-File` or raw shell writes for project scripts unless the final write is routed through `$apply-patch`.

Minimal ScriptableObject example:

```csharp
using UnityEditor;
using UnityEngine;

public static class CreateAssetsMenu
{
    private const string MenuPath = "MCP/Create Assets";
    private const string OutputFolder = "Assets/Resources/MyAssets";

    [MenuItem(MenuPath)]
    public static void Create()
    {
        var asset = ScriptableObject.CreateInstance<MyAsset>();
        var path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/MyAsset.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created asset: {path}");
    }
}
```

Notes:
- Keep code in `Assets/Editor/` so it is editor-only.
- Use `AssetDatabase.GenerateUniqueAssetPath` to avoid overwrites.
- When creating Unity `Tile` objects or other `ScriptableObject` assets with `AssetDatabase.CreateAsset`, use the `.asset` extension. Do not invent custom extensions such as `.tile`.
- Name project-owned ScriptableObject and prefab assets according to `.codex/docs/ProjectStructure.md`: `Type Domain Tags`, with the concrete type/root owner first.
- For prefabs, use `PrefabUtility.SaveAsPrefabAsset`.

### 3) Compile and recompile

After writing the script:
- Validate changed C# files with `$mcp-unity-validate-script`.
- Use `$unity-recompile-menuitem` to force a recompile pass and read Unity console errors.

### 4) Invoke the MenuItem via MCP

Call the menu item with `mcp__unityMCP.execute_menu_item(menu_path=MenuPath, unity_instance="<target Name@hash>")`. If the exposed schema does not accept `unity_instance`, stop and report that MCP routing is blocked for concurrent Unity work.

If MCP is unavailable or `execute_menu_item` cannot accept `unity_instance`, ask the user to reconnect/enable MCP or approve a weaker non-Editor fallback. Do not silently replace Editor execution with file-only checks.

### 5) Verify results

Check the Unity console for the success log and confirm assets exist (via `AssetDatabase` or MCP asset search).

### 6) Remove temporary generators

If the Editor MenuItem script was created only to generate or patch assets for this task, delete only that script and its `.meta` after the assets are generated, imported, saved, and verified. Keep the script only when the user explicitly asks for permanent repeatable tooling.

Cleanup boundary:

- Delete only temporary files that this current agent explicitly created and can name exactly.
- Do not delete untracked or modified assets that appeared after Unity refresh/import unless the current script explicitly created those exact paths as temporary outputs.
- Timing, generated-looking names, missing references from the target prefab, or a clean asset-search result are not ownership proof.
- Treat Unity import side effects, TMP materials, localization assets, scene/prefab changes, `.meta` files, and files from parallel chats as shared workspace state when ownership is unclear.
- If cleanup ownership is unclear, stop and ask before deleting or reverting anything.
