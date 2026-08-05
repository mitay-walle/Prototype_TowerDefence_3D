---
title: Data contexts, Unity forms и persistence contracts
type: greenfield data architecture contract
status: design-target
updated: 2026-08-04
scope: definitions, runtime, saves, commands, events, read models, catalogs, Addressables and mods
---

# Data contexts, Unity forms и persistence contracts

## 1. Назначение

Документ с нуля определяет все формы данных tower-defense roguelite: от Inspector authoring до runtime run, save/profile, Addressables/mod catalogs, UI read models и presentation cues.

Главное правило:

> Сначала определяется смысл и lifecycle данных, затем Unity-форма. ScriptableObject, MonoBehaviour, JSON и service не являются взаимозаменяемыми решениями.

## 2. Словарь типов

| Термин | Смысл | Mutable | Типичная форма |
| --- | --- | --- | --- |
| Definition | Authoring/content source | Нет в runtime | ScriptableObject, prefab config, serializable block |
| Catalog | Lookup Definition по stable ID | Только load/reload | Runtime index + provider handles |
| Manifest | Content pack metadata | Нет после load | JSON/SO |
| Rules | Immutable вход run/session | Нет | POCO/record snapshot |
| RuntimeState | Живое authoritative state | Да | Owner fields/POCO |
| Instance | Runtime entity/effect identity | Да | Component/POCO |
| SaveDTO | Versioned persistence snapshot | Только build/migration | Serializable POCO |
| Command | Намерение изменить state | Нет | Immutable payload/method args |
| Result | Результат операции | Нет | Immutable payload |
| Event | Факт после commit | Нет | Typed event payload |
| ReadModel | Проекция для consumer/UI | Пересоздаётся | Immutable DTO |
| Cue | Semantic VFX/SFX request | Нет | Cue ID + payload |
| Cache | Derived representation | Да/пересчёт | NavMesh, indexes, aggregates |
| PresentationState | Состояние view/effect | Да | View fields/handles |

Definition, RuntimeState и SaveDTO нельзя называть одним `Data`-классом и мутировать из разных слоёв.

## 3. Полный список data contexts

| Context | Начало | Конец | Примеры | Owner |
| --- | --- | --- | --- | --- |
| Editor authoring | Create/import | Delete/migrate asset | Definitions, prefabs, localization | Content author |
| Content build | Build catalog/bundles | Build complete | manifests, addresses, dependency graph | Editor pipeline |
| Content runtime | Application catalog load | release/quit | loaded definitions/handles | ContentCatalog |
| Application | App boot | quit | settings, scene flow, gateways | ApplicationRoot |
| Profile | load/create profile | switch/delete profile | meta currency, unlocks | Profile owner |
| Settlement | terminal RunResult | atomic profile write | reward delta/receipt | Profile owner |
| Next-run setup | open loadout | StartingRules built | selections/difficulty | Profile owner |
| Scene | scene load | unload | camera, views, scene composition | Scene root |
| Run session | Start/Continue | terminal/abandon | map, economy, Towers, Base | Run owners |
| Preparation phase | enter preparation | commit | drafts, offer, intel | Domain owners |
| Wave | StartWave | resolve/defeat | spawn cursor, active Enemy set | Wave owner |
| Actor | spawn/rent | terminal/return | HP, target, cooldown | Actor owner |
| Movement | actor init | terminal | path progress/waypoint | Movement owner |
| Effect/Aura | apply/enter | expire/exit | stacks, source, handles | Receiver/emitter |
| Command | call | result | placement/upgrade/start | Caller → owner |
| Event | publish | dispatch complete | damage/kill/currency | Publisher |
| Read/UI | bind/query | unbind | HUD, panels, tooltip | ReadModel builder/view |
| Presentation | cue play | stop/release | VFX/SFX handles | Presentation owner |
| Save operation | snapshot request | write result | immutable DTO assembly | Save coordinator/gateway |
| Frame/job | tick/schedule | frame/complete | temporary query buffers | System owner |
| Telemetry | event capture | delivery/drop | copied metrics | Optional sink |

Dependency живёт не меньше consumer. Application object не удерживает destroyed scene actor; actor не владеет profile.

## 4. Что означает `SaveSession`

Термин неоднозначен и не используется как универсальный manager.

