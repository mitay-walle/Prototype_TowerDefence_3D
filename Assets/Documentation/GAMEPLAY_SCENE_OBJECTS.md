---
title: Объекты и поведение целевой gameplay-сцены
type: hypothetical gameplay scene contract
status: design-target
updated: 2026-08-04
scope: scene objects, data contexts, behavior variants, ownership
source: Assets/Documentation/GAMEPLAY_REFERENCES.md
---

# Объекты и поведение целевой gameplay-сцены

## 1. Статус документа

Это целевой контракт гипотетической gameplay-сцены, а не утверждение о том, что всё перечисленное уже реализовано. Текущие код, `Gameplay.unity`, prefab и ScriptableObject остаются источниками истины о фактическом состоянии проекта.

Документ задаёт минимальную композицию сцены, ответственность объектов, их связи с данными и допустимые варианты развития. Название роли не означает, что под неё обязательно нужен отдельный `Manager`, `Service`, `Context` или `GameObject`: если действующий владелец уже существует, поведение расширяется в нём.

Правила:

- один владелец изменяемого состояния;
- один публичный entry point на каждое действие игрока или переход игрового цикла;
- Definition неизменяемы во время забега;
- RuntimeState изменяется только gameplay-владельцами;
- UI, VFX и SFX читают состояние и события, но не решают исход gameplay;
- обязательная Definition или ссылка не подменяется скрытым fallback;
- derived cache пересчитывается и обычно не сохраняется.

Метки вариантов:

- **Basic** — минимальный законченный loop;
- **Extended** — следующий слой вариативности без смены архитектурного владельца;
- **Deferred** — дорогое или рискованное расширение, которому нужен отдельный контракт.

## 2. Контексты данных

| Контекст | Время жизни | Содержимое | Где живёт |
| --- | --- | --- | --- |
| Definition | Между версиями контента | Статы башен и врагов, волны, тайлы, типы урона, ауры, награды, цены | ScriptableObject, prefab, Resources; позднее Addressables и мод-каталоги |
| ProfileSave | Между забегами | Мета-валюта, unlock, прогресс, настройки сложности | SaveService вне gameplay-сцены |
| RunSave | Один забег | Seed, карта, текущая волна, экономика, база, башни, выбранные награды | SaveService; рекомендованная граница — между волнами |
| RuntimeState | Только загруженный забег | Текущие HP, shield, cooldown, активные враги, цели, временные эффекты | Владельцы gameplay в памяти |
| Command | Один вызов | `StartWave`, `PlaceTower`, `BuyUpgrade`, `ChooseReward`, `PlaceTile` | UI/input → публичный метод владельца |
| Event | Мгновенное уведомление | Урон, смерть, утечка, покупка, смена состояния, завершение волны | Владелец → UI/VFX/SFX/analytics |
| ReadModel | Пока открыт экран | Деньги, прогресс волны, выбранная башня, доступность кнопки | Проекция владельцев для UI |
| DerivedCache | Пока валиден источник | NavMesh, spatial index, списки целей, агрегированные статы, path length | Пересчитываемый runtime-кэш |

Definition ссылается на другие Definition стабильными ID или прямыми asset-ссылками. Save хранит ID и числовое состояние, но не ссылки на `GameObject`, `MonoBehaviour`, material instance или runtime-цель.

## 3. Целевая иерархия сцены

```text
Gameplay
├── SceneComposition
│   └── GameplayBootstrap
├── GameLoop
│   ├── GameManager
│   ├── WaveManager
│   ├── ResourceManager
│   └── RewardOffer
├── Level
│   ├── LevelGenerator
│   ├── TileMap
│   ├── TilePlacement
│   ├── NavMesh
│   ├── SpawnPoints
│   └── PlayerBase
├── Actors
│   ├── Towers
│   ├── Enemies
│   ├── Projectiles
│   └── Pools
├── Interaction
│   ├── TowerPlacement
│   ├── Selection
│   └── PlacementPreview
├── Presentation
│   ├── CameraRig
│   ├── Canvas
│   ├── EventSystem
│   ├── WorldVFX
│   ├── WorldSFX
│   └── Music
└── Environment
    ├── DirectionalLight
    ├── GlobalVolume
    ├── Ground
    └── Decoration
```

