---
title: Gameplay data contracts
status: master-design-contract
updated: 2026-08-04
scope: all gameplay data types across authoring, content, runtime, save, commands, events, UI and presentation
sources: Assets/Documentation/GAMEPLAY_REFERENCES.md, CORE_LOOP.md, RUN_LOOP.md, META_LOOP.md, GAMEPLAY_SCENE_OBJECTS.md, UNITY_DATA_AND_SERVICE_LIFECYCLES.md
---

# Gameplay data contracts

## 1. Назначение и статус

Это master-reference данных Prototype Tower Defence 3D для будущих gameplay-задач.

Документ определяет:

- какие логические типы данных нужны игре;
- в каком контексте они живут;
- кто является владельцем mutable state;
- что редактируется в Inspector/asset;
- что передаётся в runtime;
- что сохраняется;
- какие commands, events и read models соединяют системы;
- какие presentation/localization/catalog surfaces обязательны для каждого gameplay object;
- какие типы являются Basic, Extended или Deferred.

Это design-contract, а не утверждение, что все перечисленные типы уже существуют как C# classes/assets.

Правило реализации:

> Сначала сопоставить логический контракт существующему владельцу и типу. Новый class, ScriptableObject, service, DTO или wrapper создаётся только если текущая форма не может выразить нужную границу.

Примеры:

- логический `TowerDefinition` сейчас может быть реализован парой `TowerStatsSO + Tower prefab` и записью существующего каталога;
- логический `EnemyDefinition` может быть реализован `MonsterStatsSO + enemy prefab`;
- логический `WaveDefinition` уже представлен `WaveConfig`;
- логический `RunState` распределён между `GameManager`, `WaveManager`, `ResourceManager`, `TileMapManager`, `PlayerBase` и `Tower`; отдельный `RunManager` не требуется;
- логический `ProfileSave` пока не реализован полностью; текущий `PlayerPrefs`-флаг `TD3D.StartingReserve` является мигрируемым placeholder.

Текущие код, сцена, prefab, assets и их serialized links остаются implementation truth.

## 2. Словарь суффиксов

| Суффикс/термин | Значение | Мутирует | Пример формы |
| --- | --- | --- | --- |
| `Definition` | Authoring/content source of truth | Нет во время игры | ScriptableObject, prefab fields, `[Serializable]` block |
| `Catalog` | Индекс Definition по stable ID | Только при загрузке каталога | Resources registry, Addressables catalog, runtime dictionary |
| `Manifest` | Описание content pack/version/dependencies | Нет после загрузки | JSON/ScriptableObject |
| `Rules` | Неизменяемый вход новой сессии/забега | Нет | Plain C# snapshot |
| `State` | Живое mutable состояние владельца | Да | MonoBehaviour fields, plain C# object |
| `Instance` | Экземпляр эффекта/сущности с identity | Да | Runtime object/component |
| `Snapshot` | Неизменяемая копия состояния в момент времени | Нет | Plain C# record/class |
| `SaveDTO` | Версионируемая persistence-форма | Меняется только при построении/миграции | Serializable POCO |
| `Command` | Запрос изменить state | Нет после создания | Immutable struct/class, method parameters |
| `Result` | Результат команды или расчёта | Нет | Immutable struct/class |
| `Event` | Факт уже произошедшего изменения | Нет | C# event payload/UnityEvent payload |
| `ReadModel` | Проекция состояния для consumer/UI | Пересоздаётся owner | Immutable struct/class |
| `Cue` | Запрос presentation без gameplay authority | Нет | VFX/SFX cue ID + payload |
| `Cache` | Derived ускоряющее представление | Да, пересчитывается | NavMesh, indexes, aggregate stats |
| `Id` | Stable identity, не display name | Нет | String/Guid-backed value |

Не использовать взаимозаменяемо `Definition`, `State` и `SaveDTO`. Изменение `TowerDefinition` ради upgrade конкретной башни является ошибкой контекста.

## 3. Контексты и жизненные циклы

| Контекст | Начало | Конец | Типичные данные | Владелец |
| --- | --- | --- | --- | --- |
| Editor authoring | Создание/import asset | Удаление/миграция asset | Definitions, prefabs, localization, cues | Unity assets/content authors |
| Content load | Application boot/catalog load | Application quit/catalog release | Loaded catalog, manifests, indexes | Content owner |
| Application | Запуск приложения | Quit | Settings, scene flow, save/content services | Application composition root |
| Profile | Load/create profile | Смена/удаление profile | Meta currency, unlocks, objectives | Один profile/meta owner |
| Meta settlement | Terminal RunResult | Atomic profile save | Reward delta, receipts, unlock results | Profile/meta owner |
| Next-run setup | Открытие loadout | Создание StartingRules | Selected options, difficulty, allowed content | Profile/meta owner + UI commands |
| Scene | Load Gameplay.unity | Scene unload | Camera, HUD, scene owners/anchors | Scene composition |
| Run | StartNewRun/ContinueRun | Victory/Defeat/Abandon | Map, bank, base, towers, rewards | Existing run owner chain |
| Preparation | Enter Preparation | StartNextWave | Draft/confirmed placement, offer, intel | Gameplay owners |
| Wave | StartNextWave | WaveResolve/Defeat | Spawn cursor, alive enemies, payout flags | WaveManager |
| Actor | Spawn/rent | Destroy/return | HP, target, cooldown, effects | Actor component |
| Effect/Aura | Apply/enter | Expire/remove/exit | Stacks, source, duration, handles | Target/owner |
| Command | Invocation | Return/completion | Request payload | Caller → owner |
| Event | Publication | Dispatch complete | Immutable fact | Owner → consumers |
| Frame/job | Tick/schedule | End/complete | Temporary work | System/job owner |
| Save | Snapshot build | Load/migration/application | DTOs/envelope | SaveService + domain snapshot owner |

Dependency может жить столько же или дольше consumer. Profile/Application object не удерживает уничтоженную scene reference; actor не владеет run/profile service.

## 4. Identity и reference rules

### 4.1 Stable content ID

Каждая сохраняемая или unlock-совместимая Definition имеет stable `ContentId`.

Требования:

- не зависит от display name и asset path;
- уникален в объединённом content catalog;
- не переиспользуется для другого смысла;
- сохраняется при rename/move asset;
- имеет явную migration при замене;
- используется в ProfileSave, RunSave, RunResult и mod manifests.

Формат может быть namespaced string:

```text
base.tower.basic
base.enemy.turtle
base.wave.01
base.tile.road_straight
base.reward.resource_cache
base.meta.starting_reserve
mod.author.pack.object
```

GUID Unity допустим как внутренний asset reference, но save/mod contract лучше опирается на явный стабильный ID.

### 4.2 Runtime entity ID

Каждый динамический Tower/Enemy/Projectile/Effect получает runtime `EntityId`.

- уникален в пределах run;
- позволяет events/read models ссылаться без `GameObject`;
- сохраняется только если instance входит в save boundary;
- не является content ID;
- при pooling новый logical spawn получает новый lifecycle identity либо явно сброшенный generation.

### 4.3 Session IDs

Нужны отдельные:

- `ProfileId` — профиль;
- `RunId` — забег и idempotent settlement;
- `OfferId` — один limited offer;
- `WaveInstanceId` — конкретный запуск Wave Definition;
- `SettlementReceiptId` — подтверждение meta-транзакции при расширенной схеме.

### 4.4 Reference policy

- Definition → Definition: direct asset reference в base content либо stable ContentId в mod/external catalog.
- Runtime → Definition: resolved read-only reference + ContentId.
- Runtime → Runtime: EntityId/typed reference внутри того же scope.
- Save → Definition: только ContentId.
- Save → Runtime entity: stable instance ID только для сохраняемых экземпляров.
- UI → Runtime: EntityId + read model/command, не mutable component state.
- VFX/SFX → Gameplay: cue получает result payload и ничего не возвращает в damage/economy.

## 5. Общие data blocks

Эти блоки логические. Они могут быть `[Serializable]` inline data, частью текущего asset или отдельной Definition только при переиспользовании.

### 5.1 ContentIdentity

```text
ContentIdentity
├── Id: ContentId
├── Tags[]: TagId
├── Version
├── PackId
└── DeprecatedReplacementId?  // только для migration tooling, не runtime fallback
```

### 5.2 LocalizedTextRefs

```text
LocalizedTextRefs
├── Name: LocalizedString
├── ArtisticDescription: LocalizedString
├── GameplayTooltip: LocalizedString
├── RoleLabel?: LocalizedString
└── ShortLabel?: LocalizedString
```

Для каждого player-facing gameplay object обязательны name, artistic description и gameplay tooltip. Если объект не показывается игроку, это фиксируется как `N/A: non-player-facing technical object`.

### 5.3 PresentationRefs

```text
PresentationRefs
├── GameplayPrefab?: GameObject
├── Icon: Sprite
├── IconRenderSource?: prefab/3D view reference
├── PreviewPrefab?: GameObject
├── Build/Spawn/Use/Impact/DeathVfxCueIds[]
├── Build/Spawn/Use/Impact/DeathSfxCueIds[]
└── PresentationTags[]
```

Icon gameplay-объекта производится рендером его 3D representation в sprite по project workflow. Ручная несвязанная иконка допустима только как явное art-direction решение.

### 5.4 StatCurveDefinition

Текущий аналог — `BaseStatEntry`.

```text
StatCurveDefinition
├── BaseValue
├── GrowthCurve
├── RoundToInt
├── Min?
├── Max?
└── Unit/DisplayFormat
```

### 5.5 ModifierDefinition

Текущие аналоги — `StatModifier`, `BasicModifier`, `GradeScalingModifier`, `ModifierSO`.

```text
ModifierDefinition
├── Id?                    // нужен для shared/saveable modifier
├── TargetStat
├── Operation              // add, add-percent, multiply, override, clamp
├── Magnitude/Curve
├── Priority
├── StackingGroup
├── MaxStacks
├── DurationPolicy
└── SourceTags
```

Маленький уникальный modifier хранится через `[SerializeReference]` внутри owner Definition. Переиспользуемый modifier с собственной identity — ScriptableObject/ContentId.

### 5.6 CostDefinition

```text
CostDefinition
├── CurrencyId
├── Amount/Curve
├── RefundFraction?
└── ScalingPolicy?
```

