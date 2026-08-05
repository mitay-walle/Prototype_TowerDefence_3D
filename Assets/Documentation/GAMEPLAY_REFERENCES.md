---
title: Геймплейные референсы Prototype Tower Defence 3D
type: геймплейный референс и база будущего плана задач
status: active
updated: 2026-08-05
scope: Rogue Tower, Tower Dominion, Citadelic
---

## Правило реализации

- Никакого fallback: если основной путь недоступен или не сработал, остановиться и сообщить о блокере; обходной или запасной путь использовать только по явному запросу.

# Геймплейные референсы: Rogue Tower, Tower Dominion, Citadelic

## 1. Назначение

Документ фиксирует направление следующих геймплейных проходов Prototype Tower Defence 3D.

Связанные контракты проекта:

- `Assets/Documentation/GameplayGreenfield/00_INDEX.md` — авторитетная greenfield-спецификация, спроектированная независимо от готового кода;
- `Assets/Documentation/CORE_LOOP.md` — одна волна;
- `Assets/Documentation/RUN_LOOP.md` — один забег;
- `Assets/Documentation/META_LOOP.md` — прогрессия между забегами;
- `Assets/Documentation/GAMEPLAY_SCENE_OBJECTS.md` — объекты сцены и владельцы;
- `Assets/Documentation/UNITY_DATA_AND_SERVICE_LIFECYCLES.md` — Unity-формы данных и сервисов;
- `Assets/Documentation/GAMEPLAY_DATA_CONTRACTS.md` — master-контракт Definition, Runtime, Save, Command, Event, ReadModel и object completeness;
- `Assets/Documentation/GAMEPLAY_SERVICES_AND_INTERACTIONS.md` — необходимые services/owners, их команды, события, lifecycle и последовательности взаимодействия;
- `Assets/Documentation/GAMEPLAY_MONOBEHAVIOUR_COMPONENTS.md` — component recipes сцены и prefabs, data/lifecycle boundaries, road-contact Towers, auto-repair и Flying enemies.

Референсы используются для механик и темпа, а не для копирования названий, визуального стиля, текста, числового баланса или контента. Этот документ задаёт design intent для будущих задач. В greenfield-пакете готовый код, текущая сцена, prefabs и сериализованные ассеты не являются источником архитектуры; сопоставление с реализацией выполняется отдельно в будущей implementation-задаче.

Главный вывод:

> Карта должна стать частью билда игрока. Волна должна проверять оборону, которую игрок осознанно сформировал, а не только набор башен на статичном маршруте.

## 2. Референсы

### 2.1 Rogue Tower

Источники:

- Steam: https://store.steampowered.com/app/1843760/Rogue_Tower/
- Community wiki, upgrade cards: https://rogue-tower.fandom.com/wiki/Upgrade_Cards
- Community wiki, map features: https://rogue-tower.fandom.com/wiki/Map_features
- Community wiki, monsters: https://rogue-tower.fandom.com/wiki/Monsters
- Community wiki, upgrades: https://rogue-tower.fandom.com/wiki/Upgrades

Подтвержденные механики:

- Путь постоянно расширяется, а игрок выбирает направление расширения.
- Путь может изгибаться, разделяться и объединяться, поэтому игрок влияет на длину маршрута и концентрацию врагов.
- Место башни важно из-за покрытия и бонуса от высоты.
- Внутри забега игрок сочетает трату денег с выбором случайных upgrade cards.
- Постоянные unlocks и upgrades сохраняются между забегами.
- Типы врагов имеют разные способности и усиливают друг друга в группах, поэтому оборону нужно адаптировать к угрозе.

Выводы для нашего проекта:

- Фаза тайлов должна давать тактическое последствие: длину пути, объединение, разделение, choke point или выгодную позицию для огня.
- Игрок иногда должен выбирать между немедленной башней/улучшением и сохранением сильной позиции для будущей угрозы.
- Геометрия пути и высота должны объяснять, почему одна клетка лучше другой.
- Состав врагов должен менять выбор башни; простого увеличения количества врагов недостаточно.
- Сотни карт не нужны для первого прохода. Нужен небольшой и понятный offer, который заставляет выбрать один вариант.

### 2.2 Tower Dominion

Источники:

- Официальный сайт: https://www.towerdominion.com/
- Community wiki: https://towerdominion.wiki.gg/
- Wiki, doctrines: https://towerdominion.wiki.gg/wiki/Doctrines
- Wiki, buildings: https://towerdominion.wiki.gg/wiki/Buildings
- Wiki, currencies: https://towerdominion.wiki.gg/wiki/Currencies

Подтвержденные механики:

- Забег начинается с выбора faction, commander и difficulty.
- Волна состоит из reinforcement, расширения карты на определенных волнах, размещения/улучшения зданий и запуска следующей атаки.
- Башни и здания выдаются ограниченным выбором, поэтому игрок выбирает, что войдет в текущий забег.
- Terrain tiles можно размещать и менять, создавая choke points и влияя на pathing.
- Высота и маршрут являются отдельными стратегическими инструментами.
- Factions, heroes, doctrines, buildings и upgrades формируют разные identities забега.
- Objectives и unlocks расширяют набор доступных вариантов в будущих забегах.

Выводы для нашего проекта:

- Между волнами нужна полноценная последовательность решений, а не автоматическая награда перед немедленным стартом.
- Ограниченный offer интереснее бесконечного магазина, когда игрок формирует identity текущего забега.
- Тайлы нужно оценивать вместе с дальностью башен, маршрутом врагов и следующей угрозой.
- Асимметрия faction/commander - поздний слой. Сначала достаточно доказать ее несколькими ролями башен, врагов или стартовыми modifiers.
- В проекте уже есть владелец тайлов; нужно усилить его, а не создавать новый map manager.

### 2.3 Citadelic

Источники:

- Steam и список особенностей: https://store.steampowered.com/app/2248390/Citadelic/
- Gameplay video: https://www.youtube.com/watch?v=LCdjSSLERBY

Подтвержденные механики:

- В начале каждой волны игрок расширяет и строит базу.
- Intel о входящих врагах помогает выбрать оборону, использующую их слабости.
- В конце волны предлагается пять случайных rewards: augments, temporary effects и resource shipments.
- Ore получается от врагов и mines, а тратится на здания, repair и upgrades.
- Tech production buildings создают отдельный progression resource и сами нуждаются в защите.
- Структуры можно улучшать или преобразовывать в другие типы.
- Augments устанавливаются на структуры, а skills помогают обороне во время опасной атаки.
- Короткие забеги дают experience, новые base types, optional modifiers, harder difficulties, statistics и medals.

Выводы для нашего проекта:

- Игроку нужно показать, что придет в следующей волне, до фиксации трат.
- Экономика должна создавать видимый выбор между немедленной обороной и будущим доходом/гибкостью.
- Для изменения забега не нужен полный deckbuilder; на первом проходе достаточно трех вариантов.
- Production, repair и upgrade полезны только тогда, когда волна наказывает за их игнорирование.
- Ручные skills не должны блокировать первый milestone: ядро проекта - автоматическая атака башен и сильная подготовка.