`ContentCatalog`, `SaveService`, `Profile/Meta`, `Settings`, `Localization`, `SceneFlow` и `ModLoader` относятся к application-слою. Они могут переживать сцену, но не должны дублироваться внутри `Gameplay`.

## 4. Application-граница

### 4.1 ContentCatalog

**Данные:** все Definition и стабильные content ID.

**Поведение:** загружает и проверяет обязательный набор контента до создания забега; отдаёт Definition по ID; не хранит состояние экземпляров.

**Варианты:**

- Basic — сериализованные ссылки и существующие `Resources` (`WaveConfigs`, `TowerStats`, `TileDefs`, `TagDB`);
- Extended — Addressables-каталоги с тем же ID-контрактом;
- Deferred — базовый каталог плюс упорядоченные mod-каталоги, version/dependency check и конфликт-политика.

Отсутствие обязательной Definition блокирует загрузку с явной ошибкой. Автоматическая замена «похожим» контентом запрещена.

### 4.2 SaveService

**Данные:** `ProfileSave`, `RunSave`, версии схемы и миграции.

**Поведение:** сериализует snapshot, проверяет версию, атомарно записывает слот, загружает данные до входа в gameplay. Не решает, сколько денег, HP или наград должно быть у игрока.

**Варианты:**

- Basic — один локальный слот забега между волнами;
- Extended — несколько слотов, autosave и ручной save в разрешённых состояниях;
- Deferred — mid-wave snapshot и cloud conflict resolution.

### 4.3 Profile/Meta

**Данные:** мета-валюта, unlock ID, постоянные уровни, выбранная сложность.

**Поведение:** применяет награду завершённого забега и формирует стартовый набор разрешённого контента. Не выдаёт ресурсы посреди волны напрямую.

**Варианты:** Basic — одна валюта и горизонтальные unlock; Extended — стартовые модификаторы и difficulty tiers; Deferred — сезоны, ресеты и live-экономика.

### 4.4 SceneFlow, Settings, Localization, ModLoader

- `SceneFlow` выбирает новый забег или загрузку и передаёт запрос `GameplayBootstrap`.
- `Settings` предоставляет громкость, графику, управление и accessibility; gameplay читает только нужные параметры.
- `Localization` разрешает ключи Definition и UI, но не содержит gameplay-правил.
- `ModLoader` только собирает и валидирует каталоги до старта; runtime-объекты не ищут моды самостоятельно.

## 5. SceneComposition и single entry point

### 5.1 SceneComposition

**Данные:** сериализованные ссылки сцены и application-зависимости.

**Поведение:** связывает существующих владельцев и передаёт их в `GameplayBootstrap`. Здесь нет правил волн, урона, наград или экономики.

**Варианты:** Basic — явные inspector-ссылки; Extended — существующий BehaviourInject context; Deferred — отдельный session scope, только если реально нужны несколько одновременных сессий.

### 5.2 GameplayBootstrap

Это единственный entry point gameplay-сцены.

**Вход:** `StartNewRun(seed, loadout)` или `ContinueRun(runSnapshot)` от `SceneFlow`.

**Порядок:**

1. проверить application-зависимости и Definition;
2. создать или восстановить чистое состояние забега;
3. собрать карту;
4. построить NavMesh и spawn anchors;
5. создать базу и сохранённые башни;
6. инициализировать `ResourceManager`, `WaveManager`, `GameManager`;
7. опубликовать готовые read models;
8. передать управление `GameManager.EnterPreparation()`.

**Варианты:** Basic — новый забег; Extended — продолжение между волнами; Deferred — mid-wave restore. Частичная инициализация не считается успешным стартом и не запускает параллельную цепочку.

## 6. GameLoop

### 6.1 GameManager

**Данные:** `GameState`, индекс этапа, terminal result, ссылки на текущих владельцев.

**Поведение:** единственный владелец переходов
`Boot → MapBuild → Preparation → WaveActive → WaveResolve → Victory/Defeat`.