Basic run использует одну run currency; Basic meta использует одну отдельную meta currency.

### 5.7 TargetFilterDefinition

```text
TargetFilterDefinition
├── RequiredTags[]
├── ExcludedTags[]
├── LayerMask
├── FactionRule
├── AliveOnly
├── Air/Ground policy
└── LineOfSightPolicy
```

### 5.8 CueDefinition

```text
VfxCueDefinition
├── Id
├── Prefab/Graph reference
├── Lifetime
├── Pool policy
├── Attach policy
├── Quality tier
└── Required payload fields

SfxCueDefinition
├── Id
├── AudioClip/AudioContainer
├── Mixer bus
├── Spatial policy
├── Volume/Pitch variation
├── Priority/Voice limit
└── Required payload fields
```

Cue может быть inline prefab reference в Basic. Отдельный cue catalog нужен только при ID-driven/pooling/mod pipeline.

## 6. Authoring/Definition contracts

### 6.1 ContentManifestDefinition

Нужен для catalog validation; отдельный asset обязателен только при нескольких packs/mods.

```text
ContentManifestDefinition
├── PackId
├── PackVersion
├── RequiredGameVersion
├── Dependencies[]
├── LoadPriority
├── Entries[]: ContentId + type + asset reference/address
├── LocalizationTables[]
├── CatalogChecksum
└── Author/License metadata
```

Basic base content может собрать manifest Editor-инструментом из существующих Resources/direct references.

### 6.2 RunRulesDefinition

```text
RunRulesDefinition
├── Id
├── WaveDefinitionIds[]
├── LevelGenerationDefinitionId
├── BaseDefinitionId
├── EconomyDefinitionId
├── AllowedTowerIds[]
├── RewardPoolId?
├── StartingTileRules
├── DifficultyId
├── AllowedChallengeIds[]
├── SavePolicy
└── Win/Loss rules
```

Это authored template. Конкретный новый run получает immutable `StartingRules` snapshot.

### 6.3 DifficultyDefinition

```text
DifficultyDefinition
├── Id + LocalizedTextRefs + Icon
├── EnemyHealthMultiplier
├── EnemyCountMultiplier
├── EnemySpeedMultiplier
├── RewardMultiplier
├── StartingCurrencyModifier
├── BaseHealthModifier
├── Allowed/ForcedChallengeIds[]
├── UnlockPrerequisiteId?
└── SortOrder
```

Не хранит runtime wave index и не меняется в ходе run.

### 6.4 ChallengeModifierDefinition

```text
ChallengeModifierDefinition
├── Id + LocalizedTextRefs + Icon
├── CompatibilityTags
├── Threat modifiers[]
├── Economy modifiers[]
├── Map/build restrictions[]
├── MetaRewardMultiplier/Bonus
├── Conflicts[]
└── Validation rules
```

Текущий `ChallengeModifier` enum — Basic identity. Definition нужна при росте числа modifiers, локализации и mod support.

### 6.5 EconomyDefinition

```text
EconomyDefinition
├── StartingCurrency
├── PassiveIncomePerWave
├── PassiveIncomeEnabled
├── KillRewardPolicy
├── CompletionRewardPolicy
├── SellRefundFraction
├── RepairCostPolicy?
├── TileCostPolicy?
├── Interest/Production policy?   // Extended
└── CurrencyPresentation
```

Текущий owner runtime-валюты остаётся `ResourceManager`.

### 6.6 LevelGenerationDefinition

Текущие аналоги — `TowerDefenceLevelGeneratorSO`, generation profiles и `LevelGenerator` settings.

```text
LevelGenerationDefinition
├── Id
├── SeedPolicy
├── Width/Height/Bounds
├── InitialPathRules
├── Branch/OpenEnd rules
├── BasePlacement rules
├── SpawnPlacement rules
├── InitialTilePoolId
├── Terrain/height rules
├── NavMesh settings reference
└── Presentation/biome references
```

Generated map dictionary, road cells и NavMesh являются runtime/derived data, не Definition.

### 6.7 TileDefinition

Текущие аналоги — `RoadTileDef`, `RoadTileComponent`, `TileDatabase` entry и tile prefab.

```text
TileDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── PresentationRefs
├── Connections/edge sockets
├── AllowedRotations
├── Footprint
├── BuildCells/BuildSockets[]
├── Road/path cells
├── Height/Elevation data
├── Placement tags/restrictions
├── CostDefinition?
├── Spawn/Base compatibility
└── NavMesh contribution metadata
```

`TileDatabase` остаётся текущим catalog owner. Не создавать второй map/tile registry.

### 6.8 BaseDefinition

```text
BaseDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── PresentationRefs
├── MaxHealth
├── MaxShield?
├── Shield rules?
├── DefaultLeakPolicy
├── Repair policy?
├── Footprint/Anchor rules
└── Damage/Status immunities
```

Текущий `PlayerBase` содержит Basic max/current health; Definition нужна при нескольких base types или meta starting options.

### 6.9 WaveDefinition

Текущий тип — `WaveConfig`.

```text
WaveDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── WaveNumber/SortOrder
├── DelayBeforeWave
├── SpawnGroups[]
├── CompletionReward
├── HealthScaling
├── CountScaling
├── ThreatTraits[]
├── SpecialRuleIds[]
└── ResultPresentation
```

`WaveName` должен стать localized при player-facing использовании; authored numeric index не является stable ContentId.

ML-generated `WaveConfig` дополнительно хранит применённые adaptive health/count/speed/reward-факторы как provenance конкретной сгенерированной волны. Эти поля описывают generation snapshot и не заменяют authored difficulty rules или runtime state `WaveManager`.

### 6.10 SpawnGroupDefinition

Текущий тип — `EnemySpawnData`.

```text
SpawnGroupDefinition
├── EnemyDefinitionId / enemy prefab reference
├── Count
├── SpawnInterval
├── DelayBeforeGroup
├── HealthMultiplier
├── SpeedMultiplier
├── Lane/SpawnPoint selector
├── Formation/Spacing?
├── GroupModifiers[]
└── Telegraph/Intel policy
```

Basic может хранить prefab reference, но save/mod/intel требуют разрешаемый Enemy ContentId.

### 6.11 EnemyDefinition

Текущая реализация распределена между `MonsterStatsSO`, enemy prefab, `MonsterHealth`, `MonsterMove` и presentation assets.

```text
EnemyDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── PresentationRefs
├── Stats
│   ├── Health
│   ├── MoveSpeed
│   ├── LeakDamage
│   ├── InstantReward
│   ├── IncomeReward
│   └── EarlyKillModifier
├── ArmorProfile?
├── ShieldProfile?
├── Resistances[]
├── ImmunityTags[]
├── Ability/AuraIds[]
├── Pathing/Targetability tags
├── Death/Leak policy
└── Difficulty/Spawn tags
```

Текущий `MonsterStatsSO.Damage` логически соответствует damage/leak contribution и должен быть уточнён owner-side перед расширением типов урона.

### 6.12 TowerDefinition

Текущая реализация распределена между `TowerStatsSO`, Tower prefab, weapon component, `TowerStatsVisual`, localization и shop data.

```text
TowerDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── PresentationRefs
├── Cost
├── MaxGrade
├── StatCurves
│   ├── UpgradeCost
│   ├── Damage
│   ├── FireRate
│   ├── Range
│   ├── CritChance
│   ├── ProjectileSpeed
│   └── RotateSpeed
├── WeaponDefinitionId / prefab weapon reference
├── ProjectileDefinitionId?
├── TargetFilter
├── AllowedTargetPriorities[]
├── UpgradeRules/Branches[]
├── AuraIds[]
├── Placement restrictions
├── Sell policy
├── Role/DefensiveIdentity localization
└── BalanceProfile reference
```

Не создавать wrapper `TowerDefinitionSO`, пока текущий `TowerStatsSO + prefab/catalog` покрывает контракт. При появлении Addressables/mods wrapper/catalog entry может стать оправданным.

### 6.13 WeaponDefinition

Текущие Basic-варианты реализуют `IWeapon`: Projectile, Instant, Beam, AoE, Pierce.

```text
WeaponDefinition
├── ContentIdentity?             // нужен для shared/modded weapon
├── DeliveryType
├── DamageApplicationType
├── DamageProfile
├── Fire/Hit cadence
├── MaxRange
├── ProjectileDefinitionId?
├── SplashRadius/Falloff?
├── PierceCount/Falloff?
├── BeamDuration/TickInterval?
├── Chain rules?
├── TargetFilter
├── Fire/Impact Cue refs
└── Required prefab component contract
```

Basic unique weapon settings могут оставаться serialized fields на Tower/weapon prefab. Отдельный asset нужен для переиспользования, save ID, mod catalog или authoring масштаба.

### 6.14 ProjectileDefinition

```text
ProjectileDefinition
├── ContentIdentity?             // нужен для catalog/shared projectile
├── PresentationRefs
├── Mode                         // homing, straight, ballistic, spherecast
├── Speed source/policy
├── Radius/Collision mask
├── MaxLifetime
├── LostTargetPolicy
├── AreaDamage radius/percent?
├── Pierce policy?
├── Trail/Impact cues
└── Pool policy
```

Damage amount не должен независимо дублироваться в projectile, если source weapon уже передаёт immutable `DamagePacket`.

### 6.15 DamageTypeDefinition

Basic может использовать enum (`Physical`, `Energy`, `Explosive`, `True`). Mod-ready вариант использует ContentId.

```text
DamageTypeDefinition
├── Id + LocalizedTextRefs + Icon
├── Tags
├── Default shield multiplier
├── Default armor multiplier
├── Default health multiplier
├── Presentation cue overrides?
└── Ordering/Rules text
```

Точные matchup multipliers лучше хранить в resistance profile цели либо общей matrix Definition, но не одновременно в нескольких владельцах.

### 6.16 DamageProfileDefinition

```text
DamageProfileDefinition
├── DamageTypeId
├── Base/Stat source
├── Crit policy
├── ShieldBypassFraction
├── ArmorPenetration
├── Splash/Pierce scaling
├── StatusApplications[]
└── DamageTags[]
```

### 6.17 ArmorProfileDefinition

```text
ArmorProfileDefinition
├── ArmorTypeId
├── FlatReduction?
├── PercentageReduction?
├── TypeModifiers[]
├── MinimumDamage policy
├── Break/Degrade rules?
└── Presentation refs
```

