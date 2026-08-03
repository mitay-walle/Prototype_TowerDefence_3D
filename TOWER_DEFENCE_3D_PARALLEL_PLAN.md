---
title: Prototype Tower Defence 3D
type: project-specific gameplay systems plan
status: planning
scope: 3D gameplay loop only
---

# Prototype Tower Defence 3D

Рабочий план систем и контента для одного 3D tower-defence забега с roguelite-решениями. План опирается на живой код, Gameplay.unity, prefab и ScriptableObject-ассеты, а не на исторические статусы старого плана.

## Нормативные источники

- AGENTS.md — проектные границы, KISS, owner-first изменения, Unity/MCP-проверки и правила сохранения dirty/untracked work.
- Assets/Documentation/GAMEPLAY_REFERENCES.md — design intent: карта как часть билда, meaningful preparation, next-wave intel, tower roles, limited rewards и поздняя meta progression.
- .agents/game-director.md — gameplay direction и task decomposition.
- .agents/read-only-gameplay-architect.md — read-only owner/lifecycle/serialization review.
- .agents/gameplay-designer.md — authored map, roles, rewards, pacing and risk.
- .agents/gameplay-systems-programmer.md — runtime owner-side systems.
- .agents/gameplay-tester.md — focused tests and verification evidence.
- .agents/ui-designer.md — player-facing UI/UX and authored UI implementation.
- .agents/unity-editor-tools-programmer.md — editor-only tools when direct Unity authoring needs one.
- .agents/project-auditor.md — project structure, Resources and serialized ownership audit.
- .codex/skills/unity-mcp-skill/SKILL.md — Unity Editor/MCP routing for scene, prefab, asset, import, console and Play Mode operations.
- .codex/skills/mcp-unity-find-in-file/SKILL.md — поиск по Unity-файлам.
- .codex/skills/mcp-unity-validate-script/SKILL.md — валидация изменённых C#-скриптов.
- .codex/skills/unity-recompile-menuitem/SKILL.md — принудительная перекомпиляция, только если она нужна по правилам проекта.
- .codex/skills/prefab-creation/SKILL.md — Unity authoring и проверка prefab, только для карточек, которые действительно меняют prefab.
- .codex/skills/ui-prefab-authoring/SKILL.md и .codex/skills/ui-prefab-localization/SKILL.md — только для UI-owned authoring.
- .codex/skills/test-writing/SKILL.md — Unity EditMode/PlayMode tests и smoke evidence.
- .codex/skills/apply-patch/SKILL.md — convention-aware запись этого файла и будущих текстовых файлов.

## 1. Дизайн-срез и gameplay contract

Игрок защищает базу на процедурно собранной 3D карте. Между волнами он читает угрозу, выбирает ограниченную награду, меняет маршрут тайлами, строит или улучшает башни и вручную фиксирует старт следующей атаки. Во время волны башни автоматически выбирают цели и стреляют, враги идут к базе, убийства и завершение волны дают валюту. Победа — после последней настроенной волны; поражение — после уничтожения базы; Restart начинает новый run без временного состояния прошлого run.

### Целевой capability envelope

Цель плана — довести текущую owner-цепочку до следующего набора наблюдаемых возможностей. Это scope будущих задач, а не утверждение, что все возможности уже существуют в checkout:

- Round/wave authoring: гибкое создание последовательности раундов и волн через существующий WaveManager и WaveConfig, включая управляемый endless/loop режим без нового RoundManager.
- Path: поддержать текущий grid/tile route с автоматической проверкой связности, расширением маршрута и поиском пути врагом к базе; SplinePath, GridPath и GridAutopath не считаются существующими системами и могут появиться только после отдельной owner/data проверки.
- Placement: зафиксировать поддерживаемые правила размещения башен — на grid-cell, в разрешённой области с collision checks или в заранее authored spots — через существующий TowerPlacementSystem и TileMapManager, без параллельного placement owner.
- Save/Load: после стабильного in-run loop добавить продолжение run с того же состояния и отдельный meta result; временные и постоянные данные должны иметь одну явную boundary и не протекать между restart.
- Towers and units: сохранить tower/enemy ядро и оставить возможность allied units только как поздний scope, если будет найден существующий owner; не создавать unit manager ради capability checklist.
- Attack: покрыть существующими Tower, IWeapon, Projectile и GameObjectPool melee/ranged/beam-like delivery и модификации multitarget, bounce, splash, crit и bash только там, где подтверждены текущие data/weapon owners.
- Ability: отложить гибкие abilities, auras, stun, burning и heal до stable combat; авторить их через существующую stat/modifier/upgrade цепочку, если audit подтвердит её пригодность, без отдельного ability manager.
- Leveling: позднее разрешить XP/levels для выбранных entities или towers через существующие stats/upgrades; не вводить leveling state до утверждённой economy и persistence boundary.
- Technology: позднее ограничивать доступность tower/ability контента через существующую progression boundary; technology tree не входит в первый run milestone.
- Resources: расширять текущий ResourceManager только при доказанной необходимости новых resource types; первый loop использует одну currency и source-aware ledger.
- Armor/Damage: сначала стабилизировать единый damage/death/reward chain; дополнительные armor/damage types добавлять как data-driven extension существующих MonsterHealth, Tower/IWeapon и stats owners.
- Audio: важные атаки, попадания, смерть, награда и terminal outcomes должны иметь authored audio hooks через реальных owners/UI, без отдельной глобальной audio gameplay-системы.

Первый обязательный loop:

