---
title: Core loop одной волны
status: design-target
updated: 2026-08-04
scope: one wave, player actions, immediate effects, feedback, resolution
excludes: meta progression, complete run loop, long-term unlocks
related: Assets/Documentation/GAMEPLAY_REFERENCES.md, Assets/Documentation/GAMEPLAY_SCENE_OBJECTS.md
---

# Core loop одной волны

## 1. Граница документа

Документ описывает одну повторяемую gameplay-итерацию:

`получить информацию о волне → подготовить оборону → зафиксировать решение → пережить атаку → получить результат`.

Начало итерации — игроку доступны состав текущей входящей волны, карта, башни и бюджет подготовки. Конец — все враги текущей волны убиты или достигли базы, результат рассчитан, награды этой волны выданы и показан итог.

Не входят:

- выбор нового забега, seed, loadout и difficulty;
- последовательность всех волн забега;
- victory полного забега;
- мета-валюта, unlock и постоянные улучшения;
- следующий reward offer и решения следующей волны;
- ручное управление героем и обязательные active abilities.

Это design-target. Текущие код, сцена, prefab и assets остаются источником истины о фактической реализации.

## 2. Core loop одной строкой

> Увидеть угрозу → выбрать контрмеру → разместить ограниченный бюджет → запустить волну → увидеть, как билд взаимодействует с врагами → получить деньги, повреждение базы и объяснимый результат.

Смысл цикла не в самом нажатии `Start Wave`, а в проверке решения игрока реальной симуляцией.

## 3. Фазы волны

```text
WavePreparation
├── ThreatIntel
├── LimitedWaveRewardChoice
├── MapDecision
├── BuildDecision
└── Commit
        ↓ StartNextWave
WaveActive
├── Spawn
├── Move
├── Target
├── Attack
├── Kill / Leak
└── TacticalObservation
        ↓ spawn exhausted && enemiesAlive == 0
WaveResolve
├── CompletionPayout
├── StateSummary
└── Feedback
```

На уровне текущей state machine это укладывается в:

`Preparation → WaveActive → WaveResolve`.

`MapDecision`, `BuildDecision` и `Commit` — подэтапы подготовки, а не обязательные новые глобальные состояния или managers.

## 4. Что игрок должен решить

Перед каждой волной игрок отвечает на четыре вопроса:

1. **Что придёт?** Количество, роли врагов, armor/shield, скорость, lane и опасные traits.
2. **Где их остановить?** Маршрут, choke point, длина обстрела, доступные build positions.
3. **Чем их остановить?** Новая башня, upgrade, специализация, target policy.
4. **Сколько потратить сейчас?** Потратить весь бюджет на текущую угрозу или оставить резерв.

В базовом loop хотя бы два ответа должны иметь несколько допустимых вариантов. Если подготовка сводится к одной обязательной покупке или одной валидной позиции, выбора нет.

## 5. Фаза Preparation

### 5.1 ThreatIntel

Игрок видит до расходов:

- общее количество врагов;
- последовательность или группы спавна;
- основные роли: runner, tank, armored, shielded, support;
- lane/spawn direction, если их несколько;
- ожидаемые resistances, иммунитеты и leak damage;
- особое правило волны, если оно существует.

**Действие игрока:** открыть карточку/панель волны, выбрать группу или trait для подробностей.

**Непосредственный эффект:** runtime state не меняется; уменьшается неопределённость решения.

**Gameplay-эффект:** игрок может осознанно выбрать damage type, control, range и позицию, а не угадывать.

**Feedback:** иконки ролей, количество, понятные подсказки «shield», «armor», «fast», подсветка lane и точки появления.

Если скрытая информация является особенностью конкретного challenge, скрытие должно быть явно обозначено. Отсутствие данных из-за незаполненной Definition не считается fog of war.

### 5.2 LimitedWaveRewardChoice

Это условный подэтап: он присутствует только если для текущей волны уже предоставлен limited reward offer.

**Действие игрока:** выбрать один из небольшого числа вариантов, например временный modifier, resource cache, repair или tower augment.

**Непосредственный эффект:** выбранный эффект применяется ровно один раз к своему владельцу.

**Gameplay-эффект:** меняет доступный бюджет, прочность базы или специализацию обороны текущей волны.

**Feedback:** до выбора показывается точное изменение; после выбора карточка фиксируется, значения HUD обновляются, повторный выбор блокируется.

Стартовый challenge modifier не является частью этого подэтапа: он выбирается ровно один раз в `GameState.ChallengeSelection` до первой волны и обязателен. Между волнами повторного challenge выбора нет; здесь остаётся только текущий limited reward offer.