**Входы:** `EnterPreparation`, `StartNextWave`, `NotifyWaveResolved`, `NotifyBaseDestroyed`.

**Выходы:** событие смены состояния, разрешённые команды, запрос сохранения на безопасной границе.

**Варианты:** Basic — линейная последовательность волн; Extended — развилка награда/перестройка перед следующей волной; Deferred — endless и mutator states. Новый вариант добавляется в эту машину, а не в соседний `RunManager`.

### 6.2 WaveManager

**Данные:** `WaveDefinition`, runtime spawn cursor, alive count, elapsed time, lane state.

**Поведение:** по команде `GameManager` запускает расписание, создаёт врагов через фабрику/pool, учитывает живых и завершает волну только когда расписание исчерпано и живых врагов нет.

**Варианты:**

- Basic — authored список групп: тип, количество, интервал, задержка, spawn point;
- Extended — scaling от номера волны и несколько lane;
- Deferred — генерация по threat budget и адаптивный director.

Публичный запуск один. UI и spawn point не могут запускать собственную волну.

### 6.3 ResourceManager

**Данные:** текущая валюта забега, ledger причин изменения, run-модификаторы дохода.

**Поведение:** `CanAfford`, `TrySpend`, `Grant`; все расходы атомарны. Доход начисляется за подтверждённую смерть, завершение волны и явно описанные источники.

**Экономика одной волны:** стартовый банк → расходы на подготовку → kill income → опциональный пассивный/производственный доход → completion reward.

**Экономика забега:** бюджет переносится между волнами; цена усиления конкурирует с резервом на карту и новые башни; кривые дохода и стоимости задаются Definition.

**Варианты:** Basic — одна валюта, kill и wave reward; Extended — bounty, production и активные способности; Deferred — interest/debt. Несколько валют добавляются только при разной функции, а не как переименование одного числа.

### 6.4 RewardOffer

**Данные:** reward pool Definition, seed/random state, уже выбранные награды, текущий offer.

**Поведение:** после `WaveResolve` формирует допустимые варианты, показывает их через UI и применяет выбранный эффект через реального владельца: unlock в run state, деньги через `ResourceManager`, upgrade через башню/каталог.

**Варианты:** Basic — выбор одного из трёх; Extended — reroll/banish и rarity; Deferred — deckbuilding. Сам объект не становится универсальным хранилищем всех бонусов.

## 7. Level

### 7.1 LevelGenerator

**Данные:** seed, `TileDefinition`, правила размера и связности, сохранённая раскладка.

**Поведение:** создаёт стартовую карту или восстанавливает точную раскладку; передаёт клетки `TileMap`; не управляет башнями и волнами.

**Варианты:** Basic — процедурный стартовый путь; Extended — authored старт плюс выбор расширений; Deferred — biome rules и многоуровневая карта.

### 7.2 TileMap

**Данные:** координаты, занятость, orientation, socket/edge, route graph, buildable flags.

**Поведение:** источник истины по размещённым тайлам и топологии пути; проверяет запрос изменения через `TilePlacementValidator`; после подтверждения инвалидирует path/NavMesh cache.

**Варианты:** Basic — один связный путь; Extended — ветвления и слияния; Deferred — мосты, высота, переключаемые маршруты.

### 7.3 TilePlacement

**Данные:** доступные варианты тайла, выбранная клетка и rotation, цена, validation result.

**Поведение:** в `Preparation` ведёт цикл выбрать → preview → проверить → подтвердить → при необходимости списать цену → изменить `TileMap` → запросить rebuild.

**Варианты:** Basic — один обязательный бесплатный тайл между волнами; Extended — платные дополнительные тайлы и несколько предложений; Deferred — перестройка ранее уложенных тайлов. Недопустимое размещение не меняет деньги и карту.

### 7.4 NavMesh

**Данные:** геометрия подтверждённой карты, bake settings, результат достижимости spawn-to-base.

**Поведение:** строится после пакетного изменения карты; сообщает готовность bootstrap/game loop; валидирует существование маршрута.

