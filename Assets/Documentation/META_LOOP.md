---
title: Meta loop
status: design-target
updated: 2026-08-04
scope: cross-run progression, profile persistence, unlocks, next-run setup
related: Assets/Documentation/RUN_LOOP.md, Assets/Documentation/CORE_LOOP.md, Assets/Documentation/UNITY_DATA_AND_SERVICE_LIFECYCLES.md
---

# Meta loop

## 1. Граница документа

Meta loop начинается после получения неизменяемого `RunResult` завершённого забега и заканчивается созданием `StartingRules` для нового независимого забега.

В него входят:

- постоянная награда за результат забега;
- meta currency;
- unlock контента и стартовых вариантов;
- persistent objectives/milestones;
- выбор разрешённого стартового loadout/modifier;
- сохранение `ProfileSave`;
- переход к новому забегу.

Не входят:

- действия внутри волны;
- экономика текущего забега;
- run rewards и temporary modifiers;
- карта, башни и деньги завершённого забега как переносимые объекты;
- live-service, seasons, battle pass и магазин реальных денег.

Meta progression не должна ремонтировать слабый core/run loop. Она расширяет варианты повторного прохождения после того, как отдельная волна и забег уже дают осмысленные решения.

## 2. Текущее наблюдаемое состояние

Сейчас в проекте существует минимальный meta-placeholder:

```text
Victory
  → GameManager.Victory()
  → ResourceManager.UnlockStartingReserve()
  → PlayerPrefs["TD3D.StartingReserve"] = 1
  → следующий забег получает +25 starting currency
```

Свойства текущего решения:

- срабатывает только при `Victory`;
- содержит один boolean unlock;
- сохраняется напрямую через `PlayerPrefs`;
- автоматически влияет на стартовую валюту следующего забега;
- не имеет общей схемы `ProfileSave`, version/migration, UI выбора или нескольких unlock.

Это рабочий прототипный флаг, но не полный meta loop. При введении ProfileSave он мигрирует в одного persistence owner и перестаёт параллельно читаться как второй источник истины.

## 3. Meta loop одной строкой

> Завершить забег → получить понятный постоянный результат → открыть или купить новый вариант → настроить следующий старт → войти в новый забег с большей вариативностью или новым challenge.

Meta loop должен отвечать на вопрос игрока: «Что изменилось для следующей попытки?»

## 4. Схема

```text
Run terminal state
  ↓
RunResult
  ↓
Validate result and settlement receipt
  ↓
Already settled? ── yes ──→ Show existing result, no duplicate reward
  │ no
  ↓
Calculate meta reward and completed objectives
  ↓
Apply to ProfileSave atomically
  ├── MetaCurrency
  ├── Unlocks
  ├── ObjectiveProgress
  ├── Difficulty access
  └── Statistics
  ↓
Persist ProfileSave
  ↓
Result / Progression UI
  ├── review result
  ├── inspect unlocks
  ├── purchase/select option
  └── configure next run
  ↓
Build immutable StartingRules snapshot
  ↓
StartNewRun(StartingRules)
```

Награда применяется до того, как UI предлагает выйти или начать новый забег. Result screen показывает подтверждённую транзакцию и не является её владельцем.

## 5. Цели meta loop

### 5.1 Повторяемость

Игрок получает новую причину пройти тот же run loop иначе: другая tower role, стартовый выбор, reward pool или difficulty.

### 5.2 Выражение билда

Unlock увеличивает пространство вариантов, но не выбирает победную стратегию за игрока.

### 5.3 Мастерство

Новые challenges и objectives позволяют доказать понимание core loop, а не только накопить время.

### 5.4 Мягкая компенсация поражения

Осмысленно пройденная часть забега может давать ограниченную meta reward, чтобы не превращать поражение в полностью пустое время.

### 5.5 Сохранение ценности нового забега

Новый забег должен начинаться с чистой карты, валюты, башен и run modifiers. Profile влияет только через явные `StartingRules` и разрешённый content pool.

## 6. Основной принцип мощности

Приоритет Basic meta progression:

1. горизонтальные unlock;
2. новые способы стартовой настройки;
3. новые challenges;
4. косметика/статистика;
5. только затем — небольшие ограниченные vertical bonuses.