### 5.3 MapDecision

Подэтап существует, если правила текущей волны разрешают размещение terrain tile.

**Действия игрока:**

- просмотреть варианты тайла;
- выбрать вариант;
- повернуть его;
- навести на допустимую клетку;
- подтвердить размещение.

**Проверка до подтверждения:**

- клетка свободна;
- стыки тайла совместимы;
- spawn points остаются соединены с базой;
- база и обязательные anchors достижимы;
- tile не перекрывает занятые tower cells;
- известна цена, если действие платное.

**Непосредственный эффект:** после подтверждения меняется `TileMap`; при платном размещении `ResourceManager.TrySpend` списывает цену атомарно.

**Gameplay-эффект:** меняются маршрут, его длина, choke points, build positions, firing coverage и иногда количество открытых lane.

**Telemetry-контракт:** `GameplayTelemetry` фиксирует `TilePlacementChoiceSelected`, `TilePlaced` или `TilePlacementCancelled` с индексом варианта и snapshot до/после изменения карты.

**Derived effect:** инвалидируются route/NavMesh caches, затем выполняется один rebuild подтверждённой карты. Preview не перестраивает NavMesh и не меняет runtime state.

**Feedback:** ghost tile, валидный/невалидный цвет, причина отказа, preview нового маршрута, открывающиеся build cells и зоны покрытия существующих башен.

Если размещение отклонено, карта и деньги не изменяются.

### 5.4 BuildDecision

Игрок распределяет доступный бюджет между новыми башнями и усилением существующих.

#### Разместить башню

**Действия:** выбрать Tower Definition → навести на build position → увидеть preview → подтвердить.

**Проверка:** доступность Definition, цена, занятость клетки, footprint, buildable flag и дополнительные ограничения башни.

**Непосредственный эффект:** `TrySpend(cost)` → создаётся `Tower` → позиция регистрируется занятой.

**Gameplay-эффект:** появляется новая зона поражения, новый damage/effect profile и новая пропускная способность обороны.

**Feedback:** радиус/arc, видимая цена, призрак башни, анимация строительства, изменение денег, первый доступный target indicator.

#### Улучшить башню

**Действия:** выбрать башню → сравнить текущий и следующий уровень → подтвердить upgrade.

**Проверка:** допустимый уровень/ветка, цена, несовместимые варианты и состояние волны.

**Непосредственный эффект:** списывается стоимость; `Tower` меняет runtime level/branch и пересчитывает derived stats.

**Gameplay-эффект:** меняются конкретные параметры: damage, attack interval, range, projectile, radius, damage type, status или aura.

**Feedback:** UI показывает старое → новое значение, изменившийся stat подсвечивается, башня получает визуальный/audio cue.

#### Изменить target policy

**Действие:** выбрать `first`, `last`, `nearest`, `strongest`, `weakest`, `lowest shield` или другую поддерживаемую policy.

**Непосредственный эффект:** меняется правило следующего выбора цели этой башней.

**Gameplay-эффект:** тот же DPS распределяется иначе: добивание, фокус tank, снятие shield, удержание runner.

**Feedback:** активная policy видна в панели и над выбранной башней; смена не должна выглядеть как stat upgrade.

#### Продажа и перенос

Это Extended-вариант, не обязательный Basic loop.

- продажа возвращает явно показанную долю вложений и освобождает клетку;
- перенос имеет цену/ограничение и не допускается после commit;
- нельзя продавать и покупать через разные цены для бесконечного дохода.

### 5.5 Commit

**Действие игрока:** нажать `Start Wave`.

**Единственный entry point:** `GameManager.StartNextWave()`.

Перед переходом проверяются:

- текущее состояние равно `Preparation`;
- обязательный map/choice step завершён;
- существует хотя бы один валидный spawn-to-base route;
- NavMesh и spawn anchors готовы;
- определена текущая `WaveDefinition`;
- нет уже запущенного spawn task.

**Непосредственный эффект:** подготовительные команды блокируются; `GameManager` разрешает `WaveManager` запустить текущую волну.

**Gameplay-эффект:** решение становится обязательством. В Basic-варианте нельзя строить, улучшать или менять карту до `WaveResolve`.

**Feedback:** кнопка меняет состояние, preview скрываются, UI переходит в combat layout, звучит wave-start cue, spawn direction подсвечивается.

Повторное нажатие не создаёт вторую волну.

## 6. Фаза WaveActive

### 6.1 Разрешённые действия игрока

В Basic loop игрок может:

- двигать, масштабировать и вращать камеру;
- выбирать башни и врагов для просмотра;
- ставить и снимать pause;
- смотреть текущий состав, progress, базу и деньги;
- менять target policy, если это разрешено контрактом башни.

