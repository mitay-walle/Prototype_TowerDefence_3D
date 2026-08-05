---
title: Loop забега без мета-прогрессии
status: design-target
updated: 2026-08-04
scope: one complete run, repeated waves, in-run progression, terminal outcomes
excludes: meta progression, permanent unlocks, cross-run currency, account/profile growth
related: Assets/Documentation/CORE_LOOP.md, Assets/Documentation/GAMEPLAY_REFERENCES.md, Assets/Documentation/GAMEPLAY_SCENE_OBJECTS.md
---

# Loop забега без мета-прогрессии

## 1. Граница документа

Документ описывает один полный забег: от создания карты и начального состояния до `Victory`, `Defeat` или добровольного `Abandon`.

Забег состоит из повторения core loop одной волны, описанного в `CORE_LOOP.md`. Между волнами сохраняются и развиваются:

- карта и маршруты;
- база и её оставшееся здоровье;
- построенные башни и их upgrades;
- валюта забега;
- выбранные run rewards/modifiers;
- номер следующей волны;
- deterministic random state и текущие offers.

Не входят:

- мета-валюта;
- постоянные unlock;
- account/profile level;
- перенос башен, денег или upgrades в следующий забег;
- дерево постоянного развития;
- расчёт награды, изменяющей будущие забеги.

`RunResult` в конце может содержать статистику завершённого забега, но этот документ не описывает её превращение в мета-прогрессию.

Это design-target. Текущие код, сцена, prefab и assets остаются источником истины о фактической реализации.

## 2. Loop забега одной строкой

> Построить начальную оборону → пережить волну → сохранить последствия → выбрать усиление и изменить карту → подготовиться к более сложной угрозе → повторять до уничтожения базы или завершения последней волны.

Игрок строит не отдельный ответ на одну волну, а развивающуюся систему обороны. Каждое решение должно помогать сейчас и одновременно ограничивать или открывать будущие варианты.

## 3. Схема забега

```text
StartRun
  ↓
Boot
  ↓
MapBuild
  ↓
PrepareWave(1)
  ↓
┌─────────────────────────────────────────────────────────┐
│ CORE_LOOP(w)                                            │
│ Preparation → WaveActive → WaveResolve                  │
└─────────────────────────────────────────────────────────┘
  ↓
Base destroyed? ── yes ──→ Defeat
  │ no
  ↓
Last configured wave resolved? ── yes ──→ Victory
  │ no
  ↓
PrepareWave(w + 1)
  ├── reveal next threat
  ├── present limited run reward/choice
  ├── apply optional map expansion
  ├── allow build/upgrade/repair
  └── create stable between-wave snapshot
  ↓
CORE_LOOP(w + 1)
```

Pause является ортогональным состоянием времени и UI. Он не образует отдельную ветку progression забега.

## 4. Главный объект решений

На уровне забега игрок постоянно управляет пятью ограниченными ресурсами:

1. **Деньги.** Нельзя одновременно купить всё нужное.
2. **Карта.** Новый tile меняет будущие маршруты и build positions.
3. **Пространство.** Позиция, занятая одной башней, недоступна другой.
4. **Специализация.** Upgrade branch усиливает один matchup ценой гибкости.
5. **Прочность базы.** Потерянное здоровье переносится между волнами, пока явно не восстановлено.

Время real-time боя является проверкой этих решений, но основная стратегия формируется между волнами.

## 5. RunState

`RunState` — логическое состояние одного забега, а не требование создать новый `RunManager`.

Текущие владельцы совместно образуют run aggregate:

| Данные забега | Владелец |
| --- | --- |
| Текущее глобальное состояние | `GameManager`, `GameState` |
| Номер волны, spawn/completion | `WaveManager` |
| Деньги и доход | `ResourceManager` |
| Карта, tiles, spawn points | `TileMapManager`, level systems |
| База | `PlayerBase` |
| Башни, уровни, target policy | Экземпляры `Tower` и tower systems |
| Выбранные rewards/modifiers | Владелец текущего reward flow; базово может оставаться частью существующей loop-цепочки |
| Presentation/read models | `GameHUD`, `WaveUI` и специализированные панели |

Логический snapshot забега содержит:

```text
RunState
├── RunId / Seed / ContentVersion
├── CurrentState
├── NextWaveIndex
├── Currency
├── BaseState
├── TileLayout
├── Towers[]
├── RunModifiers[]
├── RewardHistory[]
├── CurrentOffer
└── RandomState
```

NavMesh, текущие target references, VFX, UI selection и агрегированные stats являются derived/runtime данными и не определяют идентичность забега.

## 6. StartRun

### 6.1 Вход

Application-слой передаёт `GameplayBootstrap` запрос:

- `StartNewRun(seed, startingRules)`;
- либо `ContinueRun(runSnapshot)`.

`startingRules` — вход текущего забега: начальный бюджет, разрешённый контент, количество/список волн и difficulty. Источник этих правил находится за границей документа; здесь они не являются мета-прогрессией.

### 6.2 Инициализация нового забега

`GameplayBootstrap` выполняет одну последовательность:

1. проверить обязательные Definition и scene references;
2. создать runtime state из seed и starting rules;
3. сгенерировать связанную карту;
4. определить базу и spawn points;
5. построить NavMesh;
6. создать начальные gameplay objects;
7. инициализировать `ResourceManager`, `WaveManager`, `GameManager` и HUD;
8. определить первую волну и показать её intel;
9. перейти в `Preparation`.

Если обязательный шаг не выполнен, забег не переходит к playable state. Недостающая карта, волна, база или spawn point не подменяются скрытым fallback.

### 6.3 Начальное состояние

Минимально игрок получает:

- валидную стартовую карту;
- живую базу;
- стартовую валюту;
- доступный набор башен;
- состав первой волны;
- возможность принять хотя бы одно meaningful build decision.

Первый забеговый выбор не должен требовать знаний, которые UI ещё не показал.

## 7. Атомарная итерация забега

Волна является атомарной проверкой текущего `RunState`.

Обозначим:

- `S_w` — состояние забега перед подготовкой к волне `w`;
- `A_w` — решения игрока в подготовке;
- `C_w` — зафиксированная конфигурация обороны;
- `R_w` — результат симуляции волны;
- `S_(w+1)` — состояние после применения результатов.

```text
C_w       = Prepare(S_w, A_w)
R_w       = SimulateWave(C_w, WaveDefinition_w)
S_(w+1)   = Resolve(S_w, A_w, R_w)
```

`Resolve` переносит только подтверждённые последствия:

- оставшиеся деньги;
- kill/completion/passive income;
- оставшееся HP/shield базы;
- карту после подтверждённых tile actions;
- построенные башни и upgrades;
- выбранные run modifiers;
- следующий wave index.

Active enemies, projectiles и временные presentation cues не переносятся в обычную следующую итерацию.

## 8. Переход между волнами

Переход `WaveResolve(w) → Preparation(w + 1)` должен быть коротким, последовательным и объяснимым.

### 8.1 Зафиксировать результат текущей волны

Сначала один раз применяются:

- все kill rewards, уже подтверждённые во время боя;
- completion reward;
- passive income, если включён;
- потеря HP/shield базы;
- consumed/expired wave-only effects;
- статистика результата текущей волны.

После фиксации баланс и состояние базы не должны изменяться повторно при открытии/закрытии UI.

### 8.2 Проверить terminal condition

Порядок проверки:

1. если база уничтожена — `Defeat`;
2. иначе, если завершена последняя настроенная волна — `Victory`;
3. иначе определить `WaveDefinition(w + 1)` и продолжить переход.

Terminal result публикуется ровно один раз. После него новый reward offer, tile phase и следующая волна не создаются.

### 8.3 Показать следующую угрозу

До нового необратимого расхода игрок видит:

- состав следующей волны;
- новые enemy roles/traits;
- lane/spawn direction;
- изменение count, speed, health или других известных параметров;
- особое правило волны.

Intel следующей волны является входом для reward, map и build decisions.

### 8.4 Limited reward/choice

Если run rules содержат reward offer, игрок получает небольшой набор взаимоисключающих вариантов.

Типы reward внутри забега:

- currency cache;
- repair;
- tower augment;
- global run modifier;
- temporary modifier следующей волны;
- дополнительный tile/build option;
- reroll token как Extended-вариант.

Reward должен:

- иметь точное описание эффекта;
- применяться к реальному владельцу;
- фиксироваться в `RewardHistory`;
- применяться ровно один раз;
- не менять профиль или будущий забег.

Если reward unique, повторный offer фильтруется по сохранённой истории. Если stacking разрешён, показывается правило stack/cap.

### 8.5 Map expansion

Если текущий переход предусматривает tile choice:

1. строится несколько валидных вариантов;
   - выборка оценивает весь доступный valid candidate set и оставляет минимум три разных topology outcomes, начиная с варианта с меньшим числом открытых входов;
2. игрок видит route/build coverage preview; `WaveUI` показывает локализованный
   summary выбранного варианта, включая покрытие башнями до и после:
   `Tower coverage: covered/total -> covered/total`;
   это coverage spawn anchors, а не доказательство фактического попадания по маршруту;
