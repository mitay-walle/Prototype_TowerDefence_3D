---
name: localization-table-authoring
description: Add, repair, audit, or synchronize entries in Unity Localization string tables and shared table data. Use when Codex needs to add player-facing text keys/translations, fix "No translation found" errors, compare shared keys against locale tables, update StringTable assets under Assets/Localization, or create temporary Editor tooling for LocalizationEditorSettings/StringTableCollection work. For prefab component bindings, LocalizeStringEvent wiring, or UI prefab text binding, use ui-prefab-localization instead.
---

# Localization Table Authoring

Use this skill for table data: keys, shared IDs, locale values, and missing-entry audits. Use `$ui-prefab-localization` for prefab bindings that reference existing table entries.

## Baseline

Always load the project baseline skills first when working in Outcasts:

- `$apply-patch` for any text script edits.
- `$code-style` before creating or changing Editor C#.
- `$unity-mcp-orchestrator` for Unity Editor routing and asset state.

If creating a temporary Editor MenuItem, also use:

- `$create-assets-menu-item`
- `$mcp-unity-validate-script`
- `$unity-recompile-menuitem`

## Workflow

1. Identify the table collection and key(s) from the error, asset, prefab, or code path.
2. Search `Assets/Localization` and the referencing assets with `rg`; compare shared data IDs with locale table IDs.
3. With one Unity Editor, use the typed Unity MCP tools directly and do not resolve an instance. Resolve the target Unity MCP instance and check `mcpforunity://custom-tools` only when multiple Unity Editors or projects are active.
4. Prefer Unity Editor APIs over raw `.asset` YAML edits:
   - Use `LocalizationEditorSettings.GetStringTableCollection(tableName)`.
   - Use `collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key)`.
   - For each `StringTable`, use `table.GetEntry(sharedEntry.Id) ?? table.AddEntry(sharedEntry.Id, value)`.
   - Set dirty on modified tables and shared data, then `AssetDatabase.SaveAssets()` and `AssetDatabase.Refresh()`.
5. Use a temporary `Assets/Editor/...` MenuItem when no existing project tool performs the exact table update.
6. Execute the MenuItem through Unity MCP, verify the console, then delete temporary generation scripts and their `.meta` files after successful asset updates.
7. Re-audit shared and locale tables to confirm every required shared ID has a locale row.

## Table Rules

- Use the domain table that owns the text: `UI` for UI labels/prompts/buttons, `Items` for item names and descriptions, `Dialogue` for dialogue and quest text, `StatusEffects` for status effects, and `Parameters` for parameter labels. Do not put ordinary domain text in `Misc` or code fallbacks when a domain table exists.
- `Assets/AddressableAssetsData/AssetGroups/Localization-*` groups are generated/managed by Unity Localization. Do not hand-create new `Localization-*` Addressable groups for ordinary content.
- Preserve existing shared entry IDs. Do not hand-generate or rewrite localization IDs.
- Add missing locale rows by shared ID, not by duplicating key text in serialized YAML.
- Do not overwrite non-empty translations unless the user asked to revise the translation.
- If a locale value is missing and no translation is provided, ask when the language quality matters. For unblocker/debug fixes, use a clear fallback value only when the user accepts fallback text or the project already follows that pattern.
- Keep player-visible text localized; do not fix missing table entries by adding code fallbacks in UI or gameplay logic.
- Keep table changes narrowly scoped to the requested keys unless an audit shows adjacent required rows are part of the same broken batch.

## Audits

For missing-entry errors, compare shared keys to locale rows before editing. A quick text audit is acceptable for diagnosis:

```powershell
rg -n "Key Name|m_Id: <id>" Assets\Localization
```

When auditing serialized tables, treat the shared data as the source of key-to-ID truth and locale tables as ID-to-localized-value maps. Missing locale rows with present shared IDs cause runtime/editor `No translation found` errors for the active locale.

## Temporary MenuItem Pattern

Use a single editor-only static class under `Assets/Editor/`:

```csharp
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class CodexEnsureLocalizationEntries
{
	private const string MenuPath = "MCP/Ensure Localization Entries";
	private const string TableName = "Items";

	[MenuItem(MenuPath)]
	public static void Ensure()
	{
		StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
		if (collection == null)
		{
			Debug.LogError($"Localization table collection not found: {TableName}");
			return;
		}

		EnsureEntry(collection, "Example Key", "Example RU", "Example");
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[Codex] Ensured localization entries in {TableName}");
	}

	private static void EnsureEntry(StringTableCollection collection, string key, string ru, string en)
	{
		SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);
		for (int i = 0; i < collection.StringTables.Count; i++)
		{
			StringTable table = collection.StringTables[i];
			string value = table.LocaleIdentifier.Code.StartsWith("ru") ? ru : en;
			StringTableEntry entry = table.GetEntry(sharedEntry.Id) ?? table.AddEntry(sharedEntry.Id, value);
			if (string.IsNullOrWhiteSpace(entry.Value))
				entry.Value = value;

			EditorUtility.SetDirty(table);
		}

		EditorUtility.SetDirty(collection.SharedData);
	}
}
```

After executing the MenuItem, remove this temporary script unless the user asked for permanent tooling.
