---
name: item-content-authoring
description: Create, complete, or audit Outcasts item content. Use when adding ItemConfig assets, wiring item icons or pickup models, filling localization, updating ItemConfigDatabase, or reconciling existing partial item content such as PNG icons, FBX pickup models, generated icons, and missing item configs.
---

# Item Content Authoring

Use this skill for Outcasts item content work. Also use the baseline project skills: `$apply-patch`, `$code-style`, and `$unity-mcp-orchestrator`; use `$prefab-creation` when creating or editing pickup prefabs.

## Asset Priority

- Prefer existing project content over generated content.
- If a normal project icon/model exists for an item, wire that asset into the ItemConfig. Do not replace it with a generated icon/model.
- Use generated icons or generated models only as a fallback when no suitable existing project asset is available and the user actually wants generated filler.
- When existing partial content exists for an item, such as only an icon or only a pickup model, create or complete the ItemConfig and related registration instead of generating replacement content.
- If only an icon exists and no pickup model exists, leave `_pickupPrefab` empty so the item can use the configured fallback. Do not fabricate a model unless requested.

## Workflow

1. Inventory existing ItemConfig assets under `Assets/ScriptableObjects/Items`.
2. Inventory item icons under `Assets/UI/Items`, including non-generated icons first; compare by GUID references, not only by filename.
3. Inventory pickup models/prefabs under `Assets/Graphics/Models/Assets/DropedItems` and existing pickup prefabs.
4. For each candidate:
   - If ItemConfig exists but points at `Assets/UI/Items/GeneratedIcons` while a normal matching icon exists, switch the ItemConfig to the normal icon.
   - If icon/model exists but ItemConfig is missing, create the closest matching ItemConfig from an existing template in the same category.
   - Preserve existing category, item classification, stack size, trade/use components, fallback, and pickup behavior from the closest local template unless there is a clear item-specific reason to change them.
5. Add or update item localization keys in `Assets/Localization/Items`.
6. Register new ItemConfig ids in `Assets/Resources/ItemConfigDatabase.asset`.

Item content is not complete as an isolated asset. When creating item content, complete the chain that applies to the item: `ItemConfig`, icon, optional pickup model/prefab, `Items` localization rows, and `ItemConfigDatabase` registration. If part of the chain is intentionally missing, report that explicitly.

## Database And Validation

- Prefer Unity Editor/MCP or a temporary Editor MenuItem for ScriptableObject, localization, and database changes.
- Delete temporary Editor scripts after use and recompile Unity.
- Before using the database `Collect()` helper, check for duplicate ItemConfig ids. If duplicates already exist, avoid letting `Collect()` clear the database and then fail; rebuild/register entries with explicit duplicate handling and preserve the previous selected item for existing duplicates.
- Verify changed ItemConfigs reference the intended icon/model GUIDs, localization entries exist for English and Russian, and `ItemConfigDatabase` contains the expected id.
- Report any pre-existing duplicate ids or Unity package warnings separately from the item-content change.