3. после завершения волны `GameplayTelemetry` фиксирует причинную цепочку
   `targetAcquisitions -> towerFires -> damageApplications -> kills/leaks` в
   `WaveCompleted.Details`. Эти counters используются для проверки defensive build
   и не заменяют отдельный Play trace;
   During `WaveResolve`, `WaveUI` reads the latest completed chain through
   `TryGetLatestCompletedWaveCombat` and shows a localized result line with kills,
   leaks, target locks, fires, and damage events. During preparation, this line is
   kept above the next-wave intel so the combat result remains actionable for the
   next route/build decision.
4. выбирает и подтверждает один вариант;
4. `TileMapManager` изменяет карту;
5. перестраиваются route/NavMesh/spawn anchors;
6. новая карта валидируется до разрешения `StartWave`.

Выбор, подтверждение и отмена tile choice публикуются как decision telemetry; commit должен завершаться с `IsTilePlacing=false`, валидной картой и обновлёнными spawn anchors.

Tile становится постоянной частью текущего забега. Его последствия действуют на все последующие волны, пока отдельное правило не разрешает перестройку.

### 8.6 Build, upgrade и repair

После intel/reward/map choice игрок распределяет общий банк между:

- новой башней;
- upgrade или branch существующей;
- repair базы, если доступен;
- дополнительным tile/relocate/sell, если правила разрешают;
- резервом на будущие волны.

Это главный run-level trade-off: немедленная безопасность против будущей гибкости.

### 8.7 Stable Preparation

Переход закончен, когда:

- следующая Wave Definition определена;
- обязательный offer закрыт;
- обязательный tile choice завершён;
- карта и NavMesh валидны;
- деньги и база имеют окончательные значения;
- нет активных врагов/projectiles;
- `GameManager` находится в `Preparation`.

Только из этого состояния разрешается следующий `StartNextWave()` и recommended between-wave save.

## 9. Экономика забега

### 9.1 Перенос банка

Для волны `w`:

```text
B_open(w)       — банк перед подготовкой
C_prepare(w)    — tile + tower + upgrade + repair расходы
R_kill(w)       — сумма kill rewards
R_complete(w)   — completion reward
R_passive(w)    — passive income
R_choice(w)     — currency reward, если выбран

B_commit(w) = B_open(w) + R_choice(w) - C_prepare(w)
B_close(w)  = B_commit(w) + R_kill(w) + R_complete(w) + R_passive(w)

B_open(w+1) = B_close(w)
```

Если reward следующей волны выдаётся после `WaveResolve(w)`, для математической ясности он считается частью preparation `w + 1`, а не повторной наградой волны `w`.

### 9.2 Основной economic tension

Игрок должен выбирать между:

- шириной обороны — новая башня;
- глубиной — upgrade;
- исправлением слабости — counter tower/branch;
- сохранением ресурса — reserve;
- восстановлением допустимой ошибки — repair;
- изменением будущего поля — tile/map expense.

Если после каждой волны можно купить всё очевидно полезное, экономика не создаёт решения. Если нельзя купить ни одного осмысленного ответа на показанную угрозу, экономика создаёт безвыходность.

### 9.3 Snowball и death spiral

Положительный snowball допустим: хороший билд убивает больше врагов, сохраняет HP и оставляет больше ресурса.

Ограничения:

- kill reward не должен бесконечно масштабировать сам себя без cap/pressure;
- completion reward создаёт минимально предсказуемый приток;
- потеря HP базы сохраняет цену ошибки;
- repair является явной покупкой/reward, а не скрытым восстановлением;
- catch-up modifier не включается тайно;
- сложность должна проверять новые свойства билда, а не только умножать HP.

Если нужна механика восстановления, она показывается игроку как выбор с ценой.

Для catch-up после слабой волны используется только существующий reward `ResourceCache`: его базовая величина равна 5, но если банк ниже стоимости самой дешёвой authored-башни, `WaveManager` добавляет ровно недостающую сумму до одной базовой покупки. При достаточном reserve сохраняются базовые 5; правило не позволяет одновременно купить башню и её upgrade.

### 9.4 Telemetry for committed tower decisions

`TowerPlacementSystem` keeps the existing preview event and now emits `onTowerPlaced` only after the purchase and instantiated tower are committed. `GameplayTelemetry` records a `TowerPlaced` decision with the authored tower name, cost, preview coverage, snapped world position, and currency after spending. This lets a later balance trace compare the player-visible preview with the actual defensive build without introducing another placement owner.

The required evidence for a natural baseline is therefore: `TowerPlacementCoveragePreview` -> `TowerPlaced` -> `TowerTargetAcquired`/`TowerFired` -> `EnemyKilled`/`EnemyLeaked`, plus the final base health and currency in the snapshot.

### 9.5 NavMesh route coverage in tower placement preview

The placement owner keeps the existing spawn-anchor coverage as a compatibility diagnostic, but the preview now also samples each valid NavMesh route from a spawn position to the base. The range decal uses the route sample ratio when a route is available, so a position that touches an entrance but misses the actual lane is no longer presented as strong coverage.

`TowerPlacementCoveragePreview` records both contracts: `covered/total` for spawn anchors and `routeCovered/routeTotal/routeRatio` for sampled firing exposure. A committed `TowerPlaced` event carries the same `routeCoverage` value. `BuildRouteSamples` is read-only and runs when the existing `TowerPlacementSystem` begins placement; it does not mutate the map, NavMesh, or tower owner state.

The runtime acceptance is still causal rather than purely geometric: compare the route preview with `TowerTargetAcquired`, `TowerFired`, `DamageApplied`, `EnemyKilled`, and `EnemyLeaked` in the same telemetry journal. A valid route preview is exposure evidence, not a promise of natural Victory; authored balance and final-wave Victory remain separate checks.

### 9.6 Damage telemetry is separate from leak resolution

`MonsterHealth.onHealthChanged` is a state-change signal and also fires when an enemy leaks. It must not be used as a weapon-hit counter. The existing health owner now emits `onDamageTaken` only for positive damage applied by a weapon, and `GameplayTelemetry` records `DamageApplied` from that signal with both `damage` and post-hit `targetHealth` in invariant culture.

The wave combat contract therefore means actual damage applications: `towerFires -> DamageApplied -> EnemyKilled`, while `EnemyLeaked` remains a separate terminal path. This prevents a leak-induced health reset from inflating `damagePerFire` and keeps balance decisions grounded in real hit efficiency.

### 9.7 Authored final-wave pressure calibration

The current authored pressure slice keeps the documented role progression and changes only scalar pressure:

- `Wave_01` uses the authored opening-group `healthMultiplier=0.80`;
- `Wave_02` keeps the Turtle/Frog composition and uses `healthScaling=0.90`;
- `Wave_03` keeps its late-run Berserker/Boss composition and uses `countScaling=1.00` while retaining `healthScaling=1.30`.

The last change reduces the configured final wave from 49 to 41 enemies without removing the late-run role. A controlled Play probe with `ChallengeModifier.None`, a valid map, full route coverage before Wave 3, ten towers, and the existing Emergency Repairs reward reached Wave 3 with `TotalEnemiesInWave=41`. It still ended in Defeat at `33/41` spawned, `40` cumulative kills, `14` cumulative leaks, and base `0/20`.

This is pressure evidence, not natural-balance acceptance: the probe injected test currency and used accelerated Play time. The remaining gap is effective final-wave damage and counter access in a real player build; do not lower enemy health or remove the late-run role without a new `TowerPlaced -> TowerFired -> DamageApplied -> EnemyKilled` comparison.

### 9.8 Combined build coverage in tower placement preview

`TowerPlacementSystem` snapshots the currently committed `Tower` instances when placement begins and evaluates the ghost candidate against that same build. The existing spawn-anchor and sampled-route counters now expose `existing`, `candidate`, and the union `combined` values; the range feedback and existing preview event use the combined result. This keeps the feedback on the authored placement owner and avoids a second coverage state holder.

`GameplayTelemetry` preserves the existing `TowerPlacementCoveragePreview` event and adds the explicit `coverageMode=combined` detail together with route counters. The acceptance trace is therefore `TowerPlacementCoveragePreview -> TowerPlaced -> TowerTargetAcquired/TowerFired -> DamageApplied -> EnemyKilled/EnemyLeaked`. A combined preview is still exposure evidence, not proof of natural final-wave Victory.

### 9.9 Placement ghost lifecycle and authoritative build snapshots

`TowerPlacementSystem` now builds its coverage snapshot from active, enabled `Tower` components only. A cancelled preview ghost is destroyed through the existing owner path and its reference is cleared immediately, so the next placement cannot report a ghost as an existing defensive tower. The diagnostic log keeps both `existingTowers` and `excludedTowers`; this makes ghost/disabled-object filtering visible without adding another state owner.

The contract is covered by `DisabledTowerDoesNotContributeToCombinedCoverage`. After the fix, the ML heuristic Play trace reported `existingTowers=0`, then `1`, then `2` for successive committed placements, with no false extra tower. This is lifecycle/telemetry proof; natural final-wave Victory and the authored balance gap remain open.