1. Boot запускает GameplayBootstrap.
2. MapBuild создаёт связанную карту, базу, spawn points и актуальный NavMesh.
3. Preparation показывает next-wave intel и даёт решения в фиксированном порядке.
4. Игрок выбирает ровно одну доступную reward offer, затем выбирает valid tile/rotation, строит или улучшает башни и проверяет маршрут/coverage.
5. Игрок нажимает Start Wave; после lock новые preparation-решения не применяются.
6. WaveActive выполняет spawn → target → weapon → projectile/hit → damage → death → reward и движение к базе.
7. WaveResolve единожды закрывает волну, начисляет completion/passive rewards и формирует следующий preparation context.
8. После последней волны — Victory; при уничтожении базы — Defeat; Restart возвращает run к Boot через существующий scene reload path.

Целевая state machine: Boot → MapBuild → Preparation → WaveActive → WaveResolve → Preparation; terminal states Victory и Defeat; Restart — lifecycle transition обратно в Boot, а не отдельный второй state owner. Paused остаётся существующим orthogonal pause overlay, не конкурирующим с run state.

Preparation order фиксируется так:

1. показать состав и defensive implication следующей волны;
2. применить одну mutually exclusive limited reward offer;
3. выбрать tile option и rotation, увидеть route preview и consequences для spawn/coverage, принять или отменить;
4. купить башню или улучшение, увидеть placement cell, elevation/coverage и изменившиеся stats;
5. нажать Start Wave, после чего GameManager lock’ит phase и передаёт разрешённый запуск WaveManager.

Combat contract:

- Tower — единственный owner target selection, fire cadence, aim и upgrade request;
- IWeapon либо существующий projectile path — единственный owner weapon delivery;
- Projectile — единственный owner projectile travel/impact и вызова damage;
- MonsterHealth — единственный owner enemy health/death/reward event;
- MonsterMove — единственный owner movement и damage to PlayerBase on reaching the base, без ручного дублирования death event;
- WaveManager — единственный owner wave accounting/resolve, подписывается на уже завершённый enemy outcome;
- ResourceManager — единственный owner currency ledger и affordability;
- GameHUD, WaveUI и future UI-owned screens только показывают state и отправляют command существующему owner.

## 2. Живой baseline и gaps

Проверенные входы:

- GameplayBootstrap.cs выполняет GenerateLevelAsync → BakeNavMeshAsync → PlaceGameplayObjectsAsync → InitializeSystemsAsync; после генерации создаёт runtime spawner transforms и вызывает WaveManager.Initialize.
- GameManager.cs сейчас владеет Initial, WavePreparing, WaveActive, Paused, GameOver, Victory, но PlayingState вычисляется через WaveManager.IsWaveActive; отдельного Boot/MapBuild/WaveResolve contract нет.
- WaveManager.cs владеет spawn, alive/spawn counters, WaveConfig, kill reward subscription, completion/passive reward и текущей случайной TilePlacementPhase; autoStartNextWave в Gameplay.unity выключен.
- ResourceManager.cs владеет одной currency, starting currency, TrySpend, kill/wave/passive additions и change events; source ledger и competing-spend contract отсутствуют.
- LevelGenerator.cs и MapGenerator.cs генерируют 10 тайлов вокруг четырёхсторонней base tile через TilePlacementValidator; TileMapManager.cs хранит placed tile state, base position и открытые spawn positions.
- TilePlacementSystem.cs уже принимает один RoadTileDef и prefab, позволяет rotation, проверяет CanPlaceTile, коммитит через TileMapManager и вызывает NavMeshSurfaceWrapper.BuildNavMesh; offer, route preview, path-length consequence и firing coverage отсутствуют.
- TileDatabase.cs в сцене содержит четыре tile prefab: Cross_3, Cross_4, Straight, Turn. WaveManager сейчас выбирает один случайный prefab; availableTiles serialized в TilePlacementSystem, но не является player-facing choice flow.
- Tower.cs владеет target priority, range query, aim, fire и upgrade; TowerStats читает TowerStatsSO, у которого есть Cost, UpgradeCost, Damage, FireRate, Range, CritChance, ProjectileSpeed, RotateSpeed и upgrade rules.
- TowerShopUI.cs показывает serialized список tower prefab и передаёт выбор в TowerPlacementSystem.cs; placement проверяет currency и intersection, но cell/elevation/coverage contract не оформлен.
- IWeapon.cs, Projectile.cs и GameObjectPool.cs уже образуют два weapon paths: interface weapon или fallback pooled projectile. Projectile сам вызывает MonsterHealth.TakeDamage.
- MonsterHealth.cs владеет health, death, early-kill reward и events; MonsterMove.cs владеет NavMeshAgent и base collision. При base collision MonsterMove напрямую вызывает health.onDeath, не переводя MonsterHealth в dead state и не выдавая его reward — это owner mismatch для resolve.
- GameHUD.cs показывает currency, wave, enemy count, progress, base health, pause и terminal screens. WaveUI.cs отдельно показывает start button и wave text; start-wave presentation частично дублируется.
- WaveConfig assets: Assets/Resources/WaveConfigs/Wave_01.asset, Wave_02.asset, Wave_03.asset; текущие EnemySpawnData содержат prefab, count, delay и health/speed multipliers, но не role/intel metadata.
- Tower data: Assets/Resources/TowerStats/TowerStatsSO 00 Basic.asset, 01 Tesla.asset, 02 Clever Girl.asset; в Assets/Prefabs/Towers есть более широкий каталог, который должен быть инвентаризирован перед content pass.
- Monster data: Assets/Resources/TowerStats/MonsterStats 00 Turtle.asset, 01 Frog.asset, 02 Boss No Damage.asset, 03 Boss.asset; role semantics сейчас не являются отдельным читаемым контрактом.
- Scene wiring: Gameplay.unity serializes GameplayBootstrap, GameManager, WaveManager, TileDatabase, TileMapManager, TilePlacementSystem, NavMeshSurfaceWrapper, TowerShopUI, GameHUD и WaveUI в одной основной сцене.
- Prefab GUIDs for current scripts are present, но m_EditorClassIdentifier в старых prefab blocks всё ещё содержит legacy labels (TD.Turret, TD.Stats.StatsTower, TD.EnemyHealth, TD.MoveToBase). Это требует технической проверки живого component type через Unity, но не является основанием для raw YAML repair.
- Persistence owner отсутствует: в Assets/Scripts нет runtime save/load contract для run result, temporary run state или meta progression.

