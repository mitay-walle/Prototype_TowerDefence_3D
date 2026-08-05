---
title: Gameplay scene objects и MonoBehaviour composition
type: greenfield scene and prefab contract
status: design-target
updated: 2026-08-04
scope: hypothetical gameplay scene, object behavior, data links and custom components
---

# Gameplay scene objects и MonoBehaviour composition

## 1. Назначение

Документ описывает гипотетическую gameplay scene с нуля: объекты, поведение, data contexts, component recipes и lifecycle variants.

Логическое имя не требует отдельного MonoBehaviour. Новый component нужен только при собственном Unity callback, reusable implementation, replaceable behavior, serialized references или самостоятельном state/lifecycle.

## 2. Component architecture rules

1. Required topology authorится в scene/prefab до Play Mode.
2. Gameplay object имеет один root owner.
3. Не создаётся component на каждое число.
4. Shared authored values живут в Definitions.
5. Runtime values живут у actor/service owner.
6. Save — DTO snapshot, не component.
7. UI/VFX/SFX downstream.
8. Same-object dependencies кешируются локально.
9. Scene/run dependencies передаются EntryPoint/composition/spawn context.
10. Missing required component/reference — initialization error.
11. Pool rent/return симметрично очищает state.
12. One prefab = complete logic/data/visual/interaction/lifecycle contract.

## 3. Hypothetical scene hierarchy

```text
GameplayScene
├── Composition
│   └── GameplayEntryPoint
├── Run
│   ├── RunFlowOwner
│   ├── WaveFlowOwner
│   ├── RunEconomyOwner
│   └── RunRandomOwner/adapter
├── Level
│   ├── MapOwner
│   ├── MapGenerator
│   ├── TilePlacement
│   ├── GroundNavigationBuilder
│   ├── SpawnAnchors
│   └── Base
├── Actors
│   ├── Towers
│   ├── Enemies
│   ├── Projectiles
│   └── Pools
├── Interaction
│   ├── TowerPlacement
│   ├── Selection
│   └── PlacementPreviews
├── Presentation
│   ├── CameraRig
│   ├── Canvas/HUD/Panels
│   ├── EventSystem/InputAdapters
│   ├── WorldVFX
│   ├── WorldSFX
│   └── Music
└── Environment
    ├── Lighting
    ├── Volume
    ├── Ground
    └── Decoration
```

ApplicationRoot, ContentCatalog, SaveService, Profile owner, Settings, Localization и SceneFlow живут вне gameplay scene либо входят как явные application dependencies.

## 4. Scene objects и data links

| Object role | Definition inputs | Runtime state | Save | Outputs |
| --- | --- | --- | --- | --- |
| GameplayEntryPoint | Run/content rules | startup progress/error | none | ready/blocking result |
| RunFlowOwner | win/loss/save policy | phase/terminal/pause | run phase/receipts | state/read models/RunResult |
| WaveFlowOwner | Wave Definitions | cursor/active set/payout | next wave; full only mid-wave | intel/progress/result |
| RunEconomyOwner | Economy Definition | balance/ledger | balance/modifiers | currency results |
| MapOwner | Tile/map Definitions | layout/occupancy/revision | tile instances | topology/spawn snapshots |
| NavigationBuilder | committed map | derived handles | none | readiness/error |
| Base | Base Definition | HP/shield/effects | HP/shield | damage/destroyed |
| Tower | Tower Definition | target/cooldown/grade/effects/durability | placement/build state | attack/read model |
| Enemy | Enemy Definition | movement/HP/effects/terminal | mid-wave only | damage/Kill/Leak |
| Projectile | Projectile Definition | flight/hit state | mid-wave only | impact/expire |
| Placement | Tower/Tile Definitions | draft/validation | none | mutation result |
| Selection | selectable contracts | selected ID | none | selection read model |
| UI | localized read models | focus/navigation | settings only | commands |
| VFX/SFX | Cue Definitions | handles/pools | none | presentation only |

## 5. GameplayEntryPoint