| Намерение | Точный контракт |
| --- | --- |
| Живой текущий session/run | Distributed authoritative RuntimeState |
| Сохраняемая копия run | `RunSaveDTO` |
| Операция save | SnapshotCoordinator command scope |
| I/O | `SaveService`/storage gateway |
| Profile между runs | `ProfileSaveDTO` |
| Запуск нового run | `StartingRules` |
| Результат run | `RunResultDTO` |

Новый `SaveSession` type оправдан только при самостоятельном transaction/lifecycle, не покрытом этими границами. Он не должен копировать RunState.

## 5. Stable identity

### 5.1 ContentId

Каждая saved/unlocked Definition имеет stable namespaced ID:

```text
base.tower.cannon
base.enemy.flying_scout
base.wave.001
base.damage.energy
base.reward.repair_cache
mod.author.pack.object
```

Требования:

- уникален во всех loaded packs;
- не зависит от display name/path;
- не переиспользуется с другим смыслом;
- сохраняется при asset rename/move;
- removal/replacement требует migration;
- save хранит ContentId, не Resources path/address.

### 5.2 Runtime IDs

- `EntityId` — actor instance в run;
- `RunId` — run и settlement idempotency;
- `WaveInstanceId` — конкретный wave execution;
- `AttackId` — одна атака;
- `OfferId` — exact reward offer;
- `EffectInstanceId`/`AuraInstanceId`;
- `TransactionId`/`CorrelationId`;
- `ProfileId`/`SaveId`.

ContentId и EntityId нельзя смешивать.

## 6. Unity-формы данных

### 6.1 `const`, enum, struct/value type

Для закрытой технической vocabulary и маленьких immutable values: phase, MovementDomain, vector-like payload, reason code.

Не подходит для designer balance, localized text и versioned saved entity.

### 6.2 Serialized field

`[SerializeField]`/serialized property на MonoBehaviour или ScriptableObject.

Подходит для:

- local prefab/scene references;
- small authored values;
- Definition links;
- child anchors/colliders/cue assets.

Runtime mutation instance field не является save. Shared balance не копируется в сотни prefabs без причины.

### 6.3 `[Serializable]` inline data

Подходит для owned nested block: spawn row, cost row, damage profile, curve settings.

Не подходит для independently shared identity или asset reuse.

### 6.4 `[SerializeReference]`

Подходит для small polymorphic rule/strategy owned одним host: modifier, reward effect, upgrade condition.

Риски:

- concrete type rename требует migration;
- ownership внутри host;
- не заменяет UnityEngine.Object reference;
- не нужен для одного варианта.

### 6.5 ScriptableObject Definition

Подходит для shared authored content:

- Tower/Enemy/Wave/Tile/Base;
- damage/shield/status/aura;
- reward/meta unlock;
- economy/difficulty/map generation;
- catalogs/presentation profiles.

Asset immutable во время run. Runtime state создаётся отдельно. Build не записывает пользовательский save обратно в `.asset`.

### 6.6 Prefab

Хранит authored GameObject topology, components, child hierarchy, local references и default config.

Не хранит глобальный balance/profile/wave index. Required components присутствуют до Play Mode; runtime не добавляет/удаляет их как repair.

### 6.7 Scene

Хранит unique composition: entry point, environment, anchors, views, scene-scoped owners.

Scene lifecycle: load → activate → run → unload. Persistent application services не удерживают stale scene references.

### 6.8 Direct asset/reference

KISS Basic для known required dependency. Type-safe, Inspector-visible, без string lookup.

Prefab asset не ссылается на scene instance. Application service не удерживает scene object после unload.

### 6.9 Resources

Допустим для small prototype catalog, если centralized content owner действительно грузит по runtime ID.

Rules:

- leaf Tower/Enemy/UI не вызывает `Resources.Load`;
- save не хранит path;
- missing required entry — blocking error;
- Resources не fallback для failed Addressables.

### 6.10 Addressables

Для large/dynamic content, DLC, remote bundles и mod-like packs.

Lifecycle:

```text
initialize catalog → load handle → use/instantiate → release symmetrically
```

Handle owner имеет application/scene/run scope. Save по-прежнему хранит ContentId.

### 6.11 Mods/external data

