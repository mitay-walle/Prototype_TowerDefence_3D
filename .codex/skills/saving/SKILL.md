---
name: saving
description: "Work on Outcasts save/load persistence: the Saving namespace, SaveData/ISaveData/ISaveable/IChildSaveable, Saver, SaveFile/SaveMethodJSON, GameSaveState/GameRuntimeState, SceneSaveState, scene variant deltas, Entity/EntityComponent save DTOs, save slots/files, GUID identity, runtime-spawn restoration, and save/load tests. Use when Codex needs to add or review persisted game state, fix save/load bugs, design serialized runtime state, or wire a gameplay/UI system into existing saves."
---

# Saving

## Baseline

- Read `AGENTS.md`, `$apply-patch`, `$code-style`, and `$unity-mcp-orchestrator` before changing project files. For changed Unity C# or editor scripts, also use `$mcp-unity-validate-script` and `$unity-recompile-menuitem`.
- Keep the save design inside the existing `Saving.SaveData` / `ISaveData` / `ISaveable` contracts. Do not invent parallel `SaveDTO`, wrapper, bridge, migration, or persistence service layers when a `SaveData`-derived type can represent the state.
- Start by classifying the data owner: global game state, scene delta, root `ISaveable`, entity component state, or child saveable state. Pick the existing owner instead of adding a second entry point.

## Map

- Global run state: `GameSaveState` stores quests, world flags, passive skills, visited scenes, and per-scene `SceneSaveState`; `GameRuntimeState` owns the runtime copy and dispatch-facing mutations.
- Scene state: `SceneSaveState` stores persistent and variant `SceneObjectSaveState` deltas by `SceneReferenceKey`; `SceneVariantService` captures and applies active-scene deltas.
- Root saveables: `ISaveable` exposes `File`, `Origin`, `Data`, `Save()`, `Load(ISaveData)`, and `SpawnSceneObject()` through the returned `SaveData`.
- Entity aggregate: `Entities.Entity` is the `ISaveable` owner. `EntityComponent` implementations return `EntityComponentSaveDTO` subclasses; the entity assigns `ComponentId` and stores them in `EntitySaveDTO.Components`.
- Child saveables: `IChildSaveable` is for subordinate data owned by another runtime object, such as wallet-like state. Do not promote child data to independent scene identity unless it must load/spawn independently.
- Files and slots: `Saver` routes `eFile.Settings`, `eFile.Shared`, and `eFile.Scene` through `SaveFile`/`SaveMethodJSON`. Avoid adding new save files or slots unless the task explicitly needs a new persistence boundary.

## Workflow

1. Find nearby save/load code and tests with `rg` before designing new state. Prefer extending the closest existing `SaveData` hierarchy.
2. Decide the serialized shape first. Save data should be plain serializable data with field initializers or constructors; it must not hold dependencies or runtime behavior.
3. Decide the runtime owner next. Load from save state during the existing owner-driven initialization path, and save through the existing owner snapshot path.
4. Preserve identity. Do not hand-create `.meta` GUIDs, do not rewrite serialized save GUIDs casually, and do not make prefab asset/source GUID repair happen through raw file edits.
5. Add focused EditMode tests for new save/load behavior when the change touches persisted state, identity, scene deltas, or entity component save contracts.
6. If Unity-owned assets, scenes, prefabs, or ScriptableObjects must change, use Unity MCP, Unity APIs, or a focused Editor MenuItem; do not raw-edit serialized assets unless the change is tiny and well understood.

## Rules

- Persisted data classes should derive from `SaveData` or the established narrower save base, such as `EntityComponentSaveDTO`, when they participate in the save graph.
- `SaveData.SpawnSceneObject()` should return `null` for data that cannot restore an independent scene object. Only implement spawning when the saved data has a real resource/prefab path and runtime-spawn ownership.
- `EntityComponent` persistence belongs to `Entity`. Do not make an entity component implement independent `ISaveable` for data that belongs to the entity aggregate.
- Set the root `Entity.File` to the lifetime of the aggregate, not the scene where the prefab happens to be placed. If the player or another entity's components must survive scene travel, the root `Entities.Entity` must save to `eFile.Shared`; scene-only objects remain `eFile.Scene`.
- When a save bug appears as `PlayerAgent.Save()` -> `SceneVariantService.CaptureActiveSceneDeltas()` -> `Entity.Save()` recursion, first verify the player root `Entity` is not incorrectly marked `eFile.Scene`. Fix the ownership/file routing before adding re-entrancy guards, special-case filters, or independent player save owners.
- `SceneVariantService` captures only scene deltas. It must not be responsible for carrying the player or other cross-scene aggregates between scenes; those aggregates belong in `eFile.Shared` through their root `Entity`.
- When changing an `Entity` prefab's file ownership, update the serialized `Entity._file` and the nested `EntitySaveDTO.File` consistently through Unity MCP/Unity APIs so the runtime `SyncSaveHeader()` and future saved data agree. Do not raw-edit prefab YAML unless MCP is unavailable and the user explicitly approves that fallback.
- Keep `ComponentId` stable. If multiple components of the same type can exist on one entity, require explicit unique `_id` values instead of relying on type name collisions.
- Do not add `EnsureInitialized()`, lazy `??=` repair, migration/backfill normalization, corrupted-save recovery, or null fallback branches unless the user explicitly asks for migration or recovery.
- Treat required saved collection entries and payloads as present in runtime consumers. Validate at the boundary where the API actually accepts missing or corrupted data.
- Do not clone, allocate snapshots, mutate state, dispatch events, or perform load/save work from property getters. Use explicit methods such as `Create...Snapshot()` when copying is required.
- Do not add broad compatibility reads to legacy systems while replacing persistence. If migration is requested, read old data as source, write the new save shape, then remove the old runtime path.

## Verification

- For C# changes, validate changed scripts with Unity MCP, force Unity recompilation through `MCP/Force Recompile Scripts`, and read console errors.
- Run relevant EditMode tests around the touched save area, for example entity save tests, inventory save tests, quest/passive skill save tests, scene variant tests, or new focused tests.
- Inspect the final diff for accidental serialized asset churn, GUID changes, broad formatting, mojibake, and unrelated save file/schema changes.