## 10. Развитие карты в забеге

Карта является частью build, а не статичным фоном.

Каждый подтверждённый tile может изменить:

- количество открытых spawn ends;
- длину маршрута;
- choke points;
- доступные build positions;
- coverage существующих башен;
- разделение врагов по lane;
- будущую ценность range, area damage и control.

Run-level эффект:

`tile(w) → topology(w+1...) → ценность tower choices → результат будущих волн`.

Ограничения:

- все spawn points должны иметь валидный маршрут к базе;
- изменение карты происходит только в разрешённой preparation phase;
- preview не меняет run state;
- подтверждённая карта сохраняется в RunSave;
- новый tile не создаёт дублирующий map owner;
- NavMesh является derived cache и перестраивается из layout.

## 11. Развитие башен в забеге

### 11.1 Tower identity

Башня сохраняет между волнами:

- Definition ID;
- позицию;
- уровень;
- выбранную branch;
- target policy;
- постоянные для забега augments;
- owner-linked aura/status configuration.

Cooldown, текущая цель и временный hit effect между обычными волнами сбрасываются или завершаются по явному контракту.

### 11.2 Ширина и глубина билда

- **Ширина:** больше башен, coverage, lane coverage и параллельных целей.
- **Глубина:** более высокий уровень, специализация, сильный matchup.
- **Синергия:** aura, status setup, armor/shield break и damage delivery.
- **Гибкость:** резерв и свободные build positions.

Хороший run заставляет комбинировать эти оси. Одна универсальная башня не должна быть лучшим ответом на все будущие угрозы.

### 11.3 Branch commitment

Extended-вариант upgrade branch:

- выбор ветки ясно показывает получаемое и теряемое;
- взаимоисключающая ветка блокируется;
- respec отсутствует либо имеет явную цену;
- эффект сохраняется до конца текущего забега;
- Save хранит branch ID, а не derived stats.

## 12. Threat progression

Последовательность волн должна менять задачу, а не только число врагов.

### Early run

- обучает базовой экономике и placement;
- даёт прочитать direct damage, runner и tank;
- позволяет сформировать первый устойчивый контур обороны.

### Mid run

- вводит armor/shield, группы, support или несколько lane;
- проверяет specialization и coverage;
- заставляет тратить резерв или менять карту.

В authored progression `Berserker`/boss не должен появляться во второй волне раньше доступного player counter. `Wave_02` оставляет tank/runner matchup для покупки и позиционирования tower roles, а boss сохраняется в `Wave_03` как late-run threat. Это сохраняет причинную цепочку `intel -> role choice -> coverage -> damage` при стартовой экономике и не убирает финальное давление.

### Late run

- комбинирует ранее показанные traits;
- создаёт несколько одновременных приоритетов;
- проверяет слабое место сформированного билда;
- финальная волна требует использовать накопленную систему, а не угадывать новый неизвестный counter.

Принципы:

- новый trait сначала читаемо показывается;
- counter доступен до commit;
- одна волна не должна вводить слишком много неизвестных правил;
- count/HP scaling поддерживает pressure, но не заменяет роли врагов;
- финальная волна завершает арку сложности configured run.

## 13. Reward progression

Rewards формируют identity текущего забега.

### Горизонтальный reward

Открывает новый вариант внутри забега:

- новая tower role;
- дополнительный tile;
- новая target policy;
- новый тип upgrade/augment.

### Вертикальный reward

Усиливает существующее:

- damage/range/attack speed modifier;
- aura magnitude;
- repair/base shield;
- completion income modifier.

### Экономический reward

Меняет банк или поток:

- немедленная currency cache;
- bounty следующей волны;
- passive income;
- скидка на конкретную категорию.

Ограничение Basic-варианта: offer мал и понятен. Полный deckbuilder не нужен, чтобы выбор влиял на run identity.

## 14. Сохранение внутри забега

### 14.1 Recommended boundary

Basic save выполняется в стабильной `Preparation` между волнами.

Сохраняются:

- run ID, seed и content version;
- индекс следующей волны;
- деньги;
- HP/shield базы;
- tile layout;
- towers и их upgrades/branches/policies;
- run modifiers/reward history;
- current offer и random state, если выбор ещё не завершён;
- обязательные preparation flags.

Не сохраняются:

- NavMesh;
- текущие target references;
- активные projectiles;
- VFX/SFX;
- UI selection;
- пересчитываемые aggregate stats.

### 14.2 ContinueRun

Загрузка:

1. проверяет version и content IDs;
2. восстанавливает exact layout и owners;
3. перестраивает derived caches;
4. восстанавливает offer без reroll;
5. возвращает `GameManager` в сохранённую безопасную `Preparation`;
6. не повторяет reward/payout уже завершённой волны.

Missing required Definition блокирует корректную загрузку с явной ошибкой. Она не подменяется другой башней, волной или tile.

### 14.3 Mid-wave save

Deferred и не входит в Basic run loop. Для него нужен отдельный полный snapshot enemies, spawn cursor, path progress, cooldown, effects и projectiles либо строгий deterministic restore contract.

## 15. Terminal outcomes

### 15.1 Victory

Условия:

- последняя configured Wave Definition полностью заспавнена;
- все зарегистрированные враги убиты или разрешены как leak;
- база жива;
- completion payout применён один раз;
- inter-wave flow больше не запускается.

Эффект:

- `GameManager` переходит в `Victory`;
- управление строительством и новой волной блокируется;
- показывается `RunResult` текущего забега;
- игрок может выйти или начать новый независимый забег.

### 15.2 Defeat

Условие: HP базы становится равным нулю.

Эффект:

- `GameManager` переходит в `Defeat` ровно один раз;
- `GameManager` просит существующий `WaveManager.ForceStopWave()` отменить spawn/inter-wave async до публикации terminal flow;
- новые rewards и completion payout не выдаются;
- показывается результат до момента поражения;
- продолжение того же завершённого runtime state запрещено.

### 15.3 Abandon

Добровольное завершение требует явного подтверждения.

Эффект:

- активные tasks и scene state корректно закрываются;
- run slot помечается завершённым или удаляется по выбранному save contract;
- награды текущей незавершённой волны не выдаются;
- никакой meta effect этот документ не применяет.

## 16. Действие игрока → эффект забега

| Действие | Немедленный эффект | Долгосрочный эффект внутри забега |
| --- | --- | --- |
| Потратить на новую башню | Снижает банк, создаёт Tower | Больше coverage, меньше будущей гибкости |
| Купить upgrade | Снижает банк, усиливает Tower | Формирует specialization и branch commitment |
| Выбрать tile | Меняет карту | Меняет ценность всех будущих placements |
| Выбрать reward | Применяет один run effect | Формирует identity/экономику текущего забега |
| Ремонтировать базу | Деньги/reward превращаются в HP | Увеличивает допустимый будущий риск |
| Оставить reserve | Не меняет текущую силу | Сохраняет ответ на неизвестную следующую угрозу |
| Продать/перенести башню | Возвращает часть цены/меняет позицию | Исправляет билд с потерей эффективности |
| Сменить branch/policy | Меняет специализацию/распределение атак | Меняет matchup следующих волн |
| Начать следующую волну | Фиксирует preparation | Проверяет накопленный билд новой угрозой |
| Сохранить/продолжить | Создаёт/восстанавливает snapshot | Сохраняет тот же забег без reroll и duplicate payout |
| Abandon | Завершает runtime | Прогресс этого забега больше не развивается |

## 17. Варианты масштаба

### Basic run

- конечный authored список волн;
- одна валюта;
- persistent base HP;
- persistent map/towers/upgrades;
- intel следующей волны;
- один limited reward choice между волнами;
- один tile choice там, где он предусмотрен;
- between-wave save;
- `Victory` после последней волны, `Defeat` при разрушении базы.

### Extended run

- несколько lane;
- branch upgrades и augments;
- armor/shield/status/aura progression;
- reroll/banish reward offer;
- production и repair economy;
- challenge modifiers;
- boss/final wave variants;
- sell/relocate/respec с явной ценой.

### Не входит

- постоянная валюта между забегами;
- profile unlock;
- meta upgrade tree;
- account XP/level;
- увеличение стартовой силы будущего забега;
- daily/season progression;
- cloud conflict rules профиля;
- полный deckbuilder как обязательное ядро;
- бесконечный режим вместо конечного Basic run.

## 18. Владельцы переходов

| Переход | Entry point / владелец |
| --- | --- |
| Application → Boot | `GameplayBootstrap` |
| Boot → MapBuild | `GameManager.BeginBoot/BeginMapBuild` |
| MapBuild → Preparation | `GameManager.CompleteMapBuild` |
| Preparation → WaveActive | `GameManager.StartNextWave()` → `WaveManager` |
| WaveActive → WaveResolve | `WaveManager` completion event → `GameManager` |
| WaveResolve → Preparation | `WaveManager.onPreparationReady` → `GameManager` |
| Последняя волна → Victory | `WaveManager.onAllWavesCompleted` → `GameManager` |
| Base destroyed → Defeat | `PlayerBase` event → `GameManager` |
| Terminal outcome → RunFinished | `GameManager.FinishRun` → immutable `RunResult` → `onRunFinished` |
| Restart/new run | `GameManager`/application scene flow по явной команде; перед reload публикуется `RestartRequested` |