Формы: JSON/CSV/YAML/TextAsset/StreamingAssets/custom package.

Gameplay actors не парсят файлы. Content loader:

1. читает manifest;
2. валидирует game/API version;
3. разрешает dependencies/order;
4. импортирует/загружает Definitions;
5. проверяет IDs/types/assets/localization;
6. создаёт immutable merged catalog.

Duplicate ID, type mismatch, missing dependency и cycle — errors. Base content не подменяет missing required mod.

### 6.12 Save DTO/file

Versioned POCO inside envelope. Хранит IDs и values, но не GameObject/MonoBehaviour/Transform/NavMesh/delegate/VFX handle.

Load DTO применяется owners и перестаёт быть mutable runtime source.

### 6.13 PlayerPrefs

Подходит для small setting/flag. Не подходит для run/profile economy, Tower list, settlement transactions и complex migration.

### 6.14 Plain runtime C# object

Для formulas, deterministic random, ledgers, state aggregates, commands, events, read models, serializers.

Создаётся явно, получает dependencies constructor-ом и не требует Unity callbacks.

### 6.15 MonoBehaviour runtime state

Для actor/scene object, которому нужны Transform, physics, Inspector или Unity lifecycle.

При pooling обязателен explicit reset. Component не становится глобальным save/profile store.

### 6.16 Native/Jobs/ECS

Только после profiling, если масштаб actors требует data-oriented execution. Allocator/world владеет dispose. Не входит в Basic greenfield architecture.

## 7. Definition contracts

### 7.1 Shared blocks

```text
ContentIdentity: ContentId, tags, content version, pack ID
LocalizedText: name, artistic description, gameplay tooltip, short labels
Presentation: gameplay prefab, icon, preview, VFX/SFX cue IDs
Cost: currency ID, amount/curve, refund policy
TargetFilter: faction, tags, layers, Ground/Flying/Both, line of sight
StatCurve: base, growth, min/max, rounding, unit
Modifier: target stat, operation, magnitude, priority, stacking group, scope
```

### 7.2 MovementDomain

```text
MovementDomain
├── Ground
└── Flying
```

Явно хранится в Enemy Definition и используется targeting, spawn, movement, contact, aura/status и UI. Physics layer может оптимизировать query, но не заменяет design field.

### 7.3 TowerDefinition

```text
TowerDefinition
├── Identity, Localization, Presentation
├── BuildCost, SellPolicy
├── PlacementCategory/Footprint
├── MaxGrade, StatCurves
├── WeaponDefinitionId
├── TargetFilter, TargetPolicies
├── UpgradeBranches[]
├── Aura/AbilityIds[]
├── DurabilityDefinition?
├── RoadContactDefinition?
└── Save/pool/presentation requirements
```

### 7.4 TowerDurabilityDefinition

```text
TowerDurabilityDefinition
├── MaxHealth
├── Damage/Resistance profile
├── BrokenBehavior: Remain | Remove
├── Disable weapon/contact/aura flags
├── ManualRepair policy
├── AutoRepairDefinition?
└── damage/break/restore cues
```

### 7.5 RoadContactDefinition

```text
RoadContactDefinition
├── Enabled
├── TargetDomain: Ground | Flying | Both
├── Target filters
├── ResolutionPolicy
├── EnemyDamage
├── TowerDamage
├── PerEnemyCooldown/OneContact
├── Air/Ground authored volume refs
└── contact/outcome cues
```

RoadContact never implies path blocking.

### 7.6 AutoRepairDefinition

```text
AutoRepairDefinition
├── Mode: None | BetweenWavesFull | BetweenWavesAmount | InWaveDelay | InWaveRegen
├── Delay
├── Rate/Amount
├── ResetDelayOnDamage
├── CanRebuildFromBroken
├── RebuildDelay
├── AllowedPhases
└── Free/Paid/Reward source policy
```

### 7.7 EnemyDefinition

```text
EnemyDefinition
├── Identity, Localization, Presentation
├── MovementDomain
├── Health, Speed, LeakDamage, Reward
├── GroundMovementDefinition? XOR FlyingMovementDefinition?
├── Armor/Shield/Resistance
├── Abilities/Auras/Immunities
├── Targetability/Role/Threat traits
└── spawn/death/leak/pool requirements
```