## 3. Общие принципы

1. Подготовка между волнами является настоящей фазой решений.
2. Игрок формирует и оборону, и поле боя.
3. Информация о врагах приходит достаточно рано для counterplay.
4. Rewards формируют забег, а не только добавляют плоскую валюту.
5. Башни, upgrades, terrain и economy должны взаимодействовать.
6. У игры есть короткий loop забега и отдельный long-term unlock loop.
7. Randomness должна создавать адаптацию, а не лишать игрока осмысленного выбора.
8. Каждая новая механика требует обратной связи: preview маршрута, intel врагов, объяснение reward или понятное изменение stats.

## 4. Текущая цепочка владельцев

Будущий план расширяет именно этих владельцев:

| Игровая область | Текущий владелец |
|---|---|
| Bootstrap и инициализация | Assets/Scripts/GameLoop/GameplayBootstrap.cs |
| Состояния, pause, victory, defeat | Assets/Scripts/GameLoop/GameManager.cs |
| Spawn волн, scaling, completion, tile phase | Assets/Scripts/GameLoop/WaveManager.cs |
| Валюта забега и passive income | Assets/Scripts/GameLoop/ResourceManager.cs |
| Начальная генерация карты | Assets/Scripts/Levels/LevelGenerator.cs и Assets/Scripts/Levels/MapGenerator.cs |
| Состояние и размещение тайлов | Assets/Scripts/Levels/TileMapManager.cs, TilePlacementSystem.cs, TilePlacementValidator.cs |
| Доступные tile prefabs | Assets/Scripts/Levels/TileDatabase.cs |
| Targeting, attack, weapon и upgrade башни | Assets/Scripts/Towers/Tower.cs, TowerStatsSO.cs, Assets/Scripts/Weapons/ |
| Покупка и размещение башен | Assets/Scripts/Towers/TowerPlacementSystem.cs, Assets/Scripts/UI/TowerShopUI.cs |
| Health и movement врагов | Assets/Scripts/Monsters/MonsterHealth.cs, MonsterMove.cs |
| Обратная связь игроку | Assets/Scripts/UI/GameHUD.cs, Assets/Scripts/UI/WaveUI.cs |

Не добавлять второй WaveManager, MapManager, EconomyManager или параллельное состояние башен.

## 5. Целевой loop проекта

Первый reference-informed loop:

1. Сгенерировать связанную карту, базу и spawn points.
2. Показать состав следующей волны и главные threat traits.
3. Войти в preparation:
   - выбрать один обязательный стартовый run modifier до первой волны;
   - между волнами выбрать limited reward;
   - разместить или улучшить башни;
   - выбрать и разместить valid terrain tile, если фаза доступна;
   - увидеть новый маршрут и firing coverage.
4. Запустить волну.
5. Башни автоматически выбирают цели и атакуют; враги идут по маршруту и наносят урон базе.
6. Выдать kill и wave rewards.
7. Переоценить следующую угрозу и повторить loop.
8. При victory или defeat записать результат забега. Persistent unlocks остаются поздним milestone, а стартовый run modifier выбирается один раз и не повторяется после волн.

На первом проходе combat остается real-time и автоматическим. Глубина строится на preparation, placement, path shaping, threat information и limited rewards.

## 6. Gap assessment

| Паттерн референсов | Факты текущего проекта | Оценка | Направление |
|---|---|---|---|
| Preparation и active attack разделены | GameManager имеет WavePreparing/WaveActive; reward offer, next-wave intel и placement decisions идут через существующих владельцев | Есть, bounded | Дальше проверять читаемость выбора на fresh run |
| Карта является частью билда | LevelGenerator, TileMapManager и TilePlacementSystem дают несколько валидных вариантов; topology/seed/entry telemetry additive | Есть, bounded | Расширять preview последствий только при подтверждённом UX gap |
| Path shaping влияет на стратегию | TilePlacementValidator проверяет граф и стыковку; tile choice меняет topology/spawn anchors без второго pathfinding owner | Есть, bounded | Сохранять route/coverage readback в telemetry |
| Limited run rewards | WaveManager владеет mutually exclusive reward offer, открытие/выбор имеют OfferId telemetry, выбор применяется один раз и покрыт контрактами | Есть, bounded | Проверять баланс offer на полном fresh run |
| Enemy counterplay | WaveConfig/WaveUI показывают scaled composition и localized role/defensive identity; MonsterMove имеет четыре archetype behaviors | Есть | Балансировать counters по фактическим run traces |
| Tower identities и synergies | Три authored tower roles имеют разные weapons, upgrade paths и targeting situations | Есть | Оставить runtime owner `Tower` и добавлять content точечно |
| Economy tension | Starting bank, distinct tower costs, upgrades и reward reserve образуют documented mutually exclusive choices | Есть, первый tuning pass | Проверять решения на полном fresh run |
| Persistent roguelite progression | WaveManager умеет in-run loop и difficulty scaling | Deferred | Начать только после стабильных terminal/restart runs и выбора save owner |
| Active battle skills | В текущей цепочке нет обязательного владельца | Deferred | Не блокировать текущий milestone ручными abilities |
| Analytics и challenge modifiers | GameplayTelemetry, topology/generated-wave metrics, map-choice events и обязательный стартовый challenge modifier имеют additive contracts/tests | Есть, bounded | Балансировать профили по runtime traces |

## 7. Будущий план задач

### TD3D-R0 - Зафиксировать gameplay contract

- Task: определить один wave-to-wave loop, vocabulary rewards, первые tower roles и enemy roles.
- Docs: этот документ, разделы 3, 5, 6.
- Inspect: текущие WaveConfig assets, tower stats/assets, tile prefabs и references в Gameplay.unity.
- Owner agent: Gameplay Designer.
- Boundary: только документация и content inventory; не создавать managers и не переписывать runtime.
- Acceptance: таблица описывает выбор игрока перед первыми пятью волнами и наблюдаемое последствие каждого выбора.

### TD3D-R1 - Сделать inter-wave phase limited reward offer

- Task: после волны предложить небольшой mutually exclusive offer, влияющий на текущий забег.
- Docs: reward choices Citadelic, card choice Rogue Tower, разделы 2 и 5 этого документа.
- Inspect: WaveManager.OnWaveCompleted, GameManager state changes, ResourceManager, GameHUD, tower/stat assets.
- Owner agent: Gameplay Systems Programmer.
- Executor skill: unity-mcp-orchestrator для serialized UI/asset wiring.
- Boundary: расширить текущую wave/preparation chain, оставить одного reward owner и переиспользовать существующих currency/tower/stat owners; не строить general deckbuilder.
- Acceptance: completed wave оставляет игру в preparation, offer понятен, применяется ровно один reward, следующая wave использует измененное value или availability; bounded smoke подтверждает inter-wave owner chain, а отдельный изолированный Play Mode smoke еще должен подтвердить Victory/Defeat.

