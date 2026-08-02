---
name: default-preset-authoring
description: Create, audit, or update Unity Preset assets and Preset Manager default preset registrations for project-owned ScriptableObject, MonoBehaviour, Component, and importer defaults. Use when Codex needs to create `.preset` assets, set default values for new SO/MonoBehaviour/components, configure default TextureImporter presets, or add Preset Manager filters such as glob path filters for different texture families.
---

# Default Preset Authoring

Use this skill for durable Unity default Preset work. Also load `$apply-patch`, `$code-style`, `$unity-mcp-orchestrator`, `$create-assets-menu-item`, `$mcp-unity-validate-script`, and `$unity-recompile-menuitem` when writing Editor tooling.

## Core Rules

- Treat `.preset` assets and Preset Manager default lists as Unity Editor-owned state. Prefer Unity MCP, Unity Editor APIs, or a focused Editor MenuItem. Do not hand-edit `.preset`, `.meta`, importer `.meta`, or ProjectSettings YAML.
- Store project-owned presets under `Assets/Presets` unless a nearby established preset folder is more specific.
- Name preset assets by target type and role: `TextureImporter Editor Icon.preset`, `TextureImporter Skybox Cubemap.preset`, `CanvasScaler.preset`, or `MySettings.preset` for a singleton/default settings type.
- For ScriptableObject singleton/config/database defaults, create presets from a temporary or selected instance of the exact target type and name the preset after the class/contract role.
- For MonoBehaviour or Component defaults, create the preset from a temporary GameObject with only the target component and required same-object dependencies. Destroy the temporary object after creating the preset.
- For importers, create the preset from a real importer instance such as `TextureImporter` from a representative source asset. Do not write importer serialized properties by hand.
- Register defaults through `UnityEditor.Presets.Preset.GetDefaultPresetsForType` and `Preset.SetDefaultPresetsForType`, preserving existing entries unless the task explicitly asks to replace them.
- Keep default preset order intentional. Unity evaluates default presets in list order, and later matching filters can override earlier ones.
- Default importer presets apply to newly imported assets. Do not claim that adding a default preset retroactively updates existing textures; existing assets need an explicit reset/reimport/apply workflow requested by the user.

## Texture Filters

When the same importer type needs different defaults, add several default presets with filters instead of creating importer scripts. For textures, use `TextureImporter` presets plus Preset Manager filters.

Read `references/unity-preset-api.md` before writing filter strings or Editor code that modifies default preset lists.

Common texture filter shapes:

```text
glob:"Assets/UI/**/*.png"
glob:"Assets/Gizmos/**/*.png"
glob:"Assets/Graphics/**/Skybox/*.png"
glob:"Assets/**/(*_N|*_n|*_Normal|*_normal).png"
glob:"Assets/**/(*_Mask|*_mask|*_ORM|*_orm).(png|tga)"
```

Use path-based filters for project folders when possible. Use suffix filters only when the naming convention is stable and audited.

## Workflow

1. Identify the target type and owner: ScriptableObject, MonoBehaviour/Component, or importer.
2. Inspect existing presets in `Assets/Presets` and existing default preset registrations before creating anything.
3. Create or update a source object through Unity APIs with the intended default values.
4. Create the `.preset` asset through `new Preset(source)` and `AssetDatabase.CreateAsset` or update an existing preset with `Preset.UpdateProperties(source)`.
5. Register the preset as a default only when requested, using `DefaultPreset { enabled = true, filter = filterText, preset = preset }` and preserving other entries for that `PresetType`.
6. For multiple texture presets, order broad fallbacks first and narrower overrides later.
7. Verify the preset target type, default list entries, filters, and console output through Unity/MCP. For importer presets, test on a safe new scratch asset or report that no runtime asset import test was run.
8. Delete only current-run temporary Editor scripts and temporary scratch assets after successful verification, unless the user asked for permanent tooling.

## Safety

- Do not create fallback `AssetPostprocessor` scripts just because a Preset Manager filter is hard to express. Use an importer processor only when the user explicitly asks to apply defaults to existing assets or Preset Manager cannot represent the required rule.
- Do not add default presets for Project settings, Preferences settings, Materials, Animations, or SpriteSheets; Unity documents those as unsupported default-preset targets.
- Do not replace existing default preset lists wholesale. Append, update, reorder, or remove the exact entry requested.
- Do not infer texture filters from generated/vendor/sample paths. Use project-owned folders and naming conventions only.