Exactly one movement definition matches domain.

### 7.8 Ground/Flying movement definitions

```text
GroundMovementDefinition
├── Nav agent dimensions/speed/acceleration
├── route constraints
└── arrival policy

FlyingMovementDefinition
├── Direct | Waypoints | Spline | Hover
├── altitude/speed/turning
├── aerial path requirements
├── avoidance policy?
└── arrival policy
```

### 7.9 Weapon/ProjectileDefinition

```text
WeaponDefinition
├── Delivery: Instant | Projectile | Beam | AoE | Pierce | Chain
├── DamageProfile
├── attack interval/range
├── TargetFilter
├── projectile/area/pierce/chain data
└── fire/impact cues

ProjectileDefinition
├── mode, speed, radius, lifetime
├── lost target policy
├── collision/area/pierce policy
├── pool policy
└── visuals/cues
```

Damage amount не дублируется одновременно в weapon и projectile без явной ownership reason.

### 7.10 Damage/defense/effects

```text
DamageTypeDefinition: ID, shield/health matchup rules, icon/text
DamageProfileDefinition: type, base source, crit, bypass, penetration, statuses
ArmorDefinition: flat/percent, type modifiers, order, minimum damage
ShieldDefinition: max, recharge, type factors, bypass, barrier rules
StatusDefinition: duration, tick, magnitude, stacks, refresh, immunity, cues
AuraDefinition: radius, target filter, payload, cadence, stacking, cleanup, cues
```

### 7.11 Wave/SpawnGroupDefinition

```text
WaveDefinition
├── Identity/label
├── ordered SpawnGroups[]
├── threat/intel
├── scaling
├── completion reward
├── special rules
└── result presentation

SpawnGroupDefinition
├── EnemyDefinitionId
├── count/interval/delay
├── lane/spawn selector
├── stat modifiers
└── intel policy
```

### 7.12 Map/Tile/Base

```text
MapGenerationDefinition: seed policy, bounds, route/base/spawn rules, tile pool
TileDefinition: sockets, rotations, footprint, road/build cells, elevation, cost
BaseDefinition: HP/shield/leak/repair/footprint/presentation
SpawnAnchorDefinition/metadata: lane, Ground/Flying/Both, path/approach requirements
```

### 7.13 Economy/Reward/Run

```text
EconomyDefinition: starting currency, kill/completion/passive, refund, repair/tile costs
RewardDefinition: eligibility, weight, scope, effect, target owner route
RewardPoolDefinition: choices, weights, duplicate/reroll policy
RunRulesDefinition: waves, map, economy, Base, allowed content, save/win/loss
DifficultyDefinition: threat/reward/start modifiers
ChallengeDefinition: restrictions, effects, meta reward
```

### 7.14 Meta Definitions

```text
MetaEconomyDefinition: reward curves, currency presentation, sink policy
UnlockDefinition: cost, prerequisites, granted content/options/difficulty
ObjectiveDefinition: condition, progress, outcomes, reward, one-time/repeatable
StartingOptionDefinition: slot/category, conflicts, StartingRules delta
```

## 8. Runtime state contracts

### 8.1 StartingRules

Immutable boundary profile/application → run:

```text
RunId, seed, content version, run/difficulty/challenge IDs,
allowed/loadout content IDs, waves/map/economy/Base IDs,
starting currency/options, save policy
```

### 8.2 RunRuntimeState

Логический aggregate, не обязательный giant object:

```text
RunId, random state, phase, next wave index,
economy, Base, map, Towers, run modifiers,
reward history/pending offer, terminal result
```

Каждую часть мутирует один domain owner.

### 8.3 TowerRuntimeState

```text
EntityId, TowerDefinitionId, placement,
grade/branches/policy, cooldown/target,
modifiers/effects/auras,
current health/Broken/repair state when durable
```

### 8.4 EnemyRuntimeState

```text
EntityId, EnemyDefinitionId, WaveInstanceId,
MovementDomain, lane/spawn,
HP/shield/armor/effects,
Ground path progress OR Flying waypoint/flight progress,
Kill/Leak/reward guards
```

### 8.5 Contact/repair state

