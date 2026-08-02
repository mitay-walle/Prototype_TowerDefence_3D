# Unity Preset API And Filters

This reference is grounded in Unity's official Preset documentation. Verify API members with `unity_reflect` when writing code against a specific installed Unity version.

## API Surface

Namespace:

```csharp
using UnityEditor;
using UnityEditor.Presets;
```

Key types:

- `Preset`: stores serialized default values for a `UnityEngine.Object` target and can be saved as a `.preset` asset.
- `PresetType`: stores the target type a preset can apply to; use `preset.GetPresetType()` instead of manually constructing type metadata when possible.
- `DefaultPreset`: default-list entry with `enabled`, `filter`, and `preset` properties.

Creation pattern:

```csharp
Preset preset = new Preset(sourceObject);
AssetDatabase.CreateAsset(preset, "Assets/Presets/MyTarget Role.preset");
```

Update pattern:

```csharp
Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
preset.UpdateProperties(sourceObject);
EditorUtility.SetDirty(preset);
```

Default registration pattern:

```csharp
Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
PresetType presetType = preset.GetPresetType();
DefaultPreset[] existing = Preset.GetDefaultPresetsForType(presetType);

var entries = existing.ToList();
entries.Add(new DefaultPreset
{
    enabled = true,
    filter = "glob:\"Assets/UI/**/*.png\"",
    preset = preset,
});

Preset.SetDefaultPresetsForType(presetType, entries.ToArray());
AssetDatabase.SaveAssets();
```

Useful validation:

```csharp
if (!preset.IsValid()) throw new InvalidOperationException("Preset is not valid.");
if (!preset.GetPresetType().IsValidDefault()) throw new InvalidOperationException("Preset type cannot be used as a default preset.");
```

Official docs note that `Preset.GetDefaultPresetsForObject(target)` returns the ordered matching presets for an object. This is useful for audits and importer verification.

## Filter Strings

DefaultPreset filter text is compared against the object instance. Importer filters can match imported asset path details.

For advanced path filters, use glob syntax:

```text
glob:"pattern"
```

Unity documents these glob tokens:

- `*`: zero or more characters within one path segment, excluding `/`.
- `?`: exactly one character, excluding `/`.
- `[0-9]` or `[!a-z]`: one character from, or not from, a range/set.
- `(pattern-1|pattern-2)`: alternatives separated by `|`.
- `**`: zero or more folders/subfolders.

Glob filters are case-sensitive. Prefer explicit alternatives for case variants when project filenames are inconsistent.

Examples:

```text
glob:"Assets/UI/**/*.png"
glob:"Assets/Gizmos/**/*.png"
glob:"Assets/Graphics/**/Skybox/*.(png|jpg|exr)"
glob:"Assets/**/(*_N|*_n|*_Normal|*_normal).png"
glob:"Assets/**/(*_Mask|*_mask|*_ORM|*_orm).(png|tga)"
glob:"Assets/Audio/**/*.wav"
```

For default TextureImporter presets, broad entries should appear before narrow entries. Example order:

1. `TextureImporter Default` with `glob:"Assets/Graphics/**/*.(png|tga|jpg)"`
2. `TextureImporter Normal Map` with `glob:"Assets/Graphics/**/(*_N|*_Normal).(png|tga)"`
3. `TextureImporter UI Sprite` with `glob:"Assets/UI/**/*.png"`
4. `TextureImporter Editor Icon` with `glob:"Assets/Gizmos/**/*.png"`

## Official Documentation Links

- Unity Manual, Preset Manager: https://docs.unity3d.com/6000.4/Documentation/Manual/class-PresetManager.html
- Unity Scripting API, `Preset`: https://docs.unity3d.com/ScriptReference/Presets.Preset.html
- Unity Scripting API, `DefaultPreset`: https://docs.unity3d.com/ScriptReference/Presets.DefaultPreset.html
- Unity Scripting API, `PresetType`: https://docs.unity3d.com/ScriptReference/Presets.PresetType.html
