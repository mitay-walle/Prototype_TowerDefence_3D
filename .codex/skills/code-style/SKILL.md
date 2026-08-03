---
name: code-style
description: Project C# code style and engineering rules for project-owned C# under Assets/Scripts only. Use when writing or reviewing C# code in Assets/Scripts, choosing type layout, SaveData/state/facade naming, runtime/editor boundaries, or KISS/SOLID and general architecture there. For BehaviourInject DI/context/provider/injection work, use `$behaviourinject`.
---

# Code Style

## Purpose

Use this skill to keep project C# changes small, explicit, and aligned with the existing Unity architecture.

Scope: this skill applies only to project-owned C# source under `Assets/Scripts`. Use it for code audits and code changes in that directory, and do not use it to enforce style for packages, generated files, project settings, assets outside `Assets/Scripts`, or other non-script content unless a more specific skill explicitly routes there.

Mandatory gate: before creating any Unity C# `.cs` script under `Assets/Scripts`, load and apply this skill before choosing the path, filename, type layout, dependencies, or implementation. This also applies when another skill is the primary workflow, including `$apply-patch`, `$unity-mcp-orchestrator`, `$create-assets-menu-item`, or `$unity-recompile-menuitem`.

For BehaviourInject DI, context lifecycle, providers, injected views/controllers, commands, events, factories, or execution order, load `$behaviourinject`. For Unity test authoring and test helper `MonoBehaviour` placement, load `$test-writing`. For prefab asset creation, prefab edits, UI prefab hierarchy, RectTransform layout, TextMeshPro components, UI layers, Canvas/CanvasScaler/GraphicRaycaster, or prefab migration work, load `$prefab-creation` and `$ui-prefab-authoring` instead of keeping those rules here.

When a task requires choosing between a direct implementation and extra state, fallback behavior, helper abstractions, services, DTOs, events, factories, or public API, read `references/KISS.md` before writing code.

When a task involves responsibilities, interfaces, inheritance, polymorphism, or dependency direction, read `references/SOLID.md` before choosing the design.

When changing or extending one of the project's large polymorphic systems, also read `references/ProjectPolymorphicAbstractions.md` before choosing the abstraction boundary.

When replacing serialized types, changing serialized data shape, converting lists to `[SerializeReference]`, or planning temporary migration/backfill/refactor work, also read `references/DataMigrationRefactoring.md` before choosing names, compatibility strategy, or migration tooling.

## Rule Priorities

Apply rules in this order. Higher-priority rules win when guidance conflicts, and lower-priority rules never justify violating P0.

P0 - Focused changes and explicit ownership:

- Keep changes focused on the current task and avoid speculative layers.
- Before adding any new field, serialized option, runtime flag, fallback state, helper method, or behavior switch, identify the existing source of truth and runtime owner. If one exists, use it directly instead of adding local mirror state.
- Do not add local state for availability, completion, save/load, quest flags, interaction gating, or UI visibility unless the current bug cannot be expressed through the existing owner and the reason is explicit in the change.
- Every workflow and subsystem must have one explicit runtime entry point. If an owner already exists, fix that owner instead of adding a second entry point.
- Never fix a missing side effect by replaying another workflow's setup or state transition from a new call path. A method rename does not make a second entry point acceptable.
- Ownership audit compares runtime workflows, not class names. Before adding behavior to an existing owner, trace the sequence of runtime steps it would execute and search sibling owners for the same or materially overlapping sequence.
- Do not add a general workflow to a specialized owner. If the requested behavior is a generalized form of behavior already owned by one or more specialized owners, extract or reuse one shared workflow owner first, then keep specialized owners responsible only for their domain-specific before/after effects.
- Do not add extra files, layers, services, factories, DTOs, events, generalized helpers, fallback paths, lazy initialization, or rescue flows unless the current task has a demonstrated need.
- If a requested change appears to require a second entry point, speculative layer, or duplicated state, stop and explain the tradeoff before implementing.

P1 - Runtime ownership and lifecycle: dependencies, initialization, save/load, DI, command execution, and Unity lifecycle must follow the explicit owner chain.

P2 - Existing architecture: prefer established project extension points, polymorphism, and local APIs over new parallel mechanisms.

P3 - Data and serialization compatibility: preserve Unity serialized data, save contracts, `[SerializeReference]` type names, script GUIDs, and migration boundaries.