Причина: слишком большой постоянный бонус к damage, HP или starting money постепенно отменяет экономику и threat counterplay самого забега.

Хороший unlock говорит: «теперь можно играть иначе».

Плохой unlock говорит: «теперь те же решения просто на 20% сильнее».

## 7. RunResult как граница

`GameManager` завершает runtime-забег и один раз передаёт application-слою immutable `RunResult`.

Минимальный состав:

```text
RunResult
├── RunId
├── ResultVersion
├── Outcome: Victory / Defeat / Abandon
├── Seed
├── DifficultyId
├── WavesCompleted
├── FinalWaveIndex
├── ObjectivesCompleted[]
├── ChallengeModifiers[]
├── BaseStateSummary
├── EconomySummary
├── Tower/BuildSummary
├── Duration
└── ContentVersion
```

`RunResult` не содержит:

- живые `GameObject`/`MonoBehaviour` references;
- весь `RunSave` как mutable объект;
- callback/delegate;
- VFX/SFX/UI state;
- право самостоятельно изменять ProfileSave.

После создания результат не меняется. Все meta calculations должны быть воспроизводимы из результата и meta Definition текущей версии.

## 8. Settlement — применение результата

### 8.1 Единственный entry point

У application-level meta owner есть одна операция вида:

`SettleRun(RunResult result)`.

Название конкретного класса выбирается при реализации. Нужен один владелец — например `ProfileService` или `MetaProgressionService`, но не оба как параллельные mutable stores.

### 8.2 Порядок

1. проверить `RunId`, версию и terminal outcome;
2. проверить, не был ли результат уже применён;
3. вычислить currency/objective/unlock deltas;
4. применить их к копии/транзакции `ProfileSave`;
5. записать settlement receipt;
6. атомарно сохранить ProfileSave;
7. опубликовать immutable `MetaSettlementResult` для UI.

Если запись ProfileSave не удалась, UI не объявляет награду подтверждённой. Скрытая запись в другой storage как fallback запрещена.

### 8.3 Idempotency

Повторная обработка одного `RunId` не выдаёт награду второй раз.

Basic-вариант при одном активном run slot хранит `LastSettledRunId` или эквивалентный receipt. Несколько slots/cloud требуют bounded settlement ledger и отдельного conflict contract.

## 9. Meta reward

### 9.1 Источники

Meta reward может зависеть от:

- количества завершённых волн;
- outcome;
- difficulty;
- challenge modifiers;
- впервые выполненных objectives;
- first victory/first clear конкретной сложности;
- ограниченных performance milestones.

Не следует напрямую конвертировать каждую единицу run currency в meta currency: это заставляет игрока портить боевые решения ради фарма профиля.

### 9.2 Базовая формула

```text
M_progress   = ProgressReward(WavesCompleted)
M_outcome    = VictoryBonus | DefeatBonus | 0 for Abandon
M_difficulty = DifficultyMultiplier
M_challenge  = Σ ChallengeBonus
M_objective  = Σ FirstCompletionBonus

M_reward = max(0,
    round((M_progress + M_outcome + M_challenge) × M_difficulty)
    + M_objective)
```

Конкретные числа задаются meta balance Definition, а не hardcode UI.

### 9.3 Outcome policy

#### Victory

- полный progress reward;
- victory bonus;
- first-clear objective/unlock;
- доступ к следующему challenge tier, если предусмотрено.

#### Defeat

- reward за реально достигнутый прогресс;
- без victory bonus;
- objectives засчитываются только если их контракт допускает defeat;
- награда должна поддерживать повтор, но не делать intentional loss эффективнее игры на победу.

#### Abandon

Basic-вариант не выдаёт outcome/progress reward за незавершённый результат. Явно завершённые account-level objectives могут сохраняться только если их правила это заранее объявляют.

### 9.4 Ограничения фарма

- reward одного RunId применяется один раз;
- restart/reload result screen не повторяет settlement;
- intentional быстрый defeat не должен иметь лучшую reward-per-time, чем осмысленный progress;
- first-clear bonus одноразовый;
- difficulty multiplier применяется только к реально включённой сложности;
- challenge bonus не начисляется, если challenge был отключён во время забега;
- offline/system clock не должен определять Basic reward.