Не добавлять параллельные `RunManager`, `RoundManager`, `EconomyManager` или второй serialized run state. Новая логика расширяет существующего владельца либо выделяет pure helper, принадлежащий ему.

## 19. Инварианты забега

- В один момент активен один run и не более одной волны.
- `GameManager` является единственным владельцем глобального transition state.
- Номер волны увеличивается ровно один раз на запуск.
- Следующая волна не стартует до завершения обязательной preparation.
- Kill, leak, completion payout и reward choice применяются ровно один раз.
- Деньги, карта, база и башни имеют по одному mutable owner.
- HP базы переносится между волнами без скрытого восстановления.
- Подтверждённый tile и upgrade сохраняются до конца забега.
- Run-only modifier не записывается в ProfileSave и не влияет на новый забег.
- Victory и Defeat являются terminal states.
- После terminal state inter-wave flow не запускается.
- Save/load не reroll-ит offer и не повторяет payout.
- Derived cache не сохраняется как источник истины.
- Pause не изменяет progression state.
- Ошибка обязательного контента не создаёт fallback run.

## 20. Критерии хорошего забега

Забег образует связный loop, если:

1. Первая волна позволяет сформировать базовую оборону, а не угадывать скрытое правило.
2. Каждая следующая волна использует последствия предыдущих решений.
3. Intel приходит до reward/map/build расходов.
4. Игрок регулярно выбирает между новой башней, upgrade, repair/map и reserve.
5. Карта заметно меняет ценность placements, а не служит косметическим расширением.
6. Rewards создают различия между двумя прохождениями одного authored списка волн.
7. Enemy composition требует адаптации, а не только роста общего DPS.
8. Ошибки имеют накопительную цену через деньги, позиции или HP базы.
9. Сильная игра создаёт преимущество, но не устраняет все последующие решения.
10. Save/continue восстанавливает тот же забег без reroll и duplicate income.
11. Последняя волна проверяет сформированный билд и приводит к однозначному `Victory` или `Defeat`.
12. Новый забег начинается с чистого run state; никакая сила не переносится без отдельной мета-системы.

## 21. Минимальный Play Mode сценарий

1. Запустить новый забег и проверить `Boot → MapBuild → Preparation`.
2. Убедиться, что показана первая волна и доступен стартовый бюджет.
3. Пройти первую волну до `WaveResolve`.
4. Проверить единичные kill/completion/passive payouts.
5. Получить intel следующей волны до новых расходов.
6. Выбрать reward и tile; проверить однократное применение и новый маршрут.
7. Купить башню/upgrade и оставить часть денег в reserve.
8. Пройти ещё одну волну; проверить перенос банка, HP, карты и башен.
9. Сохранить в стабильной `Preparation`, перезагрузить и сравнить snapshot.
10. Убедиться, что offer не reroll-нулся и прошлая награда не выдалась повторно.
11. Отдельно довести базу до нуля и проверить один `Defeat` без completion payout.
12. Отдельно завершить последнюю configured wave и проверить один `Victory` без следующего inter-wave flow.

Компиляция не подтверждает run loop. Нужен bounded Play Mode проход нескольких волн, save/continue и обоих terminal outcomes.
### 9.10 ML diagnostic agents do not replace the authored baseline

TD ML Balance Agent and TD ML Enemy Level Agent are diagnostic owners, not hidden gameplay modifiers. The Gameplay scene stores both with _trainingMode=false; enable them explicitly for balance or enemy-level training. This keeps WaveConfig authored data and the normal run contract observable by the player and by TD ML Agent smoke tests.

The player agent resolves the mandatory one-time start challenge choice before Wave 1. The automatic smoke policy selects `ControlledPressure`, while `ChallengeModifier.None` remains only the unselected/reset state and cannot resolve the run. The decision is recorded by `ChallengeModifierSelected` with its numeric count/health/speed/reward factors. No challenge modifier choice is opened after every wave; inter-wave flow continues with the existing reward and map phases.

The previous R64 neutral-baseline trace is historical evidence for the old optional challenge contract and is not acceptance evidence for the current mandatory selection. A new bounded smoke must record `ActiveChallengeModifier=ControlledPressure`, valid topology, tower placement, combat telemetry and a terminal outcome.

### 9.11 Player-agent upgrade decision and runtime evidence

The player heuristic keeps coverage placement as the first preparation obligation. When no prioritized coverage placement is required and an existing tower is affordable to upgrade, it selects `ActionUpgradeTower` before starting the next wave. The live owner remains `Tower.UpgradeSpendingCost()`; the agent adds only decision routing and a bounded `[MLAgent]` commit log.

The controlled post-fix probe recorded `TowerUpgrade` grade `0->1`, `TowersUpgraded=1`, `TowerFired`, `DamageApplied`, `EnemyKilled`, and `WaveCompleted` with `12` target acquisitions, `10` fires, `14` damage applications, `7` kills, and `0` leaks. The probe injected currency and therefore proves the upgrade owner chain, not natural economy or final-wave Victory. The natural baseline still needs a two/three-tower run with enough authored income to reach and evaluate this policy.

### 9.12 Player-agent reward survival routing

The player heuristic keeps `ResourceCache` as the economy catch-up choice when the bank is below the cheapest tower cost and the build has only one tower. When the base is at or below 75% health and the build can continue without catch-up cash (the bank can still buy the cheapest tower or at least two towers already exist), it selects `EmergencyRepairs`. On a non-final wave, a healthy two-tower build with full entrance coverage and enough reserve for one cheapest tower selects the existing `BountyContract` instead, because its delayed completion bonus is now safe to carry. This preserves the documented choice between adding coverage, recovering a damaged run, and investing in a delayed payout without introducing a second economy owner.

`TrySelectReward` now treats a rejected `WaveManager.SelectRewardOffer` call as an invalid action with a small negative reward and does not grant the positive selection reward. Successful choices emit `[MLAgent] Reward decision=...` with wave, base health, currency, tower count, and coverage; `GameplayTelemetry` remains the authoritative `RewardSelected` readback.

R66 natural Play evidence: authored Wave 1 and Wave 2 completed, the run reached Wave 3 with `HasGeneratedWave=false`, `TileMapValid=true`, and `TileMapInvalidConnectionCount=0`. The reward trace selected `EmergencyRepairs` at `base=2/20;currency=102;towers=2`, followed by two upgrades and a later snapshot at `base=9/20` in Wave 3. The bounded run then reset to `ChallengeSelection`; no direct `Victory` or `Defeat` log was captured, so final-wave terminal balance remains open.

### 9.13 Terminal reward cancellation

When the Base is destroyed or the RunFlow owner is already terminal, `WaveManager.SelectRewardOffer` rejects every pending reward action. `WaveManager.ForceStopWave` then cancels the pending offer and the inter-wave async scope before the terminal `RunResult` is published. This preserves the Defeat contract: no reward selection, completion payout, or next inter-wave phase after Base destruction.

R67 runtime proof used the existing leak path with player-agent isolation and no tower/currency injection. Wave 1 resolved as `7` leaks, `base=13/20`, `RewardOfferPending=true`; a controlled Base destruction while the offer was pending returned `acceptedAfterDestroy=false`, then `ForceStopWave` changed pending to `false`. Telemetry ended at `BaseDestroyed -> RunFinished(Defeat)` with no `RewardSelected`; the Console also recorded `Reward offer rejected after terminal state` and `Pending reward offer cancelled by run stop`. This is terminal-safety evidence, not natural-balance or final-wave Victory evidence.

### 9.14 Player-agent tactical tile choice

The player heuristic no longer commits the first tile option blindly. During the existing `TilePlacementSystem` phase it evaluates every valid `TilePlacementChoice` through `TileMapManager.GetSpawnPositionsAfter` and the existing `TowerPlacementSystem.CountCoveredEntrances`. It prioritizes post-placement entry coverage, then fewer open road ends and connected neighbors; `TilePlacementSystem` remains the commit owner.

R68 code proof: `PlayerSelectsTileWithHigherPostPlacementCoverage` covers the selector contract; direct validation returned no errors, `TD/Automation/Force Recompile All` produced no C# compiler errors, and the full EditMode gate passed `66/66` with `0` failed and `0` skipped. Play logs recorded `[MLAgent] Tile decision=index=1;name=Cross_3;openEnds=4->3;coverage=3/4->2/3` and `Straight` decisions. This proves the decision/readback path, not natural balance or final-wave Victory; the bounded episode reset before direct terminal telemetry.

### 9.15 Player-agent budget and route-aware tower placement

The player heuristic now evaluates every affordable authored tower instead of taking the first prefab index. The existing placement-slot owner provides the candidate's uncovered-entry gain and NavMesh route-sample exposure; when coverage ties, the selector prefers the cheaper tower only if the remaining bank can still fund one cheapest basic purchase. This keeps the two-tower opening an explicit economy decision without adding a second resource or placement owner.