### TD3D-R2 - Превратить tile placement в tactical map choice

- Task: заменить один opaque random tile на несколько valid choices с preview последствий.
- Docs: path expansion Rogue Tower, terrain shaping Tower Dominion, разделы 2 и 5 этого документа.
- Inspect: WaveManager.TilePlacementPhase, TileDatabase, TileMapManager, TilePlacementSystem, TilePlacementValidator, владелец NavMesh rebuild.
- Owner agent: Gameplay Systems Programmer.
- Executor skill: unity-mcp-orchestrator.
- Boundary: сохранить connection validation, grid coordinates, tile prefabs и владение TileMapManager; не добавлять второй pathfinding или map state.
- Acceptance: при наличии вариантов игрок выбирает минимум из трех valid options/rotations, видит affected route/firing area, placement меняет маршрут следующей волны и не ломает NavMesh/spawn initialization.

### TD3D-R3 - Добавить next-wave intel и enemy roles

- Task: показать composition будущей волны и дать каждой первой enemy role ясное defensive implication.
- Docs: enemy interactions Rogue Tower, enemy intel Citadelic, разделы 2, 3 и 6.
- Inspect: WaveConfig, EnemySpawnData, MonsterHealth, MonsterMove, enemy prefabs, GameHUD, localization/UI bindings.
- Owner agent: Gameplay Designer plus Gameplay Systems Programmer.
- Executor skill: mcp-unity-validate-script при изменении scripts.
- Boundary: использовать те же wave data, из которых выполняется spawn; начать с малого набора roles; не создавать отдельную enemy database и duplicate stats.
- Acceptance: до старта wave игрок понимает enemy roles и может выбрать counter; smoke run показывает минимум два разных preparation decisions для разных compositions.

### TD3D-R4 - Авторить tower roles и upgrade interactions

- Task: небольшой набор башен должен создавать разные placement и upgrade decisions.
- Docs: elevation/coverage trade-off Rogue Tower, building identity Tower Dominion, structure upgrades/augments Citadelic.
- Inspect: Tower, TowerStatsSO, IWeapon implementations, TowerPlacementSystem, TowerShopUI, tower prefabs и UpgradeRule assets.
- Owner agent: Gameplay Designer plus Content/Unity Editor author.
- Executor skill: prefab-creation, если существующие prefab patterns покрывают нужные assets.
- Boundary: предпочитать существующие Tower/TowerStatsSO/IWeapon и reusable ScriptableObjects; не добавлять parallel upgrade runtime и не переименовывать owners.
- Acceptance: минимум три tower roles имеют разные best-use situations, upgrades видны в UI, а placement у meaningful route/elevation дает measurable trade-off.

### TD3D-R5 - Доказать economy tension до production buildings

- Task: настроить текущую currency loop так, чтобы игрок выбирал между новой башней, upgrade и сохранением на следующую threat.
- Docs: ore/mine versus defense Citadelic, limited building rewards Tower Dominion.
- Inspect: ResourceManager, tower/upgrade costs, kill/wave rewards, current wave configs.
- Owner agent: Gameplay Designer plus Gameplay Systems Programmer.
- Boundary: первый проход меняет только числа и существующих owners; production-building system - отдельное позднее решение.
- Acceptance: в bounded run минимум две покупки нельзя сделать одновременно, обе остаются defensible в зависимости от next-wave intel; tuning и test evidence записаны.

### TD3D-R6 - Добавить persistent progression после стабилизации run loop

- Task: небольшой meta layer: unlocks, один-два starting modifiers и run result.
- Docs: permanent upgrades Rogue Tower, objectives/unlocks Tower Dominion, experience/base types/modifiers Citadelic.
- Inspect: existing save-related assets/code, GameManager victory/defeat events, WaveManager loop state, serialization conventions.
- Owner agent: Gameplay Systems Programmer.
- Executor skill: test-writing для save/load и run-result coverage.
- Boundary: сначала выбрать одного persistence owner; не добавлять meta-progression, пока in-run reward и map choices нестабильны.
- Acceptance: completed run открывает одну documented option, новый run может ее использовать, restart не переносит temporary rewards.

### TD3D-R7 - Balance и verification pass

- Task: проверить, что map, rewards, enemy intel, tower roles и economy образуют один loop.
- Docs: этот документ и acceptance criteria всех задач.
- Inspect: Gameplay.unity, serialized prefabs/assets, Unity Console, tests и bounded Play Mode run.
- Owner agent: Gameplay Tester.
- Executor skill: test-writing.
- Boundary: исправлять минимальный owner-side issue; не расширять pass на visual/package refactoring.
- Acceptance: fresh run проходит несколько waves без Console errors, один обязательный стартовый modifier фиксируется до Wave 1, до каждой следующей wave есть documented reward/map/build decision, map expansion остается valid; inter-wave/restart owner chain подтверждены bounded smoke, а isolated terminal smoke подтвердил Defeat и final-wave Victory. Natural balance run с authored HP базы и реальным defensive build остается следующим balance check.
- R61 follow-up reduced only `Wave_03.countScaling` from `1.20` to `1.00` and added a progression contract. Controlled Play read back `TotalEnemiesInWave=41`, but the ten-tower probe still ended in final-wave Defeat; natural balance and player-counter viability remain open.
- R62 adds the missing placement readback on the existing owner: `TowerPlacementSystem` evaluates committed towers plus the candidate as one union for spawn-anchor and NavMesh-route coverage, while `GameplayTelemetry` records `existing`, `candidate`, `combined`, and `coverageMode=combined`. Focused placement and full EditMode contracts pass; fresh Play evidence shows `existing=1;candidate=1;combined=2` before the third tower commit. This improves decision visibility but does not close natural final-wave Victory.
- R63 fixes the placement ghost lifecycle: coverage snapshots now include only active/enabled towers, CancelPlacement clears the ghost reference immediately, and the log records excludedTowers. The post-fix Play trace shows existingTowers=0 -> 1 -> 2 across committed towers. Full EditMode remains green at 61/61; natural final-wave Victory is still open.

## 8. Порядок зависимостей

TD3D-R0
  -> TD3D-R1 + TD3D-R2 + TD3D-R3
  -> TD3D-R4
  -> TD3D-R5
  -> TD3D-R6
  -> TD3D-R7

TD3D-R1, R2 и R3 можно разрабатывать параллельно только после TD3D-R0. Любые runtime changes требуют Unity compilation, проверки Console и bounded Play Mode smoke.

## 9. Явные non-goals ближайшего прохода