В Basic loop игрок не может:

- размещать или продавать башни;
- покупать upgrades;
- менять terrain;
- повторно запускать волну;
- вручную наносить урон врагам.

Деньги за убийства начисляются сразу и видны, но тратятся только в следующей `Preparation`. Это сохраняет смысл commit и позволяет честно оценить подготовленный билд.

Active abilities, emergency build/repair и mid-wave tower placement — Extended-механики. Они не входят в минимальное ядро.

### 6.2 Spawn

`WaveManager` проходит authored spawn schedule.

Для каждой записи определены:

- enemy Definition/prefab;
- количество;
- spawn interval;
- delay группы;
- lane/spawn point;
- wave multipliers.

**Эффект:** создаётся враг, инициализируются HP/shield/speed/reward, увеличивается alive count, UI обновляет spawn progress.

Враг с невалидной обязательной Definition не заменяется fallback-врагом. Ошибка блокирует корректное выполнение волны и должна быть видна.

### 6.3 Move и exposure

Враг движется от spawn point к базе по валидному маршруту.

Карта влияет на бой через:

- длину пути;
- время нахождения в range каждой башни;
- порядок входа в зоны разных башен;
- choke points и плотность группы;
- разделение по lane;
- line-of-sight/elevation, если эти правила включены.

Упрощённая причинная цепочка:

`изменение пути → изменение времени под огнём → изменение числа атак → изменение kills/leaks`.

### 6.4 Targeting

Каждая башня самостоятельно:

1. получает допустимые цели в range;
2. исключает dead/untargetable/несовместимые цели;
3. сортирует по target policy;
4. выбирает одну цель;
5. удерживает или пересматривает её по contract башни.

**Эффект действия игрока:** смена target policy влияет на следующий target selection, но не отменяет уже разрешённый hit задним числом.

**Feedback:** selected target marker, линия/arc при выборе башни, понятное переключение на новую цель.

### 6.5 Attack и damage resolution

Башня проверяет cooldown и валидность цели, затем weapon доставляет `DamagePacket` через hitscan, projectile, beam, chain или area impact.

Минимальный порядок результата:

1. определить target и попадание;
2. применить damage-type modifier;
3. поглотить допустимую часть shield;
4. применить armor/resistance к остатку;
5. вычесть HP;
6. применить status/effect;
7. опубликовать один damage result;
8. при `HP <= 0` выполнить одну смерть.

**Feedback:** muzzle/shot, impact, число/индикатор урона при необходимости, shield hit/break, armor resistance, status icon и death cue.

VFX/SFX показывают рассчитанный результат и не определяют урон.

### 6.6 Kill

Когда враг умирает:

1. он помечается dead;
2. движение и новые эффекты прекращаются;
3. kill reward выдаётся ровно один раз;
4. `WaveManager` уменьшает alive count;
5. UI/VFX/SFX показывают смерть и доход;
6. объект уничтожается или возвращается в pool.

**Экономический эффект:**

`B_afterKill = B_beforeKill + KillReward × RewardModifiers`.

Награда имеет видимую причину. Один враг не может одновременно выдать kill reward и leak result.

### 6.7 Leak

Когда живой враг достигает базы:

1. база получает `LeakDamage`;
2. UI показывает потерю HP/shield;
3. враг снимается с alive count;
4. kill reward не выдаётся;
5. враг уничтожается или возвращается в pool.

**Эффект:**

`BaseHP_after = max(0, BaseHP_before - ResolvedLeakDamage)`.

Если HP базы становится равным нулю, `GameManager` получает один terminal signal `Defeat`. Остаток wave task отменяется владельцем или переводится в контролируемое завершение.

### 6.8 Тактическое наблюдение

Наблюдение является частью loop, потому что оно даёт данные для следующего решения, но само не должно маскировать отсутствие действий подготовки.

Игрок считывает:

- где враги выходят из coverage;
- какая башня простаивает или стреляет не в ту цель;
- где shield/armor контрит текущий damage;
- какие враги прорываются;
- сколько HP теряет база;
- сколько денег фактически приносит состав волны.

UI должен позволять связать результат с причиной: маршрут, tower role, target policy, resistance или недостаток бюджета.

## 7. WaveResolve

Волна успешно разрешается, когда одновременно:

- spawn schedule полностью исчерпан;
- spawn task завершён;
- `enemiesAlive == 0`;
- база не уничтожена.

### 7.1 Completion payout

После выполнения условия один раз начисляются:

- authored completion reward;
- passive income, если он включён;
- модификатор текущей волны, если он был заранее выбран.