```text
TowerContactRuntimeState
├── active pair/correlation guards
├── per-Enemy cooldowns
└── enabled/disabled state

TowerRepairRuntimeState
├── mode
├── time since damage
├── repair/rebuild progress
├── last applied WaveInstanceId
└── cancellation/active flags
```

### 8.6 Wave/Economy/Map/Base

```text
WaveState: schedule cursor, active Enemy IDs, spawned/terminal sets, payout guards
EconomyState: balance, modifiers, ledger sequence
MapState: tile instances, occupancy, revisions, spawn anchors
BaseState: HP/shield/effects/destroyed guard
RewardOfferState: OfferId, exact choices, selected, RNG before/after, resolved
```

### 8.7 Damage/effect runtime

```text
DamagePacket: AttackId, source, type, raw, crit, bypass, penetration, statuses
DamageResult: shield/health damage, prevented values, kill/break flags
StatusInstance: source/target, stacks, duration/tick
AuraState: source, current targets, handles, scan timer
ProjectileState: source/target, packet, transform/velocity/lifetime/hit set
```

## 9. Persistence contracts

### 9.1 SaveEnvelope

```text
SchemaVersion, GameVersion, ContentVersion,
ProfileId, SaveId, timestamps,
Payload, integrity/checksum, migration history
```

Write atomic: temp + replace/platform equivalent. Corrupt save не подменяется новой игрой без решения пользователя.

### 9.2 ProfileSaveDTO

```text
ProfileId, meta currency,
unlocked/purchased IDs,
objective progress,
available difficulties,
selected loadout/starting options,
statistics,
settlement receipts,
migration flags
```

### 9.3 RunSaveDTO — between-wave

```text
RunId, seed/random/content version,
StartingRules snapshot/IDs,
saved phase = stable Preparation,
next wave index,
economy/Base/map/Tower DTOs,
run modifiers/reward history/pending offer,
preparation and payout receipts
```

TowerSave includes durability/Broken/persistent repair policy. Active trigger overlaps and derived stats не сохраняются.

### 9.4 Mid-wave save — Deferred

Дополнительно:

- spawn cursor/timers/active IDs;
- Ground path progress;
- Flying position/velocity/waypoint/altitude;
- HP/shield/effects/terminal guards;
- Tower cooldown/target/contact/repair timers;
- projectiles и aura/effect instances.

Partial snapshot запрещён.

### 9.5 RunResultDTO

Immutable run → meta:

```text
RunId, outcome, seed, difficulty/challenges,
waves completed, objective evidence,
Base/economy/build summaries,
duration, content/result version
```

### 9.6 SettingsSaveDTO

Audio, graphics, input binding overrides, camera, accessibility, locale и schema version. Может использовать отдельный lifecycle/storage.

## 10. Command contracts

| Command | Owner result |
| --- | --- |
| Build StartingRules | immutable rules или validation errors |
| Start/Continue run | launch result |
| Preview/Confirm Tile | validation/mutation result |
| Preview/Confirm Tower | placement result |
| Upgrade/Sell/Relocate Tower | transaction result |
| Set target policy | policy result |
| Repair Tower/Base | repair transaction/result |
| StartWave | transition result |
| ApplyDamage/Status | typed receiver result |
| ResolveTowerContact | aggregate contact result |
| SelectReward | application result |
| SaveRun | SaveResult |
| SettleRun/PurchaseUnlock | MetaSettlement/PurchaseResult |

Command refusal does not mutate state.

## 11. Events

### Lifecycle

`RunStarted`, `PhaseChanged`, `WaveStarted`, `WaveResolved`, `RunEnded`.

### Economy

`CurrencyChanged`, `RewardOffered`, `RewardSelected`, `MetaSettled`, `UnlockPurchased`.

### Combat

`EnemySpawned`, `TargetChanged`, `WeaponFired`, `DamageResolved`, `ShieldBroken`, `StatusChanged`, `AuraMembershipChanged`, `EnemyKilled`, `EnemyLeaked`, `BaseDestroyed`.

### Tower durability/contact

`TowerDamaged`, `TowerBroken`, `TowerRepairStarted`, `TowerRestored`, `TowerEnemyContactResolved`.

### Flying

Отдельные domain events для Flying не нужны, если payload содержит MovementDomain. Presentation может выбирать air-specific cues.

Events публикуются после commit и не содержат mutable owner references.