Единственный executable scene entry.

### Inputs

- `StartingRules` или `RunSaveDTO`;
- ContentCatalog endpoint;
- Save endpoint;
- authored scene references;
- scene/run cancellation.

### Behavior

1. validate scene topology and content;
2. create/restore run state;
3. generate/restore map;
4. build Ground navigation and Flying approaches;
5. initialize Base/spawn anchors;
6. initialize owners/factories;
7. create/restore Towers;
8. bind UI/VFX/SFX;
9. enter Preparation.

### Variants

- Basic: new run only;
- Extended: between-wave continue;
- Deferred: exact mid-wave restore.

Partial startup does not become playable.

## 6. Run objects

### 6.1 RunFlowOwner

Behavior: phase transitions, StartWave command gate, pause integration, terminal outcome, snapshot/RunResult orchestration.

Data: phase, run identity, transition revision, terminal guard.

Does not contain map/balance/Enemy lists as mirror fields.

### 6.2 WaveFlowOwner

Behavior: build intel, execute spawn schedule, register Enemy, accept one terminal result, resolve/payout once, coordinate mandatory inter-wave phases.

Variants:

- authored finite waves Basic;
- multiple lanes/scaling Extended;
- threat-budget generation Deferred.

Ground/Flying share this owner.

### 6.3 RunEconomyOwner

Behavior: `CanAfford`, atomic spend/grant, ledger, snapshot/read model.

Variants:

- one currency Basic;
- bounty/production Extended;
- multiple functional currencies only with distinct decisions.

### 6.4 RunRandom

Prefer plain C# object owned by run, not MonoBehaviour unless Unity callback/Inspector is truly required.

## 7. Level objects

### 7.1 MapGenerator

Command-scoped behavior: create initial layout from Definition + seed. После commit source of truth is MapOwner.

### 7.2 MapOwner

Owns tiles, graph, occupancy, revision, spawn/Base topology.

Variants:

- single route Basic;
- branches/merges/multiple lanes Extended;
- height/multi-level Deferred.

### 7.3 TilePlacement

Owns draft/input/ghost only. Uses same validator as confirm. Successful confirm mutates MapOwner and economy transaction.

### 7.4 GroundNavigationBuilder

Builds derived navigation after committed map changes. Never saved. Preview does not rebuild.

### 7.5 SpawnAnchor objects

Authored/generated markers may expose:

- anchor/lane ID;
- `Ground | Flying | Both`;
- Ground route handle;
- Flying waypoint/approach reference;
- transform/presentation.

Markers do not spawn Enemy themselves.

### 7.6 Base

Owns HP/shield/effects/destroyed-once/repair. Receives Leak damage and publishes terminal result.

Variants:

- HP only Basic;
- shield/repair/aura Extended;
- modular Base Deferred.

## 8. Common gameplay prefab recipe

```text
ActorRoot
├── RootOwner
├── RequiredStateModules
├── RequiredUnityComponents
├── OptionalBehaviorModules
├── OptionalInteraction/EffectModules
└── VisualRoot
    ├── Renderers/Animator
    ├── authored sockets/anchors
    ├── local VFX/SFX
    └── selection/tooltip visuals
```

Root owner accepts spawn/restore context, validates modules, owns active/terminal guard, routes local operations, builds snapshots/read models and cleans up.

## 9. Tower prefab

### 9.1 Basic composition

```text
TowerRoot
├── Tower                         // root owner
├── TowerStats                    // Definition + derived stats
├── exactly one primary Weapon    // unless multi-hardpoint is explicit
├── authored Collider/selection surface
├── Tooltip/Selection adapter if inspectable
└── VisualRoot
    ├── mesh/sprite/model
    ├── Muzzle/Weapon sockets
    ├── Range visual
    └── fire/upgrade/sell cues
```

### 9.2 Tower root behavior

Owns:

- identity and active state;
- target policy/current target;
- cooldown/fire sequence;
- grade/branch/modifier coordination;
- upgrade/sell commands;
- optional durability/contact/repair coordination;
- snapshot/read model.