- Не копировать visual style, names, factions, cards или numbers референсов.
- Не начинать с сотен cards, множества factions или большого meta-tree.
- Не добавлять manual hero-control layer до того, как preparation loop станет интересным.
- Не добавлять duplicate managers или parallel serialized state.
- Не считать старые статусы планов доказательством текущей реализации; перед каждой задачей перепроверять live scene, code, prefabs и assets.
- R64 ML smoke isolation: the active player agent now resolves the unavailable challenge branch to explicit ChallengeModifier.None; TD ML Balance Agent and TD ML Enemy Level Agent are saved with _trainingMode=false so they cannot silently replace authored waves. EditMode remains green at 62/62. Post-fix Play proved authored Wave_01=7 and Wave_02=16, HasGeneratedWave=false, valid map, placement and combat telemetry; natural final-wave Victory is still open because the heuristic run loses base health and resets after terminal.
- R65 player-agent preparation policy: after mandatory coverage placement, the existing owner chain may now choose an affordable tower upgrade before starting the next wave. The contract suite is 63/63; natural smoke still lacks enough income in some seeds, while a controlled currency probe verified TowerUpgrade grade 0->1, 7 kills, 0 leaks in Wave 1, and no generated-wave substitution. This closes upgrade-action routing evidence but not natural final-wave Victory.
- R66 player-agent reward routing: `ResourceCache` remains the catch-up choice for a one-tower build below the cheapest tower cost; at base health <=75% with a survivable build, the existing reward owner now receives `EmergencyRepairs`. Invalid reward selection receives a small negative reward and no positive selection shaping. Full EditMode passed 64/64; natural Play completed authored Waves 1-2, reached Wave 3 with `HasGeneratedWave=false` and a valid map, and logged `EmergencyRepairs` at base 2/20 with two towers. The episode reset before a direct `Victory` or `Defeat` log, so final-wave natural balance remains open.
- R67 terminal reward safety: `WaveManager` now rejects reward selection after Base destruction/terminal state and clears pending inter-wave reward state during `ForceStopWave`. The contract suite passed 65/65; natural ML smoke still read authored Wave 1/2 and valid `None` baseline state. A controlled no-tower terminal probe verified `7` leaks, `base=13/20` at pending reward, then `acceptedAfterDestroy=false`, no `RewardSelected`, and `RunFinished(Defeat)`. This closes reward-after-defeat leakage but not natural final-wave Victory.
- R68 player-agent tile choice: the preparation heuristic now evaluates valid tile options with the existing `TileMapManager` spawn readback and `TowerPlacementSystem` entrance coverage owner before committing through `TilePlacementSystem`. The selector contract and full EditMode suite passed 66/66; Play logs recorded scored `Cross_3` and `Straight` decisions with before/after open-end and coverage values. The bounded episode reset before final-wave terminal telemetry, so natural Victory remains open.
- R69/R70 player-agent tower placement: affordable tower selection now evaluates candidate coverage and preserves a basic-purchase reserve on coverage ties; placement slots also score the existing NavMesh route samples. Play logged two opening Novice purchases from the 50 starting bank and later `TowerPlaced` route coverage `13/30`; EditMode passed 69/69 with no C# compiler errors. Natural final-wave Victory remains open because the bounded run reached Wave 2 only.
- R71 player-agent opening counter role: on an empty Wave 1 build, a coverage tie may select the authored Tesla `AoEWeapon` role before the cheaper single-target basic tower; a real coverage advantage still wins. Placement logs now expose `role` and `openingDefense`. Direct validation/recompile had no C# errors and EditMode passed 71/71. Fresh Play committed Tesla then Novice and reached Wave 2 with causal combat readback; MCP recovery was required before stopping, and natural final-wave Victory remains open.
## R72 - route-aware tile decisions

The player-agent tile policy scores the existing route samples after each valid tile choice, using `TileMapManager.GetSpawnPositionsAfter` and `TowerPlacementSystem` route coverage. Route coverage is preferred over anchor-only coverage, while open ends and connected neighbors remain bounded tie-breakers. The decision is committed by the existing `TilePlacementSystem` and logged as `[MLAgent] Tile decision ... routeCoverage=covered/total`.

Acceptance evidence: the pure selector contract prefers the higher route ratio when anchor coverage ties; the full EditMode suite passed `72/72`. A bounded isolated Play run recorded `routeCoverage=17/23`, `18/23`, and `19/23`, then read Wave 2 with a valid map, `3/3` covered entrances, and a `PathComplete` enemy path. Natural final-wave Victory remains unproven.
## R73 - opening reserve and map pressure

The opening AoE-role bonus is gated by preservation of the cheapest basic-tower reserve. This keeps the authored Tesla counter available when the bank supports it, while a `50`-currency opening now commits two `25`-cost Novice towers when coverage is tied. Tile selection also penalizes each post-placement open road end by a bounded `500` score term, so a small route-coverage improvement cannot justify uncontrolled entrance expansion.

Evidence: the selector and reserve contracts are covered by the full `74/74` EditMode gate; direct validation and forced recompilation report no C# errors. Fresh Play logs show `Novice 50->25` and `25->0`, with `openingDefense=True` on the first commit and `openingAreaRoleEligible=False`. Wave 1 reached `1 kill / 1 leak`, base `19/20`, and valid map topology; the episode reset before direct terminal readback. Natural final-wave Victory remains unproven.

## R74 - recovery and reinforcement preparation

The reward policy keeps `ResourceCache` for a recoverable damaged build whose bank is still below the cheapest basic tower cost. `EmergencyRepairs` is reserved for critical base health at or below 50% or for a bank that can already buy the cheapest basic tower. Once entrance coverage is complete and no affordable upgrade exists, the player policy routes one more affordable tower purchase before `StartWave`; the existing placement owner remains responsible for the commit.

Evidence: the reinforcement selector contract is included in the full `75/75` EditMode gate; direct validation and forced recompilation report no C# errors. Play telemetry recorded `Reward decision=ResourceCache;base=14/20;currency=12;towers=2`. Another bounded authored run reached Wave 2 with three Novice towers, `currencySpent=75`, base `16/20`, `3` kills, `4` leaks, valid topology, and an active `PathComplete` enemy. That seed used coverage placement for the third tower, so the specific `placementIntent=reinforcement` natural log remains unobserved. Natural final-wave Victory/Defeat acceptance remains open.

## R75 - player smoke isolation and episode-restart hygiene

The runtime smoke menu now isolates only `TowerDefenceBalancerAgent` and `TowerDefenceEnemyLevelAgent`; the active `TowerDefenceAgent` remains enabled. Destroyed diagnostic-agent references are pruned before scene-reload reapplication, keeping the isolation owner bounded across restarted episodes.

Evidence: direct validation and forced recompilation report no C# errors; the full EditMode gate passed `75/75`. The corrected Play smoke logged `2 diagnostic agents; player agent remains active`, reached authored Wave 2 with `WavesCompleted=1`, three towers, `2` kills, `5` leaks, base `15/20`, valid topology, and a live `PathComplete` enemy. Three scene-reload logs remained at `2` diagnostic agents. Natural final-wave Victory/Defeat remains unproven.

## R76 - direct natural terminal readback

Для bounded Play smoke isolation active player agent теперь временно получает `RestartSceneOnEpisodeReset=false`, чтобы первый естественный terminal state не исчезал до чтения `GameplayTelemetry`. Исходное значение восстанавливается при выходе из isolation; serialized training setup и ownership диагностических агентов не меняются.

