---
title: Геймплейные референсы Prototype Tower Defence 3D
type: геймплейный референс и база будущего плана задач
status: active
updated: 2026-08-03
scope: Rogue Tower, Tower Dominion, Citadelic
---

## Правило реализации

- Никакого fallback: если основной путь недоступен или не сработал, остановиться и сообщить о блокере; обходной или запасной путь использовать только по явному запросу.

# Геймплейные референсы: Rogue Tower, Tower Dominion, Citadelic

## 1. Назначение

Документ фиксирует направление следующих геймплейных проходов Prototype Tower Defence 3D.

Референсы используются для механик и темпа, а не для копирования названий, визуального стиля, текста, числового баланса или контента. Текущий код, сцена, prefab и сериализованные ассеты остаются источником истины реализации. Этот документ задает design intent, которому должны соответствовать будущие задачи.

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
   - выбрать limited reward или run modifier;
   - разместить или улучшить башни;
   - выбрать и разместить valid terrain tile, если фаза доступна;
   - увидеть новый маршрут и firing coverage.
4. Запустить волну.
5. Башни автоматически выбирают цели и атакуют; враги идут по маршруту и наносят урон базе.
6. Выдать kill и wave rewards.
7. Переоценить следующую угрозу и повторить loop.
8. При victory или defeat записать результат забега. Persistent unlocks и optional modifiers - поздние milestones.

На первом проходе combat остается real-time и автоматическим. Глубина строится на preparation, placement, path shaping, threat information и limited rewards.

## 6. Gap assessment

| Паттерн референсов | Факты текущего проекта | Оценка | Направление |
|---|---|---|---|
| Preparation и active attack разделены | GameManager имеет WavePreparing/WaveActive, WaveManager ждет запуска волны | Есть, но thin | Сделать preparation значимой через reward, intel и placement decision |
| Карта является частью билда | Есть LevelGenerator, TileMapManager, TilePlacementSystem; WaveManager запускает tile phase после completion | Есть, но thin | Предлагать несколько валидных тайлов и показывать последствия маршрута |
| Path shaping влияет на стратегию | TilePlacementValidator сохраняет connection rules, но текущая tile phase выбирает один random prefab | Partial | Сохранить validation, добавить choice и preview маршрута/coverage |
| Limited run rewards | Есть kill reward, completion reward, passive income и currency для башен/upgrades | Missing как выбор | Добавить inter-wave offer во владельца текущего loop |
| Enemy counterplay | WaveConfig задает composition, health, speed и count; HUD показывает текущую wave/enemies | Thin | Показать будущий composition и ввести читаемые enemy roles |
| Tower identities и synergies | Есть Tower, TowerStatsSO, IWeapon и upgrade grades | Есть, но не reference-shaped | Сначала авторить небольшой набор distinct roles |
| Economy tension | ResourceManager владеет одной currency, kill rewards, wave reward и passive income | Thin | Сначала настроить конкуренцию между spend decisions; production buildings отложить |
| Persistent roguelite progression | WaveManager умеет in-run loop и difficulty scaling | Missing между забегами | Отложить до стабильного run loop и выбора save owner |
| Active battle skills | В текущей цепочке нет обязательного владельца | Deferred | Не блокировать первый milestone ручными abilities |
| Analytics и challenge modifiers | Gameplay contract не зафиксирован | Deferred | Добавить после первого сравнимого и сбалансированного забега |

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
- Acceptance: completed wave оставляет игру в preparation, offer понятен, применяется ровно один reward, следующая wave использует измененное value или availability; Play Mode smoke сохраняет victory/defeat.

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
- Acceptance: fresh run проходит несколько waves без Console errors, до каждой wave есть documented decision, map expansion остается valid, victory/defeat/restart работают.

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