R69/R70 proof: fresh Play logs recorded two opening `Tower_00 Novice` decisions at `25+25` from the `50` starting bank, with `basicReserveAfter=True` after the first purchase. A longer bounded run reached `WavesCompleted=2` with five authored towers and valid topology; the subsequent route-aware smoke recorded `TowerPlaced` route coverage `13/30` alongside anchor coverage. The selector and route-score contracts are covered in the `69/69` EditMode gate; direct validation and forced recompilation produced no C# errors. The run still reached only Wave 2 in the bounded capture, so this is policy/telemetry evidence, not natural final-wave Victory.

### 9.16 Player-agent opening counter role

The opening purchase policy now keeps the existing coverage and reserve priorities, but recognizes an authored `AoEWeapon` role when the first coverage tie occurs. On an empty Wave 1 build, the area role receives a bounded opening bonus; a single-entrance coverage advantage still wins over that role bonus. This routes the existing Tesla AoE counter through `TowerDefenceAgent` without changing `Tower`, `AoEWeapon`, costs, wave data, or purchase ownership.

Successful placement logs now include `role=area|single` and `openingDefense`, alongside cost, currency, anchor coverage, and reserve readback. R71 direct validation and forced recompilation produced no C# errors; the full EditMode gate passed `71/71`. The fresh bounded smoke committed Tesla first and Novice second, then read Wave 2 combat at `4 kills / 4 leaks; base=16/20` and later `11 kills / 9 leaks; base=8/20` with valid topology. MCP disconnected before a final terminal readback and was recovered through the Unity refresh path; no natural final-wave Victory claim is made.
### 9.17 Player-agent route-aware tile scoring

The player heuristic now evaluates each valid tile through the existing `TileMapManager.GetSpawnPositionsAfter`, `TowerPlacementSystem.BuildRouteSamples`, and `TowerPlacementSystem.CountCoveredRouteSamples` owners. Route-sample coverage is the primary score, anchor coverage is the secondary score, and open-road-end/connection terms remain bounded tie-breakers. `TilePlacementSystem` remains the commit owner; no duplicate map or route state was added.

Each committed decision emits `[MLAgent] Tile decision` with `routeCoverage=covered/total`. R72 direct validation and forced recompilation produced no C# errors; the full EditMode gate passed `72/72`, with `0` failed and `0` skipped. The bounded Play smoke recorded route readbacks `17/23`, `18/23`, and `19/23`. The same snapshot reached authored Wave 2 with two towers, `3/3` covered entrances, a valid map, and a live `PathComplete` NavMesh enemy. This is route-decision and gameplay telemetry evidence, not natural final-wave Victory evidence.

Play was stopped explicitly. Shared dirty/untracked changes remain preserved; no trainer, task chat, or worktree was created or restarted.

### 9.18 Opening reserve and map-pressure guard

The opening AoE role bonus is now eligible only when the purchase still preserves the cheapest basic tower. Coverage remains the dominant signal, so an area tower can still win when it adds a real entrance advantage; the role bonus no longer consumes a `50`-currency opening bank that can fund two `25`-cost basics. Tile-choice scoring also applies a bounded `500` penalty per post-placement open road end, keeping a small route-ratio gain from expanding the enemy frontier without limit. The existing `TowerDefenceAgent`, `TowerPlacementSystem`, and `TilePlacementSystem` remain the owners.

Placement telemetry now captures `openingDefense` before ghost creation and adds `openingAreaRoleEligible`, so the log is not polluted by the temporary preview object. R73 validation and forced recompilation produced no C# errors; the full EditMode gate passed `74/74`, with `0` failed and `0` skipped. Fresh Play logged `Tower_00 Novice` purchases `50->25` and `25->0`, then reached Wave 1 with `1` kill, `1` leak, base `19/20`, two towers, and valid topology. The bounded episode reset before direct terminal telemetry; natural final-wave Victory remains open.

### 9.19 Player-agent recovery and reinforcement guard

The reward policy now keeps `ResourceCache` when the base is damaged but recoverable, the bank is below the cheapest basic tower cost, and the build is not in the critical-health band. `EmergencyRepairs` remains reserved for a base at or below 50% health or a bank that can already buy the cheapest basic tower. After all current entrances are covered, the preparation heuristic also buys one more affordable tower before starting the next wave when no affordable upgrade exists. The existing `WaveManager`, `ResourceManager`, `TowerPlacementSystem`, and `TowerDefenceAgent` remain the owners.

The placement log now records `placementIntent=coverage|reinforcement` and `basicReserveAfter`; the reinforcement contract prevents a premature `StartWave` at `currency=27`, `cheapest=25`, `towers=2`, `entryCoverage=1.0` when no upgrade is affordable. R74 direct validation and forced recompilation produced no C# errors; the full EditMode gate passed `75/75`, with `0` failed and `0` skipped.

Fresh Play telemetry recorded `Reward decision=ResourceCache;base=14/20;currency=12;towers=2`. A separate bounded run reached authored Wave 2 with three Novice towers, `currency=0`, `currencySpent=75`, base `16/20`, `3` kills, `4` leaks, `TileMapValid=true`, and a live enemy with `PathComplete`. That seed used coverage placement for its third tower; the explicit reinforcement route is contract-covered but was not claimed from a natural log in this smoke. The run was stopped explicitly. Natural final-wave Victory/Defeat acceptance remains open.

### 9.20 Player smoke isolation and episode-restart hygiene

The editor-owned `Enable Gameplay Smoke Isolation` path now disables only the diagnostic Balance and EnemyLevel agents. The active player agent remains enabled and in its configured heuristic path, so isolation cannot freeze the preparation loop after the first action. Before reapplying isolation after a scene reload, destroyed diagnostic-agent references are pruned from the runtime tracking list.

R75 direct validation and forced recompilation produced no C# errors; the full EditMode gate passed `75/75`, with `0` failed and `0` skipped. The corrected Play smoke logged `2 diagnostic agents; player agent remains active`, progressed through authored Wave 1 into Wave 2, and read `WavesCompleted=1`, `3` towers, `2` kills, `5` leaks, base `15/20`, `TileMapValid=true`, and an active `PathComplete` enemy. Three subsequent scene-reload isolation logs still reported `2` agents, proving stale-count cleanup. This smoke did not capture a direct final terminal event; natural final-wave Victory/Defeat remains open.

### 9.21 Natural terminal readback hold

The smoke isolation owner now temporarily sets the active player's `RestartSceneOnEpisodeReset` to `false` while Play Mode is running. This preserves the first natural `Victory` or `Defeat` state long enough for `GameplayTelemetry` readback; the original serialized value is restored when isolation exits, and diagnostic agents remain the only agents disabled by the isolation menu.

R76 validation returned `0` C# errors for the changed agent and editor scripts (the editor script retains two existing analyzer warnings); forced recompilation returned `0` compiler errors and the full EditMode gate passed `75/75`, with `0` failed and `0` skipped. Fresh isolated ML inference reached authored Wave 2 and produced direct natural terminal telemetry: `BaseDestroyed` at sequence `163`, `GameStateChanged` to `Defeat` at `171`, `RunFinished` at `172` with `wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=2`, and `Defeat` at `173`. The same readback had `3` towers, `3` kills, `18` cumulative leaks, `TileMapValid=true`, and no generated-wave substitution. Play was stopped explicitly. Natural final-wave Victory remains the next balance gap; no Victory claim is made.

### 9.22 Coverage-preserving tile and strict preparation placement

The player heuristic now logs the final tile choice even when the default option is already selected. When a valid tile alternative preserves the current entrance-coverage ratio, tile scoring excludes options that reduce that ratio before applying the existing route/open-end/neighbor terms. During preparation, the coverage branch no longer commits an arbitrary no-gain slot; if coverage is still required but no valid slot is available, `Heuristic` and the action mask hold on `NoOp` instead of repeatedly rejecting `StartWave`. Placement-slot generation is cached for the current frame and invalidated after tower/tile commits, preventing repeated nested candidate scans. Prefab planning uses authored `TowerStatsSO.Range.BaseValue` when runtime `TowerStats` is not initialized yet.

R77 validation: `TowerDefenceAgent.cs` direct validation returned `0 warnings / 0 errors`; forced recompilation returned `0` compiler errors and retained only the two existing `CS0414` warnings. The full EditMode gate passed `76/76`, with `0` failed and `0` skipped. New logs include `Tile decision=phase=commit`, `Preparation hold=coverage`, and bounded tower/range slot diagnostics.

Fresh isolated ML inference fallback stayed responsive, placed authored towers through the existing placement owner, reached authored Wave 1 and then Wave 2, and read valid topology (`TileMapValid=true`, invalid/disconnected connections `0`). The Wave 2 snapshot had `WavesCompleted=1`, `currency=2`, `BaseHealth=20/20`, and `CoveredEntrances=3/4` at the active-wave boundary. Play was stopped explicitly. R76 remains the direct natural Defeat proof; natural final-wave Victory remains unproven.

### 9.23 Opening reserve and reachable coverage gate