Evidence: changed scripts validated with `0` C# errors; the editor script retained only two existing analyzer warnings. Forced recompilation returned `0` compiler errors, and the full EditMode gate passed `75/75` with `0` failed and `0` skipped. Fresh ML inference reached authored Wave 2 and naturally produced `BaseDestroyed` sequence `163`, `GameStateChanged=Defeat` sequence `171`, `RunFinished` sequence `172` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=2`), and terminal `Defeat` sequence `173`. The snapshot had three towers, three kills, 18 cumulative leaks, and `TileMapValid=true`; no generated wave was used. Play was stopped explicitly. Natural final-wave Victory remains unproven and is the next balance gap.

## R77 - coverage-preserving tile and strict placement policy

The player-agent tile policy now preserves the current entrance-coverage ratio whenever a valid alternative can do so, while retaining route coverage and open-road-end scoring. Commit telemetry is emitted even when the agent keeps the default option. The strict coverage branch no longer falls back to an arbitrary no-gain placement: when coverage is mandatory but no valid slot exists, the heuristic and action mask hold in preparation until the placement owner exposes a valid coverage slot. Placement-slot recalculation is cached per frame and invalidated after tower/tile commits. Planning reads the authored `TowerStatsSO.Range.BaseValue` for prefab candidates before their runtime `TowerStats` has awakened.

Evidence: `TowerDefenceAgent.cs` validation returned `0/0`; forced recompilation returned no C# errors and retained the two existing `CS0414` warnings; full EditMode passed `76/76` with `0` failed and `0` skipped. Fresh isolated ML inference fallback placed towers through `TowerPlacementSystem`, reached authored Wave 2, and read `TileMapValid=true`, `CoveredEntrances=3/4`, `BaseHealth=20/20`, and `currency=2` at the active-wave boundary. Play was stopped explicitly. Natural terminal acceptance is covered separately by R76 Defeat proof; no Victory claim is made.

## R78 - opening reserve and reachable coverage gate

The player-agent purchase policy now protects one cheapest-basic purchase during the opening when a candidate would otherwise spend that reserve for only one additional covered entrance. A two-entrance coverage advantage is still allowed. The existing `TowerDefenceAgent` selection and `TowerPlacementSystem` commit owners remain unchanged; the placement log exposes `openingReserveGuard` for the decision readback.

The preparation gate now holds only when an affordable coverage placement is reachable through the authored placement-slot search. If all affordable slots cover already-covered entrances, the agent logs `Coverage gate=unreachable` and proceeds to `StartWave`; it does not fabricate a no-gain placement. Contract coverage locks both branches.

Evidence: direct validation returned `0` C# errors; forced recompilation returned `0` compiler errors; full EditMode passed `79/79`, `0` failed, `0` skipped. Fresh isolated ML Play selected two opening Novices from `50` (`50->25->0`), later placed a third after reward income, reached authored Wave 2, and produced natural `BaseDestroyed` seq `216`, `RunFinished` seq `224` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=6`), and `Defeat` seq `225`. The map stayed valid and no generated wave was used. Natural final-wave Victory remains open.

## R79 - terminal wave resolution and spawn guard

The delayed `GameManager` defeat presentation exposed a terminal race in `WaveManager`: after `BaseDestroyed`, the active wave could still finish its `enemiesAlive` wait, issue completion payout/passive income, and spawn another enemy before `Defeat`. The existing `WaveManager` now guards reward callbacks, completion resolution, and each async spawn boundary against `PlayerBase.IsDestroyed` or `GameManager.IsGameOver`. The guard keeps the existing owner chain and does not add another terminal state holder.

Baseline evidence: `BaseDestroyed` seq `186` was followed by currency events and `WaveCompleted` seq `196` before delayed `Defeat`. Fixed evidence: the first post-change smoke ended with `BaseDestroyed` seq `177` and `RunFinished` seq `181` at `currency=29` without `WaveCompleted`; the follow-up smoke recorded `BaseDestroyed` seq `196`, then no `EnemySpawned`, `CurrencyGained`, or `WaveCompleted`, followed by `RunFinished` seq `200` and `Defeat` seq `201` at `currency=4`, with `IsSpawning=false`. Direct validation returned `0` warnings / `0` errors, forced recompilation returned no compiler errors, and full EditMode passed `80/80` with `0` failed and `0` skipped. Play was stopped explicitly. Natural final-wave Victory remains unproven.

## R80 - placement owner rejection handoff

The player-agent preparation route now reconciles planning slots with the existing `TowerPlacementSystem` commit owner. The placement owner logs bounded rejection reasons for missing surface points and blocking intersections. The agent tries the current candidate slots through that owner within one action; if all owner attempts fail, it records an owner-rejected gate, disables stale placement priority for the current preparation, and hands control back to the existing upgrade or `StartWave` policy. Invalidation after tower/tile commit, wave completion, and episode reset clears the gate.

Evidence: direct validation returned `0` errors for the changed agent and `0` errors for the placement/test files (with their existing analyzer warnings); forced recompilation returned `0` compiler errors; full EditMode passed `81/81`, `0` failed, `0` skipped. Fresh isolated Play progressed `Preparation` seq `84` to `WaveStarted` seq `87` without the previous preview-loop hold. Its terminal sequence was `BaseDestroyed` `159`, `RunFinished` `163` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=25`), then `Defeat` `164`, with `IsSpawning=false` and `TileMapValid=true`. The selected seed used the documented unreachable-coverage bypass rather than a blocking-intersection rejection, so no owner-rejected runtime line is asserted. Natural final-wave Victory remains open; Play was stopped explicitly.

## R81 - playable Wave 2 count and route-reinforcement diagnostic

The authored `Wave_02` count scale is now `1.00`, yielding `14` expected enemies. The player policy distinguishes coverage placement from route reinforcement and uses the existing route-sample/placement owners to accept reinforcement only when it adds route coverage under an unreachable-coverage, no-upgrade, affordable-bank condition. `placementIntent` and existing GameplayTelemetry events provide the causal readback.

Validation: changed C# scripts returned no compiler errors; the ML contract retained its three existing analyzer warnings; forced recompilation returned `0` `error CS` entries; full EditMode passed `83/83`, `0` failed, `0` skipped. Fresh isolated Play reached Wave 2 with `14` enemies and three towers, then naturally recorded `BaseDestroyed`, `RunFinished` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=4`), and `Defeat`. The terminal snapshot had `4` kills, `16` leaks, `EntryCoverageRatio=0.5`, `TileMapValid=true`, and no generated wave. The third placement showed no anchor-coverage gain in this seed. Natural final-wave Victory remains open; no arbitrary balance conclusion is drawn. Play was stopped explicitly.

## R82 - counter-aware opening and combat-power selector

R82 extends the existing `TowerDefenceAgent` purchase policy with two bounded authored-data terms. The existing upcoming-wave count provides swarm intel: an affordable area-role tower may spend the opening reserve when the count reaches the threshold, while the normal cheapest-basic reserve remains the default. When authored planning combat power is available, `Damage * FireRate` plus the existing area-role factor is combined with coverage so a minor coverage edge does not select a materially weaker tower. Existing coverage, affordability, and placement-owner contracts remain the source of truth.