Если одновременно используются flat и percentage reduction, Definition фиксирует порядок.

### 6.18 ShieldProfileDefinition

```text
ShieldProfileDefinition
├── MaxShield
├── RechargeDelay
├── RechargeRate
├── AllowedDamageTypes
├── TypeModifiers[]
├── Bypass rules
├── BarrierHitCount?
└── Hit/Break/Recharge cues
```

### 6.19 StatusEffectDefinition

```text
StatusEffectDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── TargetFilter
├── Duration
├── TickInterval?
├── Magnitude/Modifiers[]
├── MaxStacks
├── StackPolicy
├── Refresh/Replace policy
├── ImmunityTags
├── Dispel/Expire policy
└── Apply/Tick/Expire VFX/SFX cues
```

Примеры: slow, burn, poison, stun, mark, vulnerability.

### 6.20 AuraDefinition

```text
AuraDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── Radius
├── TargetFilter
├── Modifier/Status payload
├── UpdateCadence
├── StackingGroup/Policy
├── Enter/Exit policy
├── OwnerDeath policy
└── Persistent VFX/SFX cues
```

Aura Definition не хранит текущий набор targets.

### 6.21 UpgradeDefinition

Текущая система — `StatsSO.upgradeRules` с `[SerializeReference]` и `UpgradeRule` variants.

```text
UpgradeDefinition
├── Id? / BranchId
├── LocalizedTextRefs
├── Icon
├── StartGrade
├── RepeatEvery/RepeatMax
├── Cost policy
├── Prerequisites[]
├── Conflicts[]
├── Modifiers[]
├── NewStats/Weapon/Ability replacement?
└── Presentation cues
```

Inline UpgradeRule остаётся подходящим для маленького unique rule. Branch с UI/save identity требует stable ID.

### 6.22 RewardDefinition

```text
RewardDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── Rarity/Weight
├── Eligibility conditions
├── Conflicts/Unique groups
├── MaxStacks
├── Effect payload
├── Scope: wave/run/meta
├── Owner route
└── Select/Apply cues
```

Effect payload не мутирует произвольные systems. Он маршрутизируется к владельцу: currency → ResourceManager, repair → PlayerBase, tower augment → Tower/upgrade owner, unlock → profile owner.

### 6.23 RewardPoolDefinition

```text
RewardPoolDefinition
├── Id
├── RewardIds[] + weights
├── OfferSize
├── Rarity rules
├── Duplicate/Unique policy
├── Reroll/Banish policy
├── Eligibility filters
└── Empty-pool validation
```

Empty required pool является content error, не поводом выдать случайный fallback reward.

### 6.24 StartingOptionDefinition

```text
StartingOptionDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── Category/Slot
├── PrerequisiteUnlockIds[]
├── Conflicts[]
├── StartingRules delta
└── Preview/read model data
```

Пример: `starting.reserve`, starter tower, additional tile choice.

### 6.25 MetaUnlockDefinition

```text
MetaUnlockDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── UnlockType
├── Cost
├── Prerequisites[]
├── GrantedContentIds[]
├── GrantedStartingOptionIds[]
├── GrantedDifficultyIds[]
├── MaxLevel/Level effects?       // Extended
└── Presentation/order data
```

### 6.26 ObjectiveDefinition

```text
ObjectiveDefinition
├── ContentIdentity
├── LocalizedTextRefs
├── Icon
├── ProgressType/Target
├── Conditions
├── AllowedOutcomes
├── Repeatable/OneTime
├── Hidden/Visible policy
├── MetaReward
├── GrantedUnlockIds[]
└── Progress presentation
```

### 6.27 SettingsDefinition

```text
SettingsDefinition
├── Default audio/graphics/input/accessibility values
├── Allowed ranges/options
├── InputActionAsset reference
├── Localization defaults
└── Platform overrides
```

User values сохраняются в SettingsSaveDTO/PlayerPrefs по одному owner contract; defaults не мутируются.

## 7. Сопоставление текущих типов

| Логический контракт | Текущий тип/asset | Решение для будущей задачи |
| --- | --- | --- |
| TowerDefinition | `TowerStatsSO` + Tower prefab + weapon + localization | Расширять текущую пару; wrapper только для catalog/mod boundary |
| EnemyDefinition | `MonsterStatsSO` + enemy prefab + `MonsterHealth/Move` | Добавлять missing identity/presentation refs существующему owner chain |
| WaveDefinition | `WaveConfig` | Расширять asset, не создавать второй wave config type |
| SpawnGroupDefinition | `EnemySpawnData` | Добавить lane/ID/intel fields при необходимости |
| TileDefinition | `RoadTileDef`, `RoadTileComponent`, `TileDatabase` | Сохранить `TileDatabase` catalog owner |
| LevelGenerationDefinition | `TowerDefenceLevelGeneratorSO`, generation profiles | Отделять authored settings от generated runtime caches |
| Tower stat definition | `TowerStatsSO`, `BaseStatEntry` | Не сохранять derived values как Definition |
| Enemy stat definition | `MonsterStatsSO`, `BaseStatEntry` | Уточнить Damage/leak semantics перед damage types |
| Upgrade rule | `UpgradeRule` hierarchy через `[SerializeReference]` | Stable BranchId добавить только для UI/save веток |
| Modifier | `StatModifier`, `ModifierSO` | Inline unique, asset shared |
| Weapon runtime | `IWeapon` implementations | Общий DamagePacket вводить в реальном damage owner |
| Run transition | `GameManager`, `GameState` | Не добавлять RunManager/RoundManager |
| Wave runtime | `WaveManager` | Не добавлять второй scheduler/state holder |
| Run currency | `ResourceManager` | Profile currency хранить отдельно у profile owner |
| Meta StartingReserve | `PlayerPrefs` key | Однократно мигрировать в ProfileSave unlock ID |
| Localization | `LocalizedString` в Tower/Monster/UI | Свести name/description/tooltip для каждого object |
| Content loading | direct refs + `Resources` | Catalog owner сначала; Addressables позже без fallback |
## 8. Runtime contracts

Runtime state принадлежит существующему gameplay/application owner. Эти схемы не требуют единого большого объекта.

### 8.1 LoadedContentCatalog

```text
LoadedContentCatalog
├── ContentVersion
├── Manifests[]
├── DefinitionsById
├── DefinitionsByType
├── Localization table handles
├── Asset load handles            // только Addressables-вариант
└── ValidationReport
```

Жизненный цикл: application/content load → release. После успешной загрузки lookup read-only.

### 8.2 ProfileRuntimeState

```text
ProfileRuntimeState
├── ProfileId
├── Loaded ProfileSave data
├── Dirty/Save transaction state
├── MetaCurrency
├── Unlock sets
├── Objective progress
├── Selected starting options
├── Difficulty access
└── Settlement receipts
```

Один owner. UI получает read model и отправляет commands; gameplay actors профиль напрямую не мутируют.

### 8.3 MetaSettlementResult

```text
MetaSettlementResult
├── RunId
├── WasAlreadySettled
├── CurrencyBefore/Delta/After
├── CompletedObjectiveIds[]
├── NewlyUnlockedIds[]
├── NewDifficultyIds[]
├── ReceiptId
└── Failure reason?
```

Immutable result для UI после атомарной записи профиля.

### 8.4 StartingRules

Immutable snapshot между meta/application и новым run.

```text
StartingRules
├── RunId
├── Seed
├── ContentVersion
├── RunRulesId
├── DifficultyId
├── ChallengeIds[]
├── WaveIds[]
├── LevelGenerationId
├── EconomyDefinitionId
├── BaseDefinitionId
├── AllowedTowerIds[]
├── SelectedLoadoutTowerIds[]
├── RewardPoolId
├── StartingOptionIds[]
├── StartingCurrency
├── StartingBaseModifiers[]
├── StartingTileRules
└── SavePolicy
```

После создания StartingRules не читает mutable ProfileSave. Run-only rewards не записываются обратно в него.

### 8.5 RunRuntimeState

Логический aggregate, распределённый между owners.

```text
RunRuntimeState
├── RunId
├── Seed/RandomState
├── ContentVersion
├── GameState
├── NextWaveIndex
├── EconomyState
├── BaseRuntimeState
├── MapRuntimeState
├── TowerRuntimeStates[]
├── RunModifierInstances[]
├── RewardHistory[]
├── CurrentRewardOffer?
├── PreparationFlags
└── TerminalResult?
```

`GameManager` владеет global transition state. Остальные части остаются у своих owners и предоставляют snapshots.

### 8.6 EconomyRuntimeState

Текущий owner — `ResourceManager`.

```text
EconomyRuntimeState
├── CurrentCurrency
├── AppliedRunModifiers[]
├── PendingCompletionBonus
├── PassiveIncome state
├── Ledger sequence number
└── RecentLedgerEntries[]         // bounded/read-only audit, optional
```

`LedgerEntry`:

```text
CurrencyLedgerEntry
├── Sequence
├── ReasonId/ReasonType
├── SourceEntityId?
├── WaveInstanceId?
├── Delta
├── BalanceAfter
└── Timestamp/SimulationTick
```

Ledger не является вторым balance owner. Balance изменяется только через `TrySpend/Grant`.

### 8.7 MapRuntimeState

Текущий owner — `TileMapManager` и level systems.

```text
MapRuntimeState
├── MapInstanceId
├── Seed
├── Bounds/Grid transform
├── TilesByCell
├── OccupancyByCell
├── BaseAnchor
├── SpawnPointStates[]
├── OpenRoadEnds[]
├── TopologyRevision
└── DerivedCacheRevision
```

`TileInstanceState`:

```text
TileInstanceState
├── TileInstanceId
├── TileDefinitionId
├── GridPosition
├── Rotation
├── RuntimeFlags
├── OccupiedBuildSockets[]
└── VariantId?
```

`SpawnPointRuntimeState`:

```text
SpawnPointRuntimeState
├── SpawnPointId
├── LaneId
├── TileInstanceId/Anchor
├── Position/Rotation
├── IsActive
├── AllowedEnemyTags[]
└── RuntimeModifiers[]
```

NavMesh, route graph и world transforms, выводимые из layout, являются caches.

### 8.8 BaseRuntimeState

Текущий owner — `PlayerBase`.