## 10. Meta currency

### 10.1 Basic

Одна общая meta currency.

Она используется для:

- горизонтальных unlock;
- стартовых options;
- возможно, ограниченного числа permanent nodes;
- косметических вариантов как Extended sink.

Несколько валют вводятся только при различной функции. Разделять одну progression на «красные», «синие» и «золотые» очки без разного решения не нужно.

### 10.2 Баланс

```text
Balance_afterSettlement = Balance_before + M_reward
Balance_afterPurchase   = Balance_beforePurchase - UnlockCost
```

Покупка атомарна:

- unlock существует;
- prerequisites выполнены;
- ещё не куплен;
- хватает валюты;
- цена не отрицательна;
- после применения unlock и новый balance сохраняются одной транзакцией.

### 10.3 Cadence

- первый новый вариант должен открываться достаточно рано, чтобы игрок увидел meta loop;
- ранние unlock дешевле и заметно различаются;
- более поздние unlock требуют нескольких успешных решений, но не бессмысленного повторения;
- после полного unlock currency либо имеет честный sink, либо перестаёт выдаваться/показывается как completion, а не копится без назначения.

## 11. Типы unlock

### 11.1 Tower unlock

Открывает новую Tower Definition для разрешённого content/loadout pool.

Эффект:

- увеличивает варианты следующего забега;
- не создаёт башню бесплатно внутри активного забега;
- не меняет уже созданный `StartingRules` текущего run.

Риск: слишком много открытых башен может размыть reward/shop pool. Нужен loadout, weighting или явный pool management, если размер контента становится большим.

### 11.2 Reward/Augment unlock

Добавляет новый reward в будущий offer pool.

Эффект:

- расширяет возможные run identities;
- не гарантирует появление в каждом offer;
- отображает, что именно добавлено в pool.

Unlock не должен незаметно ухудшать профиль из-за dilution. Если новый вариант слабее, это проблема balance, а не цена progression.

### 11.3 Starting option

Примеры:

- starting reserve;
- один выбранный starter tower;
- выбор из нескольких стартовых tiles;
- небольшой starting modifier;
- стартовая target policy/loadout slot.

Эффект явно входит в `StartingRules`. Basic-ограничение: одновременно активен один вариант категории либо небольшой capped набор.

Текущий `StartingReserve` относится к этому типу.

### 11.4 Difficulty/Challenge unlock

Открывает новую сложность или mutator после понятного условия, обычно victory предыдущего tier.

Эффект:

- меняет threat/reward contract следующего run;
- выбирается игроком до старта;
- не включается автоматически без подтверждения.

### 11.5 Knowledge/Codex unlock

Открывает:

- подробный enemy trait;
- tower interaction explanation;
- статистику encounter;
- route/build tutorial.

Knowledge не должно скрывать базовую информацию, необходимую для честного counterplay. Unlock добавляет глубину, а не исправляет непонятный UI.

### 11.6 Cosmetic unlock

Не влияет на gameplay: tower palette, projectile/VFX variation, UI theme, badge.

Это безопасный поздний sink, если presentation pipeline поддерживает выбор без дублирования gameplay assets.

### 11.7 Vertical upgrade

Permanent `+damage`, `+base HP`, `+starting currency` допустим как Extended-вариант с жёсткими ограничениями:

- малое число уровней;
- cap;
- точное отображение;
- баланс сложности учитывает, но не требует grind;
- bonus не делает один tower обязательным;
- respec/refund contract определён заранее.

Basic meta loop предпочтительно запускается без широкого vertical tree.

## 12. Objectives и milestones

Objective создаёт цель поверх обычной победы.

Примеры:

- завершить заданную волну;
- выиграть с ограниченным числом башен;
- не допустить leak;
- использовать конкретную tower role;
- пройти challenge modifier;
- победить без permanent starting bonus.

Objective Definition содержит:

- stable ID;
- понятное условие;
- progress model;
- reward;
- repeatable/one-time flag;
- допустимые outcomes;
- hidden/visible policy.

Правила:

- objective оценивается по данным RunResult или отдельному проверенному progress snapshot;
- UI показывает progress и завершение;
- one-time reward выдаётся один раз;
- hidden objective не должен требовать угадывания для обязательной progression;
- objective не создаёт второй источник unlock state.

## 13. ProfileSave

### 13.1 Минимальная схема

```text
ProfileSave
├── SchemaVersion
├── ProfileId
├── MetaCurrency
├── UnlockedContentIds[]
├── PurchasedUpgradeLevels[]
├── CompletedObjectiveIds[]
├── ObjectiveProgress[]
├── AvailableDifficultyIds[]
├── SelectedStartingOptions[]
├── HighestCompletedDifficulty
├── Statistics
├── LastSettledRunId / SettlementReceipts
└── MigrationFlags[]
```

Settings могут храниться в отдельном settings DTO, если у них другой lifecycle. Активный `RunSave` не вкладывается в ProfileSave как mutable runtime object; связь выполняется через profile/run IDs.

### 13.2 Что не хранится

- runtime Tower/Enemy/GameObject references;
- current run currency;
- текущая карта;
- временные run rewards;
- current wave;
- NavMesh/derived stats;
- UI state;
- ScriptableObject как mutable save instance.

### 13.3 IDs

Profile хранит stable content IDs. ContentCatalog разрешает их в Definition.

Переименование display name не ломает save. Удаление/замена ID требует явной migration. Missing required unlocked Definition не подменяется случайным asset.

## 14. Владельцы и сервисы

Meta loop принадлежит application/profile lifecycle, а не `Gameplay.unity` actors.

| Ответственность | Владелец |
| --- | --- |
| Создать terminal RunResult | `GameManager`/run result builder у существующего terminal owner |
| Применить RunResult и изменить профиль | Один application-level meta owner |
| Сериализовать/загрузить ProfileSave | `SaveService` |
| Разрешить unlock Definition по ID | `ContentCatalog` |
| Показать result/progression/loadout | UI через read models и commands |
| Создать StartingRules | Тот же meta/profile owner либо узкий builder без mutable state |
| Запустить новый run | `SceneFlow → GameplayBootstrap` |

KISS-вариант:

- один mutable `ProfileSave` owner;
- один I/O `SaveService`;
- Definition для unlock/cost/objective;
- UI без собственной копии currency/unlocks;
- без `MetaManager + ProgressionManager + UnlockManager + CurrencyManager` одновременно.

Интерфейсы нужны на save/content/platform boundaries, а не для каждого data class.

## 15. Player-facing фазы

### 15.1 Result

Игрок видит:

- Victory/Defeat/Abandon;
- достигнутую волну/difficulty;
- выполненные objectives;
- расчёт meta reward по причинам;
- новые unlock;
- новый meta balance.

Reward уже сохранён. Кнопка `Continue` закрывает экран, а не подтверждает транзакцию.

### 15.2 Progression

Игрок просматривает:

- доступные unlock;
- цену;
- prerequisites;
- gameplay-эффект;
- статус locked/available/purchased;
- влияние на content pool или StartingRules.

### 15.3 Purchase/Unlock

Команда `TryPurchase(unlockId)` повторно проверяет состояние у owner.

Успех:

- списывает currency;
- добавляет unlock/level;
- сохраняет ProfileSave;
- обновляет read model;
- проигрывает feedback.

Отказ:

- ничего не меняет;
- показывает конкретную причину: недостаточно currency, prerequisite, уже куплено, content error.

### 15.4 Loadout/Next-run setup

Игрок выбирает только из unlocked вариантов:

- starter tower/loadout;
- starting option;
- difficulty;
- challenge modifier;
- cosmetic selection при наличии.

Выбор проверяется owner и сохраняется как profile preference. Перед стартом создаётся immutable `StartingRules` snapshot.

### 15.5 Start next run

`SceneFlow` получает `StartingRules` и запускает новый run.

Новый run:

- не получает старую карту, башни, run currency или temporary rewards;
- получает только явно выбранные starting options и разрешённый content pool;
- не читает mutable ProfileSave из каждого gameplay actor;
- не меняется, если профиль был изменён после создания snapshot.

## 16. Действие игрока → постоянный эффект

