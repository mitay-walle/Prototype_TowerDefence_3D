---
name: editor-tool-authoring
description: Build or modify long-lived Outcasts Unity Editor-only tools such as EditorWindow panels, Odin/custom inspectors, InspectorWindow integrations, SceneView tools, overlays, gizmos, handles, debug/diagnostic windows, asset browsers, authoring utilities, and persistent editor workflows. Use when the requested tool is meant to remain in the project, not for one-shot asset migration MenuItems.
---

# Editor Tool Authoring

## Scope

Use this skill for durable Unity Editor tooling:

- `EditorWindow`, `Overlay`, custom inspectors, property drawers, scene handles, toolbar/panel tools, and debug/diagnostic windows.
- UI Toolkit editor UI with UXML/USS or code-built `VisualElement` trees.
- AssetDatabase-backed browsers, validators, fixers, and authoring tools intended to stay in the repository.

For one-shot generators or migration scripts, use `$create-assets-menu-item` instead. For runtime UI, gameplay UI prefabs, or player-facing UI Toolkit screens, use the UI skills and `$code-style` runtime rules instead.

## Required Baseline

Before editing project files, load `$apply-patch`, `$code-style`, and `$unity-mcp-orchestrator`.

For any changed Editor C# file, also use `$mcp-unity-validate-script` and `$unity-recompile-menuitem`.

Keep editor tooling in Editor-only locations or assemblies:

- Use `Assets/Editor/...` for project-wide and subsystem-specific editor tools.
- Do not put Editor-only scripts under `Assets/Scripts`, including `Assets/Scripts/**/Editor` folders.
- Do not reference `UnityEditor` from runtime assemblies or gameplay scripts.

## Editor-Only Reflection Policy

Reflection, `TypeCache`, `SerializedObject`, `SerializedProperty`, `AssetDatabase`, `PrefabUtility`, `EditorUtility`, `EditorGUILayout`, UI Toolkit editor APIs, and UnityEditor scene/asset inspection APIs are allowed for Editor-only tools.

This permission is not transferable to runtime gameplay code. If a task touches runtime behavior, keep runtime code strongly typed and owner-driven; do not justify runtime reflection, scene-wide searches, or fallback ownership by pointing to this skill.

Prefer Unity editor-native discovery surfaces in this order:

1. `AssetDatabase`, `MonoScript`, `TypeCache`, `SerializedObject`, and UnityEditor APIs.
2. Reflection for editor inspection, generic type discovery, debug windows, and tooling bridges.
3. Local text search only for source-code facts that Unity APIs cannot expose.

Avoid Roslyn and generated parser code unless the tool's actual product requirement is source-code analysis.

## GitHub Research Gate

Before designing a custom long-lived Editor tool, search GitHub or package sources for existing Unity Editor solutions that already solve the same workflow.

Do this before proposing architecture or writing code unless the task is a tiny project-specific extension of an existing local tool.

For each relevant candidate, check:

- Whether it is an Editor-only Unity tool, package, sample, or source file that can realistically fit this project.
- License and whether copying, adapting, or depending on it is acceptable.
- Unity version/API compatibility and whether it uses UI Toolkit, IMGUI, overlays, inspectors, or package APIs that match the requested tool.
- Maintenance risk, scope, and whether the package would add more complexity than a small project-owned implementation.

Prefer an existing maintained package or reference implementation when it solves the workflow cleanly. If no suitable solution exists, summarize the search and then propose the smallest project-owned tool.

Do not copy external source into the project without explicitly identifying the license and asking when the license or ownership is unclear.

## Editor Surfaces And Visual Discipline

Be ready to work directly with Odin Inspector, normal Unity inspectors, InspectorWindow context, SceneView tools, overlays, gizmos, and handles. Choose the surface that matches the user's workflow instead of defaulting to a standalone window.

Surface defaults:

- Use minimal Odin attributes when the tool is mainly object inspection, inline authoring, validation, or small per-asset/per-component actions.
- Use Odin `[Button]` methods as a standard action surface for inspector tools, including buttons with arguments when that keeps the workflow local and explicit.
- Use `[Delayed]` on Odin/Inspector input fields when live per-character changes would trigger scans, validation, expensive refreshes, asset edits, noisy Undo records, or other non-trivial work.
- Use `[SerializeReference]` freely for editor-tool polymorphism, operation lists, filters, strategies, and debug/authoring state when it makes the Inspector workflow simpler; keep this editor-only and do not use it to add runtime ownership or hidden fallback state.
- When creating new long-lived `ScriptableObject` or `MonoBehaviour` types, include an icon pass by default. Prefer a suitable existing Unity/built-in/project icon first; if none fits, auto-generate a small minimal editor icon asset and attach it through the least invasive project-supported path such as `[Icon]`, importer metadata, or the existing icon tooling. Do not leave new authoring types visually anonymous in Project, Inspector, Hierarchy, or Add Component search.
- Use an Odin Attribute Processor when the inspector behavior should be applied consistently without adding attributes to every target type.
- Treat `[CustomEditor]` as an antipattern by default. Add a custom inspector only after proving that Odin attributes, an Odin Attribute Processor, normal serialized drawing, or a focused property drawer cannot express the workflow.
- When a `[CustomEditor]` is only adding preview UI, toolbar buttons, or other narrow side surfaces to an Odin-inspected target, derive from `OdinEditor`, call `base.OnEnable()`/`base.OnDisable()`, and leave Odin's normal inspector drawing in place. Do not replace `OnInspectorGUI()` with manual `SerializedProperty` fields unless the actual requirement is to replace the whole inspector; otherwise the custom editor silently disables Odin attributes, drawers, validation, and layout behavior.
- Use property drawers only for a reusable serialized value type, not as a broad inspector replacement.
- Use SceneView tools, overlays, gizmos, or handles when placement, spatial debugging, ranges, paths, volumes, points, or scene selection are the primary interaction.
- Use an `EditorWindow` only when the workflow needs cross-asset browsing, global search, batch operations, dashboards, or persistent multi-object state.