```text
BaseRuntimeState
├── BaseInstanceId
├── BaseDefinitionId
├── CurrentHealth/MaxHealth
├── CurrentShield/MaxShield?
├── AppliedEffectInstances[]
├── IsDestroyed
├── LastDamageSequence
└── Repair modifiers
```

`IsDestroyed`/terminal signal должны быть idempotent; один переход через ноль HP даёт одно событие.

### 8.9 RewardOfferRuntimeState

```text
RewardOfferRuntimeState
├── OfferId
├── Scope: wave/run/meta
├── SourcePoolId
├── ChoiceRewardIds[]
├── SelectedRewardId?
├── RerollsUsed
├── BanishIds[]
├── RandomStateBefore/After
├── IsResolved
└── CreatedForWaveIndex/RunId
```

UI не генерирует choices. Save восстанавливает тот же offer, а не reroll.

### 8.10 WaveIntelSnapshot

Создаётся `WaveManager` из Wave Definition и применённых difficulty/challenge rules.

```text
WaveIntelSnapshot
├── WaveDefinitionId
├── WaveIndex
├── TotalEnemyCount
├── SpawnGroup summaries[]
├── EnemyRole/ThreatTrait summaries[]
├── Lane summaries[]
├── Known multipliers
├── CompletionReward preview
├── HiddenFieldsReason?
└── Localization/Icons
```

Это read-only информация до commit, не отдельный owner волн.

### 8.11 WaveRuntimeState

Текущий owner — `WaveManager`.

```text
WaveRuntimeState
├── WaveInstanceId
├── WaveDefinitionId
├── WaveIndex
├── Loop/Difficulty counters if supported
├── SpawnGroupIndex
├── SpawnIndexInGroup
├── SpawnCursor/ElapsedTime
├── EnemiesSpawned
├── TotalEnemies
├── AliveEnemyIds[] / authoritative count
├── IsSpawning
├── IsResolved
├── CompletionPayoutApplied
├── ActiveChallengeIds[]
└── Cancellation scope
```

Alive count должен учитывать split/summon children. Wave resolve возможен только при исчерпанном spawn schedule и отсутствии живых зарегистрированных врагов.

### 8.12 TowerRuntimeState

Текущий owner — instance `Tower` + `TowerStats`.

```text
TowerRuntimeState
├── EntityId
├── TowerDefinitionId
├── Tile/Socket/World placement identity
├── Grade
├── BranchIds[]
├── TargetPriority
├── CurrentTargetEntityId?       // runtime only
├── AttackCooldown/LastFireTime
├── CurrentRotation
├── AppliedModifierInstances[]
├── AppliedAura handles[]
├── AggregatedStatsCache
├── SellValue
├── IsActive/IsDestroyed
└── Pool generation?             // если Tower pooling появится
```

Definition fields не мутируются. Save сохраняет identity/grade/branch/policy/placement, но не current target и derived stats.

### 8.13 EnemyRuntimeState

Текущие owners — `MonsterHealth`, `MonsterMove`, `MonsterStats`.

```text
EnemyRuntimeState
├── EntityId
├── EnemyDefinitionId
├── WaveInstanceId
├── SpawnPointId/LaneId
├── CurrentHealth/MaxHealth
├── CurrentShield/MaxShield?
├── Armor runtime state?
├── CurrentSpeed
├── PathProgress/PathSegment
├── AppliedEffectInstances[]
├── ActiveAuraIds[]
├── IsDead
├── HasLeaked
├── RewardGranted
├── Death/Leak sequence
└── Pool generation
```

`IsDead`, `HasLeaked` и `RewardGranted` образуют взаимоисключающий lifecycle terminal contract.

### 8.14 ProjectileRuntimeState

Текущий owner — `Projectile`.

```text
ProjectileRuntimeState
├── EntityId
├── ProjectileDefinitionId
├── SourceEntityId
├── TargetEntityId?
├── DamagePacket
├── Position/Rotation/Velocity
├── LastTargetPosition
├── RemainingLifetime
├── RemainingPierceCount
├── AlreadyHitEntityIds[]
├── HasResolvedImpact
└── Pool generation
```

Between-wave save не хранит projectile. Mid-wave save обязан хранить полный необходимый state либо использовать отдельно доказанный deterministic restore.

### 8.15 DamagePacket

Immutable payload атаки.

```text
DamagePacket
├── AttackId
├── SourceEntityId
├── SourceDefinitionId
├── DamageTypeId
├── RawDamage
├── IsCritical
├── ShieldBypassFraction
├── ArmorPenetration
├── DamageTags[]
├── StatusApplicationPayloads[]
├── Origin/HitPosition context
└── SimulationTick
```

Weapon создаёт/доставляет packet; target-side resolver рассчитывает результат.

### 8.16 DamageResult

```text
DamageResult
├── AttackId
├── SourceEntityId
├── TargetEntityId
├── RawDamage
├── ShieldDamage
├── HealthDamage
├── PreventedByResistance
├── PreventedByArmor
├── WasCritical
├── WasImmune
├── ShieldBroken
├── TargetKilled
├── AppliedEffectInstanceIds[]
└── Result tags
```

VFX/SFX/UI получают DamageResult, не пересчитывают урон.

### 8.17 StatusEffectInstance

```text
StatusEffectInstance
├── EffectInstanceId
├── StatusDefinitionId
├── SourceEntityId
├── TargetEntityId
├── StackCount
├── Magnitude snapshot/dynamic policy
├── RemainingDuration
├── TimeUntilNextTick
├── AppliedTick
├── IsExpired
└── Owner handle
```

Target владеет instances и снимает их по source/handle без удаления чужого stack.

### 8.18 AuraRuntimeState

```text
AuraRuntimeState
├── AuraInstanceId
├── AuraDefinitionId
├── OwnerEntityId
├── CurrentTargetEntityIds[]
├── AppliedEffectHandlesByTarget
├── TimeUntilNextScan
├── IsActive
└── Spatial query revision
```

Current targets — runtime data; Definition хранит filter/radius/stacking.

### 8.19 RunModifierInstance

```text
RunModifierInstance
├── ModifierInstanceId
├── SourceReward/Challenge/StartingOptionId
├── ModifierDefinitionId
├── Scope
├── StackCount
├── RemainingWaves/Duration?
├── AppliedOwnerIds[]
└── IsExpired
```

Run-only instance не переносится в ProfileSave.

### 8.20 PlacementDraftState

Owner — placement system; presentation-only до confirm.

```text
PlacementDraftState
├── SelectedContentId
├── PlacementType: tower/tile
├── GridPosition/Socket
├── Rotation
├── CostPreview
├── ValidationResult
├── CoveragePreview
├── RoutePreview?
└── Revision
```

Draft не меняет деньги, occupancy, map, NavMesh или save.

### 8.21 PlacementValidationResult

```text
PlacementValidationResult
├── IsValid
├── ReasonCode
├── LocalizedReasonArgs
├── ConflictingEntity/Cell?
├── Cost/Affordability
├── RouteValidity
└── PredictedRevision
```

Confirm повторно валидирует owner-side; UI preview не является авторитетным разрешением.

### 8.22 SelectionRuntimeState

```text
SelectionRuntimeState
├── SelectedEntityId?
├── SelectionType
├── HoveredEntityId?
├── InputDevice
└── Revision
```

Selection не меняет gameplay stats. При destroy выбранной сущности owner очищает selection.

## 9. Persistence contracts

### 9.1 SaveEnvelope

Каждый save-файл имеет envelope:

```text
SaveEnvelope<T>
├── SchemaVersion
├── GameVersion
├── ContentVersion
├── ProfileId
├── SaveId
├── CreatedAt/UpdatedAt
├── Payload: T
├── Checksum/Integrity data
└── MigrationHistory[]
```

Write должен быть атомарным: temp/replace либо platform equivalent. Corrupt/incompatible save не подменяется новым профилем/забегом без явного решения пользователя.

### 9.2 ProfileSaveDTO

```text
ProfileSaveDTO
├── ProfileId
├── MetaCurrency
├── UnlockedContentIds[]
├── PurchasedUpgradeLevels[]
├── CompletedObjectiveIds[]
├── ObjectiveProgressDTOs[]
├── AvailableDifficultyIds[]
├── SelectedStartingOptionIds[]
├── SelectedLoadoutIds[]
├── CosmeticSelections[]
├── HighestCompletedDifficultyId?
├── StatisticsDTO
├── LastSettledRunId / SettlementReceiptDTOs[]
└── MigrationFlags[]
```

Не содержит mutable ScriptableObject и current run objects.

### 9.3 RunSaveDTO — recommended between-wave boundary

```text
RunSaveDTO
├── RunId
├── Seed
├── ContentVersion
├── StartingRulesSnapshot/Referenced IDs
├── SavedGameState: Preparation
├── NextWaveIndex
├── RandomStateDTO
├── EconomySaveDTO
├── BaseSaveDTO
├── MapSaveDTO
├── TowerSaveDTOs[]
├── RunModifierSaveDTOs[]
├── RewardHistoryIds[]
├── RewardOfferSaveDTO?
├── PreparationFlags
└── LastAppliedPayout/Sequence receipts
```

### 9.4 EconomySaveDTO

```text
EconomySaveDTO
├── CurrentCurrency
├── PendingCompletionBonus
├── PassiveIncome state
├── AppliedEconomyModifierIds/stacks
└── LastLedgerSequence
```

Полный UI ledger сохранять необязательно; нужны значения для недублируемого восстановления.

### 9.5 BaseSaveDTO

```text
BaseSaveDTO
├── BaseDefinitionId
├── CurrentHealth
├── CurrentShield?
├── PersistentRunEffectDTOs[]
└── Repair modifiers
```

### 9.6 MapSaveDTO

```text
MapSaveDTO
├── MapInstanceId
├── LevelGenerationDefinitionId
├── LayoutRevision
├── TileSaveDTOs[]
├── ActiveSpawnPointIds/States[]
└── Map rule modifiers
```

`TileSaveDTO` хранит TileDefinitionId, grid position, rotation, persistent flags и occupied socket IDs. NavMesh/path graph пересчитываются.

### 9.7 TowerSaveDTO

```text
TowerSaveDTO
├── EntityId
├── TowerDefinitionId
├── TileInstanceId/Socket/GridPosition
├── Rotation
├── Grade
├── BranchIds[]
├── TargetPriority
├── PersistentRunModifierDTOs[]
└── Sell/Investment state if needed
```