Классификация gaps:

| Gap | Класс | Следствие |
|---|---|---|
| Explicit run/session state machine и единый transition owner | missing | UI и WaveManager могут обходить GameManager; WaveResolve не наблюдаем как отдельное состояние |
| Preparation order, lock/start и decision output | thin/mismatch | можно нажать start через несколько entry points, а tile phase живёт внутри WaveManager |
| Tile choice, route preview, path consequence, coverage/elevation | thin/missing | игрок видит ghost/validity, но не тактический результат выбора |
| Next-wave composition, roles и defensive implication | missing | WaveConfig исполняется, но не объясняет будущую угрозу |
| Tower catalogue/roles и placement cell | thin | числовые stats есть, build identity и elevation/coverage decision не зафиксированы |
| Death/reward/base-hit owner chain | mismatch | ручной onDeath от MonsterMove может обходить health/reward contract |
| Currency ledger и competing spends | thin | сумма меняется, но причина, reservation и affordability feedback не являются единым output |
| Limited reward offer | missing | между волнами нет mutually exclusive one-time application |
| Route/intel/offer/stat feedback | thin | GameHUD/WaveUI показывают базовые counters, но не decision consequences; start UI дублируется |
| Persistent meta progression | missing | нет owner и безопасной boundary; не начинать до stable in-run loop |
| Legacy prefab labels and live component identity | mismatch/blocked | нельзя считать prefab pipeline подтверждённым только по YAML labels |

## 3. Технические prerequisite tasks

Эти карточки не являются gameplay-системами. Они только снимают технические блокеры до соответствующей gameplay authoring. Placement/NavMesh verification вынесена в отдельную карточку и не смешивается с tactical map choice.

### TD3D-T0 — Freeze live owner and serialized contract

- Тип: technical prerequisite, read-only audit.
- Player-facing outcome: отсутствует; будущие task cards получают одну подтверждённую owner map.
- Role-agent: read-only-gameplay-architect + project-auditor.
- Project-local skill: mcp-unity-find-in-file только для точечного поиска; unity-mcp-skill для read-only scene/prefab inspection.
- Единственный runtime owner: не создаётся; фиксируются существующие owners GameplayBootstrap, GameManager, WaveManager, ResourceManager, LevelGenerator, TileMapManager, TilePlacementSystem, Tower, TowerPlacementSystem, TowerShopUI, MonsterHealth, MonsterMove, GameHUD, WaveUI.
- Existing files/classes: перечисленные owner scripts, GameState, WaveConfig, TowerStatsSO, MonsterStatsSO, Gameplay.unity.
- Data/serialized inputs: scene references, Resources/WaveConfigs, Resources/TowerStats, tile database and current prefabs.
- Callbacks/events/output state: документировать реальные onGameStateChanged, onWaveStarted, onWaveCompleted, onEnemyKilled, onEnemySpawned, currency events, base events и tower/monster events; не добавлять события ради красивой схемы.
- Dependencies: none.
- Acceptance scenario: reviewer can trace each player command to one owner and can name the exact output state/event; every gap is labeled missing, thin, mismatch or blocked.
- Test/smoke evidence: read-only rg/Unity inspection, file inventory and plan review; no Play Mode claim.
- Non-goals: no runtime, scene, prefab, asset, YAML, namespace or manager changes; no placement/NavMesh diagnosis here.

### TD3D-T1 — Verify prefab component identity and scene wiring