Does not own run balance, Enemy HP, map or profile.

### 9.3 TowerStats

Stores read-only TowerDefinition and derived runtime values. Upgrade changes instance grade/modifiers; Definition asset remains immutable.

Do not create `DamageComponent`, `RangeComponent`, `FireRateComponent` for numbers alone.

### 9.4 Weapon variants

- Instant/hitscan;
- Projectile;
- Beam;
- AoE;
- Pierce;
- Chain;
- aura pulse only if attack-like.

Weapon owns delivery-specific short state. Receiver owns damage result.

### 9.5 Targeting extraction

Keep inside Tower until a separate component is justified by:

- replaceable sensor implementations;
- own physics trigger callbacks;
- independent lifecycle/pooling;
- reuse by other actor types.

Sensor returns candidates; Tower still owns target decision.

### 9.6 Conditional Tower modules

| Role | Component when justified | State |
| --- | --- | --- |
| Durability | `TowerDurability` | HP/Broken/terminal |
| Contact | `TowerContact` | pair guards/cooldowns/filters |
| Auto repair | `TowerAutoRepair` | delay/rate/rebuild progress |
| Shield | `Shield` | current/recharge/break |
| Status | `StatusEffects` | instances/ticks |
| Aura | `AuraEmitter` | target membership/handles |
| Ability | concrete ability | cooldown/cast |
| Multiple weapons | `WeaponMount` | mount geometry/local cadence |
| Cues | `TowerCueEmitter` | presentation handles |

Если mechanic проста и unique, state остаётся в Tower/root-owned plain object.

## 10. Road-contact Tower

### 10.1 Composition

```text
RoadContactTowerRoot
├── Tower
├── TowerStats
├── Weapon?                       // optional
├── TowerDurability               // if Tower can break
├── TowerContact                  // authored trigger adapter
├── TowerAutoRepair?              // optional
├── authored Trigger/Collider
├── Tooltip/Selection
└── VisualRoot
    ├── Active/Damaged/Broken/Restoring states
    └── contact/break/repair cues
```

### 10.2 Navigation invariant

- does not close route graph;
- does not carve NavMesh;
- contact volume is trigger/interaction geometry;
- placement/break does not require repath;
- surviving Enemy continues route.

### 10.3 Contact behavior

1. trigger receives Enemy;
2. validate active states, pair guard, role and Ground/Flying filter;
3. create typed contact command;
4. resolve Tower and Enemy damage in authored order;
5. aggregate result;
6. Enemy terminal Kill if applicable;
7. Tower Broken if applicable;
8. show confirmed cues.

Outcomes:

| Enemy | Tower | Result |
| --- | --- | --- |
| Kill | Active/Damaged | reward once; Tower remains |
| Survive | Broken | Enemy continues; Tower disabled |
| Kill | Broken | mutual result once |
| Survive | Damaged | Enemy continues; Tower may repair |

### 10.4 Broken variants

- `RemainBroken`: object remains, weapon/contact/offensive aura disabled; repair can restore.
- `RemoveOnBreak`: unregister/destroy; automatic rebuild requires explicit factory/run command and is not Basic auto-repair.

Recommended with auto-repair: `RemainBroken`.

### 10.5 Auto-repair

Between waves:

- full HP;
- fixed amount;
- optional rebuild from Broken;
- one application per WaveInstanceId.

Inside wave:

- delay since last damage;
- regeneration rate/ticks;
- delay reset policy;
- optional longer Broken rebuild;
- stop on sell/run end/disable.

No global RepairManager.

### 10.6 Flying contact

Default `Ground`. Flying contact only when Definition says `Flying/Both` and prefab has authored air interception volume. Accidental collider overlap is not design truth.

## 11. Enemy prefabs

### 11.1 Shared aggregate

```text
EnemyRoot
├── EnemyRootOwner/TerminalGate
├── EnemyStats
├── HealthReceiver
├── exactly one Movement implementation
├── Targetable/Tooltip if inspectable
├── authored Collider
├── optional Shield/Status/Aura/Ability
└── VisualRoot + cues
```