Не хранит current target, cooldown между обычными волнами, derived stats или prefab reference.

### 9.8 RewardOfferSaveDTO

```text
RewardOfferSaveDTO
├── OfferId
├── SourcePoolId
├── ChoiceRewardIds[]
├── SelectedRewardId?
├── RerollsUsed
├── BanishIds[]
├── RandomStateBefore/After
└── IsResolved
```

### 9.9 Mid-wave SaveDTOs — Deferred

Если mid-wave save станет обязательным, дополнительно нужны:

- `WaveSaveDTO`: definition/instance IDs, spawn group/index, timers, isSpawning, payout flags;
- `EnemySaveDTO`: identity, definition, lane, HP/shield, path progress, effects, terminal flags;
- `ProjectileSaveDTO`: trajectory, payload, lifetime, hit set;
- `EffectSaveDTO`: definition, source/target, stacks, remaining/tick;
- `TowerCombatSaveDTO`: cooldown, rotation, target policy; target link только через EntityId;
- `AuraSaveDTO`: owner/definition; target set можно пересобрать, если contract это допускает.

Частичный mid-wave snapshot запрещён.

### 9.10 RunResultDTO

Immutable boundary run → meta:

```text
RunResultDTO
├── RunId
├── ResultVersion
├── Outcome
├── Seed
├── DifficultyId
├── WavesCompleted/FinalWaveIndex
├── ObjectiveResultDTOs[]
├── ChallengeIds[]
├── BaseSummaryDTO
├── EconomySummaryDTO
├── BuildSummaryDTO
├── Duration
└── ContentVersion
```

Не предоставляет UI права изменить ProfileSave.

### 9.11 SettingsSaveDTO

```text
SettingsSaveDTO
├── Audio volumes/mutes
├── Graphics quality/display
├── Input rebinding overrides
├── Camera sensitivity/speed
├── Accessibility options
├── Language/locale
└── SettingsSchemaVersion
```

PlayerPrefs допустим как storage для небольших settings, если один settings owner и version contract сохраняются. Полный ProfileSave/RunSave туда не раскладывается.

### 9.12 MigrationContext

```text
MigrationContext
├── FromSchemaVersion
├── ToSchemaVersion
├── Game/Content version
├── AvailableContentCatalog
├── AppliedMigrationIds[]
└── ValidationReport
```

Миграция работает до создания runtime state. DeprecatedReplacementId применяется только явной migration, не как runtime fallback.

## 10. Command contracts

Command — намерение изменить state. Owner повторно проверяет инварианты и возвращает `CommandResult`.

### 10.1 Общий CommandResult

```text
CommandResult
├── Success
├── ErrorCode
├── LocalizedErrorArgs
├── ChangedRevision?
├── TransactionId?
└── Result payload?
```

Отказ не мутирует state.

### 10.2 Application/Profile commands

| Command | Payload | Owner | Эффект |
| --- | --- | --- | --- |
| `LoadProfile` | ProfileId | Save/Profile owner | Загружает и мигрирует профиль |
| `SettleRun` | RunResultDTO | Profile/meta owner | Один раз применяет meta delta |
| `TryPurchaseUnlock` | UnlockId | Profile/meta owner | Атомарно списывает и открывает |
| `SelectStartingOption` | OptionId/slot | Profile/meta owner | Меняет profile selection |
| `SelectLoadout` | ContentIds[] | Profile/meta owner | Валидирует unlocked pool |
| `SelectDifficulty` | DifficultyId | Profile/meta owner | Меняет next-run selection |
| `StartNewRun` | StartingRules request | SceneFlow/application owner | Создаёт immutable StartingRules и грузит gameplay |
| `ContinueRun` | RunSaveId | SceneFlow/Save owner | Загружает snapshot до bootstrap |

### 10.3 Run/Preparation commands

| Command | Payload | Owner | Эффект |
| --- | --- | --- | --- |
| `SelectReward` | OfferId, RewardId | Reward flow owner | Применяет один reward и закрывает offer |
| `PreviewTilePlacement` | TileId, cell, rotation | Placement/validator | Только PlacementDraft/ValidationResult |
| `ConfirmTilePlacement` | Draft revision | TilePlacement owner | Транзакционно меняет map/currency |
| `PreviewTowerPlacement` | TowerId, cell/socket | TowerPlacement owner | Только preview/read model |
| `ConfirmTowerPlacement` | Draft revision | TowerPlacement + ResourceManager | Списывает, создаёт, регистрирует Tower |
| `UpgradeTower` | TowerEntityId, upgrade/branch | Tower + ResourceManager | Списывает, меняет grade/branch |
| `SetTargetPriority` | TowerEntityId, policy | Tower | Меняет следующее target selection |
| `SellTower` | TowerEntityId | TowerPlacement/economy owner | Возврат + unregister/destroy |
| `RelocateTower` | TowerEntityId, destination | Placement owner | Extended transaction |
| `RepairBase` | Amount/policy | PlayerBase + ResourceManager | Деньги → HP по правилу |
| `StartNextWave` | none/current preparation revision | `GameManager` | Единственный переход в WaveActive |
| `Pause/Unpause` | source | TimeControl/GameManager UI path | Меняет time state, не run progression |
| `AbandonRun` | confirmation | GameManager/application | Terminal result без скрытой награды |

### 10.4 Internal gameplay operations

`ApplyDamage`, `ApplyStatus`, `RegisterEnemy`, `NotifyDeath`, `NotifyLeak`, `GrantCurrency` являются owner-to-owner operations, а не UI commands. Они принимают typed payload и сохраняют idempotency.

## 11. Event contracts

Event сообщает о факте после изменения state. Existing C# events/UnityEvents у owner предпочтительнее глобального event bus.

### 11.1 Lifecycle/state events

| Event | Основной payload | Publisher |
| --- | --- | --- |
| `GameStateChanged` | old/new GameState | GameManager |
| `RunStarted` | RunId, StartingRules summary | GameplayBootstrap/GameManager |
| `RunEnded` | RunResultDTO | GameManager terminal owner |
| `PreparationReady` | next wave index, revision | GameManager/WaveManager chain |
| `WaveStarted` | WaveInstanceId, wave index | WaveManager |
| `WaveSpawnProgressChanged` | spawned/total | WaveManager |
| `WaveResolved` | WaveResultSnapshot | WaveManager/GameManager chain |
| `Victory` / `Defeat` | RunId, terminal reason | GameManager |
| `RunFinished` | immutable RunResult summary | GameManager |
| `RestartRequested` | current run terminal snapshot before scene reload | GameManager |

### 11.2 Economy/events

| Event | Payload | Publisher |
| --- | --- | --- |
| `CurrencyChanged` | before, delta, after, reason | ResourceManager |
| `CurrencyGained` | amount, source | ResourceManager |
| `CurrencySpent` | amount, transaction | ResourceManager |
| `RewardOfferCreated` | RewardOfferReadModel | Reward owner |
| `RewardSelected` | OfferId, RewardId, applied result | Reward owner |
| `MetaSettled` | MetaSettlementResult | Profile/meta owner |
| `UnlockPurchased` | UnlockId, balance delta | Profile/meta owner |

В Basic-реализации `WaveManager` публикует `RewardOfferCreated(OfferId)` при открытии единственного pending offer, а `GameplayTelemetry` сохраняет этот OfferId и before/after состояния до последующего `RewardSelected`. UI не применяет reward и не создаёт идентификатор самостоятельно.

`GameManager.RestartGame` публикует additive `RestartRequested` перед перезагрузкой текущей сцены; `GameplayTelemetry` записывает событие с состоянием завершённого run до очистки scene runtime.

### 11.3 Map/build events

| Event | Payload | Publisher |
| --- | --- | --- |
| `TilePlaced` | TileInstance snapshot, map revision | TileMap/Placement owner |
| `MapTopologyChanged` | old/new revision | TileMapManager |
| `NavMeshReady` | map revision, success | NavMesh owner |
| `TowerPlaced` | Tower identity/position/cost | TowerPlacement owner |
| `TowerUpgraded` | entity, old/new grade/branch, cost | Tower |
| `TowerSold` | entity, refund, freed cell | Placement/economy chain |
| `TargetPriorityChanged` | entity, old/new policy | Tower |

### 11.4 Combat events

| Event | Payload | Publisher |
| --- | --- | --- |
| `EnemySpawned` | EntityId, DefinitionId, lane | WaveManager/factory |
| `TargetChanged` | TowerId, old/new EnemyId | Tower |
| `WeaponFired` | AttackId, source, target, cue context | Tower/weapon |
| `DamageResolved` | DamageResult | Damage receiver/resolver |
| `ShieldChanged/Broken` | entity, before/after/result | Shield owner |
| `StatusApplied/Changed/Expired` | EffectInstance snapshot | Effect target owner |
| `AuraTargetEntered/Exited` | AuraId, target, effect handle | Aura owner |
| `EnemyDied` | EnemyId, killer/source, reward result | MonsterHealth |
| `EnemyLeaked` | EnemyId, base damage | MonsterMove/base chain |
| `BaseHealthChanged` | before/after/reason | PlayerBase |
| `BaseDestroyed` | BaseId | PlayerBase |

VFX/SFX могут слушать события, но event payload не должен зависеть от наличия presentation consumer.
## 12. ReadModel contracts

ReadModel строится owner-ом из актуального state. UI не хранит его поля как параллельный source of truth.

### 12.1 GameplayHudReadModel

```text
GameplayHudReadModel
├── GameState
├── IsPaused
├── CurrentCurrency
├── BaseHealth/Max/Percent
├── BaseShield?
├── CurrentWave/TotalWaves
├── WaveProgress
├── EnemiesAlive
├── CanStartWave
├── StartWaveBlockedReason?
├── CurrentChallenge labels
└── Active run modifiers summary
```

### 12.2 WaveIntelReadModel

UI-форма `WaveIntelSnapshot`:

```text
WaveIntelReadModel
├── Wave label/index
├── Enemy group rows
├── Role/trait icons and localized tooltips
├── Lane/spawn indicators
├── Known multipliers
├── Reward preview
└── Hidden information labels/reasons
```

### 12.3 WaveProgressReadModel