P4 - Public API, naming, formatting, and file layout: keep code readable and consistent after P0-P3 are satisfied.

## Formatting Sources

Follow `.editorconfig` and `Outcasts.sln.DotSettings` before writing or heavily editing C#. Treat those files as the concrete naming source too: private fields, including `[SerializeField] private` Unity fields, use the project/Rider leading-underscore convention unless the local file has a stronger existing convention.

Compact formatting baseline:

- Use LF and valid UTF-8.
- Use tabs for C# indentation.
- Require braces for control statements.
- Use `var` only when the type is evident.
- Use explicit object creation when the type is evident.
- Allow expression-bodied methods, constructors, and local functions.
- Do not collapse function bodies to one line; keep declarations and bodies on separate lines even for short functions.
- Wrap near 150 columns.
- Preserve existing attribute, declaration, and block arrangement when Rider says to keep it.

Formatting rules never override stricter architecture rules below.

## Unity Script Assets And Packages

- Unity GameObject tags are forbidden for project gameplay and tooling. Do not add project tags, assign tags, search by tag, or use `CompareTag`, `GameObject.FindGameObjectWithTag`, `FindGameObjectsWithTag`, `gameObject.tag`, `transform.tag`, or serialized tag-name strings. Use layers, explicit serialized references, marker components, marker component domain IDs, interfaces, `EntityTags`, physics materials, or domain data instead.
- Existing custom entries in `ProjectSettings/TagManager.asset` are legacy only. Do not add new ones; remove legacy assignments through Unity/AssetDatabase paths when touching the affected objects.
- Never hand-create Unity `.meta` files or invent GUIDs for scripts. Let Unity import generate `.meta` files.
- Never edit, replace, swap, regenerate, or migrate GUID values in `.cs.meta` files. A script GUID belongs to that script asset permanently.
- When renaming or moving a script, move the existing `.cs.meta` with it unchanged.
- Do not resolve missing scripts or class merges by rewriting script GUIDs in `.meta`, prefabs, scenes, or assets. Use Unity/AssetDatabase/component migration paths under the appropriate Unity/prefab skill instead.
- Do not edit package source, embedded packages, `Packages/`, or `Library/PackageCache/` files to solve project problems. Treat packages as read-only reference/API context, then implement fixes in project-owned `Assets/` code or configuration.
- Do not place Editor-only C# scripts under `Assets/Scripts`, including `Assets/Scripts/**/Editor` folders. Scripts that use `UnityEditor` or exist only for editor tooling belong under `Assets/Editor` or another explicit Editor-only assembly outside `Assets/Scripts`.

## File And Type Layout

- Keep one C# type per `.cs` file: one file equals one top-level class, struct, interface, enum, or record.
- Do not create nested C# types. Keep classes, structs, interfaces, enums, and records as separate top-level types in their own files.
- Do not put multiple top-level types in one file unless Unity serialization or source generation explicitly requires it.
- Before writing or editing any C# file, count the top-level types in the intended result. If there would be more than one, split them into separate `.cs` files named after each type.
- Avoid plural/helper container filenames such as `Lists.cs`, `Helpers.cs`, or `Types.cs`.
- Keep C# source as valid UTF-8. Russian/Cyrillic comments, string literals, and localization text must not become mojibake, `?`, replacement character `U+FFFD`, or unwanted `\uXXXX` escapes.

## Architecture Defaults