Root/health terminal gate guarantees `Kill XOR Leak XOR Despawn` and publishes one terminal result.

### 11.2 Ground Enemy

```text
GroundEnemyRoot
├── shared Enemy modules
├── GroundMovement
└── NavMeshAgent/ground navigation adapter
```

GroundMovement owns route progress, speed, Base arrival and movement stop. It does not grant reward.

### 11.3 Flying Enemy

```text
FlyingEnemyRoot
├── shared Enemy modules
├── FlyingMovement
├── authored air Collider/trigger setup
└── VisualRoot
    ├── flight animation
    ├── altitude/shadow readability
    └── air hit/death/leak cues
```

FlyingMovement owns:

- altitude/speed/turning;
- direct/waypoint/spline progress;
- pause/terminal stop;
- Base aerial arrival;
- inspect/mid-wave snapshot.

No Ground NavMeshAgent. Exactly one movement implementation active.

### 11.4 Enemy variants

- runner;
- tank;
- armored;
- shielded;
- Flying scout/tank;
- support aura;
- splitter/summoner;
- regenerator;
- boss phases.

Splitter children register with the same wave owner before parent completion can resolve wave.

## 12. Base prefab

```text
BaseRoot
├── BaseOwner
├── authored physics/arrival volumes
├── Tooltip/Selection if inspectable
├── optional Shield/Status/Aura/Repair interaction
└── VisualRoot + damage/repair/destroyed cues
```

Ground and Flying may use different authored approach/arrival volumes but share one Base HP/terminal owner.

## 13. Projectile prefab

```text
ProjectileRoot
├── Projectile                    // flight/impact-once owner
├── authored pool identity or pool external map
├── Collider if required
└── VisualRoot
    ├── mesh/sprite/trail
    └── launch/impact/dissipate cues
```

Lifecycle:

```text
rent/create → reset → launch → fly → impact XOR expire
→ resolve at most once → clear target/packet/trail → return/destroy
```

Instant/Beam/Pierce do not need Projectile GameObject unless they have independent spatial/lifetime state.

## 14. Tile prefab

```text
TileRoot
├── TileInstanceAdapter
├── authored collision/build surfaces
├── optional build sockets/spawn/aura/hazard markers
└── VisualRoot
```

Authoritative topology remains MapOwner. Tile component adapts prefab data and local geometry, not global map dictionary.

Conditional modules:

- build socket/surface;
- SpawnAnchor view;
- TileAuraZone;
- TileHazard;
- world interaction;
- cue emitter.

## 15. Placement previews

Preview uses a separate authored prefab, not a real actor clone with runtime component deletion.

```text
Tower/TilePreview
├── authored trigger/collider if needed
├── validation color/view
├── preview visual
└── no authoritative combat/economy/map owner
```

Receives PlacementReadModel only.

## 16. Selection/Tooltip/Input

### Selection

Reads Input System, raycasts, owns hovered/selected reference/ID, calls selectable contract and binds inspect panel. Does not mutate stats.

### Tooltip

Reads localized ReadModel. Buttons send Commands. It does not calculate final price/upgrade eligibility.

### Input

One existing action asset/provider path per player. Views do not independently serialize and find the same action by string.

## 17. UI objects

```text
Canvas
├── HUD
├── WaveIntel/Progress
├── TowerShop
├── TowerPanel
├── EnemyInspect
├── Tile/RewardOffer
├── Pause/Settings
└── RunResult/Meta/Loadout screens if scene scope allows
```

UI behavior:

- render read models;
- send commands;
- handle focus/navigation/localization;
- show owner rejection reason;
- never own balance/HP/wave/unlocks.

## 18. VFX/SFX/Visual components

### Representation variants

- authored 3D mesh/model;
- voxel-generated then authored/embedded mesh;
- directional sprite with sockets/shadow;
- hybrid only by explicit art design.

### Cue emitter

Domain Result → local cue adapter → VFX/SFX player. Cue adapter owns handles only.