```text
WaveProgressReadModel
├── Spawned/Total
├── Alive
├── SpawnProgress
├── Combat/Resolve status
├── Time to next group?           // только если design показывает
└── Current group summary
```

### 12.4 TowerShopReadModel

```text
TowerShopReadModel
├── Entries[]
│   ├── TowerDefinitionId
│   ├── Icon/Name/Role/Tooltip
│   ├── Cost
│   ├── IsUnlocked
│   ├── IsAffordable
│   ├── IsAvailableByRunRules
│   └── DisabledReason
└── CurrentCurrency
```

### 12.5 PlacementReadModel

```text
PlacementReadModel
├── SelectedDefinition identity/presentation
├── Cost/Affordability
├── Draft cell/rotation
├── ValidationResult
├── Range/Coverage preview
├── Route preview/delta?
├── Confirm/Cancel availability
└── Input prompts
```

### 12.6 TowerPanelReadModel

```text
TowerPanelReadModel
├── EntityId
├── Tower Definition identity
├── Grade/MaxGrade
├── Current aggregated stats
├── Next upgrade old→new stats
├── Upgrade cost/availability/reason
├── Branch choices/conflicts
├── Current/Allowed target priorities
├── Active effects/auras
├── Sell value/availability
└── Range/Coverage presentation data
```

### 12.7 EnemyInspectReadModel

```text
EnemyInspectReadModel
├── EntityId
├── Enemy identity/icon/role/tooltip
├── Health/Shield/Armor
├── Move speed/path progress
├── Resistances/Immunities
├── Active effects/auras
├── Reward/Leak damage if visible
└── Threat traits
```

### 12.8 RewardOfferReadModel

```text
RewardOfferReadModel
├── OfferId
├── Choices[]: id/icon/name/tooltip/effect/rarity/stack state
├── SelectedId?
├── Reroll/Banish availability and costs
├── Required selection flag
└── Disabled reasons
```

### 12.9 WaveResultReadModel

```text
WaveResultReadModel
├── Wave index/outcome
├── Enemies killed/leaked
├── Base HP/shield delta
├── Kill income
├── Completion/passive income
├── Closing balance
├── Main threat/lane summary
└── Continue state
```

### 12.10 RunResultReadModel

```text
RunResultReadModel
├── Outcome
├── Waves completed
├── Difficulty/challenges
├── Duration
├── Base/economy/build summaries
├── Objectives
└── Navigation commands
```

Meta reward не рассчитывается UI; он приходит отдельным `MetaSettlementResult`.

### 12.11 MetaProgressionReadModel

```text
MetaProgressionReadModel
├── ProfileId
├── MetaCurrency
├── Settlement breakdown
├── Unlock entries[]
├── Objective entries[]
├── Available difficulties
├── Selected starting options/loadout
├── CanStartRun/BlockedReason
└── Save/error status
```

### 12.12 LoadoutReadModel

```text
LoadoutReadModel
├── Unlocked/Allowed tower entries
├── Selected slots
├── Starting option categories/selections
├── Difficulty/challenge choices
├── StartingRules summary preview
├── Conflicts/prerequisites
└── Start command availability
```

## 13. Presentation payloads

### 13.1 VfxCueRequest

```text
VfxCueRequest
├── CueId
├── SourceEntityId?
├── TargetEntityId?
├── Position/Rotation/Normal
├── Attach target?
├── Intensity/Scale
├── Color/DamageType context
├── Lifetime override?
└── Event/Attack sequence ID
```

### 13.2 SfxCueRequest

```text
SfxCueRequest
├── CueId
├── SourceEntityId?
├── Position
├── Spatial/2D override?
├── Intensity
├── Variation seed
├── Mixer snapshot/context
└── Event/Attack sequence ID
```

### 13.3 IconRenderRequest — Editor only

```text
IconRenderRequest
├── Source gameplay prefab/3D object
├── Camera preset
├── Lighting/background preset
├── Pose/rotation
├── Resolution
├── Output sprite path
└── Import settings
```

Icon generator должен сохранять связи и не создавать случайный несвязанный art asset.

### 13.4 InputPromptReadModel

```text
InputPromptReadModel
├── InputActionId
├── Current device/control scheme
├── Localized action label
├── Device icon/glyph
├── Rebindable
└── Availability context
```

Player action использует Input System action и поддерживает rebinding. Synthetic input остаётся debug/test surface, не отдельным gameplay command path.

## 14. Derived caches

Caches никогда не являются save/source of truth.

| Cache | Источник | Инвалидация | Пересоздаёт |
| --- | --- | --- | --- |
| Content index | Loaded Definitions/manifests | Catalog reload | Content owner |
| Aggregated tower/enemy stats | Definition + grade + modifier instances | Grade/effect/definition change | Stats owner |
| NavMesh | Confirmed map geometry | Map revision | NavMesh owner |
| Route graph/path lengths | Tile layout/spawn/base | Map revision | Level/path owner |
| Build occupancy index | Tile/tower placement states | Place/sell/relocate | TileMap/placement owner |
| Target spatial index | Active actors/transforms | Register/unregister/movement cadence | Targeting owner |
| Tower coverage preview | Tower stats + draft/map | Draft/stat/map revision | Placement/read model builder |
| Wave totals/intel summaries | Wave Definition + difficulty/challenges | Selected wave/rules | WaveManager/read model builder |
| Affordability flags | Prices + current currency | Currency/selection change | UI read model owner |
| Localization resolved text | Keys + locale | Locale/table change | Localization system |
| VFX/SFX pools | Cue definitions/prefabs | Scene/content release | Presentation owner |

Если cache не может быть пересоздан из source data, это не cache, а потерянный owner state.

## 15. Content catalog, Resources, Addressables и моды

### 15.1 Basic catalog

Текущий проект использует direct references, `Resources`, `TileDatabase`, WaveConfig/TowerStats assets и prefab links.

Basic `ContentCatalog` обязан:

- собрать Definition по типам;
- обеспечить lookup по ContentId;
- обнаружить duplicate/missing IDs;
- проверить обязательные prefab/localization/presentation refs;
- отдать immutable Definition;
- не делать leaf-level `Resources.Load` нормой.

### 15.2 Resources format

- путь является implementation detail catalog-а;
- save не хранит Resources path;
- переименование требует catalog update, но не save migration при стабильном ContentId;
- missing required resource блокирует content validation;
- `Resources.Load` из Tower/Enemy/UI не добавляется как fallback.

### 15.3 Addressables format — Extended

Catalog entry хранит typed AssetReference/address и ContentId.

```text
AddressableCatalogEntry
├── ContentId
├── DefinitionType
├── AssetReference
├── Labels[]
├── Dependency group
└── Preload/Release policy
```

Load handle принадлежит application/scene/run scope и имеет симметричный release. Save по-прежнему хранит ContentId.

### 15.4 Mod content pack — Deferred

```text
ModManifest
├── PackId/Version
├── Required game/API version
├── Dependencies/conflicts
├── LoadPriority
├── Definitions[]
├── Asset bundle/address catalog
├── Localization tables
├── License/author
└── Signature/checksum?
```

Merge rules:

- ContentId уникален;
- override разрешён только явной policy;
- type mismatch — ошибка;
- dependency cycle — ошибка;
- deterministic load order;
- save записывает pack/content versions;
- missing required mod не подменяется base content;
- cosmetic optional content может быть пропущен только если контракт явно помечает его optional.

## 16. Service I/O contracts

Полный owner/service graph, lifecycle и interaction sequences зафиксированы в `Assets/Documentation/GAMEPLAY_SERVICES_AND_INTERACTIONS.md`. Здесь остаются data I/O contracts этих границ.

Названия интерфейсов ниже описывают границы, а не требуют интерфейс на каждый concrete class.

### 16.1 Content owner

```text
LoadAndValidateContent(request) -> LoadedContentCatalog | BlockingError
Resolve<T>(ContentId) -> T Definition | NotFound/TypeMismatch
Release(scope)
```

Lifecycle: application; Addressables handles могут иметь scene/run subscopes.

### 16.2 SaveService

```text
LoadProfile(ProfileId) -> ProfileSaveDTO + MigrationReport
SaveProfile(ProfileSaveDTO) -> SaveResult
LoadRun(SaveId) -> RunSaveDTO + MigrationReport
SaveRun(RunSaveDTO) -> SaveResult
Delete/ArchiveRun(SaveId) -> explicit result
Load/SaveSettings(SettingsSaveDTO)
```

SaveService сериализует и пишет. Он не рассчитывает награду, цену, HP или unlock eligibility.

### 16.3 Profile/meta owner

```text
SettleRun(RunResultDTO) -> MetaSettlementResult
TryPurchaseUnlock(UnlockId) -> CommandResult
SelectStartingOption(OptionId/Slot) -> CommandResult
SelectLoadout(ContentIds[]) -> CommandResult
BuildStartingRules(request) -> StartingRules | ValidationError
BuildMetaReadModel() -> MetaProgressionReadModel
```

Lifecycle: profile/application. Один mutable owner.

### 16.4 GameplayBootstrap

```text
StartNewRun(StartingRules)
ContinueRun(RunSaveDTO)
```

Порядок: validate → create/restore state → map → NavMesh → gameplay objects → owners → Preparation. Partial success не переводит run дальше.

### 16.5 GameManager

```text
BeginBoot()
BeginMapBuild()
CompleteMapBuild()
StartNextWave()
NotifyWaveResolved(result)
NotifyBaseDestroyed(result)
BuildRunResult(outcome)
```

Владеет global GameState, terminal transition и single start-wave gate.

### 16.6 WaveManager

```text
Initialize(wave definitions, spawn anchors, rules)
BuildWaveIntel(index)
StartNextWave()                   // вызывается только GameManager
RegisterSpawnedEnemy(EntityId)
NotifyEnemyTerminal(EntityId, Kill/Leak)
ResolveWave()
BuildWaveRuntimeSnapshot/SaveDTO
```

### 16.7 ResourceManager

```text
CanAfford(cost)
TrySpend(cost, reason) -> CommandResult
Grant(amount, reason) -> CurrencyLedgerEntry
BuildEconomyReadModel/SaveDTO
```

### 16.8 TileMap/Placement owners

```text
Preview/Validate placement -> PlacementValidationResult
Confirm placement -> CommandResult + revision
Register/Unregister tower occupancy
BuildMapSaveDTO
RebuildDerivedCaches(revision)
```

