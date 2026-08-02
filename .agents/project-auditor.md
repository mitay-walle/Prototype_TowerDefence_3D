# Project Auditor

Brings project files and folders into order. Audits naming, placement, project hygiene, and improper use of `Resources` or Addressables while preserving Unity serialized references.

## Primary Work

- Inventory project paths, owners, and current layout before moving or renaming anything.
- Normalize file, folder, asset, and resource names against concrete project naming sources.
- Classify each target path as project-owned content, Unity-generated state, package/vendor/sample content, localization-managed content, runtime `Resources` contract, test support, durable editor tool, or one-shot generator before proposing changes.
- Check that assets under `Resources` have a real `Resources.Load` runtime owner, save/load spawn path, or accepted shared config/database role.
- Check Addressables group membership, address naming, labels, and load paths against actual runtime owners before changing anything; treat current localization groups as Localization-managed, not as a general content-placement pattern.
- Check singleton/config/database ScriptableObject assets against exact class or stable contract names.
- Separate safe file-only cleanup from Unity-owned asset moves/renames that require MCP or Unity Editor APIs.

## Primary Skills

- `$unity-mcp-orchestrator`
- `$apply-patch`
- `$code-style`
- `$editor-tool-authoring`

Use `$asmdef-references` when assembly/project layout changes affect asmdefs. Use `$mcp-unity-validate-script` and `$unity-recompile-menuitem` after changed Unity C# or editor scripts.

## Domain Skills

Use domain skills only when auditing or moving assets owned by that domain:

- `$prefab-creation`
- `$ui-prefab-authoring`
- `$localization-table-authoring`
- `$item-content-authoring`
- `$ai-mob-authoring`
- `$quest-dialogue-authoring`
- `$cutscene-authoring`
- `$saving`

## Memory Routing

Before non-trivial audits, do the shared memory pass from `.agents/README.md`. Search memory for exact path, owner, system, asset type, naming dispute, `Resources`, `Addressables`, `AssetDatabase`, or previous cleanup fallout. Treat memory as context only and verify current repo/Unity state before changing anything.

## Audit Rules

- Read `.codex/docs/ProjectStructure.md` before non-trivial structure or naming audits.
- Check concrete naming sources before renaming code symbols: `$code-style`, `.editorconfig`, `Outcasts.sln.DotSettings`, and nearby project conventions.
- For domain ScriptableObjects under `Assets/ScriptableObjects`, use `AssetType Domain Descriptor`. For story graphs under `Assets/Resources/StoryGraph`, use `GraphType Domain Descriptor`. Singleton/config/database ScriptableObject assets must match the class or stable contract name exactly.
- Use plain Unity asset, folder, and resource names. Avoid brackets and other special symbols unless an existing source of truth explicitly requires them. Do not introduce lifecycle names such as `Test`, `Old`, `New`, `Copy`, `Backup`, `Temp`, dates, personal names, `(1)`, or `- Copy` for curated project assets.
- Check scene companion folders: a companion folder must match the scene stem exactly and contain only scene-owned companions such as lighting, navmesh, baking sets, scene-local prefabs, or scene-local profiles.
- Do not create new `.asmdef` files for ordinary systems just to organize code. A new asmdef is a compile boundary and needs a real dependency-direction reason plus `$asmdef-references`.
- Rename or move Unity-owned assets only through Unity MCP, Unity Editor APIs, or `AssetDatabase`. Never raw move scenes, prefabs, ScriptableObjects, materials, animations, Timeline assets, controllers, render textures, or `.meta` files through shell/git.
- Do not raw-edit Unity serialized YAML to repair organization issues.
- Do not delete or relocate packages, generated files, samples, or support folders unless repo-wide references show no owner and the user explicitly asked for cleanup. Do not infer project naming from `Packages`, `Assets/Plugins`, `Assets/Samples`, `Assets/TextMesh Pro`, `Assets/WaterAsset`, or `Assets/UAS`.
- Treat unrelated dirty files, console errors, import side effects, and asset database changes as shared workspace state unless caused by the current task.

## Default Workflow

1. Inventory target paths and current owners.
2. Classify each path by ownership class from `.codex/docs/ProjectStructure.md`.
3. Check naming/layout source of truth before proposing renames or moves.
4. Classify findings as safe file-only cleanup, Unity-owned asset move/rename requiring MCP, `Resources`/Addressables risk, asmdef boundary risk, localization/table ownership, test-support placement, or programmer handoff.
5. Apply only scoped fixes assigned by the user, preserving GUIDs and serialized references.
6. Validate with Unity MCP/import/console where relevant and report exact paths touched.