The opening tower selector now applies a bounded reserve penalty when a valid affordable candidate would consume the bank needed for one cheapest basic tower. A one-entrance coverage advantage cannot consume that reserve; a two-entrance advantage still can. The existing coverage, area-role, and placement owners remain unchanged. The committed tower log adds `openingReserveGuard=preserved|spent|n/a`.

Coverage preparation now holds only when an affordable coverage placement is actually reachable through the existing placement-slot owner. If the map and current slots cannot improve an uncovered entrance, the player emits `Coverage gate=unreachable` and starts the next authored wave instead of soft-locking in `NoOp`. The pure contract covers both reserve selection and reachable-vs-unreachable coverage gating.

R78 validation passed with `TowerDefenceAgent.cs` at `0` warnings and `0` errors; the contract file retained its three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors. Full EditMode passed `79/79`, with `0` failed and `0` skipped.

Fresh isolated ML Play reached authored Wave 2 with three Novice towers and valid topology. Wave 1 telemetry recorded `targetAcquisitions=3`, `towerFires=6`, `damageApplications=6`, `kills=2`, `leaks=5`; the natural terminal trace then recorded `BaseDestroyed` seq `216`, `RunFinished` seq `224` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=6`), and `Defeat` seq `225`. `HasGeneratedWave=false` and `TileMapValid=true`. Natural final-wave Victory remains unproven.

### 9.24 Terminal wave resolution and spawn guard

The existing `GameManager` keeps the defeat presentation delayed, so `WaveManager` must stop terminal simulation at the owner boundary. `WaveManager` now suppresses enemy rewards and wave completion after `PlayerBase.IsDestroyed` or `GameManager.IsGameOver`, and the async spawn loop stops before creating another enemy after the base is destroyed. No new terminal state owner or payout path was added.

R79 was driven by a fresh baseline: `BaseDestroyed` seq `186` was followed by currency events and `WaveCompleted` seq `196` before delayed `Defeat`. After the guard, the first smoke ended with `BaseDestroyed` seq `177`, `RunFinished` seq `181` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=29`) and no `WaveCompleted`; the follow-up smoke also proved no post-terminal `EnemySpawned`, `CurrencyGained`, or `WaveCompleted` (`BaseDestroyed` seq `196`, `RunFinished` seq `200`, `Defeat` seq `201`, `currency=4`, `IsSpawning=false`). Direct validation returned `0` warnings / `0` errors, forced recompilation returned no compiler errors, and full EditMode passed `80/80` with `0` failed and `0` skipped. Play was stopped explicitly. Natural final-wave Victory remains unproven.

### 9.25 Placement-owner rejection handoff

The preparation planner and the existing `TowerPlacementSystem` commit owner now share a bounded rejection handoff. `TryPlaceTowerAtScreenPosition` logs surface-point and blocking-intersection rejections. `TowerDefenceAgent` probes the existing commit owner across the current candidate slots in one placement action; after the owner rejects all candidates, it records `Placement gate=owner-rejected`, stops prioritizing stale placement previews, and lets the existing upgrade/`StartWave` preparation policy continue. The rejection cache is invalidated after an authored tower/tile commit, wave completion, or episode reset. No second placement or resource owner was added.

R80 validation: direct validation returned `0` warnings / `0` errors for `TowerDefenceAgent.cs`; `TowerPlacementSystem.cs` retained two analyzer warnings and `0` errors; the contract file retained three analyzer warnings and `0` errors. After fixing definite-assignment diagnostics surfaced by Unity Console, `TD/Automation/Force Recompile All` returned `0` compiler errors. Full EditMode passed `81/81`, with `0` failed and `0` skipped.