| Действие | Непосредственный эффект | Эффект следующего забега |
| --- | --- | --- |
| Завершить run | Создаёт RunResult | Даёт settlement и progression |
| Просмотреть result | Ничего не мутирует | Объясняет полученный прогресс |
| Купить tower unlock | Currency уменьшается, ID открывается | Tower доступна в pool/loadout |
| Купить reward unlock | Добавляет Definition ID | Reward может появиться в offer |
| Открыть difficulty | Добавляет difficulty ID | Игрок может выбрать новый challenge |
| Выбрать starting option | Меняет profile selection | Опция входит в StartingRules |
| Выполнить objective | Фиксирует completion/reward | Открывает заявленный вариант |
| Сменить loadout | Меняет выбор, не ownership | Новый run получает другой набор |
| Начать новый run | Создаёт StartingRules snapshot | Запускает чистый runtime с явными meta inputs |
| Перезагрузить result | Проверяет receipt | Не выдаёт награду повторно |

## 17. Progression cadence

### Ранний этап

- базовый набор уже позволяет играть и побеждать;
- первый unlock появляется быстро;
- игрок понимает связь `run result → новый вариант`;
- unlock преимущественно горизонтальные.

### Средний этап

- игрок собирает предпочтительный loadout;
- открывает counters и новые reward synergies;
- пробует difficulty/challenge tiers;
- objectives направляют к альтернативным стратегиям.

### Поздний этап

- unlock становятся более специализированными;
- challenges проверяют мастерство;
- cosmetics/statistics дают цели без обязательного power creep;
- профиль может перейти в состояние completion.

Нельзя требовать поздний meta level для честного прохождения базовой сложности. Иначе progression превращается в обязательный grind.

## 18. Связь с балансом забега

Meta layer меняет вход забега, поэтому каждый gameplay unlock должен быть включён в balance model.

### Горизонтальный unlock

Меняет доступные решения, но не обязан повышать среднюю силу.

### Starting modifier

Меняет стартовую точку. Его стоимость — потеря другого slot/option либо ограниченная величина.

### Difficulty

Компенсирует освоенную систему новой угрозой и может увеличивать meta reward.

### Vertical power

Повышает win rate и поэтому требует cap. Если без него поздняя сложность непроходима, это скрытый level gate.

Полезные проверки:

- win rate без meta upgrades;
- win rate с максимальными upgrades;
- pick rate каждого unlock;
- доля профилей, где unlock фактически никогда не используется;
- время до первого и последующих meaningful unlock;
- размер разрыва starting power между новым и развитым профилем.

## 19. Миграция текущего StartingReserve

При введении ProfileSave текущий ключ `TD3D.StartingReserve` переводится в один stable unlock ID, например `starting.reserve`.

Однократная migration:

1. загрузить/создать ProfileSave;
2. проверить `SchemaVersion/MigrationFlags`;
3. если migration ещё не применена, прочитать старый PlayerPrefs key;
4. при значении `1` добавить unlock ID;
5. записать migration flag и атомарно сохранить ProfileSave;
6. после подтверждения использовать только ProfileSave.

Gameplay больше не читает PlayerPrefs как fallback. Удаление legacy key возможно отдельной подтверждённой migration/cleanup задачей.

## 20. Feedback

Meta feedback должен показывать причинность.

### После run

- breakdown reward;
- first-clear/objective banner;
- новый balance;
- открытые nodes/content;
- доступность следующей сложности.

### При покупке

- старая → новая currency;
- что именно разблокировано;
- где это появится: loadout, reward pool, difficulty, cosmetics;
- prerequisite для следующего варианта.

### Перед новым run

- summary StartingRules;
- активные starting options;
- выбранная difficulty/challenge;
- доступный loadout;
- предупреждение о взаимоисключающих choices.

UI не должен обещать `+damage`, если фактическая Definition не изменила StartingRules/content contract.

## 21. Basic, Extended, Deferred

### Basic meta loop

- один ProfileSave;
- одна meta currency;
- immutable RunResult;
- idempotent settlement;
- reward за progress + victory bonus;
- небольшой набор горизонтальных tower/reward unlock;
- существующий StartingReserve как starting option;
- одна следующая difficulty после victory;
- loadout/starting option selection;
- atomic local save.

