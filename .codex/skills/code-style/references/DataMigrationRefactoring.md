# Data Migration Refactoring

Use this reference when a refactor changes serialized data shape, replaces a type, moves data between runtime definitions, or needs temporary compatibility while project assets are rewritten.

## Default Position

Do not add migration code, conversion assets, backfill paths, compatibility branches, or dual-read runtime logic for a new system unless the user explicitly asks for migration or the current task is already a data refactor.

When migration is required, keep it short-lived and explicit:

- Read old data only as source data.
- Write the new canonical data shape.
- Remove runtime fallback reads and old compatibility paths after the project data has been rewritten and verified.
- Prefer Unity/AssetDatabase or prefab/scene migration workflows for Unity-owned serialized state.
- Do not hand-edit `.meta` GUIDs or serialized script GUID references.

## Replacing Types

Choose the canonical domain name for the replacement type. Do not name the replacement with quality/version wording such as `New`, `Fixed`, `Updated`, `Better`, `Improved`, `Final`, `V2`, or similar terms.

If the old type is not serialized through `[SerializeReference]`, it may be temporarily renamed from `MyName` to `MyNameObsolete`, keeping its existing `.cs.meta` with the old script asset. The replacement should take the canonical `MyName` type name. Use this only as a controlled migration step, not as permanent naming.

If the old type is serialized through `[SerializeReference]`, do not rename it casually. Unity stores the concrete managed reference type name, so renaming breaks serialized entries unless there is an explicit migration/backfill plan for those managed references.

## SerializeReference Changes

For polymorphic serialized collections, prefer `[SerializeReference]` when behavior genuinely varies by entry type and Odin's type picker is enough for authoring.

When converting an existing non-polymorphic list to `[SerializeReference]`:

- Treat existing assets as data that must be migrated, not as data Unity will automatically preserve.
- Create a temporary Editor migration path only when needed for existing assets.
- Migrate each old entry into the correct concrete managed-reference type.
- Verify the rewritten assets in Unity before deleting the temporary migration tool.
- Do not keep duplicate old fields as runtime fallback state after migration.

## Asset And Scene Data

For `.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, `.playable`, `.anim`, `.renderTexture`, or `.meta` data, use Unity MCP/Editor/AssetDatabase workflows unless the user explicitly approves a raw serialized-file fallback after being told MCP is unavailable.

Temporary migration tools should live under `Assets/Editor/`, use a clear `MenuItem`, and be deleted after the migration is run and verified unless the user asks for permanent tooling.

## Save Data

Persisted save data must use the existing `Saving.SaveData` hierarchy. Do not invent parallel `SaveDTO`, `SaveState`, wrapper, bridge, or migration types when an existing `SaveData`-derived contract can represent the data.

Do not add lazy `??=`, `EnsureInitialized()`, null repair branches, or scattered normalization for save data unless the user explicitly asks for corrupted-save recovery or old-save migration.