- Prefer the smallest direct solution. Do not add abstractions for future use.
- Keep systems simple. Do not create extra `Registry`, `Pipeline`, `Resolver`, `Context`, `Manager`, `Service`, `Provider`, `View`, or similar boundaries unless the current implementation has a real demonstrated need for that exact boundary.
- Prefer polymorphism over `switch`/type-code branching. Use interfaces, virtual methods, strategy objects, command dispatch, or event dispatch when behavior varies by type or mode.
- Audit gate for refactors and reviews: before choosing a target in a polymorphic/domain runtime system, search that area for central dispatch such as `switch (` / pattern matching over domain base types, mode enums, operation hierarchies, or action hierarchies. Treat existing owner-side dispatch methods as higher priority than local cleanup.
- When a system already has a polymorphic extension point, use it instead of adding a parallel procedural API. Add a new implementation behind the existing interface, abstract base type, factory, or `[SerializeReference]` list rather than adding one public method, enum branch, or special case per concrete behavior.
- Do not pass broad runtime, save-root, or facade objects into polymorphic operation `Execute` methods when the operation only needs specific dependencies. Prefer constructor dependencies for stable requirements and narrow per-call arguments for varying data; use a shared context only when the subsystem already has a real context contract.
- Before adding a new interface or side-channel for UI, interaction, or player feedback data, trace the existing owner chain from source component through event/presenter first. Prefer extending the existing source-to-presenter pipeline over adding feature-specific prompt or feedback plumbing.
- Do not use reflection in runtime code. Prefer properties with `{ get; private set; }`.
- Treat static state, static services, static helpers, singleton-style access, and the Singleton pattern as antipatterns by default. Do not add private static helper methods just because they do not currently touch instance fields; keep helpers instance-owned unless there is a concrete stateless utility reason. Do not add `Instance` accessors or private-constructor single-instance objects for runtime ownership; use explicit ownership, DI, serialized references, or per-call command objects instead.
- Allow `static` only for constants, extension methods, pure math, enum-specific utilities, or similarly stateless utility functions with a clear reason that does not hide ownership, dependencies, lifecycle, or mutable state.
- When a stateless utility is justified, add it to the existing system-level utility surface or create one cohesive utility for that subsystem. Do not create separate small static `*Utility`, `*Utils`, or `*Helper` classes for individual operations.
- Do not use `goto`.
- Use UniTask instead of Coroutines.
- Prefer event-driven or physics-event-driven flow over polling in `Update`. Use `Update` only when continuous per-frame sampling is the actual requirement.
- Write compact code and avoid duplicated forwarding layers. Do not create paired `public static` functions plus duplicated private instance functions for the same behavior; choose one clear API shape.
- Prefer existing framework, Unity, LINQ, collection, and project APIs over handwritten loops or helpers. Do not write a custom helper for behavior already provided by a standard or local API such as `List<T>.Contains`, `TryGetComponent`, `Mathf`, `Physics`, or existing domain services.
- Do not pass functions as `Action`; pass interfaces or direct references.
- For sequential calls, use `Debug.Log()` when a bool-gated log is needed.
- For serialized domain quantity data, use the existing value object and command APIs for that subsystem. Do not create parallel id/count DTOs, structs, tuples, or duplicated command plumbing when a domain quantity type already represents the data.

## Runtime Ownership

- Never change the component composition at runtime: do not call AddComponent, Destroy, or replace components from Awake, Start, or other runtime code. Component replacement must be authored before Play Mode in the scene or prefab; runtime code may change values only on an existing component.
- Require a single explicit runtime entry point for every workflow and subsystem.
- Before choosing the owner for a change, compare the full runtime workflow against nearby services/components with similar lifecycle responsibilities. A smaller local diff is not a valid reason to duplicate workflow ownership or place generalized behavior inside a specialized owner.
- Before adding dependencies or side effects to a service/workflow, explicitly identify the current runtime workflow owner, the provider/DI/composition path for each long-lived dependency, and the closest shortcut that must not be used. If the proposed path is "serialize a reference on the nearest MonoBehaviour" or cannot name the provider path, stop before editing.
- When a lifecycle already has an owner such as scene flow, entity initialization, save/load, DI composition, or a command execution path, fix that owner instead of adding a second local entry point.
- Before adding any call that creates, mutates, restores, finalizes, or replays gameplay/UI/save state, identify the owning entry point for that state transition. If the call would make another workflow produce the same transition, stop and fix the original owner, ordering, capture point, or overwritten result instead.
- Do not pass long-lived services or dependencies as method parameters to objects that own behavior needing them. If a class, mode, or service uses a dependency as part of its responsibility, inject it through the constructor/DI and keep it as a field; reserve method parameters for per-call data, commands, DTOs, or event payloads.
- For small frequently-created immutable per-call DTOs that are not used as `ICommand`, BehaviourInject command payloads, or `[InjectEvent]` payloads, prefer `readonly struct` over `class`.
- Do not add `Awake`, `Start`, `OnEnable`, lazy initialization, retry binding, idempotent rescue paths, or serialized boolean fallbacks to make a subsystem start itself when the intended owner failed to call it. Treat that as masking the ownership bug.
- When runtime state was already valid earlier in the flow and becomes missing after any workflow boundary, do not fix it by re-creating expected state from config, content defaults, scene objects, initial setup, or another lifecycle path. Trace the owner chain and fix the exact point where state is skipped, saved, loaded, carried across contexts, captured too early, or overwritten.
- Use fixed, linear `ICommand.Execute -> ExecuteAsync` or equivalent owner-driven flow instead of scattered `EnsureSomething` calls from multiple entry points.
- Treat `EnsureSomething` methods as an antipattern by default. Do not add them as lazy initialization, validation, repair, or workflow coordination wrappers; make initialization ownership explicit, validate at the boundary, and use a clearly named command/service method when a real workflow step is needed.
- This rule is semantic, not name-based: do not replace a banned `Ensure...`/lazy repair/fallback/control-path method with the same behavior under another name such as `CreateOrUpdate...`, `Capture...`, `Resolve...`, `Refresh...`, or `Sync...`. If a consumer/tick/update path creates, restores, validates, or catches up missing state that the owner should have produced, remove that path or move the work into the explicit owner entry point.
- Do not create multiple independent gameplay entry points through combinations of UnityEvents and Unity lifecycle methods. Choose one explicit owner for the workflow; keep UnityEvents as view/inspector hooks only when truly needed.

