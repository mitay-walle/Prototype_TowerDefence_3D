---
name: ui-prefab-authoring
description: Unity UI prefab authoring and rearrangement rules for Outcasts. Use when creating, editing, moving, nesting, or deduplicating uGUI/UI Toolkit prefabs, especially when changing RectTransform hierarchy, Canvas, CanvasScaler, GraphicRaycaster, raycast targets, window prefabs, timeline-bound UI, or reusable UI prefab templates.
---

# UI Prefab Authoring

## Scope

Use this with `$prefab-creation` and `$unity-mcp-orchestrator` for Unity-owned prefab state. Use `$ui-prefab-localization` too when changing localized TMP text or string table bindings.

Edit prefab assets through Unity Editor APIs/MCP/MenuItems, not raw prefab YAML. Keep C# changes under `$code-style`, `$behaviourinject` for DI, and the normal validate/recompile flow.

## Reparenting Rules

Before moving existing uGUI under a new parent, inspect the source object while it is active in the loaded prefab or scene. Inactive UI often reports zero-size layout; do not use that inactive state to infer anchors, corners, or screen coverage.

Preserve these `RectTransform` values from the active source object unless the requested change explicitly wants a layout change: `anchorMin`, `anchorMax`, `pivot`, `anchoredPosition`, `sizeDelta`, `offsetMin`, `offsetMax`, local rotation, and local scale.

If a moved UI root has a `CanvasScaler`, treat that object as a fullscreen UI root. When it becomes a child under another owning Canvas, keep it fullscreen by setting stretch anchors (`anchorMin=(0,0)`, `anchorMax=(1,1)`) and zero offsets, then remove the nested `CanvasScaler`. Keep only the owning/root CanvasScaler for that UI surface.

Avoid keeping nested `Canvas`/`GraphicRaycaster` components unless the UI needs independent sorting, render mode, or input isolation. Decorative or timeline-driven nested UI should not have a `GraphicRaycaster`.

## Input And Raycasts

Interactive UI should have one clear raycast path: the owning window/root Canvas has the `GraphicRaycaster`, and actual controls keep their needed raycast targets.

Disable `raycastTarget` on decorative Images/TMP text, cinematic frames, subtitles, backgrounds, masks, and non-interactive overlays that sit above buttons. After moving overlay UI into a window, put buttons or controls later in sibling order when they must render and receive hits above decoration.

Do not use a `Graphic` with alpha `0` as an invisible clickable target; Unity raycast filtering can make fully transparent graphics stop receiving pointer hits. Use the project's `NonDrawingGraphic` component for invisible hit areas instead.

Do not fix blocked pointer events with lifecycle flags, lazy setup, or duplicate input paths. Inspect the hierarchy for Canvas order, `GraphicRaycaster`, `CanvasGroup.blocksRaycasts`, active state, sibling order, and decorative `Graphic.raycastTarget` first.

## Bindings

Before removing or moving UI objects from a prefab, search for references from `PlayableDirector`, Timeline tracks/clips, UnityEvents, animation bindings, serialized fields, and localization components. Moving the visual hierarchy does not preserve bindings automatically.

If a Timeline-bound UI target is moved out of a cutscene prefab into a shared UI/window prefab, make the runtime owner explicitly pass the shared component into `PlayableDirector` bindings before `Play()`. For Timeline assets that use both track bindings and `ExposedReference`, set both `SetGenericBinding(...)` and `SetReferenceValue(...)` as needed.

Do not leave duplicate UI in every cutscene prefab to satisfy bindings. Prefer one shared UI prefab/window plus explicit rebinding by the flow that starts the cutscene.

## Naming Rules

Name a UI prefab root object, or an entity root object with a facade script, after the script type, for example `ListViewUI`.

Name child UI objects as `Type|Role - Purpose Words`, with role omitted when there is only one primary UI type or component. Use spaces in the purpose. Examples: `Panel - Store Items`, `Image - Background`, `Toggle - Close Window`, `ScrollRect - Items List`, `Image|ScrollRect - Complex Logic Object`.

## Creation Rules

## UI Kit Composition