## 12. ReadModels

- HUD: phase, currency, Base, wave progress, start availability.
- WaveIntel: Ground/Flying groups, traits, lanes, counters.
- TowerShop: unlocked/allowed, cost, role, Ground/Flying support.
- Placement: draft, validation, cost, route/coverage.
- TowerPanel: grade, stats, policy, filters, HP/Broken/repair, upgrades.
- EnemyInspect: domain, HP/shield/armor, speed/path, effects, reward/leak.
- RewardOffer: exact choices, eligibility, reroll state.
- WaveResult: kills/leaks/income/Base/Tower durability.
- RunResult: outcome/build/economy/objectives.
- MetaProgression/Loadout: currency, unlocks, selections, StartingRules preview.

UI не хранит поля ReadModel как второй source of truth.

## 13. Derived caches

| Cache | Источник | Инвалидация |
| --- | --- | --- |
| Content index | loaded Definitions | catalog reload |
| Final stats | Definition + grade/modifiers | grade/effect change |
| Ground NavMesh/route | committed map | map revision |
| Aerial path summary | anchors/map | map revision |
| Occupancy | map/Towers | placement/sell |
| Target spatial index | active actors | register/move/unregister |
| Aura membership | emitters/receivers | movement/scan/source end |
| Coverage preview | Tower/map/draft | stat/map/draft revision |
| Wave intel totals | wave/rules | selected wave/rules |
| Affordability | balance/cost | balance/selection |

Cache не сохраняется. Если его нельзя пересоздать, это потерянный owner state.

## 14. Source-of-truth matrix

| Значение | Source of truth | Не source |
| --- | --- | --- |
| Base stats/content | Definition | UI/save derived values |
| Current Tower grade/HP/Broken | Tower owner | Definition/UI |
| Current Enemy HP/domain progress | Enemy owners | health bar/path visual |
| Run balance | run-economy owner | HUD/ledger copy |
| Meta balance/unlocks | profile owner | UI/PlayerPrefs mirror |
| Wave active set/cursor | wave owner | wave UI |
| Map layout/occupancy | map owner | NavMesh/preview |
| Damage result | receiver commit | projectile/VFX |
| Save file | snapshot of owners | live mutable owner after restore |

## 15. Completeness checklist gameplay object

Для каждой Tower, Enemy, Tile, Weapon, Projectile, Base, Reward и effect:

```text
Identity/ContentId
Definition/settings
Runtime owner/state
Save/restore boundary
Catalog membership/provider
Prefab/3D representation
Icon generated/derived from representation
Localized name, artistic description, gameplay tooltip
Commands/results/events/read models
Input/rebinding if player-controlled
VFX/SFX cues
Ground/Flying filters
Lifecycle create/register/use/terminal/unregister/destroy
Failure/no-fallback behavior
Validation/tests/Play Mode scenario
```

`N/A` требует причины.

## 16. Validation

### Definition

- stable unique ID;
- valid numeric ranges/units;
- required links/IDs exist and type-match;
- prefab component contract complete;
- localization/icon/cues complete or N/A;
- no runtime mutable state;
- Ground/Flying/path/contact consistency;
- no dependency/upgrade/reward cycles.

### Runtime

- one mutable owner;
- one command entry per action;
- Kill XOR Leak;
- payout/contact/repair/settlement idempotent;
- pool reset complete;
- async has owner cancellation;
- required topology authored before Play Mode.

### Save

- versions and IDs present;
- no Unity runtime references;
- exact offer/random restored;
- derived caches absent;
- receipts prevent duplicates;
- missing required content produces blocking error.

## 17. Basic, Extended, Deferred

### Basic

- direct refs or small catalog;
- SO Definitions and authored prefabs;
- POCO runtime rules/read models;
- between-wave RunSave;
- ProfileSave/settings;
- Ground/Flying domain;
- typed damage/result;
- road-contact/repair data only for enabled content.

### Extended

- Addressables;
- branches/statuses/auras;
- saved offers/rerolls;
- objectives/challenges;
- multiple slots/profiles;
- production economy;
- mid-wave save.

### Deferred

- mod packs/hot reload;
- cloud conflicts;
- online account;
- ECS/native storage;
- live migrations/content delivery;
- network/replay data.