## Public API And Naming

- Do not create public properties or functions for future use. Public API must be used by the current implementation, required by Unity serialization/lifecycle, or required by an existing contract.
- Do not use `internal`; use `public` or `private` intentionally.
- Use explicit `private` modifiers for private fields, methods, properties, constructors, and nested types; do not rely on C# default private accessibility.
- Do not abbreviate variable names. Use complete words that describe the value clearly.
- Avoid names that collide with or read like reserved C# keywords, framework terms, Unity lifecycle methods, or broad CLR concepts; prefer `Begin` over `Start` and `Pattern` over `Type`.
- Do not name new or replacement types with temporary quality/version prefixes or suffixes such as `New`, `Fixed`, `Updated`, `Better`, `Improved`, `Final`, `V2`, or similar wording. Choose the domain role name directly. If replacing an old type that is not serialized through `[SerializeReference]`, temporarily rename the old type from `MyName` to `MyNameObsolete` and give the replacement the canonical `MyName` name.
- Name polymorphic operation/action classes by the concise action itself, following the subsystem's existing operation vocabulary; do not add suffixes like `Operation`, `Action`, `Command`, `Handler`, or domain repetition unless an existing interface contract explicitly requires it. For operation logs, pass the operation object and use `operation.GetType().Name` at the logging boundary instead of passing operation names as strings.
- Use `OnUpdate` for frame-update hook names instead of `Tick`.
- Use `Factor` for multiplicative values instead of `Multiplier`.
- Prefer constructors over `SetValues()`/post-construction initialization methods. Constructors with many arguments are acceptable if they make dependencies clear.
- Prefer public fields over private fields plus trivial `Set...()` methods when the value is intentionally mutable/public state.
- For Unity-serialized mutable state that is read outside the class and written only by Unity serialization or owner code, use `[field: SerializeField] public Type Name { get; private set; }`.
- Do not add `private [SerializeField]` fields paired with trivial public getters for the same value. Use a backing field only when it has a real reason such as validation, compatibility with an existing serialized field name, non-trivial getter behavior, or a migration boundary, and state that reason before editing.
- Name `MonoBehaviour` components with concise domain nouns when the noun is clear; do not add a `Component` suffix by default. Prefer `Faction : MonoBehaviour` over `FactionComponent : MonoBehaviour`. Use a suffix only when it resolves a real ambiguity in existing code.
- Use explicit project suffixes for non-save state and facade types: `RuntimeDTO` for runtime data transfer state, `Service` for facade-backed services, `Provider` for providers that expose view facade instances, and `View` for the view-facing facade itself.
- Do not use a `SaveDTO` suffix for new persisted data unless the project already has that concrete contract and the change is extending it.
- Property getters must be cheap and unsurprising. Do not clone, allocate snapshots, mutate state, dispatch events, or perform load/save work from a property getter. Use an explicitly named method such as `Create...Snapshot()` when copying is required.

## MonoBehaviour And User Feedback