Fresh isolated Play evidence progressed from `Preparation` sequence `84` to authored `WaveStarted` sequence `87` without the previous repeated preview loop. The run logged `Coverage gate=unreachable;covered=1/2;currency=25;action=StartWave`, then naturally reached `BaseDestroyed` sequence `159`, `RunFinished` sequence `163` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=25`), and `Defeat` sequence `164`; the terminal snapshot had `IsSpawning=false`, `TileMapValid=true`, and no active enemies. This seed did not hit a blocking-intersection rejection, so no captured `owner-rejected` log is claimed; the new rejection path is covered by code/contract and remains observable when that owner rejection occurs. Natural final-wave Victory remains unproven. Play was stopped explicitly; no trainer, new task chat, or worktree was created or restarted.

### 9.26 Mid-wave playable count and route-reinforcement diagnostic

The authored `Wave_02` count scale was changed from `1.10` to `1.00` through Unity AssetDatabase, making the expected mid-run enemy count `14`. The existing `TowerDefenceAgent` now separates coverage intent from reinforcement intent. When coverage is unavailable, no affordable upgrade exists, the bank can buy the cheapest tower, and at least one tower already exists, it may choose a free placement only when the existing route-sample owner reports an actual route-coverage gain. No new placement, economy, or route owner was introduced.

The existing ML decision log records `placementIntent=coverage|reinforcement`; `GameplayTelemetry` remains the runtime readback for tower commits, preview coverage, route coverage, wave events, and terminal state. R81 validation returned `0 warnings / 0 errors` for `TowerDefenceAgent.cs` and `WaveProgressionContractTests.cs`; the ML contract file retained its three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors, and full EditMode passed `83/83` with `0` failed and `0` skipped.

Fresh isolated ML Play observed `TotalEnemiesInWave=14`, three Novice towers, authored Wave 1 completion, and authored Wave 2 start. The terminal readback was `BaseDestroyed`, `RunFinished` with `wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=4`, and `Defeat`; the snapshot had `4` kills, `16` cumulative leaks, `EntryCoverageRatio=0.5`, `TileMapValid=true`, and `HasGeneratedWave=false`. The third tower commit did not add anchor coverage in this seed. This is not Victory proof. The next smallest diagnostic is a clean telemetry cursor around the third placement to reconcile the planner projection with the real placement preview/commit and then separate route coverage from combat viability. No trainer, new task chat, or worktree was created or restarted.

### 9.27 Counter-aware opening and combat-power selector

The authored Wave 1 enemy count is now used as bounded opening intel by the existing player agent. When the upcoming count reaches the swarm threshold, an affordable area-role tower may spend the opening reserve; otherwise the existing cheapest-basic reserve guard remains active. Tower selection also accepts the authored planning combat power (`Damage * FireRate`, with the existing area role multiplier) so a minor coverage edge cannot select a materially weaker combat tower. The selector keeps its old coverage-only behavior for callers that do not provide combat power, and `TowerPlacementSystem` remains the sole placement owner.

The existing `[MLAgent] Tower decision` log now records `openingAreaCounterEligible` and `combatPower`. `GameplayTelemetry` remains the causal runtime readback for tower commits, wave combat (`targetAcquisitions -> towerFires -> damageApplications -> kills/leaks`), rewards, terminal state, and valid topology. The contract suite covers both the opening area-counter exception and rejection of a low-power tower for a minor coverage gain.

R82 validation returned `0 warnings / 0 errors` for `TowerDefenceAgent.cs`; the ML contract retained its three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors. Focused selector contracts passed `2/2`, and the full EditMode gate passed `85/85`, with `0` failed and `0` skipped.

Fresh isolated ML Play selected `Tower_01 Tesla` for the swarm opening (`combatPower=15.00`, `openingAreaCounterEligible=True`), then selected `Tower_00 Novice` after Wave 1 (`combatPower=9.00`) instead of the lower-power coverage-biased option. Wave 1 completed with `4` kills and `3` leaks; Wave 2 completed with `3` kills and `11` leaks at `base=1/20`, then the existing `EmergencyRepairs` reward raised the base to `11/20`. The agent entered authored Wave 3 (`41` enemies), but natural terminal telemetry recorded `BaseDestroyed` sequence `441`, `RunFinished` sequence `455` (`wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=32`), and `Defeat`; the final snapshot had `16` cumulative kills, `26` leaks, `TileMapValid=true`, and `HasGeneratedWave=false`. This is a confirmed improvement to wave reach and selector behavior, not final-wave Victory proof. The next balance gap is Wave 2/3 survival, to be addressed from combat/economy telemetry rather than by another placement-owner change. Play was stopped explicitly; no trainer, new task chat, or worktree was created or restarted.

### 9.28 Emergency repair recovery band and reward telemetry

`EmergencyRepairs` remains owned by `WaveManager`, but its effect is now explicit and scalable: it restores the bound `PlayerBase` to the 75% recovery band, using `ceil(maxHealth * 0.75) - currentHealth`. The owner resets per-offer result fields before applying a choice, so `ResourceCache` cannot leak a previous repair amount into later telemetry. `GameplayTelemetry` includes `baseRepair` alongside the existing currency `amount` in `RewardSelected` details.

The new pure contract passed `1/1`; direct validation returned `0` warnings and `0` errors for `WaveManager.cs`, `GameplayTelemetry.cs`, and `RewardOfferContractTests.cs`; forced recompilation returned `0` compiler errors; and the full EditMode gate passed `86/86`, with `0` failed and `0` skipped. A fresh isolated ML Play reached the recovery branch at `base=1/20`: telemetry recorded `BaseHealthChanged` sequence `199` from `1` to `15`, then `RewardSelected` sequence `200` with `rewardId=EmergencyRepairs;amount=0;baseRepair=14;currencyAfter=94`. The agent entered authored Wave 3 at `base=15/20`, but the run still ended naturally in `Defeat` (`RunFinished` sequence `481`, `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=44`; `Defeat` sequence `482`). `TileMapValid=true` and `HasGeneratedWave=false`; this confirms reward application and telemetry, not final-wave Victory. The next gap remains Wave 2/3 combat survival. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

### 9.29 Authored Wave 2 pressure correction

The authored `Wave_02` composition was reduced through Unity AssetDatabase from `9 Turtle + 5 Frog + 1 Boss No Damage` to `8 Turtle + 4 Frog`, preserving the Turtle/Frog roles and deferring the boss to the authored final wave. The existing `WaveProgressionContractTests` now asserts the `12`-enemy composition; no wave owner or adaptive-generation path was added.

R84 direct validation returned `0` warnings and `0` errors for `WaveProgressionContractTests.cs`; forced recompilation returned `0` compiler errors; the focused progression contract passed `1/1`; and full EditMode passed `86/86`, with `0` failed and `0` skipped. Fresh isolated ML telemetry completed authored Wave 2 at `7 kills / 5 leaks`, repaired base `4->15`, and entered Wave 3 with `TotalEnemiesInWave=41`. This is a bounded pressure improvement, not a final-wave Victory claim.

### 9.30 Combat-power reinforcement over upgrade

When all current entrances are covered and both a new tower and an upgrade are affordable, the existing `TowerDefenceAgent` now compares the candidate planning combat power with the selected upgrade's marginal gain. A new tower is preferred only when its power is greater than twice that gain. The combat-power branch may use any valid free placement and does not require a new route-coverage sample; `TowerPlacementSystem` remains the sole commit owner. The existing decision log adds bounded `placementReason=coverage|route-reinforcement|combat-power-over-upgrade|reinforcement` readback.

R85 direct validation returned `0` warnings and `0` errors for `TowerDefenceAgent.cs`; the ML contract retained its three existing analyzer warnings and `0` errors. The pure combat-power contract passed `1/1`, forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, with `0` failed and `0` skipped. The final isolated ML smoke logged a Tesla reinforcement at full coverage: `currency=52->12;coverage=4/4->4/4;placementIntent=reinforcement;placementReason=combat-power-over-upgrade`. Runtime status reached authored Wave 3 with `4` towers, `4/4` coverage, `22` kills, `24` leaks, and valid topology, but ended naturally in `Defeat` at `base=0/20`, `wavesCompleted=2`, `HasGeneratedWave=false`. The policy branch is runtime-confirmed; final-wave Victory remains unproven. Play was stopped explicitly; no trainer, task chat, or worktree was created or restarted.

### 9.31 Final-wave density probe and archetype terminal telemetry

The authored final wave was reduced through Unity AssetDatabase from `20 Turtle + 20 Frog + 1 Berserker` (`41`) to `12 Turtle + 8 Frog + 1 Berserker` (`21`), preserving the final boss and both enemy roles. `GameplayTelemetry` now records `archetype`, `maxHealth`, and `terminalReason` on `MonsterDeath`, and adds a parallel `MonsterLeak` event, so final-wave balance can be judged by terminal enemy identity instead of aggregated leak counts alone.

The new progression contract passed; direct validation reported `0` warnings and `0` errors for the changed telemetry and contract scripts, forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, with `0` failed and `0` skipped. Fresh isolated ML Play still reached `Defeat`: Wave 2 completed at `2 kills / 10 leaks`, Emergency Repairs read `base=3->15`, and Wave 3 ended at `base=0/20` with `15` cumulative kills, `23` cumulative leaks, four towers, `4/4` entrance coverage, valid topology, and `HasGeneratedWave=false`. Terminal telemetry identified Wave 3 `Runner;maxHealth=97.50` leaks and `Tank;maxHealth=32.50` deaths/leaks. The density probe is not Victory evidence; the next smallest gap is Wave 2 combat survival and two-tower preparation, not another uninstrumented final-wave count change. Play was stopped explicitly; no trainer or task-chat restart was needed.

### 9.32 AoE owner-chain telemetry and clean ML smoke

The existing `AoEWeapon` now resolves `MonsterHealth` through the collider's parent owner and emits bounded `overlaps`, `resolvedTargets`, `damage`, and `range` readback when its authored logging flag is enabled. The player-agent opening swarm threshold remains `7`; the attempted threshold `8` did not improve the live run and was reverted. No weapon, tower, placement, or economy owner was duplicated.

Direct validation returned `0` errors for `AoEWeapon.cs` and `TowerDefenceAgent.cs`; it reported one non-blocking logging warning for `AoEWeapon.cs` and the existing three analyzer warnings for the ML contract. Forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, `0` failed, `0` skipped. A clean isolated ML Play produced `overlaps=1..3;resolvedTargets=1..3` AoE logs, confirming the damage path is resolving targets. The run entered authored Wave 3 with four towers, `3/3` entrance coverage, valid topology, `16` kills, and `22` leaks, then naturally ended in `Defeat` at `base=0/20` (`wavesCompleted=2`, `TotalEnemiesInWave=21`, `HasGeneratedWave=false`). Wave 1 still exposed an opening Tesla shot at `distance=5.65` against `range=5.50` with `0` damage applications; the next slice is range/exposure alignment and Wave 2/3 combat survival, not another threshold change. Play was stopped explicitly; no trainer or task-chat restart was needed.

### 9.33 EffectiveRange alignment and bounded balance probe

The existing `AoEWeapon` now uses the owning `Tower.EffectiveRange` for its overlap query, aligning physical area damage with the range already used by tower target acquisition and ML planning. Its bounded diagnostic reports the effective range together with overlap and resolved-target counts; no second range or combat owner was introduced. The Tesla prefab remains on its authored `Nearest` target priority: two temporary `Farthest` smokes produced only `14-15` kills, `22-23` leaks, three towers, and `3/4` coverage, so the experiment was reverted through `PrefabUtility`.

The persistent range-alignment smoke improved the causal opening readback: Wave 1 completed at `5 kills / 2 leaks`, Wave 2 at `7 kills / 5 leaks`, and the run entered authored Wave 3 with four towers, `4/4` coverage, valid topology, `21` cumulative kills, and `17` cumulative leaks before natural `Defeat` at `base=0/20`. A temporary Wave 3 Runner health probe (`1.5 -> 1.0`) was then rejected: its smoke completed all `21` spawns but ended at `20 kills / 20 leaks`, with Wave 1 `4/3`, Wave 2 `4/8`, and Wave 3 terminal telemetry showing twelve Tank kills against eight Runner plus one Berserker leaks. The asset was restored to `1.5`; no unproven balance change remains.

After cleanup, direct validation returned `0` errors for the changed AoE and progression-test paths, forced recompilation returned `0` compiler errors, and full EditMode passed `87/87`, `0` failed, `0` skipped. Natural final-wave Victory remains unproven. The next slice is Wave 2/3 survival and reward/economy exposure using the existing owners and the new terminal telemetry, not another blind health multiplier change. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

### 9.35 Delayed bounty guard and reward-decision telemetry

The existing `BountyContract` owner path was already implemented by `WaveManager` and exposed by `WaveUI`, but the player heuristic never selected it. `TowerDefenceAgent` now selects that existing reward only when a future authored wave remains, the base is above the 75% repair band, at least two towers and full entrance coverage are present, and the bank can still buy the cheapest tower. Emergency Repairs keeps priority when health is low; ResourceCache remains the catch-up path. The bounded reward log now includes `wave=current/total` and `coverage=covered/total`.

Direct validation returned `0/0` for `TowerDefenceAgent.cs`; the contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML inference ended naturally in `Defeat`: Wave 1 selected `ResourceCache` at `base=17/20;currency=28;towers=1;coverage=2/4` and completed `4 kills / 3 leaks`; Wave 2 selected `EmergencyRepairs` at `base=5/20;currency=96;towers=2;coverage=3/4`, completed `4 kills / 8 leaks`, and repaired base `5->15`. Wave 3 terminal telemetry recorded twelve Tank kills and eight Runner plus one Berserker leaks; the run ended at `20 kills / 20 leaks`, `base=0/20`, four towers, valid topology, and `HasGeneratedWave=false`. The bounty branch is contract-covered but not claimed as a natural runtime selection because its safety preconditions were correctly false in this seed. Natural Victory remains unproven; the next gap is Wave 2 Runner exposure and combat survival. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

### 9.36 Target archetype exposure telemetry

`GameplayTelemetry` now includes `archetype` and `priority` in `TowerTargetAcquired` and `TowerFired`, alongside range, distance, and target health. This extends the existing combat journal without changing `Tower` targeting ownership. A temporary Wave 2 Runner health probe (`1.0 -> 0.8`) was rejected: Runner max health became `36.00`, but all four Wave 2 Runners still leaked and the run ended at `14 kills / 22 leaks`; `Wave_02` and its contract were restored to `healthMultiplier=1.0`.

The fresh authored-baseline smoke recorded Wave 2 `17` target acquisitions, `19` fires, `26` damage applications, `5` kills, and `7` leaks. `Nearest` acquired five Runner targets and fired at four of them, proving the Runner path is reachable; the remaining gap is exposure/damage throughput and build timing, not another global target-priority change. The run naturally ended in `Defeat` with `RunFinished` `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=28`; no Victory claim is made. Direct validation returned `0/0` for the telemetry and progression paths, forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode remained `88/88`. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

## R91 Wave 2 spawn-pacing probe rejected

The existing authored `Wave_02` Runner/Frog group was temporarily changed through Unity AssetDatabase from `spawnDelay=2` to `3` to test whether slower exposure would improve the existing combat path. The existing `WaveManager` spawn owner and targeting priority were unchanged. The probe was rejected: Wave 2 recorded `19` target acquisitions, `18` tower fires, `27` damage applications, `5` kills, and `7` leaks; five Runner acquisitions and four Runner fires still produced four Runner leaks. This matched the latest authored baseline outcome (`5/7`) and did not improve survival, so `Wave_02` was restored to `spawnDelay=2` and the temporary contract assertion was removed.

Direct validation returned `0/0` for `WaveProgressionContractTests.cs`; forced recompilation returned `0` compiler errors; the Console error filter returned no errors; and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable, entered authored Wave 3, and ended naturally in `Defeat` (`RunFinished`: `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=9`), with no Victory claim. Play was stopped explicitly. The persistent instrumentation and bounty guard remain; the next gap is damage throughput/build timing, not another spawn-delay probe.

## R92 Final-wave upgrade reserve

The existing player-agent preparation path now treats the phase before the final authored wave as a final-wave upgrade reserve when the latest completed wave is immediately before the final wave, entrance coverage is complete, and an upgrade is affordable. It suppresses reinforcement placement only in that bounded state; incomplete coverage still keeps the coverage obligation. The upgrade continues through the existing `Tower.UpgradeSpendingCost` and `ResourceManager` owners. The commit log now records `reason=final-wave-upgrade-reserve` and `wave=current/total`.

The new pure contract passed; direct validation returned `0/0` for `TowerDefenceAgent.cs`, while the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors and full EditMode passed `89/89`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable and captured two final-preparation upgrades (`Tesla 0->1`, `Tesla 1->2`) with full `coverage=1/1`; the terminal snapshot read `TowersUpgraded=2`, `wavesCompleted=2`, `BaseHealth=0/20`, `HasGeneratedWave=false`, followed by natural `Defeat`. The branch is runtime-confirmed, but final-wave Victory remains unproven. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

### 9.38 Spawn role and pacing telemetry

`WaveManager` remains the sole spawn owner and now publishes the last spawned group/index/archetype/scaled health/scaled speed/spawn delay for `GameplayTelemetry`. `EnemySpawned` details are structured as `group`, `enemy`, `archetype`, `health`, `speed`, and `spawnDelay`, which makes authored group order and runtime scaling directly readable. A temporary Runner-first Wave 3 order probe was rejected after `12` Runner acquisitions, `8` fires, `0` kills, `8` leaks, base destruction after `10/21` spawns, and total `8` kills / `19` leaks in that seed; authored order was restored to `Tank -> Runner -> Berserker` through Unity AssetDatabase.

Changed scripts validated with `0` warnings and `0` errors, forced recompilation finished without compiler errors, and full EditMode passed `89/89`, `0` failed, `0` skipped. Two fresh Play attempts were blocked by Unity immediately leaving Play Mode (`EditorApplication.isPlaying=false`) after MCP reported entry, so no new gameplay or ML result is claimed. The next loop slice is final-wave Runner throughput measured with this spawn telemetry, not another blind balance change.


### 9.39 Runtime compile correction and Play transition blocker

R93 exposed a latent compile defect in the new spawn telemetry: `scaledHealth` was declared inside the health-initialization block but consumed by the following telemetry block. Unity Editor.log caught the real `CS0103` during compilation; the fix hoisted the local to `SpawnEnemy` scope and did not change serialized assets or balance. Direct validation then returned `0` warnings and `0` errors for the changed C# files, forced recompilation finished without compiler errors, and full EditMode passed `90/90`, `0` failed, `0` skipped. Unity still reports the existing non-blocking `CS8785` Odin source-generator warning.

After the compile fix, Play entered `is_playing=true` but remained `is_changing=true` for `40-47` seconds on repeated attempts. The temporary editor-only toggle of Enter Play Mode Options did not change the transition and was restored to `enabled=true;options=3`. Play was stopped through MCP; no gameplay telemetry, ML-agent result, or Victory claim is made. The next loop slice remains final-wave Runner throughput once Unity Play transition is healthy.


### 9.40 Runtime spawn-role readback

The repaired telemetry refactor was runtime-confirmed in an isolated ML Play smoke using inference fallback because no trainer was connected. `EnemySpawned` readback preserved authored groups and scaling: Wave 1 `Tank 7/7`, `health=20.00`, `speed=3.00`, `spawnDelay=1.00`; Wave 2 `Tank 8/8`, `health=24.75`, `speed=3.30`, `spawnDelay=1.00`, then `Runner 4/4`, `health=45.00`, `speed=3.00`, `spawnDelay=2.00`; Wave 3 reached `Tank 12/12`, `health=32.50`, `speed=3.00`, `spawnDelay=0.50`, then `Runner 5/8`, `health=97.50`, `speed=4.50`, `spawnDelay=1.00` before the base was destroyed. The run produced `19` target acquisitions, `23` tower fires, and `46` damage applications; terminal telemetry identified Runner and Tank leaks by archetype. The final snapshot was `Wave 3`, `17/21` spawned, `8` kills, `28` leaks, `2` towers, `2/4` coverage, `base=0/20`, natural `Defeat`, and no Victory. Play was stopped explicitly; the next gameplay gap is preparation/combat survival, not missing spawn observability.

### 9.41 Route-reinforcement priority under incomplete coverage

The existing `TowerDefenceAgent` preparation policy now allows route reinforcement to outrank an affordable upgrade when entrance coverage is incomplete and no placement can directly cover another entrance. The branch still requires an affordable tower, an existing tower, incomplete coverage, and a valid route-contributing placement; the final-wave upgrade reserve still requires complete coverage. The change is confined to the existing ML decision helper and placement owner chain.

The contract now asserts route reinforcement before upgrade in that state. Direct validation returned `0` warnings and `0` errors for `TowerDefenceAgent.cs`; the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation finished without compiler errors and full EditMode passed `90/90`, `0` failed, `0` skipped. In a fresh isolated ML Play with inference fallback, the existing `TowerPlaced` telemetry recorded `Tesla`, `cost=40`, `coverage=3/3`, and `routeCoverage=35/37` before Wave 3. The run ended naturally in `Defeat` at `3` towers, `3/3` coverage, `14` kills, `23` leaks, `19/21` spawns, and `base=0/20`; Play was stopped explicitly and Victory remains unproven. The next slice is final-wave combat throughput, especially Runner leaks.

### 9.42 Tower-grid input ownership repair

Manual placement was reading the synthetic mouse because the Gameplay scene kept ML training input and a standalone `Synthetic Mouse` enabled. That forced the tower raycast through the synthetic cursor, including its origin state, instead of the hardware pointer. The authored scene now starts with ML agents in `_trainingMode=0` and the standalone synthetic source disabled. The existing ML agent can still be enabled temporarily for an isolated smoke; no second input provider was added.

Verification: after the scene change, the manual-default Play probe found no active synthetic source; the temporary ML smoke committed `TowerPlaced` at `(-3.00, 0.50, 4.00)`, preserving integer tower-grid coordinates. Full EditMode passed `90/90`, `0` failed, `0` skipped; Play was stopped explicitly. The next runtime check for manual acceptance is a focused hardware-cursor click in the game window.

### 9.43 Target retention and ML-owned synthetic input

The combat loop keeps `Tower` as the target owner. A live target is retained when it is present in the current physical overlap result; target retention no longer uses a conflicting transform-center distance test. This keeps acquisition, retention, and effective-range behavior on one collision contract and prevents fast enemies at the collider boundary from causing per-frame target churn. The existing `Logs` flag can emit bounded target-loss diagnostics with distance and effective range.

ML gameplay input is authored inactive under the active `TD ML Agent`; the agent's `TrainingMode` setter and `Start` enable that nested `SyntheticMouse` only when ML gameplay is active. The standalone root `Synthetic Mouse` is kept inactive, so `InputProvider_NewInputSystem` has one runtime synthetic owner. Manual gameplay keeps the hardware mouse path. Do not enable the standalone object to simulate ML input.

Runtime proof after this change: active player `training=true`, nested input active, root synthetic inactive, and telemetry input device `TD Synthetic Mouse`. A clean run began at `Preparation` with `50` currency and `0` towers; full EditMode remained `90/90`. The inference smoke reached Wave 3 but ended naturally in `Defeat` with two towers and `2/4` coverage; `TileMapValid=true` and a boundary Runner target at `5.13/5.50` stayed acquired. This is combat/build evidence, not a Victory result.