### 16.9 Tower/Enemy/Damage owners

- Tower выбирает target, вызывает weapon, применяет upgrade/policy, строит snapshot/read model.
- Weapon доставляет DamagePacket.
- Enemy/Base resolver применяет shield/armor/HP/effects и публикует DamageResult.
- MonsterHealth/Move выполняют ровно один Kill или Leak terminal path.

### 16.10 Presentation owners

```text
Consume(domain event/result)
Map to CueId/payload
Play/PooledPlay
Release on lifecycle end
```

Не предоставляют gameplay result.

## 17. Data flow

### 17.1 Новая игра

```text
ProfileSave + unlocked Definitions + selections
  → profile owner validates
  → StartingRules snapshot
  → SceneFlow
  → GameplayBootstrap
  → RunRuntimeState in existing owners
  → ReadModels
  → UI
```

### 17.2 Одна волна

```text
WaveDefinition + Difficulty/Challenge + RunState
  → WaveIntelReadModel
  → Player Commands
  → Owners mutate map/towers/economy
  → GameManager.StartNextWave
  → WaveRuntimeState
  → DamagePacket/Result + Kill/Leak Events
  → WaveResult + updated RunState
```

### 17.3 Save/continue

```text
Owners build snapshots
  → RunSaveDTO
  → SaveEnvelope
  → SaveService file

file
  → SaveService + migration
  → RunSaveDTO
  → GameplayBootstrap
  → owners restore state
  → derived caches rebuild
```

DTO не становится живым state; данные копируются/применяются владельцам.

### 17.4 Meta settlement

```text
GameManager terminal state
  → immutable RunResultDTO
  → profile owner SettleRun
  → idempotency check
  → ProfileSave transaction
  → SaveService atomic write
  → MetaSettlementResult
  → UI
```

## 18. Source-of-truth matrix

| Значение | Source of truth | Не source of truth |
| --- | --- | --- |
| Tower base stats | TowerStatsSO/Definition | Tower UI, Save derived stats |
| Tower current grade/branch | Tower runtime owner | Definition, UI |
| Enemy current HP | MonsterHealth/runtime state | Health bar, Definition |
| Current currency | ResourceManager | HUD label, ledger cache, RunSave object after load |
| Base HP | PlayerBase | HUD, ProfileSave |
| GameState | GameManager | UI screens, WaveManager flags |
| Wave spawn cursor/alive | WaveManager | WaveUI |
| Tile layout/occupancy | TileMapManager | NavMesh, preview |
| Unlocks/meta currency | Profile/meta owner | PlayerPrefs after migration, UI |
| Content identity | Definition/catalog | Asset display name/path |
| Localization text | Localization tables/keys | Hardcoded UI string |
| VFX/SFX outcome | Domain result + cue Definition | Particle/audio callback |
| Save file | Snapshot of owner state | Live runtime owner after restore |

## 19. Gameplay object completeness contracts

Каждая конкретная Tower, Enemy, Weapon, Projectile, Tile, Reward и другая gameplay Definition считается полной только после заполнения всех применимых surfaces.

### 19.1 Общий checklist объекта

```text
ObjectCompleteness
├── Identity/ContentId
├── Logic owner and code path
├── Definition/settings source
├── Runtime state owner
├── Save boundary + restore path
├── Catalog/database membership
├── 3D gameplay prefab/representation
├── Icon rendered from 3D
├── Localized name
├── Localized artistic description
├── Localized gameplay tooltip
├── UI/read model
├── Input action + rebinding if player-controlled
├── SFX cues
├── VFX cues
├── Full lifecycle
└── Verification path
```

`N/A` допустимо только с явной причиной в object design/task. Пустая ссылка не равна `N/A`.

### 19.2 Tower completeness

- **Logic:** `Tower`, `TowerStats`, `IWeapon`, placement/upgrade owner.
- **Definition:** TowerStatsSO + prefab/catalog identity, weapon/projectile, upgrade/aura refs.
- **Runtime:** EntityId, placement, grade/branch, target policy, cooldown, effects.
- **Save:** TowerSaveDTO.
- **Catalog:** shop/loadout/allowed tower pools.
- **3D/Icon:** complete Tower prefab; icon rendered from it.
- **Localization:** name, artistic description, gameplay tooltip, role/defensive identity.
- **UI/Input:** shop, placement preview, tower panel, upgrade, target policy; all actions through Input System/rebindable commands.
- **SFX/VFX:** build, fire, impact via weapon, upgrade, sell/destroy, aura/status.
- **Lifecycle:** author → catalog → instantiate → initialize → register occupancy/targeting → active → upgrade/use → unregister → destroy/return → save/load restore.

### 19.3 Enemy completeness

- **Logic:** MonsterHealth, MonsterMove, stats, optional ability/aura.
- **Definition:** MonsterStatsSO + prefab/catalog identity, armor/shield/resistance/ability.
- **Runtime:** EnemyRuntimeState.
- **Save:** none between waves; EnemySaveDTO only mid-wave.
- **Catalog:** Wave spawn lookup/intel pool.
- **3D/Icon:** enemy prefab; icon rendered from 3D for intel/inspect.
- **Localization:** name, artistic description, gameplay tooltip/traits.
- **UI/Input:** intel and inspect only; direct player input N/A because enemy is not player-controlled.
- **SFX/VFX:** spawn, hit, shield/armor, status, death, leak.
- **Lifecycle:** spawn → init → register alive/targetable → move/effects → exactly one death or leak → unregister → pool/destroy.

### 19.4 Weapon completeness

- **Logic:** one IWeapon implementation and shared damage contract.
- **Definition:** serialized prefab fields or WeaponDefinition; DamageProfile; optional ProjectileDefinition.
- **Runtime:** owner Tower reference, cooldown delivered by Tower/weapon contract, attack sequence.
- **Save:** usually via Tower Definition/branch; combat cooldown only mid-wave.
- **Catalog:** only if independently selectable/reused/modded; otherwise N/A as embedded tower component.
- **3D/Icon:** weapon/barrel is part of Tower 3D; standalone icon N/A unless player selects weapon independently.
- **Localization/UI:** N/A when embedded; required name/tooltip/icon when selectable upgrade/weapon.
- **Input:** no direct input in auto-combat Basic.
- **SFX/VFX:** muzzle/beam/tracer/projectile/impact.
- **Lifecycle:** prefab authoring → injected/serialized owner → fire requests → cancel active beam/tasks → destroy/pool cleanup.

### 19.5 Projectile completeness

- **Logic:** Projectile + impact resolver/pool.
- **Definition:** prefab fields/ProjectileDefinition, collision/lifetime/lost target.
- **Runtime:** ProjectileRuntimeState + immutable DamagePacket.
- **Save:** N/A between waves; full Deferred mid-wave DTO.
- **Catalog:** N/A if embedded prefab reference; required if addressable/modded/shared.
- **3D/Icon/Localization/UI/Input:** 3D/VFX required; icon/text/UI/input N/A because projectile is technical non-player-selectable object.
- **SFX/VFX:** launch, trail, impact, dissipate.
- **Lifecycle:** rent/create → reset → launch → one impact/expire → clear refs/effects → return/destroy.

### 19.6 Tile completeness

- **Logic:** TileMapManager, TilePlacementSystem, Validator, RoadTileComponent.
- **Definition:** TileDefinition/RoadTileDef + prefab + connections/build sockets.
- **Runtime:** TileInstanceState/map occupancy.
- **Save:** TileSaveDTO in MapSaveDTO.
- **Catalog:** TileDatabase and reward/map pools.
- **3D/Icon:** tile prefab; icon/preview rendered from 3D.
- **Localization:** name, artistic description, gameplay tooltip explaining route/build effect.
- **UI/Input:** tile offer, rotate, preview, confirm/cancel; rebindable actions.
- **SFX/VFX:** preview optional, place/rotate/invalid/route rebuild feedback.
- **Lifecycle:** catalog → offer → draft → validate → confirm → instantiate/register → NavMesh rebuild → save/load → scene unload.

### 19.7 PlayerBase completeness

- **Logic:** PlayerBase → GameManager terminal signal.
- **Definition:** BaseDefinition or prefab fields.
- **Runtime:** BaseRuntimeState.
- **Save:** BaseSaveDTO.
- **Catalog:** required only for multiple selectable base types; otherwise scene/run rules direct reference.
- **3D/Icon:** base 3D; HUD icon rendered/derived from it when shown.
- **Localization:** name/description/tooltip if inspectable/selectable; otherwise explicit N/A for hidden description.
- **UI/Input:** HP/shield HUD; repair/inspect command if designed.
- **SFX/VFX:** damage, shield, repair, destroyed.
- **Lifecycle:** bootstrap place/init → receive leaks/effects → repair → terminal once → save/load → scene unload.

### 19.8 Wave completeness

- **Logic:** WaveManager + GameManager transition.
- **Definition:** WaveConfig/WaveDefinition + spawn groups.
- **Runtime:** WaveRuntimeState.
- **Save:** index between waves; full WaveSaveDTO only mid-wave.
- **Catalog:** ordered run rules wave list.
- **3D:** N/A because wave is orchestration, not world object.
- **Icon:** optional wave/challenge icon; enemy icons required in intel.
- **Localization:** wave label/special rules/threat tooltips.
- **UI/Input:** intel, progress, StartWave; action rebindable.
- **SFX/VFX:** warning/start/complete/defeat cues.
- **Lifecycle:** resolve Definition → intel → commit → spawn/register → resolve/payout once → dispose state.

### 19.9 Reward completeness

- **Logic:** reward flow owner routes effect to actual domain owner.
- **Definition:** RewardDefinition + pool.
- **Runtime:** RewardOfferState, RewardHistory, optional RunModifierInstance.
- **Save:** offer/history/modifier DTOs.
- **Catalog:** reward pool/meta unlock pool.
- **3D:** N/A unless reward is a physical world pickup; reason: abstract inter-wave choice.
- **Icon:** required UI icon; if tied to Tower/object, derive from that 3D source.
- **Localization:** name, artistic description, exact gameplay tooltip.
- **UI/Input:** offer/select/reroll/banish with rebindable navigation.
- **SFX/VFX:** offer reveal, select, apply/error.
- **Lifecycle:** eligible pool → seeded offer → save exact choices → select once → apply owner-side → history → expire/end run.