- Тип: technical prerequisite, Unity serialization verification.
- Player-facing outcome: будущая tower/monster task работает с реальными компонентами, а не с legacy labels.
- Role-agent: project-auditor with read-only-gameplay-architect review.
- Project-local skill: unity-mcp-skill; prefab-creation only if a later assigned task must author a prefab.
- Единственный runtime owner: existing prefab component owners Tower, TowerStats, MonsterHealth, MonsterMove; verification does not introduce a replacement owner.
- Existing files/classes: Assets/Prefabs/Towers/*, Assets/Prefabs/Enemies/*, Assets/Prefabs/Projectiles/*, Tower.cs, TowerStats.cs, MonsterHealth.cs, MonsterMove.cs, Gameplay.unity.
- Data/serialized inputs: script GUIDs, statsSO links, projectile/weapon links, fire points, NavMeshAgent, colliders and scene references.
- Callbacks/events/output state: record which prefab instances actually emit/use onDeath, onRewardGiven, target/fire events and stats recalculation; report missing or stale references.
- Dependencies: TD3D-T0.
- Acceptance scenario: Tower_00 Novice, Tower_01 Tesla, the configured enemy prefabs and Projectile Basic are opened in Unity and their live components resolve to current owner classes with required serialized links intact.
- Test/smoke evidence: Unity prefab inspection and Console check; no YAML-only success; Play Mode only if the verification task is assigned runtime smoke.
- Non-goals: no mass prefab migration, no rename of legacy compatible symbols, no new prefab catalogue, no runtime code changes.

### TD3D-T2 — Placement and NavMesh integration gate

- Тип: technical prerequisite, bounded integration verification.
- Player-facing outcome: после принятого существующего tile placement враг получает актуальный route/spawn context.
- Role-agent: gameplay-tester + read-only-gameplay-architect.
- Project-local skill: unity-mcp-skill, test-writing only if a focused regression test is needed.
- Единственный runtime owner: TileMapManager owns committed map/spawn state; TilePlacementSystem owns input/commit request; NavMeshSurfaceWrapper owns NavMesh build; MonsterMove owns agent movement. This task does not change that boundary.
- Existing files/classes: LevelGenerator, MapGenerator, TileMapManager, TilePlacementValidator, TilePlacementSystem, NavMeshSurfaceWrapper, WaveManager, MonsterMove, PlayerBase.
- Data/serialized inputs: Gameplay.unity, four tile prefabs in TileDatabase, NavMeshSurface, spawn points and tile size.
- Callbacks/events/output state: CanPlaceTile, PlaceTile, BuildNavMesh, TileMapManager.SpawnPositions, WaveManager.Initialize; record resulting base/spawn/agent state.
- Dependencies: TD3D-T0 and TD3D-T1.
- Acceptance scenario: place one valid existing tile, rebuild NavMesh, start one bounded wave, observe an enemy spawn at a current open end and reach the base; invalid connection remains rejected.
- Test/smoke evidence: Unity Console clear-before-run, one Play Mode smoke and captured result; any allocator/Console blocker is reported and not bypassed.
- Non-goals: no tactical option UI, route scoring, tower coverage, elevation design, new pathfinding or NavMesh fallback.

## 4. Gameplay task graph and explicit order

TD3D-T0 ─┬─> TD3D-G0 gameplay contract ─> TD3D-G1 run/session
TD3D-T1 ─┘                                  └─> TD3D-G2 preparation/lock
TD3D-T2 ───────────────────────────────────────────────┘

TD3D-G2 ─> TD3D-G3 map choice + route preview ─┐
          └> TD3D-G4 next-wave threat intel ──┴─> TD3D-G5 tower roles/upgrades
                                               └─> TD3D-G6 combat/rewards
TD3D-G5 ────────────────────────────────────────────────┘
TD3D-G6 ─> TD3D-G7 economy ─> TD3D-G8 limited reward offer ─> TD3D-G9 player feedback/UI
TD3D-G9 ─> TD3D-G10 persistence/meta progression ─> TD3D-G11 integration QA + balance

Порядок запуска: gameplay contract → run/session + preparation → map choice + route preview и threat intel → tower roles/upgrades → combat/rewards → economy → limited offer и feedback → persistence → integration QA. G3 и G4 могут быть разработаны параллельно после G2, но обе должны быть приняты до tower-role pass.

## 5. Gameplay task cards

### TD3D-G0 — Gameplay contract для первого run

- Тип: content/design task; эта текущая G0 задача меняет только этот Markdown.
- Player-facing outcome: игроку заранее понятно, что он решает между волнами, как choice меняет следующий combat и чем заканчивается run.
- Role-agent: game-director + read-only-gameplay-architect.
- Project-local skill: нет отдельного authoring skill; используются AGENTS.md, GAMEPLAY_REFERENCES.md и read-only owner inspection.
- Единственный runtime owner: не создаётся; contract закрепляет existing owner map, а не вводит RunManager, RoundManager, EconomyManager, MapManager или SaveManager.
- Existing files/classes: GameplayBootstrap, GameManager, GameState, WaveManager, ResourceManager, LevelGenerator, MapGenerator, TileMapManager, TilePlacementSystem, TilePlacementValidator, TileDatabase, Tower, TowerStatsSO, TowerPlacementSystem, TowerShopUI, IWeapon, Projectile, GameObjectPool, MonsterHealth, MonsterMove, GameHUD, WaveUI, WaveConfig.
- Data/serialized inputs: Assets/Documentation/GAMEPLAY_REFERENCES.md; Gameplay.unity; current Wave_01–03, tower/monster stats, tower/enemy/projectile/tile prefabs.
- Callbacks/events/output state: output is this state/owner/event contract and a task graph; no runtime callbacks are changed.
- Dependencies: TD3D-T0 read-only baseline; no implementation prerequisite.
- Acceptance scenario: reviewer can walk through five wave decisions, identify one owner for every transition and currency/combat outcome, and see a concrete observable consequence for route, intel, offer, tower placement and upgrade.
- Test/smoke evidence: Markdown structure review, owner/file existence checks and convention-aware diff; Unity is not run because runtime/assets are explicitly out of scope.
- Non-goals: no C#, scene, prefab, ScriptableObject, localization, UI or package changes; no copy of reference names, numbers or visual style.

### TD3D-G1 — Run/session state machine

- Тип: runtime task.
- Player-facing outcome: run visibly moves through Boot, MapBuild, Preparation, WaveActive, WaveResolve, Victory, Defeat and can restart without stale temporary state.
- Role-agent: gameplay-systems-programmer; review by read-only-gameplay-architect.
- Project-local skills: unity-mcp-skill, mcp-unity-validate-script, unity-recompile-menuitem only when C# changes require them.
- Единственный runtime owner: GameManager owns transition decisions and terminal state; GameplayBootstrap reports boot/map-build completion; WaveManager reports wave lifecycle; PlayerBase reports defeat. No second state holder.
- Existing files/classes: GameManager.cs, GameState.cs, GameplayBootstrap.cs, WaveManager.cs, PlayerBase.cs, Gameplay.unity state objects.
- Data/serialized inputs: current scene state-object dictionary, existing wave list, bootstrap references and TimeControl pause behavior.
- Callbacks/events/output state: reuse onGameStateChanged, onGameStarted, onGameOver, onVictory, onWaveStarted, onWaveCompleted, onAllWavesCompleted; output is one current run state. Paused remains an overlay over the current state.
- Dependencies: TD3D-G0, TD3D-T0, TD3D-T1; TD3D-T2 before claiming movement integration.
- Acceptance scenario: fresh scene logs/observes Boot → MapBuild → Preparation; start shows WaveActive; last enemy makes exactly one WaveResolve; last configured wave reaches Victory; base destruction reaches Defeat; Restart returns to a clean first run.
- Test/smoke evidence: changed scripts validated, Unity compilation and Console checked, bounded Play Mode smoke of start/resolve/victory or defeat/restart.
- Non-goals: no reward offer, route scoring, tower roles, new persistence owner, new manager, or broad lifecycle refactor.

### TD3D-G2 — Preparation phase, decision order и lock/start

- Тип: runtime task with existing UI command routing.
- Player-facing outcome: between waves the player has a stable decision order; Start Wave is a deliberate lock and all late preparation commands are rejected or disabled.
- Role-agent: gameplay-systems-programmer; UI handoff to ui-designer only for presentation changes.
- Project-local skills: mcp-unity-validate-script, unity-recompile-menuitem; unity-mcp-skill for scene wiring.
- Единственный runtime owner: GameManager owns preparation lock and transition to WaveActive; WaveManager owns spawn after the approved start. GameHUD/WaveUI do not own phase state.
- Existing files/classes: GameManager.cs, WaveManager.cs, WaveUI.cs, GameHUD.cs, TowerPlacementSystem.cs, TilePlacementSystem.cs.
- Data/serialized inputs: current wave index/config, currency, selected reward/tile/tower state and existing start button/input action.
- Callbacks/events/output state: preparation entry via onGameStateChanged(WavePreparing), approved start via existing wave start path, lock output via WaveActive; all offer/tile/build commands must observe the same state.
- Dependencies: TD3D-G1, plus TD3D-T2 for placement lock integration.
- Acceptance scenario: after a wave, intel is visible; a player can resolve reward/tile/build decisions in order; pressing Start once locks the phase, duplicate clicks/inputs do nothing, and a new tower/tile/reward cannot be committed while the wave is active.
- Test/smoke evidence: Play Mode attempt of each late command after lock, Console check, and focused test of one start request; no automatic-start bypass unless explicitly part of the contract.
- Non-goals: no new preparation manager, no second start button owner, no balance tuning, no persistence.

### TD3D-G3 — Tactical tile choice, route preview и firing consequences

- Тип: runtime + content/design task.
- Player-facing outcome: before the wave the player chooses from available valid tiles and rotations, sees route and firing consequences, and understands spawn/NavMesh impact before committing.
- Role-agent: gameplay-designer for authored options/decision readability + gameplay-systems-programmer for owner-side runtime; gameplay-tester review.
- Project-local skills: unity-mcp-skill, mcp-unity-validate-script; prefab-creation only if an existing tile prefab must be authored.
- Единственный runtime owner: TileMapManager owns committed tile/map/spawn state; TilePlacementValidator owns connection validity; TilePlacementSystem owns preview/input/commit request; TileDatabase owns available tile prefabs; NavMeshSurfaceWrapper remains the build owner.
- Existing files/classes: WaveManager.cs, LevelGenerator.cs, MapGenerator.cs, TileMapManager.cs, TilePlacementSystem.cs, TilePlacementValidator.cs, TileDatabase.cs, RoadTileDef.cs, RoadTileComponent.cs, NavMeshSurfaceWrapper.cs, Tower.cs.
- Data/serialized inputs: four current tile prefabs and connection masks, availableTiles, grid/tile size, placed map, base position, spawn positions, tower transforms/ranges. Height must be an explicit authored input in existing tile/tower data; do not infer a hidden bonus from the flat GridToWorld baseline.
- Callbacks/events/output state: StartTilePlacement, RotateTile, CanPlaceTile, PlaceTile, BuildNavMesh, TileMapManager.GetAllTiles, SpawnPositions; new output must expose selected option, validity reason, route delta, spawn delta and coverage/elevation delta through the existing map/UI command path.
- Dependencies: TD3D-G2 and TD3D-T2.
- Acceptance scenario: preparation presents at least three valid option/rotation combinations when the current map allows them; invalid connection is visibly rejected; hovering each valid option shows route length/direction, open spawn ends and affected tower coverage/elevation; committing one option updates map state, rebuilds NavMesh and makes the next wave use the new spawn/route.
- Test/smoke evidence: validator tests for occupied/mismatched/disconnected cells, Unity preview inspection, one commit → NavMesh rebuild → bounded enemy movement smoke; no acceptance from a random tile log alone.
- Non-goals: no second map/pathfinding manager, no arbitrary route teleport, no placement of invalid disconnected loops, no tower economy changes, no global NavMesh rewrite.

### TD3D-G4 — Next-wave threat intel and enemy roles

- Тип: content/design + runtime presentation task.
- Player-facing outcome: before spending, the player sees next-wave composition, role traits and one readable defensive implication.
- Role-agent: gameplay-designer owns role vocabulary/content; gameplay-systems-programmer owns data exposure; ui-designer owns the final readable surface.
- Project-local skills: mcp-unity-validate-script for data/runtime changes; ui-prefab-authoring/ui-prefab-localization only for assigned UI implementation.
- Единственный runtime owner: WaveManager owns which WaveConfig is next and provides the same data used for spawn; WaveConfig/EnemySpawnData remain the data source; GameHUD/WaveUI present it.
- Existing files/classes: WaveConfig.cs, EnemySpawnData, WaveManager.cs, MonsterStatsSO.cs, MonsterStats.cs, MonsterHealth.cs, MonsterMove.cs, enemy prefabs, GameHUD.cs, WaveUI.cs.
- Data/serialized inputs: current Wave_01–03 composition, enemy prefab/stat links, count, health/speed multipliers and completion reward; role labels/traits must be authored against existing prefab/stat data, not a separate enemy database.
- Callbacks/events/output state: next-wave read from WaveManager before StartNextWave; onWaveStarted switches from intel to active display; output lists counts/roles/trait and one defensive implication without changing spawn truth.
- Dependencies: TD3D-G2; TD3D-T1 for prefab identity; G3 is needed before claiming route-specific implication.
- Acceptance scenario: Wave 2/3 intel identifies at least two distinct current enemy groups and explains a different defensive response; changing only the WaveConfig composition changes the displayed intel and the actual spawn groups; no intel remains after terminal state.
- Test/smoke evidence: data-to-spawn verification, UI inspection before start, bounded run with two compositions and Console check.
- Non-goals: no broad enemy taxonomy, no new enemy manager, no hidden resistances/abilities, no manual combat skill system, no duplicated wave data.

### TD3D-G5 — Tower catalogue, roles, placement cell, elevation/coverage и upgrades

- Тип: content/design + runtime placement task.
- Player-facing outcome: the shop offers a small readable catalogue of distinct tower roles; the player can compare cost/coverage and see upgrade stat changes before committing to a cell.
- Role-agent: gameplay-designer owns roles and tuning; gameplay-systems-programmer owns placement/upgrade runtime; ui-designer owns shop/stat presentation; prefab-creation only when prefab authoring is assigned.
- Project-local skills: prefab-creation for actual prefab changes; mcp-unity-validate-script and unity-recompile-menuitem for C# changes; ui-prefab-authoring only for UI layout.
- Единственный runtime owner: Tower owns live tower stats, target/attack and upgrade; TowerPlacementSystem owns placement command; TowerShopUI owns catalogue presentation/selection; TowerStatsSO is the numeric data source.
- Existing files/classes: Tower.cs, TowerStats.cs, TowerStatsSO.cs, TowerPlacementSystem.cs, TowerShopUI.cs, TowerPreviewGenerator.cs, tower prefabs, ResourceManager.cs, tile/map owners.
- Data/serialized inputs: the three current TowerStatsSO assets and selected tower prefabs; Cost, UpgradeCost, damage/rate/range/projectile/rotation stats, weapon links, fire points, tile cells and any explicitly authored height/elevation input.
- Callbacks/events/output state: BeginPlacement, placement validity/commit, ResourceManager.TrySpend, Tower.UpgradeSpendingCost, Tower.onTargetAcquired, onFire, TowerStats recalculation; output is placed tower, occupied cell, coverage/elevation readout and before/after stats.
- Dependencies: TD3D-G3, TD3D-G4, TD3D-G7 economy contract may be designed first but runtime spending integration follows G7; TD3D-T1.
- Acceptance scenario: at least two tower choices have different defensive implications against the intel; one valid cell and one blocked/occupied cell are visibly distinct; placing or upgrading spends exactly one cost, changes the live stats, and updates displayed range/coverage/elevation; unaffordable action leaves state unchanged.
- Test/smoke evidence: asset/prefab inspection, affordability and upgrade boundary tests, one Play Mode placement/upgrade smoke with tower firing; no claim based only on preview textures.
- Non-goals: no new tower manager, no parallel tower state, no arbitrary free placement outside existing map owner, no full content catalogue.

### TD3D-G6 — Combat and reward chain without duplicate owners

- Тип: runtime task.
- Player-facing outcome: a tower visibly kills a monster through one deterministic chain and awards the intended reward once; a monster reaching the base damages the base once and resolves once.
- Role-agent: gameplay-systems-programmer; review by read-only-gameplay-architect and gameplay-tester.
- Project-local skills: mcp-unity-validate-script, unity-recompile-menuitem, test-writing, and unity-mcp-skill for bounded Play Mode.
- Единственный runtime owner: Tower target/fire; IWeapon/configured weapon delivery; Projectile impact; MonsterHealth damage/death/reward; MonsterMove movement/base-hit; WaveManager alive/wave resolve; ResourceManager currency mutation.
- Existing files/classes: Tower.cs, IWeapon.cs, weapon implementations, Projectile.cs, GameObjectPool.cs, MonsterHealth.cs, MonsterMove.cs, PlayerBase.cs, WaveManager.cs, ResourceManager.cs, projectile/tower/enemy prefabs.
- Data/serialized inputs: TowerStatsSO damage/fire rate/projectile speed, weapon/projectile mode and area settings, MonsterStatsSO health/damage/reward, WaveConfig multipliers, prefab colliders and pool source.
- Callbacks/events/output state: Tower.onFire → weapon/projectile launch → one MonsterHealth.TakeDamage → one onDeath/reward outcome → one WaveManager.onEnemyKilled; base collision → one PlayerBase.TakeDamage and one resolve path. MonsterMove must not manually invoke a second health death event.
- Dependencies: TD3D-G5, TD3D-G7 contract can be integrated after chain owner is stable; TD3D-T1/T2.
- Acceptance scenario: one projectile reduces one target and returns to pool; lethal hit produces exactly one death, one reward and one alive decrement; area damage does not double-hit a collider; base contact damages base once and does not create a reward/death duplicate; zero enemies resolves the wave once.
- Test/smoke evidence: focused damage/death/reward tests, changed-script validation/recompile/Console, one bounded Play Mode tower-versus-enemy and enemy-versus-base smoke.
- Non-goals: no second projectile pool, no new damage manager, no replacement combat framework, no unrequested damage formula migration, no manual abilities.

### TD3D-G7 — Economy ledger, affordability and competing spends

- Тип: runtime + balance task.
- Player-facing outcome: player always sees current currency and the exact cost/reason for tower, upgrade, tile or reward spend; competing choices cannot overspend or silently consume currency.
- Role-agent: gameplay-systems-programmer owns ledger boundary; gameplay-designer tunes costs/rewards; gameplay-tester verifies edge cases.
- Project-local skills: mcp-unity-validate-script, test-writing; unity-mcp-skill for serialized tuning verification.
- Единственный runtime owner: ResourceManager owns current currency, affordability, spend/gain and source-aware ledger output; TowerPlacementSystem, Tower, map/reward owners request spends and do not keep currency copies.
- Existing files/classes: ResourceManager.cs, WaveManager.cs, TowerPlacementSystem.cs, Tower.cs, TowerStatsSO.cs, WaveConfig.cs, MonsterStatsSO.cs, future offer data stored with the accepted existing owner.
- Data/serialized inputs: starting currency 500, current tower costs/upgrade curves, current kill rewards, wave completion rewards 50/75/100, passive income 100 and any tile/offer cost contract.
- Callbacks/events/output state: CanAfford, TrySpend, AddCurrency, GivePassiveIncome, onCurrencyChanged, onCurrencyGained, onCurrencySpent; output includes accepted/rejected amount and source/reason without a second ledger.
- Dependencies: TD3D-G6 for reliable reward sources; TD3D-G5 for tower spend callers; TD3D-G2 for preparation lock.
- Acceptance scenario: with one shared balance, buying a tower then an upgrade accepts only affordable actions; a simultaneous/duplicate request cannot spend twice; kill/wave/passive gains are each visible once; rejected action leaves balance and live state unchanged.
- Test/smoke evidence: exact boundary tests at 0/exact/insufficient/max-upgrade, Play Mode sequence with competing tower/upgrade spend, Console check.
- Non-goals: no production-building economy, no second currency, no economy manager, no meta currency or save format.

### TD3D-G8 — Limited reward offer with one-time application

- Тип: content/design + runtime task.
- Player-facing outcome: during preparation the player chooses one of a small number of mutually exclusive rewards; the chosen effect applies once to this run and unchosen options cannot apply later.
- Role-agent: gameplay-designer defines choices/risk/reward; gameplay-systems-programmer implements application; ui-designer presents the offer.
- Project-local skills: mcp-unity-validate-script; ui-prefab-authoring/ui-prefab-localization only for assigned offer UI; test-writing for one-time application proof.
- Единственный runtime owner: WaveManager owns when the inter-wave offer is opened/closed; the accepted existing run owner for the effect is the affected ResourceManager, Tower/TowerStatsSO runtime value or map/tile owner. Do not create a deckbuilder or reward manager.
- Existing files/classes: WaveManager.cs, GameManager.cs, ResourceManager.cs, Tower.cs, TowerStats.cs, TowerStatsSO.cs, TileMapManager.cs, GameHUD.cs and current serialized content assets.
- Data/serialized inputs: three authored offer definitions or serialized choice entries attached to the existing reward owner; each must state display text, eligibility, one-time effect and observable consequence. No external card database.
- Callbacks/events/output state: wave resolve opens offer; select validates preparation state and eligibility; one accepted choice mutates one owner; close/lock clears offer; repeated select/reopen is rejected; next-wave state exposes applied effect.
- Dependencies: TD3D-G2, TD3D-G7, TD3D-G5 for stat effects and TD3D-G3/G4 for map/intel choices.
- Acceptance scenario: after a completed wave three offers are readable, exactly one can be selected, its effect is visible before Start Wave and remains for the intended run scope; a second click, restart or later wave cannot reapply the same one-time choice.
- Test/smoke evidence: one-time application test, offer UI state inspection, bounded Play Mode selection → next wave consequence; no acceptance from a static card screenshot.
- Non-goals: no hundreds of cards, no random offer hidden from the player, no permanent unlocks, no second reward owner, no manual battle skill system.

### TD3D-G9 — Player feedback/UI for route, intel, offer, placement and stats

- Тип: UI implementation task, driven by accepted gameplay outputs.
- Player-facing outcome: every important preparation choice has an exact readable output: route preview, next-wave roles, offer effect, placement validity/coverage/elevation, affordability and before/after tower stats; active/resolve/terminal states are unambiguous.
- Role-agent: ui-designer; gameplay requirement supplied by gameplay-designer; runtime command review by gameplay-systems-programmer.
- Project-local skills: ui-prefab-authoring, ui-prefab-localization, unity-mcp-skill; mcp-unity-validate-script/unity-recompile-menuitem only if UI C# changes.
- Единственный runtime owner: UI presents GameManager, WaveManager, ResourceManager, TileMapManager, Tower and MonsterHealth outputs; it does not calculate gameplay truth. Consolidate duplicate start-wave presentation between GameHUD and WaveUI without creating a third controller.
- Existing files/classes: GameHUD.cs, WaveUI.cs, TowerShopUI.cs, existing scene Canvas/panels/buttons/text, current localization bindings, GameManager, WaveManager, ResourceManager, TilePlacementSystem, TowerPlacementSystem, Tower.
- Data/serialized inputs: existing HUD references, start/restart/quit buttons, currency/wave/base fields, shop button prefab, current localization tables and newly accepted output fields from G3–G8.
- Callbacks/events/output state: existing currency/wave/base/game events plus accepted owner outputs; UI state mirrors Boot/MapBuild/Preparation/WaveActive/WaveResolve/Victory/Defeat and command buttons route to owner commands.
- Dependencies: TD3D-G3, G4, G5, G7, G8 and stable G1/G2 state contract.
- Acceptance scenario: before a wave, one screen scan answers “what comes, what can I choose, what will it change, what can I afford, where can I build, and when is Start locked”; during wave the same controls cannot issue preparation commands; terminal screen offers Restart and shows outcome.
- Test/smoke evidence: Unity prefab/scene inspection, localization check, UI interaction smoke in Preparation/Active/Terminal, Console check; no UI-only fake values.
- Non-goals: no gameplay logic in UI, no new UI manager/service, no broad visual redesign unrelated to the contract.

### TD3D-G10 — Persistence и meta progression после stable in-run loop

- Тип: late runtime/content task; blocked until the in-run contract is accepted.
- Player-facing outcome: после завершённого run результат и небольшой documented unlock сохраняются, а temporary currency/tile/reward state не протекает в новый run.
- Role-agent: gameplay-systems-programmer + gameplay-tester; read-only-gameplay-architect reviews the boundary.
- Project-local skill: test-writing and unity-mcp-skill; no persistence skill is assumed because this checkout currently has no persistence owner.
- Единственный runtime owner: GameManager owns run lifecycle/result boundary and invokes one selected project-owned persistence path after the choice is explicitly designed; no SaveManager is created by default. ResourceManager, WaveManager, map and towers expose snapshot data but do not write separate saves.
- Existing files/classes: GameManager.cs, WaveManager.cs, ResourceManager.cs, Tower.cs/TowerStats.cs, TileMapManager.cs, accepted offer/tower/map data owners and GameState.cs.
- Data/serialized inputs: run result, completed wave, selected unlock/modifier and versioned DTO/state boundary chosen only after G1–G9; temporary runtime state must be reset on Restart.
- Callbacks/events/output state: Victory/Defeat closes run; one result write/read path; new run starts from Boot with starting currency and no prior temporary placements/rewards; load never mutates ScriptableObject source assets.
- Dependencies: TD3D-G9 and successful TD3D-G11 preflight of stable in-run loop; all earlier G1–G8 accepted.
- Acceptance scenario: complete a run, restart application/run, observe exactly one documented unlock; start another run and observe fresh temporary state; defeat does not grant victory unlock; repeated load does not duplicate currency/towers/rewards.
- Test/smoke evidence: focused persistence tests, restart/load smoke, Console and serialized asset immutability check.
- Non-goals: no meta tree, no automatic cloud/external save integration, no persistence before stable combat/economy, no new manager merely to match a familiar name.

### TD3D-G11 — Integration QA, regression and balance scenarios

- Тип: verification + content/balance task.
- Player-facing outcome: a fresh run presents meaningful choices, remains readable, has no duplicate rewards/deaths/spends, and reaches deterministic Victory/Defeat/Restart outcomes.
- Role-agent: gameplay-tester owns evidence; gameplay-designer owns balance scenarios; project-auditor reviews resource/serialized scope when needed.
- Project-local skills: test-writing, unity-mcp-skill; mcp-unity-validate-script only for changed test/runtime scripts.
- Единственный runtime owner: no new owner; QA exercises the accepted owners and reports the exact failing owner/task.
- Existing files/classes: full accepted chain from GameplayBootstrap/GameManager through map, wave, tower, weapon, monster, currency, offer and UI owners; Gameplay.unity; current wave/tower/monster/tile/projectile assets.
- Data/serialized inputs: Wave_01–03, current tower/monster stats, four tile prefabs, scene wiring, selected offer content and any accepted persistence state.
- Callbacks/events/output state: verify state sequence, one start/resolve, one death/reward, one base-hit, one spend/gain, one offer application, current route/spawns and terminal events.
- Dependencies: all G1–G10, with G10 optional only if persistence was accepted; T0–T2 must be closed or explicitly reported as blockers.
- Acceptance scenarios: (a) fresh run through several waves with intel → offer → tile → tower decision; (b) invalid tile/occupied cell; (c) insufficient funds and competing spend; (d) mixed enemy composition and route preview; (e) tower kill/reward exactly once; (f) enemy reaches base exactly once; (g) victory, defeat and restart; (h) persistence scenario if G10 is in scope.
- Test/smoke evidence: named Unity tests, Console clean result, bounded Play Mode evidence and a balance table recording starting currency, costs, rewards, wave pressure, route consequence and next content changes. total=0 is not success.
- Non-goals: no opportunistic refactor, no package/vendor/sample cleanup, no new feature expansion during QA, no declaring success from compilation alone.

## 6. Runtime versus content/design ownership

Runtime tasks: TD3D-G1, G2, G3 runtime half, G4 data exposure half, G5 runtime half, G6, G7, G8 application half, G9 command/presentation integration, G10 persistence boundary, and verification hooks in G11.

Content/design tasks: TD3D-G0, authored WaveConfig role/intel entries in G4, tower role catalogue and tuning in G5, offer definitions and effects in G8, UI information architecture in G9, balance scenarios in G11.

Technical prerequisites, not gameplay systems: TD3D-T0 owner/serialization audit, TD3D-T1 prefab component identity verification, TD3D-T2 placement/NavMesh integration gate.

## 7. Review gates

- G0 gate: design slice, loaded docs, inspected owners/assets, classified gaps and task graph are complete; only plan file changed.
- T0/T1/T2 gate: each technical uncertainty has exact evidence and an owner; no raw serialized repair or fallback is accepted.
- G1/G2 gate: one GameManager transition owner and one preparation lock; no UI or WaveManager bypass.
- G3/G4 gate: map choice and intel use the same data that drives spawn and placement; every choice has readable consequence.
- G5/G6/G7 gate: tower, combat and economy have one owner each; death/reward/base-hit/spend are exactly-once outcomes.
- G8/G9 gate: offer applies once and UI presents real owner state without gameplay logic.
- G10 gate: no persistence work starts until in-run QA passes.
- G11 gate: report confirmed checks separately from assumptions and keep unresolved gaps linked to their task IDs.

## 8. Explicit non-goals for this plan

- No second GameManager, WaveManager, map manager, economy manager, reward manager, projectile pool, pathfinding system or save manager.
- No Outcasts namespaces, documents, paths, domain assumptions or external package requirements.
- No raw editing of .unity, .prefab, .asset or .meta; future Unity-owned changes go through the project workflow in AGENTS.md and the task card skill.
- No sprite-generation work or rendering-system expansion.
- No large meta tree, faction layer, manual hero/skill layer, production-building economy or hundreds of offers before the in-run loop is stable.
- No use of old plan statuses as implementation proof; every future task rechecks live code, scene, prefabs, serialized assets and Unity Console/Play Mode evidence.