**Варианты:** Basic — полный rebuild в конце MapBuild/Preparation; Extended — отдельные поверхности по lane; Deferred — incremental update, только после профилирования. Preview не запускает bake.

### 7.5 SpawnPoints

**Данные:** стабильный ID, lane ID, transform anchor, активность, доступные enemy tags.

**Поведение:** предоставляют позицию и направление `WaveManager`; не создают врагов сами и не считают прогресс волны.

**Варианты:** Basic — один открытый конец; Extended — несколько lane; Deferred — временно заблокированные или усиленные точки.

### 7.6 PlayerBase

**Данные:** base Definition, max/current HP, shield, временные эффекты, stable save ID.

**Поведение:** принимает leak/damage, публикует изменение состояния и ровно один раз сообщает `GameManager` об уничтожении.

**Варианты:** Basic — HP и фиксированный урон за утечку; Extended — ремонт между волнами; Deferred — shield, несколько модулей базы и разные leak profiles.

## 8. Actors

### 8.1 Towers container и Tower instance

Контейнер `Towers` только организует и регистрирует экземпляры; gameplay принадлежит каждому `Tower` и общим владельцам данных.

**Данные Tower:** `TowerDefinition/TowerStatsSO`, weapon Definition, runtime level, cooldown, текущая цель, агрегированные статы, applied effects, save ID и позиция на карте.

**Поведение:** периодически получает допустимые цели, фильтрует range/line-of-sight/tags, выбирает цель по policy, вызывает `IWeapon`, принимает upgrade и пересчитывает derived stats.

**Target policy:** first, last, nearest, strongest, weakest, lowest shield, marked target. Переключение policy — команда башне, а не логика UI.

**Роли:**

- Basic — direct damage, splash, slow/support;
- Extended — armor breaker, shield breaker, aura, chain, damage-over-time;
- Deferred — economy tower, summon, route manipulation.

**Upgrade:** Basic — линейные уровни; Extended — взаимоисключающие ветки; Deferred — временные in-wave overcharge. Цена проходит через `ResourceManager`, а эффект применяет `Tower`.

### 8.2 Weapon

**Данные:** damage, damage type, attack interval, range, projectile speed, radius, pierce, status payload, VFX/SFX cue ID.

**Поведение:** получает валидную цель от `Tower`, формирует единый `DamagePacket` и доставляет его выбранным способом. Результат урона считает получатель/сервис резолва, а не визуальный projectile.

**Варианты:** hitscan, projectile, beam, chain, splash, aura pulse. Все используют общий damage/effect contract; новый способ доставки не создаёт второй health pipeline.

### 8.3 Damage, armor, shield и status

Минимальная последовательность:

1. проверить hit/target validity;
2. применить immunity и damage-type modifier;
3. направить разрешённую часть в shield;
4. направить остаток в armor/resistance;
5. вычесть HP;
6. применить status по правилам;
7. опубликовать один `DamageResolved`;
8. при `HP <= 0` выполнить одну смерть.

**Типы урона:** Basic — physical и energy; Extended — explosive, true и damage-over-time. У типа должна быть тактическая функция: например physical силён по обычной цели, energy — по shield, explosive — по группе. `True` обходит защиту только если это явно записано в контракте.

**Shield:** отдельные current/max, recharge delay/rate и правила bypass. Варианты — обычный буфер, regenerating shield, typed shield, barrier на несколько попаданий.

**Armor/resistance:** flat reduction хорошо для частых слабых попаданий; percentage reduction — для масштабируемой защиты. Одновременно использовать обе модели можно только с фиксированным порядком.

**Status:** slow, burn, poison, stun, mark, vulnerability. Для каждого задаются duration, magnitude, tick, max stacks, refresh/replace policy, immunity tags и source ID.

### 8.4 Auras

**Данные:** owner, radius, target filter, modifier/effect Definition, update cadence, stacking group.

**Поведение:** поддерживает набор вошедших целей, добавляет effect handle при входе и снимает именно его при выходе/смерти owner. Итоговые статы рассчитываются целью.