The selection decision is observable through the existing `[MLAgent] Tower decision` line (`openingAreaCounterEligible`, `combatPower`, `openingReserveGuard`), and the runtime outcome is judged through `GameplayTelemetry` rather than selection logs alone. The acceptance slice is: Tesla is selected for the authored Wave 1 swarm, Novice is selected after Wave 1 instead of the low-power coverage-biased option, authored Wave 2 is completed, and Wave 3 is entered with valid topology. Final-wave Victory is a separate acceptance criterion and remains open until a natural terminal Victory is read.

R82 evidence: focused selector contracts `2/2`; full EditMode `85/85`, `0` failed, `0` skipped; forced recompilation `0` compiler errors. Fresh isolated ML Play recorded Wave 1 `4 kills / 3 leaks`, Wave 2 `3 kills / 11 leaks`, base `1/20` before Emergency Repairs, Wave 3 entry with `41` enemies, then `BaseDestroyed` seq `441`, `RunFinished` seq `455` (`wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=32`), and `Defeat`. `TileMapValid=true`, `HasGeneratedWave=false`. The evidence closes the selector/placement diagnostic and opens a Wave 2/3 combat-survival balance pass; it does not claim Victory.

## R83 - Emergency Repairs recovery band and causal telemetry

The existing `WaveManager` reward owner now applies `EmergencyRepairs` to the bound `PlayerBase` up to `ceil(maxHealth * 0.75)`, rather than using a flat `+10`. `SelectRewardOffer` resets the per-offer currency and repair result fields before applying a choice. `GameplayTelemetry` keeps the existing reward event but adds `baseRepair` to `RewardSelected` details; `amount` continues to mean currency, so the two effects are observable separately.

Evidence: direct validation returned `0` warnings and `0` errors for the three changed C# files; forced recompilation returned `0` compiler errors; the new recovery-band contract passed `1/1`; and full EditMode passed `86/86`, with `0` failed and `0` skipped. Fresh isolated ML Play reached Wave 2 reward selection at `base=1/20`. Telemetry recorded `BaseHealthChanged` seq `199` (`1->15`) and `RewardSelected` seq `200` with `rewardId=EmergencyRepairs;amount=0;baseRepair=14;currencyAfter=94`, then Wave 3 started at `15/20`. The natural run ended in `Defeat` with `RunFinished` seq `481` (`wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=44`) and `Defeat` seq `482`; `TileMapValid=true`, `HasGeneratedWave=false`, and Console errors `0`. This closes the repair-effect/readback gap, not final-wave Victory. The next smallest gameplay gap remains authored Wave 2/3 combat survival. No trainer, task chat, or worktree was created or restarted.

## R84 - authored Wave 2 pressure correction

The existing `Wave_02` asset was changed through Unity AssetDatabase from `9 Turtle + 5 Frog + 1 Boss No Damage` to `8 Turtle + 4 Frog`, preserving both mid-run roles while deferring the boss to the authored final wave and reducing the authored count from `15` to `12`. The progression contract now asserts the composition and total; `WaveManager` and adaptive generation remain the owners.

Evidence: direct validation of `WaveProgressionContractTests.cs` returned `0` warnings and `0` errors; forced recompilation returned `0` compiler errors; the focused progression contract passed `1/1`; and full EditMode passed `86/86`. Fresh isolated ML telemetry completed Wave 2 at `7 kills / 5 leaks`, repaired base `4->15`, and entered Wave 3 with `TotalEnemiesInWave=41`. This is a bounded pressure improvement, not a Victory claim.

## R85 - combat-power reinforcement over upgrade

The existing player-agent preparation policy now compares a candidate tower's planning combat power with the selected upgrade's marginal gain when all entrances are covered and both purchases are affordable. A new tower is accepted only when its power exceeds twice the upgrade gain; its free placement no longer needs to add route coverage. `TowerPlacementSystem` remains the sole placement owner. The existing ML decision log adds `placementReason` for bounded causal readback.

Evidence: direct `TowerDefenceAgent.cs` validation returned `0` warnings and `0` errors; the contract file retained three existing analyzer warnings and `0` errors; the pure combat-power contract passed `1/1`; forced recompilation returned `0` compiler errors; and full EditMode passed `87/87`, `0` failed, `0` skipped. Final isolated ML Play logged `placementIntent=reinforcement;placementReason=combat-power-over-upgrade` with `coverage=4/4` and `currency=52->12`. The run reached authored Wave 3 with four towers and valid topology, but ended naturally in `Defeat` at base `0/20`, with `22` kills, `24` leaks, `wavesCompleted=2`, and no generated wave. The policy branch is runtime-confirmed; final-wave Victory remains open. Play was stopped explicitly; no trainer, task chat, or worktree was created or restarted.

## R86 final-wave density probe and archetype terminal telemetry

The authored final wave was reduced through Unity AssetDatabase from `20 Turtle + 20 Frog + 1 Berserker` (`41`) to `12 Turtle + 8 Frog + 1 Berserker` (`21`), preserving the final boss and both enemy roles. `GameplayTelemetry` now records `archetype`, `maxHealth`, and `terminalReason` on `MonsterDeath`, and adds a parallel `MonsterLeak` event, so final-wave balance can be judged by terminal enemy identity instead of aggregated leak counts alone.

The new progression contract passed; direct validation reported `0` warnings and `0` errors for the changed telemetry and contract scripts, forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, with `0` failed and `0` skipped. Fresh isolated ML Play still reached `Defeat`: Wave 2 completed at `2 kills / 10 leaks`, Emergency Repairs read `base=3->15`, and Wave 3 ended at `base=0/20` with `15` cumulative kills, `23` cumulative leaks, four towers, `4/4` entrance coverage, valid topology, and `HasGeneratedWave=false`. Terminal telemetry identified Wave 3 `Runner;maxHealth=97.50` leaks and `Tank;maxHealth=32.50` deaths/leaks. The density probe is not Victory evidence; the next smallest gap is Wave 2 combat survival and two-tower preparation, not another uninstrumented final-wave count change. Play was stopped explicitly; no trainer or task-chat restart was needed.

## R87 - AoE owner-chain telemetry and clean ML smoke

The existing `AoEWeapon` now resolves `MonsterHealth` through the collider's parent owner and emits bounded `overlaps`, `resolvedTargets`, `damage`, and `range` readback when its authored logging flag is enabled. The player-agent opening swarm threshold remains `7`; the attempted threshold `8` did not improve the live run and was reverted. No weapon, tower, placement, or economy owner was duplicated.