### 19.10 Damage type/Status/Aura completeness

- **Logic:** receiver/damage/effect/aura owner.
- **Definition:** typed Definition with stack/resistance rules.
- **Runtime:** DamagePacket/Result, EffectInstance, AuraRuntimeState.
- **Save:** run-persistent modifiers; combat effects only mid-wave.
- **Catalog:** required for localized/modded IDs; enum sufficient for tiny closed Basic set.
- **3D:** N/A as abstract rule; visible VFX is mandatory representation.
- **Icon/Localization:** required when shown in intel/inspect/upgrade UI.
- **Input:** N/A unless selectable ability/upgrade.
- **SFX/VFX:** apply/hit/tick/break/expire/area indication.
- **Lifecycle:** resolve Definition → apply → stack/refresh → tick/affect → remove by expiry/source/owner → cleanup/save policy.

### 19.11 Currency completeness

- **Logic:** ResourceManager for run currency; profile owner for meta currency.
- **Definition:** economy/meta presentation and balance rules.
- **Runtime:** one balance per context.
- **Save:** EconomySaveDTO or ProfileSaveDTO.
- **Catalog/3D:** N/A unless physical pickups exist.
- **Icon/Localization:** required in HUD/result/meta UI.
- **Input:** purchase commands only.
- **SFX/VFX:** gain/spend/insufficient/large reward.
- **Lifecycle:** initialize/load → atomic grant/spend → event/read model → snapshot → reset/end scope.

### 19.12 Meta unlock/objective completeness

- **Logic:** one profile/meta owner.
- **Definition:** MetaUnlockDefinition/ObjectiveDefinition.
- **Runtime:** ProfileRuntimeState/progress/settlement.
- **Save:** ProfileSaveDTO + receipts.
- **Catalog:** meta progression catalogs.
- **3D:** N/A for abstract unlock; linked gameplay content retains its own 3D.
- **Icon/Localization:** required.
- **UI/Input:** progression/purchase/loadout, rebindable navigation.
- **SFX/VFX:** reveal, purchase, completion, error.
- **Lifecycle:** load profile → evaluate result → unlock/purchase once → save → include in StartingRules → never mutate active run retroactively.

### 19.13 Technical pool/registry completeness

- **Logic:** pool/registry owner.
- **Definition:** capacity/prefab refs if authored.
- **Runtime:** rented/inactive instances, registrations.
- **Save:** N/A; reconstructed.
- **3D/Icon/Localization/UI/Input/SFX/VFX:** N/A because technical infrastructure.
- **Lifecycle:** create/prewarm → rent/register → reset/use → unregister/return → release. Pool never owns reward, HP, wave completion or targeting decision.

## 20. Lifecycle state diagrams

### 20.1 Tower

```text
Definition/Catalog
  → PlacementDraft
  → Validate
  → TrySpend
  → Instantiate
  → Initialize Definition + EntityId + placement
  → Register occupancy/targeting
  → Active: target/fire/effects/upgrade
  → Save snapshot when requested
  → Sell/Destroy/Scene end
  → Unregister/cancel/clear effects
  → Destroy or pool return
```

Load enters at `Instantiate → Initialize from TowerSaveDTO → Register`.

### 20.2 Enemy

```text
Wave spawn entry
  → Factory/Pool rent
  → Initialize Definition + wave multipliers + EntityId
  → Register alive/targetable
  → Move/receive damage/effects
  → Death XOR Leak
  → Reward/base damage + WaveManager notification exactly once
  → Unregister/cancel/clear
  → Pool return/destroy
```

### 20.3 Projectile

```text
Weapon Fire + DamagePacket
  → Rent/create/reset
  → Launch
  → Fly/track/collide
  → Impact XOR Expire/LostTarget policy
  → Resolve packet at most once
  → Play cues
  → Clear references/hit set
  → Return/destroy
```

### 20.4 Tile

```text
Definition/Catalog
  → Offer
  → Draft/Rotate/Preview
  → Validate
  → Confirm/Spend
  → Add TileInstanceState
  → Instantiate/Register
  → Rebuild map caches/NavMesh
  → Persist in RunSave
  → Restore from layout
  → Run/scene end cleanup
```

### 20.5 Reward

```text
Pool + eligibility + seeded RNG
  → OfferState
  → Save exact offer if needed
  → Player selects
  → Validate OfferId/choice/unresolved
  → Apply through domain owner
  → Mark resolved/history
  → Event/UI feedback
  → Expire by scope
```

### 20.6 Profile/meta

```text
Load envelope
  → Migrate/validate
  → ProfileRuntimeState
  → Receive terminal RunResult
  → Idempotent settlement transaction
  → Atomic save
  → ReadModel/UI purchase/selection commands
  → StartingRules snapshot
  → New run
  → Application/profile close save
```

## 21. Basic, Extended, Deferred data scope

### Basic — required first

- stable ContentId for player-facing/saved Definitions;
- existing TowerStatsSO, MonsterStatsSO, WaveConfig, TileDatabase mappings;
- StartingRules;
- run state snapshots across current owners;
- between-wave RunSaveDTO;
- ProfileSaveDTO replacing meta PlayerPrefs placeholder through migration;
- Tower/Enemy/Tile/Base/Reward completeness surfaces;
- DamagePacket/Result even with small damage type enum;
- Commands/results for placement, spending, upgrade, start wave;
- owner events/read models;
- localization/icon/SFX/VFX contracts;
- content validation and no fallback.

### Extended

- branch IDs and saveable augments;
- shields, armor, status and aura Definitions/instances;
- multiple lanes;
- reward reroll/banish;
- production/repair economy;
- objectives/challenges;
- Addressables catalog;
- several profiles/slots;
- richer telemetry/read models.

### Deferred

- mod manifests/pack merge;
- mid-wave save DTOs;
- cloud conflict/settlement ledger;
- ECS/native actor data;
- live content migrations;
- multiple currencies;
- seasons/dailies;
- network/online account data.

Deferred types не должны заранее усложнять Basic concrete implementation.

## 22. Validation rules

### 22.1 Definition validation

Для каждой Definition:

- ContentId непустой и уникальный;
- обязательные refs не null;
- числовые ranges валидны;
- localization keys существуют во всех обязательных locales;
- gameplay prefab содержит требуемые components;
- icon существует и соответствует 3D source;
- SFX/VFX refs заполнены либо явно N/A;
- catalog membership корректна;
- linked IDs существуют и имеют ожидаемый type;
- upgrade/reward dependency graph не содержит cycle/conflict;
- Definition не содержит runtime mutable state.

### 22.2 Runtime invariants

- один mutable owner на значение;
- one command entry point per action;
- one wave active;
- one Kill XOR Leak per enemy;
- payout/reward/settlement idempotent;
- currency не отрицательна;
- preview не мутирует state;
- events публикуются после изменения state;
- pooled object полностью reset;
- async имеет owner cancellation token;
- component topology authored до Play Mode.

### 22.3 Save validation

- schema/content versions присутствуют;
- IDs разрешаются в catalog;
- DTO не содержит UnityEngine.Object/runtime references;
- offer/random state не reroll при load;
- applied receipts не повторяются;
- derived caches отсутствуют;
- load строит новый runtime state, а не использует DTO как mutable owner;
- incompatible/missing required content возвращает blocking error без fallback.

### 22.4 ReadModel/UI validation

- UI показывает owner state после команды;
- disabled reason соответствует owner validation;
- hardcoded player-facing text отсутствует;
- Input Action rebindable;
- icon/tooltip/old→new stats корректны;
- UI не рассчитывает damage, reward, price или unlock eligibility самостоятельно.

## 23. Telemetry data — optional Extended

Telemetry не является gameplay owner и не блокирует Basic.

```text
TelemetryEvent
├── EventName/SchemaVersion
├── AnonymousSession/Profile hash
├── RunId/WaveInstanceId
├── Timestamp/SimulationTick
├── ContentVersion
├── Difficulty/Challenge IDs
├── Typed payload
└── Consent/Privacy context
```

Полезные events:

- run start/end;
- wave start/resolve;
- reward offer/selection;
- tile/tower placement;
- upgrade branch;
- currency source/sink;
- leak/base destruction;
- meta settlement/purchase;
- validation/content error.

Telemetry получает copies/events, не ссылки на mutable owners. Для диагностики боя каждый элемент `GameplayTelemetrySnapshot.Towers` дополнительно сохраняет мировую позицию башни и расстояние до базы; эти поля являются additive read-only telemetry и не входят в ML observation/action contract.

## 24. Future task template

Каждая будущая задача, добавляющая/меняющая gameplay object или data type, должна заполнить:

```text
Task data contract
1. Object/domain:
2. Existing logic owner/path:
3. Existing Definition/assets:
4. Stable ContentId:
5. Authoring fields changed:
6. Runtime state changed:
7. Save/Profile/RunResult impact:
8. Commands/results:
9. Events:
10. ReadModels/UI/input/rebinding:
11. Catalog/database membership:
12. 3D prefab and generated icon:
13. Localization name/description/tooltip:
14. SFX/VFX:
15. Lifecycle create/register/use/unregister/destroy:
16. Derived cache invalidation:
17. Migration/backward compatibility:
18. Explicit N/A surfaces with reasons:
19. No-fallback failure behavior:
20. Verification: asset links, tests, Unity compile/Console/Play Mode:
```

Если пункт неприменим, задача записывает причину. Если owner неизвестен, задача сначала становится read-only owner audit, а не создаёт новый manager.

## 25. Definition of done для data-задачи

Data-задача завершена только если:

1. logical context выбран правильно: Definition, RuntimeState, SaveDTO, Command, Event, ReadModel или Cache;
2. существующий owner расширен без mirror state;
3. stable IDs и catalog membership определены;
4. authoring и runtime значения не смешаны;
5. save boundary и migration описаны;
6. UI получает read model и отправляет command;
7. localization/3D/icon/SFX/VFX surfaces заполнены или явно N/A;
8. lifecycle симметричен: create/register ↔ unregister/destroy, load ↔ release, subscribe ↔ unsubscribe;
9. required content failure блокирует операцию с явной ошибкой;
10. текстовые файлы проверены по encoding/EOL;
11. C# при наличии валидирован в Unity, Console чиста от task errors;
12. runtime change доказан bounded Play Mode сценарием, а не только компиляцией.