### Extended

- objectives;
- несколько starting options/slots;
- challenge modifiers;
- capped vertical upgrades;
- cosmetics;
- run history/statistics;
- respec/refund;
- несколько local profiles.

### Deferred

- cloud sync/conflicts;
- cross-device receipts;
- seasons/dailies;
- community challenges;
- live balance migrations;
- multiple currencies;
- rotating shop;
- online account progression.

Deferred-варианты не должны влиять на архитектуру Basic до появления реальной задачи.

## 22. Антипаттерны

- выдавать награду по кнопке UI вместо owner-side settlement;
- повторно выдавать reward после reload;
- хранить meta currency одновременно в PlayerPrefs, UI и ProfileSave;
- использовать mutable ScriptableObject как профиль;
- переносить Tower/GameObject из завершённого run;
- применять run-only reward к ProfileSave;
- создавать отдельный manager для currency, unlock, objectives и loadout без необходимости;
- скрыто увеличивать difficulty вместе с permanent power;
- делать intentional defeat самым выгодным farm;
- добавлять слабые unlock, разбавляющие offer pool;
- требовать grind для прохождения базовой сложности;
- читать ProfileSave напрямую из каждого gameplay actor;
- подменять missing unlocked Definition fallback-контентом;
- сохранять сначала currency, потом unlock отдельными неатомарными операциями.

## 23. Инварианты

- Один `RunResult` settlement применяется не более одного раза.
- ProfileSave имеет одного mutable owner.
- UI не является источником meta currency/unlocks.
- Purchase не может сделать balance отрицательным.
- Unlock применён либо полностью вместе со списанием, либо не применён.
- One-time objective reward выдаётся один раз.
- Run-only state не переносится в профиль.
- StartingRules создаётся как immutable snapshot.
- Новый run не читает старую карту, башни и run currency.
- Base content позволяет играть без meta grind.
- Difficulty/challenge включаются только явным выбором.
- ProfileSave versioned и мигрируется до использования.
- Legacy PlayerPrefs после migration не является fallback source.
- Missing required Definition блокирует unlock/loadout/start с явной ошибкой.

## 24. Критерии хорошего meta loop

Meta loop работает, если:

1. После первого завершённого run игрок понимает, что и почему получил.
2. Повторная загрузка result screen не меняет balance.
3. Defeat даёт ограниченный progress, но victory остаётся выгоднее.
4. Первый unlock открывает новый способ играть, а не только больший коэффициент.
5. Purchase сразу показывает влияние на следующий run.
6. Новый run получает только выбранные starting options.
7. Temporary rewards завершённого run не переносятся.
8. Новый профиль может пройти базовый run без обязательного grind.
9. Развитый профиль имеет больше вариантов, но core decisions остаются значимыми.
10. Save/load сохраняет currency, unlock, objectives и selection точно.
11. Удалённый/невалидный content ID вызывает понятную ошибку, а не fallback.
12. Все постоянные изменения проходят через одного profile owner.

## 25. Минимальный тестовый сценарий

1. Создать чистый ProfileSave с базовым content set.
2. Завершить run с `Defeat` после нескольких волн.
3. Проверить progress reward и один settlement receipt.
4. Повторно обработать тот же RunResult; balance не должен измениться.
5. Купить доступный horizontal unlock; проверить атомарное списание и сохранение ID.
6. Попробовать повторную покупку и покупку без денег; состояние не меняется.
7. Создать StartingRules и проверить наличие unlock/selection без run-only данных.
8. Запустить новый run и убедиться в чистой карте, башнях и run currency.
9. Завершить run с `Victory`; проверить victory/first-clear bonus и difficulty unlock.
10. Перезагрузить приложение и сравнить ProfileSave/read model.
11. Выполнить migration `TD3D.StartingReserve` и проверить однократное появление unlock ID.
12. Удалить/сломать обязательную unlock Definition в тестовом каталоге; запуск должен блокироваться явной ошибкой без fallback.

Для реализации дополнительно нужны EditMode tests settlement/purchase/migration и bounded Play Mode переход `Victory/Defeat → Result → New Run`.
