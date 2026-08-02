---
name: prefab-creation
description: Create, edit, and verify Unity prefab assets in this project. Use when Codex needs to build gameplay prefabs, UI prefabs, prefab variants, reusable UI templates, or convert runtime-created UI/object hierarchies into prefab assets.
---

# Prefab Creation

## Core Rule

Create or edit prefab assets through Unity Editor APIs, Unity MCP, or a focused Editor MenuItem. Do not hand-edit `.prefab` YAML unless the change is tiny, well understood, and Unity/MCP is unavailable or unnecessary. Preserve existing `.meta` GUIDs.

Never hand-create Unity `.meta` files or invent GUIDs for prefab assets. Never edit, replace, swap, regenerate, or migrate GUID values in `.meta`, `.prefab`, `.unity`, `.asset`, or script `.cs.meta` files to resolve missing scripts, class merges, or component migrations. Use Unity/AssetDatabase/component migration paths instead.

Use `.asset` for Unity `ScriptableObject` assets created through `AssetDatabase.CreateAsset`, including `Tile` assets. Do not use custom extensions such as `.tile`.

## UI Rule

If the task asks to create UI, create or edit a prefab. Do not implement the requested UI as runtime hierarchy creation in `Awake`, `Start`, or ad hoc factory code.

All uGUI text must be TextMeshPro. Create visible UI text with `TextMeshProUGUI` and serialized references as `TMPro.TMP_Text`; do not create or keep legacy `UnityEngine.UI.Text` components or compatibility fallback fields.

All created uGUI and UI Toolkit GameObjects must be assigned to the Unity `UI` layer, including prefab roots, canvases, UIDocument hosts, controls, labels, and generated children. Set the layer explicitly in Editor scripts/MenuItems instead of relying on Unity defaults.

When rearranging existing uGUI prefab hierarchy, preserve layout while the moved objects are active in the Editor: read and keep their `RectTransform` anchors, pivots, offsets, size delta, and anchored position from the active object state before reparenting or saving. If a moved child UI root has a `CanvasScaler`, treat it as a fullscreen root and preserve/stretch it as fullscreen under the new parent. Remove nested `CanvasScaler` components after moving UI under an existing owning Canvas; keep only the owning root CanvasScaler.

Acceptable runtime code for UI:

- Bind callbacks and data to serialized prefab instances.
- Instantiate repeated item/row prefabs from serialized prefab references.
- Toggle state/classes and update existing component values.

Not acceptable as the main implementation:

- Creating the whole Canvas, controls, labels, buttons, or UI Toolkit window tree at runtime.
- Replacing a missing prefab with procedural UI construction in code.
- Adding broad runtime factories because a prefab was not created.

## Workflow

1. Inspect nearby prefabs and target folders first, especially under `Assets/Prefabs/` and `Assets/Prefabs/UI/`.
2. Choose the nearest existing folder; do not create a redundant folder for a single prefab when a shared category folder exists.
3. For new or bulk prefab work, prefer a small Editor MenuItem that builds the hierarchy, assigns components/references, saves the prefab with `PrefabUtility.SaveAsPrefabAsset`, and logs the output path.
4. Reuse existing project graphics and prefab sources for visible content before creating new visuals. Do not leave cubes, primitive placeholders, generated-looking markers, missing/error materials, or default Unity materials as deliverable prefab or scene content unless the user explicitly asked for a blockout.
5. Use prefab variants when the new asset mostly specializes an existing prefab.
6. For repeated visible objects, create or reuse one shared prefab or prefab variant, then place instances. Do not duplicate independent scene hierarchies with copied meshes/renderers/materials for repeated content.
7. For active world-object graphics, align renderer bounds to the terrain/floor when authoring or placing the prefab. Do not leave visible content floating, sunken, or positioned by arbitrary placeholder heights.
8. For project-owned prefab asset names, follow `.codex/docs/ProjectStructure.md`: repeated prefab assets use `Type Domain Tags`, with the root owner type first. A single canonical infrastructure/component prefab may use the exact script/class name only when it is not part of a repeated content family.
9. For UI prefabs, use `$ui-prefab-authoring` and add required components and serialized references in the prefab asset, not in runtime setup code.
10. If localization is involved, use `$ui-prefab-localization` and bind user-facing TMP text with `LocalizeStringEvent` or serialized `LocalizedString` references.
11. If UI Toolkit is involved, use `$uitoolkit-window-generation`; the prefab should hold the `UIDocument`, visual tree/panel references, and controller components as assets/components, not create the window from scratch at runtime.
12. Verify through Unity/MCP, AssetDatabase queries, prefab inspection, and console errors. For visible UI, also verify layout/screenshot when practical.
13. Delete only temporary one-shot Editor/MenuItem generation scripts and their `.meta` files that this current agent explicitly created before finishing, unless the user explicitly asked for a repeatable project tool. Do not delete other dirty or untracked assets as cleanup. After deleting owned temporary tooling, run Unity recompile and read console errors/warnings.

## Migration Work

When replacing a legacy gameplay system, do not keep runtime fallbacks, compatibility branches, or dual-read paths to old prefab components. Migration may read old prefab/component values only as source data, then must write the new component configuration, remove the old components from prefabs/scenes, and delete or obsolete the old scripts/assets after a reference scan.

For Stat/Reserve migration specifically, copy required values into Stat/Reserve config, then remove legacy `ParametersComponent`, `VitalsComponent`, `VitalResourceData`, `VitalResource`, `ParametricValue`, and old Vital/Vitals assets from prefabs/scenes. Do not add or preserve fallback reads/writes to those legacy types.

## Script Changes

If prefab creation requires C# scripts or Editor tooling, load `$code-style` first. Keep one top-level type per `.cs` file. After C# changes, run `$mcp-unity-validate-script`, then `$unity-recompile-menuitem`.

## Safety

- Do not edit package source, embedded packages, `Packages/`, or `Library/PackageCache` to create prefabs.
- Do not invent `.meta` GUIDs for existing assets.
- Do not leave scene-only objects as the final deliverable when the request is for reusable UI or gameplay content; save a prefab asset.
- Keep diffs scoped to the requested prefab, its required scripts, and generated assets.
- Cleanup after prefab generation means removing owned one-shot tooling only; it does not include deleting materials, TMP assets, prefab artifacts, scene changes, localization changes, `.meta` files, or any dirty/untracked file whose ownership is not certain.
- Do not infer ownership from timing, generated-looking names, missing references from the target prefab, or a clean reference search. If a file may belong to another chat or user workflow, leave it untouched and report it.