Формула бюджета одной волны:

```text
B_commit = B_open
         + R_preparation
         - C_tile
         - Σ C_tower
         - Σ C_upgrade

B_endCombat = B_commit + Σ R_kill

B_close = B_endCombat
        + R_completion
        + R_passive
```

`B_close` — выход текущей волны. Решения о его расходовании относятся уже к следующей итерации и здесь не описываются.

### 7.2 Итог состояния

Результат волны содержит:

- wave completed или base destroyed;
- убито врагов;
- количество/суммарный урон утечек;
- изменение HP/shield базы;
- kill income;
- completion/passive income;
- итоговый баланс;
- при необходимости — самый опасный enemy trait или lane.

Не требуется сложный score screen. Достаточно короткого читаемого summary, который объясняет результат и не задерживает переход.

### 7.3 Граница итерации

На `WaveResolve` заканчивается этот документ.

Следующие действия — формирование следующей угрозы, следующий reward offer, новое размещение тайла и следующий `Preparation` — принадлежат следующей итерации core loop.

## 8. Действие → эффект → обратная связь

| Действие игрока | Немедленный эффект | Эффект на симуляцию | Обратная связь |
| --- | --- | --- | --- |
| Открыть intel | Ничего не мутирует | Улучшает выбор counter | Состав, traits, lane |
| Выбрать wave option | Применяет один effect | Бюджет/base/tower modifier | Карточка зафиксирована, значения обновлены |
| Выбрать/повернуть tile | Меняет draft | Показывает возможный путь | Ghost, route/coverage preview |
| Подтвердить tile | Меняет TileMap, возможно деньги | Меняет path/exposure/build cells | Rebuild, новый путь, цена |
| Разместить башню | Списывает цену, создаёт Tower | Добавляет coverage и attack profile | Range, build cue, HUD money |
| Улучшить башню | Списывает цену, меняет level | Усиливает/специализирует output | Old → new stats, upgrade cue |
| Сменить target policy | Меняет policy | Перераспределяет атаки | Активная policy и target marker |
| Нажать Start Wave | Закрывает preparation | Запускает spawn/simulation | Combat HUD, wave cue |
| Камера/inspect | Меняет только presentation | Не меняет исход напрямую | Focus, панели, tooltips |
| Pause | Останавливает time owner | Временно останавливает симуляцию | Pause overlay/state |
| Убийство врага | Начисляет reward | Уменьшает угрозу и alive count | Death и income feedback |
| Враг достиг базы | Наносит leak damage | Ухудшает итог/может вызвать Defeat | Base hit, HP loss, warning |
| Завершение волны | Начисляет payout | Формирует `B_close` | Короткий result summary |

## 9. Главные причинные связи

### Карта

`tile → route topology → exposure time → количество атак → kills/leaks`.

### Башня

`placement → coverage overlap → target availability → uptime → damage/control`.

### Upgrade

`cost → изменение stat/effect → новый matchup → изменение эффективности против состава волны`.

### Upgrade feedback

After a successful upgrade, the selected tower must show the new stats and its range visual must use the upgraded `EffectiveRange`. The existing `Tower` owner applies the stat change and asks `TowerStatsVisual` to refresh the range scale; no second upgrade or presentation owner is introduced. The bounded evidence is the upgrade log plus the before/after grade, range, and visual scale.

### Target policy

`policy → порядок целей → распределение урона → overkill/прорывы → результат`.

### Экономика

`расход до commit → сила текущей обороны → kills → текущий доход → closing balance`.

### Информация

`intel → осмысленный counter → наблюдаемый результат`. Если правильный ответ невозможно было вывести из показанных данных, поражение выглядит случайным.

## 10. Основные типы решений

| Решение | Вариант A | Вариант B | Trade-off |
| --- | --- | --- | --- |
| Новая башня или upgrade | Больше coverage/targets | Сильнее существующая позиция | Ширина против концентрации |
| Direct или area damage | Урон одной цели | Урон группе | Tank против swarm |
| Damage или control | Быстрее убить | Дольше удерживать | Burst против time-on-target |
| Shield breaker или physical | Снять shield | Пробить обычную цель | Контр конкретного состава |
| Дальний или choke placement | Ранний контакт | Высокая плотность целей | Uptime против синергии |
| Потратить всё или оставить резерв | Максимум силы сейчас | Больше гибкости потом | Надёжность против экономики |
| First или strongest target | Не дать runner пройти | Сфокусировать tank | Leak prevention против threat removal |
| Короткий или длинный маршрут | Быстрая карта/позиции | Больше времени под огнём | Build access против exposure |