- Keep `MonoBehaviour` components small, self-contained, and reusable when possible.
- Prefer BehaviourInject and `RequireInjector`/`Injector` for `MonoBehaviour` dependencies by default. If a component needs services, player/session objects, event dispatchers, providers, or other long-lived runtime objects, load `$behaviourinject` and use `[Inject]` with verified injector coverage instead of manual dependency forwarding.
- Treat `FindObjectOfType`, `Object.FindObjectOfType`, `FindAnyObjectByType`, singleton/static access, and scene-wide searches for services/managers/providers from runtime project UI/gameplay code as ownership antipatterns by default. `GetComponentInParent` is also a red flag outside local hierarchy wiring. First classify the target: same-object/local components may be resolved locally; long-lived dependencies must come through BehaviourInject unless an explicit owner audit proves DI cannot own them.
- Do not use `Transform.Find` or `transform.Find` in runtime project code to locate child or scene objects by name/path. Use owner-held serialized references, component queries for explicit marker components, or marker components with domain ID fields when several marked objects need to be distinguished.
- Use providers between scene prefabs for long-lived scene dependencies. Do not wire direct prefab-to-prefab references as the dependency contract, because reverting one prefab can drop references to another prefab.
- If a scene `MonoBehaviour` is a singleton-like view/state object that other DI-owned services or components need, create a narrow `ViewProvider`/domain provider and register that provider in the context/builder. The scene view injects only the provider, pushes itself into the provider from `Awake` with `SetView(this)` or the domain equivalent, and clears/unregisters itself in `OnDestroy`. Consumers inject the provider instead of the scene `MonoBehaviour`. This is distinct from multi-contributor systems, where scene components register directly with the service owner.
- When scene-local view/data components feed a long-lived service, register or construct the service in the context/builder only. Each contributing scene component injects the service with same-object `RequireInjector`/`Injector` coverage, assigns only the service during injection, calls a narrow registration method from `Awake`, and unregisters in `OnDestroy`. Do not have a context, controller, sibling reference, scene search, constructor `MonoBehaviour` parameter, or extra holder component collect and forward those scene objects into the service. The service owns its config loading and runtime workflow; scene components provide only their own scene-local data or callbacks.
- For code review of a Unity scene system, first build the code-reference graph: `Context/Builder -> Service -> scene MonoBehaviours -> sibling MonoBehaviours`. If a long-lived service depends on scene-local `MonoBehaviour`s through constructor parameters, scene searches, serialized bridges, or sibling forwarding, flag that before smaller issues; the default correction is injected self-registration from each scene component into the service owner.
- When a service-owned config asset lives in `Resources` or Addressables, the service loads that config through the project asset-loading path it owns. Do not load the config in a scene context, builder, view, or nearby MonoBehaviour just to pass it into the service constructor.
- Prefer composing entities from focused components over large entity-specific behaviours that mix unrelated responsibilities.
- Do not serialize references to components that live on the same GameObject. Resolve same-object component dependencies with `GetComponent`/`TryGetComponent` from the explicit owner initialization path instead of `[SerializeField]` fields.
- A top-level view facade may own references to objects under its own hierarchy through serialized child references or `GetComponentInChildren`; this is local view wiring, not a provider boundary between separate scene prefabs.
- Test, debug, cheat, and development-only scripts follow the same runtime ownership and public API rules as gameplay scripts.
- Do not emit logs from `Update`, `FixedUpdate`, polling loops, repeated decision loops, or per-frame/per-tick callbacks unless the log is gated by a state change, explicit user action, or a serialized debug cooldown.
- When adding a new player-visible action or user-facing system, include player feedback in the first implementation: DOTween animation or visible state motion, plus a real sound. Prefer Freesound for new audio assets and use the project Freesound search/download skills. Feedback can be component-driven or hardcoded in the owner when that is the smallest fit.
- For gameplay feedback SFX that needs downloaded audio or random variation, use this workflow:
  1. Search candidates with `$freesound-search` before downloading; keep hard duration and filesize limits appropriate to the event, usually short one-shots for hits, steps, clicks, breaks, and impacts.
  2. Prefer CC0 clips for gameplay SFX. Do not import BY-NC clips into `Assets/`. If a BY clip is chosen, report the attribution requirement and keep provenance.
  3. Download originals with `$freesound-downloader`. Keep generated `.freesound.json` sidecars outside Unity `Assets/`, for example under `Temp/FreesoundProvenance/...`, and import only audio files into `Assets/`.
  4. Create or update project `AudioContainer` ScriptableObject assets for random SFX instead of wiring raw `AudioClip[]` fields when the sound has variants, layers, delay, trim, volume, or pitch randomization. Use semantic layers such as cloth/flesh, body/gear, impact/debris, or click/release.
  5. Configure each `AudioContainerLayer` with the actual clips plus `DelayRange`, `TrimStart`, `TrimEnd`, `VolumeRange`, and `PitchRange`; verify the Odin Preview timeline/waveform after changes.
  6. Wire the container into the existing runtime owner that already plays the feedback. If replacing an old serialized `AudioClip`/`AudioClip[]`, migrate values through Unity APIs or a temporary Editor MenuItem, then remove the old field/fallback path after references are moved.
  7. Validate scripts with `$mcp-unity-validate-script`, let Unity refresh/import, read console errors, and verify the target prefab/asset references. Do not use `msbuild`.