**Варианты:** tower buff, enemy debuff, enemy support aura, статическая zone aura. Stacking: unique strongest, additive с cap, multiplicative по группам или non-stacking refresh. Частый overlap query заменяется spatial index только после измерения.

### 8.5 Enemies container и Enemy instance

Контейнер `Enemies` хранит активные экземпляры для запросов и очистки, но не владеет HP отдельного врага.

**Данные Enemy:** enemy Definition, current HP/shield, speed, path progress, effects, lane/spawn ID, reward, leak damage, runtime flags.

**Поведение:** `MonsterMove` идёт к базе; `MonsterHealth` принимает damage/effects; support-компонент выполняет только заявленную способность.

**Смерть:** пометить dead → остановить движение/атаки → один раз выдать kill reward → уведомить `WaveManager` → запустить feedback → вернуть в pool.

**Утечка:** достичь базы → один раз нанести leak damage → уведомить `WaveManager` без kill reward → вернуть в pool.

**Варианты:** Basic — runner и tank; Extended — armored, shielded, support, splitter, regenerator; Deferred — boss phases, summoner, route interaction. Splitter регистрирует детей в `WaveManager`, чтобы волна не завершилась раньше времени.

### 8.6 Projectiles

**Данные:** immutable shot payload, source ID, target/trajectory, lifetime; временное transform-состояние.

**Поведение:** летит, проверяет impact один раз, передаёт `DamagePacket`, публикует cue и возвращается в pool. Потеря цели обрабатывается определённой policy.

**Варианты:** homing, straight, ballistic, piercing, area impact. Policy потери цели: dissipate, continue to last point или retarget — задаётся Definition, не скрытым fallback. Beam/hitscan projectile не создают.

Projectiles обычно не входят в between-wave save. Mid-wave save обязан либо полностью сериализовать их, либо детерминированно восстановить результат по отдельному контракту.

### 8.7 Pools

**Данные:** prefab ID, prewarm/capacity, inactive instances.

**Поведение:** создаёт/выдаёт/возвращает технические экземпляры; при выдаче объект полностью сбрасывает runtime state. Pool не выдаёт награды, не считает живых и не принимает решения о цели.

**Варианты:** Basic — отдельные pools prefab-типа; Extended — общий registry; Deferred — adaptive capacity после профилирования.

## 9. Interaction

### 9.1 TowerPlacement

**Данные:** выбранная Tower Definition, курсор/ячейка, validation result, цена, preview state.

**Поведение:** выбрать башню → показать preview → проверить buildable/occupancy/range ограничения → `TrySpend` → создать Tower → зарегистрировать на `TileMap` → опубликовать событие. Если создание после списания не удалось, операция должна быть транзакционно отменена, а не продолжена fallback-башней.

**Варианты:** Basic — grid cell; Extended — socket и перенос/продажа; Deferred — свободное placement с footprint и elevation.

### 9.2 Selection

**Данные:** текущий selected stable ID и доступные команды/read model.

**Поведение:** преобразует click/raycast или gamepad focus в выбор; снимает выбор при уничтожении объекта; передаёт выбранный объект панели UI.

**Варианты:** Basic — одна башня; Extended — башня, тайл, база, враг для inspect; Deferred — multi-select. Selection не меняет статы.

### 9.3 PlacementPreview

**Данные:** draft position/rotation, validation reason, projected range/arc/coverage, cost.

**Поведение:** только визуализирует результат текущего запроса; использует тот же validator, что confirm, но не меняет карту, деньги, NavMesh или save.

**Варианты:** ghost mesh и цвет Basic; Extended — heatmap, маршрут и предупреждение о перекрытии; Deferred — прогноз DPS/threat.

## 10. Presentation

### 10.1 CameraRig

**Данные:** camera settings, input state, focus target; никогда не gameplay state.

**Поведение:** pan, zoom, drag, orbit и focus; блокирует world input, когда pointer захвачен UI.

**Варианты:** Basic — pan/zoom; Extended — orbit и focus selected; Deferred — cinematic wave/boss shots. Камера не запускает волну и не подтверждает placement.

### 10.2 Canvas и HUD

**Данные:** read models `GameState`, money, base HP/shield, wave progress, selected tower, offer, placement validation; localized Definition labels.