Не каждая волна обязана содержать все trade-offs. Но хотя бы один выбор должен реально менять ожидаемый результат.

## 11. Basic, Extended и не-core варианты

### Basic

- next-wave intel;
- одна валюта;
- tile choice, когда он предусмотрен;
- tower placement и upgrade только в `Preparation`;
- автоматическое targeting/attack;
- camera, inspect, pause во время боя;
- target policy как лёгкое тактическое действие, если поддержано;
- kill, leak и completion rewards;
- один понятный resolve summary.

### Extended

- несколько lane;
- branch upgrades;
- armor/shield/status/aura counterplay;
- sell/relocate;
- временный wave modifier;
- emergency repair или одна active ability;
- production income, если оно создаёт риск в пределах волны.

### Не входит в core одной волны

- мета-прогрессия;
- дерево постоянных unlock;
- выбор faction/commander;
- полный deckbuilder;
- endless scaling;
- статистика всего забега;
- mid-wave save;
- ручной герой;
- автоматический fallback-контент при ошибке Definition.

## 12. Владельцы

| Шаг | Владелец |
| --- | --- |
| Переход `Preparation → WaveActive → WaveResolve` | `GameManager`, `GameState` |
| Единственный запуск | `GameManager.StartNextWave()` |
| Состав, spawn schedule, alive count, completion | `WaveManager` |
| Деньги, `TrySpend`, kill/completion/passive income | `ResourceManager` |
| Карта и маршрут | `TileMapManager`, `TilePlacementSystem`, `TilePlacementValidator` |
| Создание башни | `TowerShopUI → TowerPlacementSystem → Tower` |
| Targeting, attack, upgrade, policy | `Tower`, `IWeapon`, `TowerStatsSO` |
| HP, смерть и движение врага | `MonsterHealth`, `MonsterMove` |
| HP базы и terminal signal | `PlayerBase → GameManager` |
| Отображение | `GameHUD`, `WaveUI`, tower/placement UI, VFX/SFX owners |

`ThreatIntel`, `OptionalWaveChoice` и result summary — логические части loop. Для них не требуется автоматически создавать отдельные managers: они расширяют текущих владельцев и их read models.

## 13. Инварианты

- Волна запускается только из `Preparation` и только один раз.
- После commit карта, placement и upgrades Basic-варианта заблокированы.
- Деньги никогда не становятся отрицательными.
- Неуспешная транзакция не меняет деньги и gameplay state.
- Один враг завершает жизнь либо kill, либо leak.
- Kill reward выдаётся ровно один раз.
- Completion reward выдаётся ровно один раз.
- Волна не завершается, пока spawn schedule не исчерпан или жив хотя бы один зарегистрированный враг.
- Split/summon-враги учитываются в alive count до проверки завершения.
- Base destruction отправляет один terminal signal.
- Preview не мутирует карту, NavMesh, деньги и save.
- UI/VFX/SFX не принимают gameplay-решений.
- Отсутствующая обязательная Definition блокирует корректное выполнение с явной ошибкой; fallback нет.

## 14. Критерии хорошей волны

Одна волна выполняет core loop, если:

1. До commit игрок понимает хотя бы главную угрозу.
2. У игрока есть минимум два валидных способа распределить ограниченный ресурс.
3. Preview показывает ожидаемый непосредственный эффект placement/map action.
4. Выбранное решение заметно влияет на coverage, matchup, income или leak risk.
5. Во время атаки понятно, почему башня эффективна или неэффективна.
6. Убийство, leak, потеря HP и деньги имеют различимую обратную связь.
7. После последнего врага нет неопределённой паузы: волна переходит в resolve.
8. Итог показывает цену решения: затраты, доход и повреждение базы.
9. Повтор той же волны с другим осмысленным решением потенциально даёт другой результат.
10. Loop не требует ручной active ability, чтобы быть интересным.

## 15. Минимальный Play Mode сценарий проверки

1. Открыть `Preparation` с видимым intel и бюджетом.
2. Проверить preview валидного и невалидного placement.
3. Разместить или улучшить башню; убедиться в единственном списании цены.
4. Нажать Start Wave дважды; должна запуститься одна волна.
5. Убедиться, что запрещённые preparation-команды заблокированы во время боя.
6. Наблюдать хотя бы одно убийство и одну соответствующую награду.
7. При отдельном сценарии допустить leak и проверить отсутствие kill reward.
8. Дождаться `spawn exhausted && enemiesAlive == 0`.
9. Проверить один completion payout и переход в `WaveResolve`.
10. Сверить closing balance и изменение HP базы с показанным summary.

Компиляция или статический просмотр кода не подтверждают этот loop без bounded Play Mode прохождения.