Keep editor UI visually minimal:

- Avoid decorative frames, nested boxes, repeated headers, large help panels, and redundant status messages.
- Do not duplicate information already visible in the Inspector, hierarchy, selected object, or SceneView label.
- Keep `EditorWindow`, overlay, tab, and panel titles short, readable, and scannable. Do not put workflow descriptions, selected-object details, state summaries, or long subsystem names in the title bar; show that information inside the content area only when it adds value.
- Avoid nested foldouts as a default structure. Use one flat scan/action surface, tabs, toolbar toggles, or compact sections when grouping is needed.
- Keep labels short and action-oriented. Prefer disabled controls, inline validation, subtle counts, and concise tooltips over paragraphs of explanatory text.
- Use color sparingly for state or risk only; do not make editor tools visually louder than the data they edit.
- For gizmos and handles, draw only the information needed for the current mode or selection, and keep occlusion/noise low.

## Undo And Serialized Editing

Every editor tool that mutates project assets, scene objects, prefabs, components, ScriptableObjects, or editor-owned state must support Undo unless the operation is explicitly read-only or generation-only and the user accepts that tradeoff.

Prefer built-in serialized editing paths:

- Use Odin Inspector's normal serialized drawing and mutation paths when implementing Odin-based tools or inspectors.
- Use `SerializedObject` and `SerializedProperty` for inspector/property drawer edits whenever possible.
- Use `Undo.RecordObject`, `Undo.RecordObjects`, `Undo.RegisterCreatedObjectUndo`, `Undo.DestroyObjectImmediate`, `Undo.SetTransformParent`, or the matching Unity Undo API when direct object mutation is required.
- Use `PrefabUtility.RecordPrefabInstancePropertyModifications` when editing prefab instances through direct object APIs.
- Mark objects/scenes dirty only after the Undo-aware mutation path is established.

Do not build custom cached mirror state that bypasses Undo. If a scan result feeds an Apply/Fix button, the button must mutate the real targets through Undo-aware APIs, not through hidden side effects during the scan.

For batch operations, create a clear Undo group name and collapse the group after all target mutations succeed. Report partial failures without continuing into unrelated targets.

## Design Rules

Make the tool maintainable as a real product surface:

- Name the tool by the authoring/debugging workflow it owns, not by a temporary task name.
- Keep one top-level C# type per `.cs` file.
- Separate editor UI, scanning/query logic, and mutation/apply actions when they are non-trivial.
- Use explicit Apply/Fix buttons for destructive or asset-mutating actions; previews/scans should be read-only.
- Show counts, target paths, and failure reasons in the window instead of relying only on console logs.
- For explicit user actions such as Apply, Fix, Generate, Rebuild, Scan, or Batch Process, emit one concise `Debug.Log()` summary with action name, affected count, skipped/failed count when relevant, and primary target path/context.
- Do not spam `Debug.Log()` from GUI repaint, selection change, validation refresh, handle drawing, or per-object loops. Aggregate action feedback into one summary line unless detailed diagnostics were explicitly requested.
- Persist user preferences with `EditorPrefs`, `SessionState`, or a project asset only when the state is useful between sessions.
- Do not add broad runtime services, singletons, or gameplay entry points for editor convenience.

For UI Toolkit tools:

- Prefer UI Toolkit for new long-lived windows and overlays.
- Keep USS styling in USS when the surface is larger than a tiny debug panel.
- Use stable element names/classes for callbacks and tests.
- Reuse `$uitoolkit-window-generation` when building substantial UI Toolkit windows.

## Verification

After implementation:

1. Validate every changed Editor C# script directly.
2. Force Unity recompilation and read console errors.
3. Open or execute the tool through Unity MCP when possible.
4. For visible windows/overlays, verify that the UI opens, renders non-empty, and callbacks do not throw.
5. For mutating tools, test on a narrow target or dry-run path first, then verify the changed assets through Unity APIs or git diff.

Do not clean, revert, or delete unrelated dirty files discovered while testing the tool.