**Поведение:** отображает snapshot и отправляет команды владельцам. Кнопка заранее вычисляет доступность из read model, но окончательная проверка всегда в команде владельца.

**Панели:**

- HUD — ресурсы, база, волна, состояние;
- tower shop — доступные Definition и цены;
- selected tower — статы, target policy, upgrade/sell;
- tile choice — варианты и confirm;
- reward offer — выбор/переброс;
- pause/result — настройки, выход, итог и мета-награда.

**Варианты:** Basic — mouse/keyboard; Extended — gamepad navigation, tooltips и combat log; Deferred — rebinding/accessibility profiles. UI не хранит собственную копию денег или уровня башни как источник истины.

### 10.3 EventSystem

**Данные:** Input System actions и UI focus.

**Поведение:** маршрутизирует pointer/navigation/submit/cancel. Synthetic mouse/gamepad допустимы как debug/test input, но не образуют отдельный production gameplay path.

### 10.4 WorldVFX

**Данные:** presentation cue, position/target, intensity, damage result; visual settings.

**Поведение:** проигрывает muzzle, trail, impact, shield break, status, death, placement и reward feedback после gameplay-события.

**Варианты:** Basic — ссылки локально у prefab; Extended — pooled cue player по ID; Deferred — quality-scaled composition. Отсутствие VFX не меняет damage result.

### 10.5 WorldSFX

**Данные:** audio cue ID, spatial position, bus, priority, variation seed.

**Поведение:** воспроизводит attack/impact/death/leak/build/upgrade/UI cues после события; соблюдает mixer/settings и voice limit.

**Варианты:** Basic — owner `AudioSource`; Extended — pooled one-shot/AudioContainer; Deferred — surface/context layers. При отсутствии обязательного cue выводится явная content-ошибка, а не случайный default sound.

### 10.6 Music

**Данные:** game state, threat/read-only wave phase, settings.

**Поведение:** переключает подготовку, бой, победу и поражение; не влияет на таймеры и спавн.

**Варианты:** Basic — preparation/combat tracks; Extended — layered threat; Deferred — boss stems и adaptive transitions.

## 11. Environment

### 11.1 DirectionalLight

Освещает карту; данные — scene/preset lighting Definition. Варианты: фиксированный свет Basic, смена времени между волнами Extended. Освещение не должно менять видимость для targeting без отдельной gameplay-системы.

### 11.2 GlobalVolume

Хранит post-processing profile. Варианты: один профиль Basic, blends по состоянию/биому Extended. Volume читает presentation events и не является владельцем damage feedback.

### 11.3 Ground

Даёт визуальное основание, collision/raycast surface и границы камеры. Если поверхность влияет на скорость, buildability или damage, это уже `TileDefinition`/zone gameplay, а не скрытая настройка material.

### 11.4 Decoration

Содержит неинтерактивные props, vegetation и фон. Варианты: статические объекты Basic, seed-based decoration Extended, pooled ambient animation Deferred. Любой объект, влияющий на путь, line-of-sight или placement, получает Definition и владельца в `Level`, а не остаётся decoration.

## 12. Граница сохранения

### 12.1 Рекомендованный between-wave RunSave

Сохраняются:

- run ID, seed, версия контента и состояние `Preparation`;
- индекс следующей волны и deterministic random state;
- деньги и run-модификаторы экономики;
- HP/shield базы;
- tile layout и открытые spawn points;
- башни: Definition ID, клетка, уровень/ветка, target policy;
- выбранные награды, unlock внутри забега и текущий reward offer;
- накопители производства, если они существуют.

Не сохраняются: активные враги, projectile, текущие target references, NavMesh, spatial index, агрегированные статы, UI, VFX и SFX.

### 12.2 Mid-wave RunSave

Это Deferred-вариант. Дополнительно нужны spawn cursor/time, все активные враги с path progress/HP/shield/effects, cooldown башен, активные abilities и projectile либо строгий детерминированный restore contract. Частичное восстановление «примерно с той же волны» недопустимо как save/load.

## 13. Последовательности runtime