For new gameplay windows and replacement UI, start from project UI Kit prefabs under `Assets/Prefabs/UI/UIKit/` instead of copying legacy window or HUD visuals. Treat existing non-UIKit UI as behavior/reference material only unless the user explicitly asks for a variant of that legacy prefab.

Use the nearest UI Kit prefab for each repeated visual role: `WindowAppear` for the window shell/animation, `Button`, `Button Hotkey Window`, or `Button Hotkey HUD` for buttons, `Toggle` or `Tab Toggle Variant` for tabs, `Label - Title` and `Label - SubTitle` for text labels, `Scrollbar` for scrollbars, `Blur`/`line` for supporting chrome, and `Hotkey` only as an approved nested input-hint piece.

When a screen needs repeated rows, cards, or entries, create a small project-owned row prefab that is visually composed from UI Kit parts and serialize that prefab into the window. Runtime code may instantiate those row prefabs and bind data/callbacks, but must not procedurally build the whole window or recreate UI Kit visuals in `Awake`, `Start`, or fallback factories.

When a repeated UI template is owned by one specific window and is not reused elsewhere, prefer a hidden local template object inside that window prefab over creating a separate prefab asset or variant. Serialize the local component reference into the owner, keep the template compatible with `Instantiate`, and hide it through the existing window/template holder pattern rather than adding runtime construction or lookup.

If an existing UI Kit prefab is missing a required variant for shared reuse, create the smallest standalone UI Kit prefab in `Assets/Prefabs/UI/UIKit/` first, then use it from the feature prefab. Do not make new UI Kit prefabs inherit from old non-UIKit prefabs; reuse only explicitly approved nested pieces.

Create visible UI text with TextMeshPro only: use `TextMeshProUGUI` components and serialized references typed as `TMPro.TMP_Text`. Do not add new `UnityEngine.UI.Text`, `Text`, legacy UI Text components, or fallback fields for them.

Put every created uGUI or UI Toolkit GameObject on the Unity `UI` layer, including roots, canvases, UIDocument hosts, controls, labels, and generated child objects. Do not leave UI objects on `Default`.

Use `RectMask2D` as the default masking component for scrollable/clipped rectangular UI. Use `Mask` only when the UI explicitly needs stencil/image-based masking behavior that `RectMask2D` cannot provide.

For flat-color UI fills, do not create or require dedicated sprite artwork. Use a `UnityEngine.UI.Image` with the shared solid-color sprite assigned, `Image.type = Simple`, and the fill color set on the `Image`.

For UI plate outlines, use `UnityEngine.UI.ProceduralImage.ProceduralImage` with `UniformModifier` instead of sprite artwork or plain `UnityEngine.UI.Outline`, so the outline can share rounded-corner geometry and be animated cleanly.

For gradient UI fills, use the same `Image` setup as a flat fill, then add `UnityEngine.UI.Extensions.GradientEffect` with `ModifyVertices` enabled. Configure the gradient on `GradientEffect`; do not replace the shared solid-color sprite or switch the `Image` away from `Simple`.

Create and assign serialized references in the prefab asset. Runtime code may bind data and callbacks to existing serialized controls, but should not create the whole UI hierarchy in `Awake`, `Start`, or fallback factories.

For windows managed by `UIManager`, implement the existing window contract and keep UI services injected directly into the view/controller that uses them. Per-call window data belongs in args; injected services must not be passed through args.

## Verification

After UI prefab rearrangement, verify through Unity/MCP or Editor APIs:

- moved fullscreen UI still has stretch anchors and zero offsets under the new parent;
- nested `CanvasScaler` components were removed under an owning Canvas;
- decorative overlays have no `GraphicRaycaster` and no `raycastTarget` graphics;
- expected buttons/controls remain clickable and are not covered by decorative siblings;
- moved UI targets are no longer duplicated in source prefabs;
- every created uGUI/UI Toolkit object is on the `UI` layer;
- visible text uses TextMeshPro, not legacy `UnityEngine.UI.Text`;
- serialized fields, Timeline bindings, `ExposedReference` values, UnityEvents, localization bindings, and console errors are checked.
