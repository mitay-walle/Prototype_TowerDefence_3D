---
name: ui-prefab-localization
description: Add or review Unity Localization bindings in uGUI/TMP UI prefabs. Use when Codex needs to add UI string table entries, attach or configure LocalizeStringEvent components, bind LocalizedString fields, or verify localized text in UI prefabs under Assets/Prefabs/UI.
---

# UI Prefab Localization


## Scope

Use this skill for uGUI/TMP prefab localization. Use `$ui-prefab-authoring` too when the localization task also changes prefab hierarchy, layout, Canvas/CanvasScaler, GraphicRaycaster, raycast targets, or reusable UI template structure. For Editor-owned prefab and localization table changes, prefer Unity MCP, Unity APIs, or a small Editor MenuItem over raw YAML edits. Hand-edit `.prefab`, `.asset`, or `.meta` files only for small, well-understood serialized changes when Unity/MCP is unavailable or unnecessary.

## Project Defaults

- UI string table collection: `UI`.
- UI text belongs in the `UI` table. Do not put UI labels, prompts, button captions, notifications, or window text in `Misc`, item/dialogue tables, code fallbacks, or hand-created `Localization-*` Addressable groups.
- UI shared table GUID: `bd5b6453b6f8bea41b8b62814dc13054`.
- Table assets: `Assets/Localization/UI/UI Shared Data.asset`, `Assets/Localization/UI/UI_en.asset`, `Assets/Localization/UI/UI_ru.asset`.
- UI text is always TextMeshPro: use `TextMeshProUGUI` components and `TMPro.TMP_Text` references. Do not create, bind, localize, or leave legacy `UnityEngine.UI.Text` in prefabs.
- TMP prefab text normally uses `UnityEngine.Localization.Components.LocalizeStringEvent` targeting `TMPro.TMP_Text.set_text`.
- Any player-visible text added from code must also use localization table entries, except numeric-only or symbol-only content. This includes HUD labels, notification titles/messages, button captions, prompts, and compact abbreviations.

## Workflow

1. Inspect the target prefab and existing nearby localized prefabs before changing anything.
2. Reuse an existing UI key when it already represents the exact text. Otherwise add a new key to the `UI` table with both English and Russian values.
3. Use Unity Localization APIs or an Editor MenuItem to add table entries so the shared data gets a stable `m_Id`. Do not invent or duplicate `m_Id` values by hand.
4. Add or configure `LocalizeStringEvent` on the TMP text object:
   - `m_TableReference.m_TableCollectionName` should point to `GUID:bd5b6453b6f8bea41b8b62814dc13054` or the `UI` collection through Unity APIs.
   - `m_TableEntryReference.m_KeyId` must be the real shared table entry id, not `0`, for a finished binding.
   - `m_UpdateString` should call `set_text` on the target `TMP_Text`.
5. Leave serialized fallback `m_text` readable, but treat the localization binding as the source of truth.
6. Verify the prefab through Unity/MCP or a focused serialized inspection, then read console errors if scripts or Editor tools changed.

## Code Changes

If localization requires C# changes, load `$code-style` first. Prefer serialized `LocalizedString` or `LocalizeStringEvent` references for UI data. Use `LocalizationSettings.StringDatabase.GetLocalizedString("UI", key)` only for runtime lookup cases where a serialized binding is not practical, and add the key to both English and Russian UI tables in the same change.

## Safety

- Keep Cyrillic as valid UTF-8; never introduce mojibake, replacement characters, or escaped Unicode as a workaround.
- Do not edit package source or `Library/PackageCache` localization code.
- Do not leave a prefab with `LocalizeStringEvent` pointing at an empty table or `m_KeyId: 0` unless the task is explicitly to create an unbound template.
- Do not localize debug-only, icon-only, or numeric-only text unless it is user-facing language.