### Icon

Prefer Editor-generated icon from complete 3D gameplay representation. Runtime actor does not need icon-generation component.

## 19. Component lifecycle

| Stage | Allowed | Forbidden |
| --- | --- | --- |
| Editor authoring | components/refs/visual generation/validation | hidden runtime repair dependency |
| OnValidate | validation/editor preview | gameplay state/mutation without tool/Undo |
| Awake | cache same-object refs, inert state | scene search, Add/Destroy component, start run |
| OnEnable | repeat-safe subscribe/register | duplicate reward/start wave |
| Initialize | apply validated context | fallback for invalid context |
| Active | mutate owned state | direct foreign state writes |
| Terminal | idempotent commit/cleanup/result | duplicate Kill/Leak/reward |
| OnDisable | unsubscribe/cancel active hooks | lose saved state accidentally |
| Pool return | clear refs/effects/trails/listeners | retain previous source/target |
| OnDestroy | final release | remove unrelated authored listeners |

`RequireComponent` documents hard same-object dependency but prefab still contains it before Play Mode.

## 20. Save relation

| Object | Between-wave | Mid-wave only | Rebuilt |
| --- | --- | --- | --- |
| RunFlow | stable phase/terminal receipts | pending timing | UI state |
| WaveFlow | next index/history | cursor/active IDs/timers | spawn views |
| Economy | balance/modifiers | pending transaction if supported | HUD |
| Map | tiles/occupancy | same | NavMesh/anchors views |
| Tower | type/placement/grade/policy/HP/Broken | cooldown/target/contact/repair timers | range/VFX |
| Enemy | none | HP/effects/movement/domain terminal | visual/agent handles |
| Flying movement | none | position/velocity/waypoint/altitude | animation/shadow |
| Base | HP/shield/effects | same | physics handles |
| Projectile | none | full flight payload | trail/pool handle |
| View/Cues | none | none | all presentation |

Restore actor order:

1. resolve Definition;
2. instantiate complete prefab;
3. assign ID/transform;
4. initialize root/modules;
5. apply saved grade/HP/effects/repair;
6. register map/target/wave/contact/aura;
7. rebuild read model/presentation;
8. activate behavior.

## 21. Prefab validation

For every gameplay prefab:

- exactly one root owner;
- required modules and Unity components;
- no missing serialized child references;
- matching Definition/ContentId/catalog entry;
- complete collider/navigation/contact topology;
- exactly one Enemy movement;
- correct Ground/Flying filters;
- 3D representation and icon source;
- localization name/description/tooltip;
- VFX/SFX or explicit N/A;
- selection/input surfaces;
- pool/terminal/reset lifecycle;
- save/restore policy;
- no runtime topology mutation.

## 22. Required, Conditional, Deferred components

### Required

- GameplayEntryPoint and run/wave/economy/map owners;
- Base;
- Tower root/stats/weapon;
- Enemy stats/health/one movement;
- Ground navigation and Flying approach data;
- placement/selection/input/UI;
- Tile/Projectile only where used.

### Conditional

- TowerDurability/TowerContact/TowerAutoRepair;
- FlyingMovement interface after real Ground/Flying implementations;
- Shield/Status/Aura;
- abilities/boss phases;
- cue emitters;
- pool identity;
- shared registries;
- sprite/voxel adapters.

### Deferred

- network/replay identities;
- runtime component composition framework;
- telemetry component on every actor;
- hot-reload adapters;
- ECS presentation bridges.

## 23. Component task template

```text
Gameplay object/prefab:
Root owner:
Required topology:
New logical role:
Why root field/plain object is insufficient:
MonoBehaviour type if justified:
Definition refs:
Owned runtime state:
Commands/Results/Events/ReadModels/Cues:
Ground/Flying behavior:
Scene/run dependency path:
Save/restore:
Pool/terminal lifecycle:
UI/Input/Localization/3D/Icon/VFX/SFX:
No runtime topology mutation:
Failure/no fallback:
Verification:
```