- If feedback is intentionally omitted, state why before implementing.

## UI And Input Code

- Before changing `Assets/Scripts/UI/General/UIManager.cs` or related window/input behavior, read `.codex/docs/UIManager.md`.
- Any player-visible text added from code must be localized through the project localization tables, except numeric-only or symbol-only content. Do not leave user-facing language as string literals, fallback-only TMP text, or enum labels.
- UI/input views must use the project Inputs service or an injected input abstraction.
- Do not read `Keyboard.current`, `Mouse.current`, or raw Input System globals directly from a view when an existing project input action/service can represent the command.

## Serialization And Editor Data

- Do not add Unity Inspector layout-only attributes such as `[Space]` or `[Header]` in new or edited C# code.
- Changing the name or type of a Unity-serialized field is a serialized data migration even when only `.cs` files change. Preserve existing serialized data by keeping the old field or migrating through Unity Editor/API tooling; do not rename the field in place and assume prefab, scene, or asset values will survive.
- Do not use `[FormerlySerializedAs]` unless the user explicitly requests that exact migration/backfill mechanism.
- For ordinary Unity-serialized collections, prefer `List<T>` over `T[]` unless a Unity API contract, fixed-size data shape, or performance-critical runtime path specifically requires an array.
- For polymorphic Unity-serialized collections, prefer `[SerializeReference] IAbstract[]` over concrete-type arrays/lists or enum/switch dispatch. Odin Inspector provides a type picker for `[SerializeReference]`, so prefer this before introducing ScriptableObject definitions only to select implementation types.
- Do not rename concrete child types already used through `[SerializeReference]`; Unity serializes the concrete type name. Rename only with an explicit migration/backfill plan requested by the user.
- Definition: `SO` means `ScriptableObject`.

## Save And Runtime State

- For any gameplay system, design and implement in this order: `SaveState -> RuntimeState -> RuntimeExecution -> EditorDefinitions -> Editor Tools`.
- Runtime must be able to execute from save/runtime state before ScriptableObject/editor authoring layers are added.
- DTOs and save data contain no logic and are not used for dependencies.
- Persisted save data must use the existing `Saving.SaveData` hierarchy. Do not invent parallel `SaveDTO`, `SaveState`, wrapper, bridge, or migration types when an existing `SaveData`-derived contract can represent the data.
- For `EntityComponent` persistence, return a `SaveData`-derived component save type, usually an existing or new subclass of `EntityComponentSaveDTO`, and let `Entity` remain the `ISaveable` owner.
- Do not make an entity component implement independent `ISaveable` for data that belongs to the entity aggregate.
- SaveData fields must be initialized by field initializers or constructors.
- Do not add `EnsureInitialized()`, lazy `??=` initialization, migration/backfill normalization, or scattered null-repair paths for save data unless the user explicitly asks for migration or corrupted-save recovery.
- Treat required save data parameters, event payloads, and collection entries as present in runtime consumers.
- Do not add `null` fallback branches unless the API explicitly accepts missing or corrupted save data.

## Legacy And Migration Work

- Do not create migration code, migration assets, conversion tools, or backfill paths for a new system unless the user explicitly asks for migration. New systems should start clean by default.
- When replacing a legacy gameplay system, do not keep runtime fallbacks, compatibility branches, or dual-read paths to the old components.
- Migration may read old component values only as source data, then must write the new component configuration and delete or obsolete the old scripts/assets after a reference scan. For prefab/scene component migration, load `$prefab-creation` and use Unity/AssetDatabase/component migration paths.
- When replacing a legacy gameplay family, treat explicitly superseded components, data objects, and assets in that family as source-only legacy.
- Do not add or preserve fallback reads/writes to superseded legacy types; copy required values into the new owner/config, then remove legacy components and delete or obsolete legacy scripts/assets after references are gone.