### 13.1 Новый забег

`SceneFlow → GameplayBootstrap → Content validation → LevelGenerator → TileMap → NavMesh → Base/Towers → Managers → GameManager.Preparation → HUD snapshot`.

### 13.2 Подготовка

Игрок размещает тайл и башни, покупает upgrade, выбирает target policy. Все команды проходят через владельцев и транзакционно меняют runtime state. `StartWave` доступен только после валидной карты и завершённых обязательных выборов.

### 13.3 Активная волна

`GameManager` блокирует запрещённые preparation-команды → `WaveManager` создаёт врагов → башни формируют атаки → damage pipeline меняет enemy state → death/leak обновляет деньги, базу и alive count → UI/VFX/SFX получают события.

### 13.4 Завершение волны

Когда spawn cursor исчерпан и alive count равен нулю: `WaveManager → GameManager.WaveResolve → completion reward → RewardOffer/MapBuild при наличии → safe save → Preparation`.

### 13.5 Завершение забега

Уничтожение базы переводит в `Defeat`; разрешение последней волны — в `Victory`. `RunResult` один раз передаётся application-слою, который рассчитывает мета-награду, сохраняет `ProfileSave` и предлагает следующий переход сцены.

## 14. Связь с текущими владельцами проекта

| Целевая роль | Текущий основной владелец/цепочка |
| --- | --- |
| Entry point | `GameplayBootstrap` |
| Run state machine | `GameManager`, `GameState` |
| Волны | `WaveManager` |
| Валюта забега | `ResourceManager` |
| Генерация уровня | `LevelGenerator`/`MapGenerator` |
| Карта | `TileMapManager` |
| Размещение тайла | `TilePlacementSystem`, `TilePlacementValidator` |
| Каталог тайлов | `TileDatabase`/`TileDefinition` |
| Башня и upgrade | `Tower`, `TowerStatsSO` |
| Оружие | `IWeapon`; совместимый путь `Projectile`, `GameObjectPool` |
| Размещение башни | `TowerShopUI → TowerPlacementSystem → Tower` |
| Враг | `MonsterHealth`, `MonsterMove` |
| HUD | `GameHUD`, `WaveUI` |

При внедрении сначала проверяется живая цепочка callback, prefab, scene reference и Definition. Таблица задаёт направление владения, но не заменяет этот аудит.

## 15. Этапы реализации

### A. Minimal complete loop

- линейный `GameState` и authored waves;
- одна валюта, kill/completion reward;
- три роли башен и два роли врагов;
- physical/energy, базовые armor/shield;
- связная карта, один tile choice между волнами;
- between-wave save либо честно заявленное отсутствие save;
- HUD, placement feedback, attack/hit/death/leak cues.

### B. Вариативность забега

- reward offer, rarity/reroll;
- upgrade branches, status и ауры;
- несколько lane и ветвление карты;
- производственный доход и напряжение «потратить или сохранить»;
- итог забега и горизонтальная мета-экономика.

### C. Production/deferred

- threat-budget generation, boss phases, difficulty mutators;
- Addressables/mod catalogs;
- mid-wave save;
- analytics/balance telemetry;
- accessibility и расширенная gamepad-навигация.

## 16. Проверка архитектурного контракта

- У каждого изменяемого значения есть один владелец.
- У каждой команды есть один entry point и повторная проверка инвариантов у владельца.
- UI/VFX/SFX не меняют деньги, HP, wave count, target или карту.
- Definition не мутируются ради конкретного забега.
- Save использует стабильные ID и snapshots, не scene references.
- NavMesh, цели, read models и агрегированные статы пересчитываются.
- Смерть, утечка, награда и terminal result исполняются ровно один раз.
- Отсутствующий обязательный контент останавливает операцию с явной ошибкой; скрытого fallback нет.
- Новая механика расширяет текущего владельца и не создаёт параллельный manager/state holder.
- Component topology сцены/prefab авторится до Play Mode, а не меняется из `Awake`/`Start`.
- Runtime-изменение считается подтверждённым только после Unity compile, чистой Console и bounded Play Mode smoke.