Direct validation returned `0` errors for `AoEWeapon.cs` and `TowerDefenceAgent.cs`; it reported one non-blocking logging warning for `AoEWeapon.cs` and the existing three analyzer warnings for the ML contract. Forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, `0` failed, `0` skipped. A clean isolated ML Play produced `overlaps=1..3;resolvedTargets=1..3` AoE logs, confirming the damage path is resolving targets. The run entered authored Wave 3 with four towers, `3/3` entrance coverage, valid topology, `16` kills, and `22` leaks, then naturally ended in `Defeat` at `base=0/20` (`wavesCompleted=2`, `TotalEnemiesInWave=21`, `HasGeneratedWave=false`). Wave 1 still exposed an opening Tesla shot at `distance=5.65` against `range=5.50` with `0` damage applications; the next slice is range/exposure alignment and Wave 2/3 combat survival, not another threshold change. Play was stopped explicitly; no trainer or task-chat restart was needed.

## R88 EffectiveRange alignment and bounded balance probe

The existing `AoEWeapon` now uses the owning `Tower.EffectiveRange` for its overlap query, aligning physical area damage with the range already used by tower target acquisition and ML planning. Its bounded diagnostic reports the effective range together with overlap and resolved-target counts; no second range or combat owner was introduced. The Tesla prefab remains on its authored `Nearest` target priority: two temporary `Farthest` smokes produced only `14-15` kills, `22-23` leaks, three towers, and `3/4` coverage, so the experiment was reverted through `PrefabUtility`.

The persistent range-alignment smoke improved the causal opening readback: Wave 1 completed at `5 kills / 2 leaks`, Wave 2 at `7 kills / 5 leaks`, and the run entered authored Wave 3 with four towers, `4/4` coverage, valid topology, `21` cumulative kills, and `17` cumulative leaks before natural `Defeat` at `base=0/20`. A temporary Wave 3 Runner health probe (`1.5 -> 1.0`) was then rejected: its smoke completed all `21` spawns but ended at `20 kills / 20 leaks`, with Wave 1 `4/3`, Wave 2 `4/8`, and Wave 3 terminal telemetry showing twelve Tank kills against eight Runner plus one Berserker leaks. The asset was restored to `1.5`; no unproven balance change remains.

After cleanup, direct validation returned `0` errors for the changed AoE and progression-test paths, forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, `0` failed, `0` skipped. Natural final-wave Victory remains unproven. The next slice is Wave 2/3 survival and reward/economy exposure using the existing owners and the new terminal telemetry, not another blind health multiplier change. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

## R89 delayed bounty guard and reward-decision telemetry

The existing `BountyContract` owner path was already implemented by `WaveManager` and exposed by `WaveUI`, but the player heuristic never selected it. `TowerDefenceAgent` now selects that existing reward only when a future authored wave remains, the base is above the 75% repair band, at least two towers and full entrance coverage are present, and the bank can still buy the cheapest tower. Emergency Repairs keeps priority when health is low; ResourceCache remains the catch-up path. The bounded reward log now includes `wave=current/total` and `coverage=covered/total`.

Direct validation returned `0/0` for `TowerDefenceAgent.cs`; the contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML inference ended naturally in `Defeat`: Wave 1 selected `ResourceCache` at `base=17/20;currency=28;towers=1;coverage=2/4` and completed `4 kills / 3 leaks`; Wave 2 selected `EmergencyRepairs` at `base=5/20;currency=96;towers=2;coverage=3/4`, completed `4 kills / 8 leaks`, and repaired base `5->15`. Wave 3 terminal telemetry recorded twelve Tank kills and eight Runner plus one Berserker leaks; the run ended at `20 kills / 20 leaks`, `base=0/20`, four towers, valid topology, and `HasGeneratedWave=false`. The bounty branch is contract-covered but not claimed as a natural runtime selection because its safety preconditions were correctly false in this seed. Natural Victory remains unproven; the next gap is Wave 2 Runner exposure and combat survival. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

## R90 target archetype exposure telemetry

`GameplayTelemetry` now includes `archetype` and `priority` in `TowerTargetAcquired` and `TowerFired`, alongside range, distance, and target health. This extends the existing combat journal without changing `Tower` targeting ownership. A temporary Wave 2 Runner health probe (`1.0 -> 0.8`) was rejected: Runner max health became `36.00`, but all four Wave 2 Runners still leaked and the run ended at `14 kills / 22 leaks`; `Wave_02` and its contract were restored to `healthMultiplier=1.0`.

The fresh authored-baseline smoke recorded Wave 2 `17` target acquisitions, `19` fires, `26` damage applications, `5` kills, and `7` leaks. `Nearest` acquired five Runner targets and fired at four of them, proving the Runner path is reachable; the remaining gap is exposure/damage throughput and build timing, not another global target-priority change. The run naturally ended in `Defeat` with `RunFinished` `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=28`; no Victory claim is made. Direct validation returned `0/0` for the telemetry and progression paths, forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode remained `88/88`. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

## R91 Wave 2 spawn-pacing probe rejected

The existing authored `Wave_02` Runner/Frog group was temporarily changed through Unity AssetDatabase from `spawnDelay=2` to `3` to test whether slower exposure would improve the existing combat path. The existing `WaveManager` spawn owner and targeting priority were unchanged. The probe was rejected: Wave 2 recorded `19` target acquisitions, `18` tower fires, `27` damage applications, `5` kills, and `7` leaks; five Runner acquisitions and four Runner fires still produced four Runner leaks. This matched the latest authored baseline outcome (`5/7`) and did not improve survival, so `Wave_02` was restored to `spawnDelay=2` and the temporary contract assertion was removed.

Direct validation returned `0/0` for `WaveProgressionContractTests.cs`; forced recompilation returned `0` compiler errors; the Console error filter returned no errors; and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable, entered authored Wave 3, and ended naturally in `Defeat` (`RunFinished`: `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=9`), with no Victory claim. Play was stopped explicitly. The persistent instrumentation and bounty guard remain; the next gap is damage throughput/build timing, not another spawn-delay probe.

## R92 Final-wave upgrade reserve

The existing player-agent preparation path now treats the phase before the final authored wave as a final-wave upgrade reserve when the latest completed wave is immediately before the final wave, entrance coverage is complete, and an upgrade is affordable. It suppresses reinforcement placement only in that bounded state; incomplete coverage still keeps the coverage obligation. The upgrade continues through the existing `Tower.UpgradeSpendingCost` and `ResourceManager` owners. The commit log now records `reason=final-wave-upgrade-reserve` and `wave=current/total`.

The new pure contract passed; direct validation returned `0/0` for `TowerDefenceAgent.cs`, while the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors and full EditMode passed `89/89`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable and captured two final-preparation upgrades (`Tesla 0->1`, `Tesla 1->2`) with full `coverage=1/1`; the terminal snapshot read `TowersUpgraded=2`, `wavesCompleted=2`, `BaseHealth=0/20`, `HasGeneratedWave=false`, followed by natural `Defeat`. The branch is runtime-confirmed, but final-wave Victory remains unproven. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

## R93 Spawn role and pacing telemetry

`WaveManager` remains the sole spawn owner and now publishes the last spawned group/index/archetype/scaled health/scaled speed/spawn delay for `GameplayTelemetry`. `EnemySpawned` details are structured as `group`, `enemy`, `archetype`, `health`, `speed`, and `spawnDelay`, preserving causal group order and runtime scaling without adding another owner. A temporary Runner-first Wave 3 order probe was rejected after `12` Runner acquisitions, `8` fires, `0` kills, `8` leaks, base destruction after `10/21` spawns, and total `8` kills / `19` leaks in that seed. Unity AssetDatabase restored the authored order to `Tank -> Runner -> Berserker`; no probe balance change remains.

The formatter contract and changed C# files validated with `0` warnings and `0` errors; forced recompilation finished without compiler errors; full EditMode passed `89/89`, `0` failed, `0` skipped. Two Play attempts were blocked because Unity reported `EditorApplication.isPlaying=false` immediately after MCP reported entry, so this slice has no new gameplay/ML result and no Victory claim. The next gap is final-wave Runner throughput using the new spawn telemetry.


## R94 Runtime compile correction and Play transition blocker

R93 exposed a latent compile defect in the new spawn telemetry: `scaledHealth` was declared inside the health-initialization block but consumed by the following telemetry block. Unity Editor.log caught the real `CS0103` during compilation; the fix hoisted the local to `SpawnEnemy` scope and did not change serialized assets or balance. Direct validation then returned `0` warnings and `0` errors for the changed C# files, forced recompilation finished without compiler errors, and full EditMode passed `90/90`, `0` failed, `0` skipped. Unity still reports the existing non-blocking `CS8785` Odin source-generator warning.

After the compile fix, Play entered `is_playing=true` but remained `is_changing=true` for `40-47` seconds on repeated attempts. The temporary editor-only toggle of Enter Play Mode Options did not change the transition and was restored to `enabled=true;options=3`. Play was stopped through MCP; no gameplay telemetry, ML-agent result, or Victory claim is made. The next gap remains final-wave Runner throughput once Unity Play transition is healthy.


## R95 Runtime spawn-role readback

The repaired telemetry refactor was runtime-confirmed in an isolated ML Play smoke using inference fallback because no trainer was connected. `EnemySpawned` readback preserved authored groups and scaling: Wave 1 `Tank 7/7`, `health=20.00`, `speed=3.00`, `spawnDelay=1.00`; Wave 2 `Tank 8/8`, `health=24.75`, `speed=3.30`, `spawnDelay=1.00`, then `Runner 4/4`, `health=45.00`, `speed=3.00`, `spawnDelay=2.00`; Wave 3 reached `Tank 12/12`, `health=32.50`, `speed=3.00`, `spawnDelay=0.50`, then `Runner 5/8`, `health=97.50`, `speed=4.50`, `spawnDelay=1.00` before the base was destroyed. The run produced `19` target acquisitions, `23` tower fires, and `46` damage applications; terminal telemetry identified Runner and Tank leaks by archetype. The final snapshot was `Wave 3`, `17/21` spawned, `8` kills, `28` leaks, `2` towers, `2/4` coverage, `base=0/20`, natural `Defeat`, and no Victory. Play was stopped explicitly; the next gameplay gap is preparation/combat survival, not missing spawn observability.

## R96 Route-reinforcement priority under incomplete coverage

The existing `TowerDefenceAgent` preparation policy no longer lets an affordable upgrade suppress route reinforcement when entrance coverage is incomplete and no placement can directly cover another entrance. The route-reinforcement branch still requires an affordable tower, an existing tower, a coverage ratio below `1.0`, and a valid route-contributing slot; final-wave upgrade reserve remains gated by full coverage. This keeps the existing `Tower`/`TowerPlacementSystem`/`ResourceManager` owner chain and adds no parallel build policy.

The pure ML contract now covers route reinforcement before upgrade in this state. Direct validation returned `0` warnings and `0` errors for `TowerDefenceAgent.cs`; the ML contract retained its three existing analyzer warnings and `0` errors. Forced recompilation finished without compiler errors and full EditMode passed `90/90`, `0` failed, `0` skipped. A fresh isolated ML Play used inference fallback and emitted an existing `TowerPlaced` telemetry event for the route branch (`Tesla`, `cost=40`, `coverage=3/3`, `routeCoverage=35/37`) before Wave 3. The terminal snapshot read `3` towers, `3/3` coverage, `14` kills, `23` leaks, `19/21` spawned, `base=0/20`, and natural `Defeat`; no Victory claim is made. Play was stopped explicitly; the remaining gap is Wave 3 combat throughput, especially Runner leaks.

## R97 Tower-grid input ownership repair

The active gameplay scene had ML training input enabled by default and an additional root `Synthetic Mouse` enabled. `InputProvider_NewInputSystem` therefore received a synthetic screen position instead of the player's hardware cursor, so tower placement could repeatedly raycast the origin and reject the snapped cell as occupied. The scene now defaults all authored ML agents to `_trainingMode=0` and disables the standalone synthetic source; ML smoke enables its existing agent input only for the bounded run. `TowerPlacementSystem` remains the sole tower-grid and commit owner.

The saved scene was checked in Play Mode with no active synthetic source in the manual default and with temporary ML input enabled: `TowerPlaced` committed at `(-3.00, 0.50, 4.00)`, confirming integer grid coordinates and the existing placement path. Full EditMode passed `90/90`, `0` failed, `0` skipped; Console error readback returned `0` errors. The ML smoke reached Wave 2 with valid `TileMapValid=true`; it was stopped explicitly and no Victory claim is made.

## R98 Target retention and ML input ownership correction

`Tower.UpdateTarget` now retains a live target when that target remains in the same `Physics.OverlapSphereNonAlloc` result used by acquisition. The previous center-distance check could acquire a collider whose bounds touched the range and immediately lose it because the enemy transform was just outside the radius; this was visible on fast Runner targets as repeated `TowerTargetAcquired`/`TowerTargetLost` churn. Existing `Tower` remains the sole target owner. When the authored `Logs` flag is enabled, target-loss diagnostics include tower, target, center distance, and effective range.

The ML input contract is now explicit: the active authored `TD ML Agent` keeps `_trainingMode=1`, its nested `TD ML Input`/`SyntheticMouse` remains inactive in the scene, and `TowerDefenceAgent.Start`/`TrainingMode` activates that input at runtime. The standalone root `Synthetic Mouse` stays inactive and is not a second input owner; diagnostic agents remain inactive/training-disabled. Runtime readback confirmed `training=true;inputActive=true;standaloneActive=false` and `input.device=TD Synthetic Mouse`.

Direct validation of `Tower.cs` returned `0` errors with one existing analyzer warning, forced recompilation returned no compiler errors, and full EditMode passed `90/90`. A clean ML inference smoke started at `Preparation`, `currency=50`, `towers=0`, `TileMapValid=true`; after enabling the authored player agent it reached Wave 3 and naturally ended `Defeat` at `base=0/20`, `2` towers, `2/4` coverage, and valid topology. A Runner target remained acquired at `distance=5.13;range=5.50` without the previous boundary-loss churn. Natural Victory is still unproven; the remaining gap is combat throughput/build timing, not grid ownership.
