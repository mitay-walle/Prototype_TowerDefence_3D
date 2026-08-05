# ML-Agents для Tower Defence

В проект установлен Unity-пакет `com.unity.ml-agents` версии 4.0.0. Runtime-агент использует существующие владельцы игрового цикла: `GameManager`, `WaveManager`, `ResourceManager`, `TowerPlacementSystem` и `TilePlacementSystem`.

## Настройка сцены

1. Открой `Assets/Scenes/Gameplay.unity`.
2. Выполни меню `TD/ML-Agents/Setup Gameplay Agent`.
3. Меню создаёт или обновляет объект `TD ML Agent`, добавляет `TowerDefenceAgent`, `BehaviorParameters` и `DecisionRequester`, а также отключённый дочерний `TD ML Input` с `SyntheticMouse`.
4. Повторный запуск меню идемпотентен и сохраняет сцену через Unity.

Игровой агент имеет 72 векторных наблюдения и пять дискретных веток действий `[9, 4, 8, 3, 8]`: основное действие, выбор башни, слот размещения, вариант тайла и цель улучшения башни. В наблюдения входят покрытие входов башнями, доля покрытых входов, концентрация башен относительно базы и текущие adaptive-факторы сложности. Улучшение использует существующий `Tower.UpgradeSpendingCost()` и маскируется, если цель отсутствует, башня достигла максимума или не хватает денег. Размещение получает reward за закрытие новых входов и малый бонус за близость к базе, но размещение без новой firing-coverage штрафуется; отдельные `TD3D/Player/Placement*` scalars показывают coverage gain, концентрацию и placement reward. Heuristic выбирает сначала позиции, закрывающие непокрытые входы. `MaxStep=0`, но в training mode каждый ML-агент имеет игровой timeout 180 секунд: зависший забег получает отдельную timeout-оценку как поражение и не блокирует trainer. Setup включает `_trainingMode` для обучения и smoke-теста; для обычной игры выставь его в `false`, тогда `SyntheticMouse` не активируется.

Рядом с ним Setup создаёт `TD ML Balance Agent` с поведением `TD3DBalanceAgent`. Он принимает четыре дискретных решения `[5, 5, 5, 5]` перед каждой доступной волной: factor здоровья врагов, количества врагов, скорости и наград. Решения применяются через `WaveManager.ApplyAdaptiveBalance(...)`, не изменяя authored WaveConfig assets. При создании ML-generated wave применённые health/count/speed/reward-факторы дополнительно записываются в сохраняемый generated `WaveConfig` как provenance этой волны.

Оценка забега находится в `GameplayEvaluationMetrics`: игровой агент получает terminal score за победу/поражение, прогресс волн, сохранённое здоровье базы, экономию денег, улучшения башен, покрытие входов и концентрацию у базы; балансировщик получает положительный score только за победный, но напряжённый забег с целевым запасом здоровья и высокой сложностью, а за поражение — отрицательный score. `GameplayTelemetry` передаёт эти данные через `GameplayTelemetrySnapshot`, включая число убитых врагов, завершённые волны, покрытые входы и текущие adaptive-факторы.

`TD ML Enemy Level Agent` генерирует не временную волну, а сохраняемый `WaveConfig` в `Assets/Resources/WaveConfigs/Generated`. Он выбирает seed, до трёх архетипов врагов, количество и pacing; визуал каждого нового врага сохраняется в `Assets/Prefabs/Enemies/Generated` через seed-based pipeline. Генератор ограничивает прогнозируемый урон текущим HP базы, сохраняет safety margin и tension score, а после забега записывает оценку, победу/поражение и наблюдаемое здоровье базы в asset.

## Запуск обучения

Официальная версия trainer для ML-Agents 4.0.0 — `mlagents==1.1.0`. Для неё используй Python 3.10.x в отдельном окружении; в текущем workspace установлен Python 3.10.11:

```powershell
py -3.10 -m venv MLAgents/.venv
MLAgents/.venv/Scripts/python.exe -m pip install --upgrade pip
MLAgents/.venv/Scripts/python.exe -m pip install mlagents==1.1.0
MLAgents/.venv/Scripts/mlagents-learn.exe MLAgents/td3d_ppo.yaml --run-id=td3d-first --force
```

Для ускорения Editor Play Mode `TowerDefenceAgent` хранит `_mlTestTimeScale` (по умолчанию `5x`) и применяет его при старте и сбросе эпизода. Значение можно менять в Inspector или во время Play Mode через `TowerDefenceAgent.MlTestTimeScale` либо `SetMlTestTimeScale(float)`. Флаг `_applyMlTestTimeScale` отключает автоматическое применение. Для trainer-managed запуска параллельно используется `engine_settings.time_scale` в YAML.

После сообщения trainer о готовности запусти Gameplay-сцену в Unity. Для длительного обучения собери standalone Player и передай его в конфиг через `--env=Build/TD3D.exe`.

```powershell
MLAgents/.venv/Scripts/mlagents-learn.exe MLAgents/td3d_ppo.yaml --run-id=td3d-build --env=Build/TD3D.exe --force
```

До подключения Python trainer `BehaviorParameters` используют `Heuristic`: игровой агент ставит доступную башню, запускает волну, выбирает награду и подтверждает вариант тайла, а балансировщик оставляет нейтральные factors. Это используется только для bounded Play Mode smoke-теста, а не как обученная модель.

## Контракт награды

- `+1` за победу.
- `-1` за поражение.
- `+0.05` за завершённую волну.
- небольшой положительный reward за принятые действия размещения, старта волны и выбора награды.
- небольшой штраф за недопустимое действие и потерю здоровья базы.
- terminal evaluation для player agent и balance agent; balance reward отрицателен при поражении и максимален, когда победа достигнута с небольшим, но не нулевым запасом здоровья базы.

Конфигурация содержит три поведения: `TD3DAgent`, `TD3DBalanceAgent` и `TD3DEnemyLevelAgent`. Один `mlagents-learn` run-id обучает их в одном Unity environment; отдельные TensorBoard series позволяют сравнивать `Environment/Cumulative Reward` всех агентов.

Если проектный `MLAgents/.venv` ещё не создан, trainer smoke-тест не считается выполненным: сначала установи совместимую версию Python 3.10.x и зависимости в отдельное окружение.
## Контроль ML-контракта и метрик

`TD3DEnemyLevelAgent` фактически формирует 30 наблюдений; scene setup и `BehaviorParameters` используют то же значение. Игровой агент маскирует невозможные действия: башни без денег, занятые или непроецируемые слоты, действия вне Preparation и неподходящие варианты тайлов. Пока есть непокрытый вход и доступна валидная оплачиваемая позиция, primary branch оставляет только `PlaceTower`, а ветки башни и слота сужаются до конкретной пары, которая закрывает новый вход. Поэтому policy не может пропустить обязательное расширение покрытия через `NoOp`, преждевременный старт волны или допустимый, но бесполезный слот. При ошибочном оставшемся ghost placement он отменяется и получает штраф, поэтому decision loop не зависает.

При завершении эпизода в TensorBoard записываются отдельные оценки:

- `TD3D/Player/*`: victory, defeat, success, completion, waves, base health/loss, currency savings, upgrade score, entry coverage, tower concentration и reward.
- `TD3D/Balance/*`: victory/defeat, completion, base health/loss, difficulty score, фактически применённые health/count/speed/reward factors и reward.
- `TD3D/EnemyLevel/*`: victory/defeat, completion, base health, собственная difficulty score, применённые health/count/speed factors, generated groups, predicted damage, tension и reward.
- Все три серии также записывают `Timeout`; timeout не смешивается с настоящим `Victory`/`GameOver` и сохраняет отрицательную terminal-оценку.
- Player agent дополнительно получает event-based shaping через `GameplayTelemetry`: небольшой положительный reward за `TowerTargetAcquired`, `TowerFired` и `MonsterDeath`, отрицательный за `EnemyLeaked`; эти события также пишутся в `TD3D/Player/*` stats.
## Проверка качества training run

Количество `Step` не равно количеству завершённых эпизодов. Перед тем как считать модель обученной, проверь отдельный свежий stdout-log:

```powershell
MLAgents/.venv/Scripts/python.exe MLAgents/validate_training_run.py `
  --stdout-log MLAgents/td3d-three-agent-20260805-r5.stdout.log `
  --status results/td3d-three-agent-20260805-r5/run_logs/training_status.json `
  --require-final-completed-summary
```

Команда завершается с ошибкой, если хотя бы один behavior не дал completed summary, последняя summary была `No episode was completed since last summary` или доля таких summary превысила порог. Такой run нельзя использовать как production policy; его артефакты сохраняются только для диагностики. Перед запуском нового run отдельно подтверждаются `Victory` или `Defeat`, `RunFinished`, движение врагов к базе и валидность карты в bounded Play Mode при том же `time_scale`, что используется trainer.
## ML smoke isolation

The Gameplay scene keeps `TD ML Agent` as the opt-in player smoke path. `TD ML Balance Agent` and `TD ML Enemy Level Agent` are saved with `_trainingMode=false` so they do not mutate authored waves during normal gameplay or player-agent smoke. Enable them explicitly for balance or enemy-level training; generated waves and adaptive factors must never silently replace the authored baseline.

The player agent resolves the mandatory one-time start challenge selection before the first authored wave. It chooses a real selectable profile (`ControlledPressure` in the automatic smoke policy); `ChallengeModifier.None` is a reset/default state and is rejected as a player selection. The `ChallengeModifierSelected` telemetry event records the selected count/health/speed/reward factors. No challenge modifier offer is opened after individual waves; inter-wave choices remain the existing reward and map phases.

## Player upgrade policy

The player heuristic treats entrance coverage as mandatory preparation. Once no prioritized coverage placement is available, it may select `ActionUpgradeTower` for the first affordable ordered live tower before starting the next wave. `Tower.UpgradeSpendingCost()` remains the spending and stat owner; the agent records a bounded `[MLAgent] Upgrade committed` log, while `GameplayTelemetrySnapshot.TowersUpgraded` and combat events provide readback.

An injected-currency Play probe verified the route with `TowerUpgrade` grade `0->1`, `TowersUpgraded=1`, `WaveCompleted` combat details `12/10/14/7/0` for target acquisitions/fires/damage applications/kills/leaks, and `EntryCoverageRatio=1.0` in Wave 2. Because the probe used `ResourceManager.AddCurrency(150)`, it is owner-chain evidence only. Training quality and natural balance still require a completed baseline run without injected currency.

## Player reward policy

The player heuristic selects `ResourceCache` while a one-tower build is below the cheapest tower cost and still needs economy catch-up. If the base is at or below 75% health and the build can survive without catch-up cash (the bank can buy the cheapest tower or at least two towers already exist), it selects `EmergencyRepairs`. On a non-final wave, a healthy two-tower build with full entrance coverage and enough reserve for one cheapest tower selects the existing `BountyContract` instead, because its delayed completion bonus is now safe to carry. The reward is still applied by `WaveManager`; `TowerDefenceAgent` owns only the decision policy and bounded telemetry log.

`TrySelectReward` now penalizes a rejected reward-selection call and does not award positive selection shaping. Successful choices emit `[MLAgent] Reward decision=<choice>;wave=<current>/<total>;base=<...>;currency=<...>;towers=<...>;coverage=<covered>/<total>` and are read back through `GameplayTelemetry` as `RewardSelected`.

R66 natural Play readback completed authored Waves 1-2 and entered Wave 3 with `HasGeneratedWave=false`, `TileMapValid=true`, and no invalid connections. The recovery branch was observed at `base=2/20;currency=102;towers=2`, followed by two tower upgrades. The bounded episode reset before a directly captured terminal event; do not use it as final-wave Victory evidence.

## Terminal reward guard

`WaveManager.SelectRewardOffer` rejects a reward action when the bound `PlayerBase` is destroyed or `GameManager` is already terminal. `WaveManager.ForceStopWave` clears a pending offer and cancels the inter-wave scope before `RunFinished`, so the player agent cannot receive or apply a reward after Defeat.

R67 controlled Play evidence: with the existing player-agent isolation and no towers, authored Wave 1 produced `7` leaks and `base=13/20` while `RewardOfferPending=true`. Destroying the Base before selecting the offer returned `acceptedAfterDestroy=false`; `ForceStopWave` cleared the pending offer. The telemetry journal contained `BaseDestroyed`, `RunFinished(Defeat)`, and `Defeat`, with no `RewardSelected`. This probe validates terminal ownership only and does not claim a balance pass.

## Player tile policy

The player heuristic scores all valid tile options during the existing preparation phase using post-placement spawn-anchor coverage from `TileMapManager` and the existing `TowerPlacementSystem.CountCoveredEntrances` owner. Coverage is prioritized, followed by fewer open road ends and connected neighbors. `TilePlacementSystem` still owns selection and commit; the agent only routes the action and emits a bounded `[MLAgent] Tile decision` log with before/after readback.

R68 Play evidence recorded `Cross_3` and `Straight` choices with explicit `openEnds` and `coverage` transitions. The selector contract passed in the `66/66` EditMode suite. The run reset before direct final-wave terminal telemetry, so this is routing and observability evidence rather than a natural Victory claim.

## Player tower purchase and route policy

The player heuristic evaluates all affordable authored tower prefabs through the existing placement-slot owner. Candidate scoring includes uncovered entrance gain and `TowerPlacementSystem` NavMesh route-sample exposure; when those coverage signals tie, a cheaper option is preferred only when the remaining currency can still buy the cheapest basic tower. Successful commits emit `[MLAgent] Tower decision` with prefab index, cost, currency transition, anchor coverage, and reserve state.

R69/R70 Play evidence recorded two opening `Tower_00 Novice` purchases at `currency=50->25->0`, then a longer bounded run reached `WavesCompleted=2` with five towers and valid topology. The fresh route-aware smoke recorded `TowerPlaced` with `coverage=1/4` and `routeCoverage=13/30`. Contract coverage and the full EditMode suite passed `69/69`; this remains decision/route observability evidence, not a natural final-wave Victory claim.

## Player opening counter-role policy

When the first Wave 1 placement has an anchor-coverage tie, the heuristic may prefer an authored prefab carrying the existing `AoEWeapon` component. The bounded opening bonus is below one whole entrance-coverage step, so a candidate that covers an additional entrance remains preferred. This gives the player policy access to the authored Tesla area counter without duplicating weapon, economy, or placement state.

Committed tower decisions emit `role=area|single` and `openingDefense` in the existing `[MLAgent] Tower decision` log. R71 Play readback committed Tesla first and Novice second; Wave 2 snapshots recorded `4 kills / 4 leaks` at base `16/20`, then `11 kills / 9 leaks` at base `8/20`, with valid topology. Direct validation, forced recompilation, and the full `71/71` EditMode suite passed. The run did not produce a final terminal readback after MCP disconnected and recovered, so this is role-routing/combat telemetry evidence, not natural Victory evidence.
### 9.17 Route-aware tile decision telemetry

The player heuristic evaluates post-placement route samples for every valid tile option before committing through `TilePlacementSystem`. It retains anchor coverage as a secondary signal and does not introduce a parallel map or route owner. The bounded log contract is `[MLAgent] Tile decision=...;routeCoverage=covered/total`.

R72 proof: `PlayerSelectsTileWithHigherRouteCoverageWhenAnchorCoverageTies` is covered by the contract suite; direct validation and forced recompilation reported no C# errors; full EditMode passed `72/72`. Isolated Play recorded `17/23`, `18/23`, and `19/23` route coverage and reached Wave 2 with `TileMapValid=true`, `CoveredEntrances=3/3`, and an active enemy with `PathComplete`. This does not claim natural final-wave Victory.
### 9.18 Opening reserve and open-end pressure

The player policy keeps the authored AoE opening role, but awards its opening tie-break only when the candidate leaves enough currency for the cheapest basic tower. The placement log records `openingDefense` before the ghost is created and exposes `openingAreaRoleEligible`. Tile scoring now gives each post-placement open road end a bounded `500` penalty after route and anchor coverage, preserving route-aware decisions without rewarding uncontrolled frontier growth.

R73 proof: the reserve, coverage-priority, and lower-open-end contracts are included in the `74/74` EditMode suite; direct validation and forced recompilation have no C# errors. Fresh isolated Play recorded `Tower_00 Novice` at `50->25` and `25->0`, with `openingDefense=True` on the first commit, then Wave 1 combat at `1 kill / 1 leak`, base `19/20`, and valid topology. The episode reset before direct terminal telemetry, so this remains policy/combat evidence rather than natural final-wave Victory evidence.

### 9.19 Recovery and reinforcement preparation

The player heuristic selects `ResourceCache` when base damage is recoverable, the bank is below the cheapest basic tower cost, and the build is not in the critical-health band. `EmergencyRepairs` remains the recovery branch at or below 50% base health or when the bank can already buy the cheapest basic tower. After all entrances are covered, `ShouldPrioritizeReinforcementPlacement` routes an affordable new tower before `StartWave` when no upgrade is affordable. The action and spend owners remain `TowerDefenceAgent` routing plus `TowerPlacementSystem`/`ResourceManager` commit.

Placement telemetry now includes `placementIntent=coverage|reinforcement` and `basicReserveAfter`. The pure contract covers the reinforcement gate and rejects it when currency, coverage, or upgrade conditions are not met. R74 validation passed with no C# errors; the full EditMode gate passed `75/75`.

Runtime proof recorded `[MLAgent] Reward decision=ResourceCache;base=14/20;currency=12;towers=2`. A fresh bounded authored run reached Wave 2 with three Novice towers, `currency=0`, `currencySpent=75`, base `16/20`, `3` kills, `4` leaks, valid topology, and an active `PathComplete` enemy. The captured seed still needed coverage for its third purchase, so it does not claim a natural `placementIntent=reinforcement` line. No trainer or new task chat was started; Play was stopped explicitly. Natural final-wave Victory/Defeat remains unproven.

### 9.20 Player smoke isolation and episode-restart hygiene

`Enable Gameplay Smoke Isolation` disables only `TowerDefenceBalancerAgent` and `TowerDefenceEnemyLevelAgent`. The active `TowerDefenceAgent` stays enabled and continues to execute its heuristic preparation/combat policy. Destroyed diagnostic-agent references are removed before scene-reload reapplication, so the runtime isolation list does not grow on every restarted episode.

R75 validation passed with no C# errors and the full EditMode suite passed `75/75`. Play logged `Runtime gameplay smoke isolation enabled for 2 diagnostic agents; player agent remains active`, then progressed to authored Wave 2 with `WavesCompleted=1`, three towers, `2` kills, `5` leaks, base `15/20`, valid topology, and `PathComplete`. Three later reapply logs remained at `2` agents. The smoke used ML inference fallback because no trainer was connected; it did not capture a direct final terminal event.

### 9.21 Direct natural terminal readback

During bounded Play smoke isolation, the editor owner temporarily sets the active player's `RestartSceneOnEpisodeReset` to `false`. This keeps the natural terminal state available for telemetry inspection while preserving the original value on isolation exit; only the two diagnostic agents are disabled, and the player remains on its configured heuristic/inference path.

R76 proof: changed scripts validated with `0` C# errors; the editor script kept two existing analyzer warnings; forced recompilation returned `0` compiler errors; full EditMode passed `75/75`. Fresh inference reached authored Wave 2 and logged `BaseDestroyed` sequence `163`, `GameStateChanged=Defeat` sequence `171`, `RunFinished` sequence `172` with `wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=2`, followed by `Defeat` sequence `173`. The run had three towers, three kills, 18 cumulative leaks, and `TileMapValid=true`, with no generated-wave substitution. Play was stopped explicitly. No natural final-wave Victory was observed.

### 9.22 Coverage-preserving tile and strict placement policy

The player policy logs the committed tile choice even when no option change was needed. It rejects a tile option that reduces current entrance coverage when a valid preserving alternative exists, then keeps route coverage and bounded topology terms as the existing tie-breakers. Coverage preparation no longer commits a no-gain slot and no longer permits `StartWave` while an affordable coverage obligation is unresolved; when placement slots are temporarily unavailable, the action resolves to `NoOp` and emits one bounded `[MLAgent] Preparation hold=coverage` diagnostic. Placement-slot candidates are cached for one frame and invalidated after authored commits. Prefab range planning uses the authored `TowerStatsSO` value until the runtime stats component is initialized.

R77 validation passed with `TowerDefenceAgent.cs` at `0` warnings and `0` errors; forced recompilation had no compiler errors and retained only the two existing `CS0414` warnings; full EditMode passed `76/76`. The bounded isolated ML inference fallback placed towers, progressed through authored Wave 1 into Wave 2, and read `TileMapValid=true`, `CoveredEntrances=3/4`, `BaseHealth=20/20`, and `currency=2` at the active-wave boundary. Play was stopped explicitly. This is preparation/combat-loop proof, not natural final-wave Victory proof.

### 9.23 Opening reserve and reachable coverage gate

During opening Wave 1 preparation, `ChooseBestAffordableTower` keeps the cheapest-basic reserve when a candidate's one-entrance coverage advantage would otherwise spend it. A larger two-entrance advantage remains eligible. Placement telemetry records `openingReserveGuard` alongside the existing currency and coverage transitions.

The coverage gate now distinguishes an affordable tower from an affordable reachable coverage placement. `NoOp` is reserved for the latter case; when no current placement slot can cover an uncovered entrance, the agent emits `[MLAgent] Coverage gate=unreachable` and allows `StartWave`. This prevents a valid but temporarily unrepairable map geometry from freezing the run, without adding a second placement owner or arbitrary no-gain placement.

R78 validation: direct agent validation `0/0`; contract validation `0` errors with the three existing analyzer warnings; forced recompilation `0` compiler errors; full EditMode `79/79`, `0` failed, `0` skipped. Fresh isolated ML inference placed three authored Novice towers, reached authored Wave 2, and naturally ended with `BaseDestroyed` seq `216`, `RunFinished` seq `224`, and `Defeat` seq `225`; `TileMapValid=true`, `HasGeneratedWave=false`, `base=0/20`, `currency=6`. No natural Victory claim is made.

### 9.24 Terminal wave resolution and spawn guard

The player smoke now exercises the existing terminal owner chain while `GameManager` retains its delayed defeat presentation. `WaveManager` blocks enemy reward and completion payout after the base is destroyed or the game is over, and its async spawn loop exits before creating a new enemy in that terminal window. The agent and telemetry paths remain unchanged; this is a runtime-owner guard, not a second terminal controller.

The pre-fix baseline recorded `BaseDestroyed` seq `186`, then currency gain and `WaveCompleted` seq `196` before `Defeat`. R79 validation returned `0/0` for `WaveManager`, forced recompilation returned no compiler errors, and the full EditMode suite passed `80/80` with `0` failed and `0` skipped. Fresh isolated ML Play recorded `BaseDestroyed` seq `196`; after it there was no `EnemySpawned`, `CurrencyGained`, or `WaveCompleted`, followed by `RunFinished` seq `200` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=4`) and `Defeat` seq `201`. Play was stopped explicitly. Natural final-wave Victory remains unproven.

### 9.25 Placement-owner rejection handoff

The player agent no longer trusts a screen-visible planning slot after the real `TowerPlacementSystem` owner rejects it. `TryPlaceTowerAtScreenPosition` now emits bounded `surface-point-unavailable` and `blocking-intersection` logs. `TowerDefenceAgent.TryPlaceTower` probes the existing owner across the current candidate slots, caches an all-candidates rejection for the preparation phase, and removes stale placement priority so the existing upgrade/`StartWave` policy can proceed. The cache resets through the existing invalidation lifecycle; no duplicate placement, economy, or terminal owner was introduced.

R80 validation: `TowerDefenceAgent.cs` direct validation was `0/0`; `TowerPlacementSystem.cs` retained two analyzer warnings and `0` errors; the contract file retained three analyzer warnings and `0` errors. Unity Console initially exposed seven definite-assignment errors from short-circuit `out var` declarations; after initialization fixes, forced recompilation returned `0` compiler errors. The full EditMode suite passed `81/81`, `0` failed, `0` skipped.

The fresh isolated ML smoke reached authored Wave 2 rather than holding in Preparation: `GameStateChanged Preparation` sequence `84` was followed by `WaveStarted` sequence `87`, with the explicit runtime log `Coverage gate=unreachable;covered=1/2;currency=25;action=StartWave`. The natural terminal readback was `BaseDestroyed` `159`, `RunFinished` `163` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=25`), and `Defeat` `164`; `IsSpawning=false` and the map remained valid. This seed did not exercise a blocking-intersection rejection, so the owner-rejected line is not claimed as observed runtime evidence. Natural final-wave Victory remains the next balance gap. Play was stopped explicitly; no trainer or new task chat was created or restarted.

### 9.26 Mid-wave count and route-reinforcement gate

`Wave_02.countScaling` is now authored as `1.00` instead of `1.10`, with a contract asserting `14` expected enemies. `TowerDefenceAgent` keeps `TowerPlacementSystem` as the placement owner and adds a bounded route-reinforcement decision: it is eligible only when coverage placement is unavailable, no upgrade is affordable, the bank can buy the cheapest tower, and an existing tower is present. The candidate is accepted only if the existing route samples report more covered samples after the candidate. The decision log exposes `placementIntent` for causal readback.

R81 direct validation returned `0/0` for the changed agent and wave contract; the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors. The full EditMode gate passed `83/83`, `0` failed, `0` skipped.

Fresh isolated ML Play reached authored Wave 2 with `TotalEnemiesInWave=14` and three towers, but naturally ended in `Defeat`: `BaseDestroyed`, `RunFinished` (`wavesCompleted=1;finalWave=2;baseHealth=0/20;currency=4`), then `Defeat`. The terminal snapshot had `4` kills, `16` cumulative leaks, `EntryCoverageRatio=0.5`, `TileMapValid=true`, and no generated wave. The third placement did not improve anchor coverage in this seed, so the new route branch is not claimed as an isolated runtime success. Natural final-wave Victory remains unproven; next pass should use a clean telemetry cursor to inspect candidate preview versus committed route coverage before changing balance again. Play was stopped explicitly. No trainer or task-chat restart was needed.

### 9.27 Counter-aware opening and combat-power selector

The player agent now reads the existing authored upcoming enemy count before the first wave. An affordable area-role tower can bypass the opening reserve only for the bounded swarm threshold; all other opening choices retain the cheapest-basic reserve rule. `ChooseBestAffordableTower` additionally receives a planning combat-power array derived from authored tower stats. Coverage remains a primary term, but combat power prevents a small coverage advantage from selecting a tower with materially lower expected damage. No alternate selector, economy owner, or placement owner was introduced.

The existing ML decision log records `openingAreaCounterEligible` and `combatPower`, while `GameplayTelemetry` records the causal combat chain and terminal readback. R82 direct validation returned `0/0` for `TowerDefenceAgent.cs`; the contract validator retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors. The two focused selector tests passed, and the full EditMode suite passed `85/85`, `0` failed, `0` skipped.

The fresh isolated ML smoke selected Tesla for the opening swarm (`50->10`, `combatPower=15.00`, area-counter eligible), then selected Novice after Wave 1 (`33->8`, `combatPower=9.00`). Wave 1 read `4` kills / `3` leaks; Wave 2 read `3` kills / `11` leaks with base `1/20`, followed by the existing Emergency Repairs reward to `11/20`. The agent entered Wave 3 with `41` authored enemies, then reached natural `BaseDestroyed` seq `441` and `RunFinished` seq `455` (`wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=32`), followed by `Defeat`; the map stayed valid and no generated wave was used. The selector and wave-reach improvement are runtime-confirmed; natural Victory remains unproven. The next gap is authored Wave 2/3 combat survival, not placement reachability. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

### 9.28 Emergency repair owner effect and readback

`WaveManager` remains the single reward-effect owner. `EmergencyRepairs` now computes a bounded repair to the 75% max-health recovery band and applies it through the existing `GameManager.TryRepairBase` path. Per-offer currency and repair amounts are reset before each choice, and `GameplayTelemetry.RewardSelected` records both `amount` and `baseRepair` so currency and health effects are not conflated.

The focused contract passed `1/1`; direct validation reported `0` warnings and `0` errors for the three changed C# files; forced recompilation returned `0` compiler errors; and the full EditMode suite passed `86/86` with `0` failed and `0` skipped. In the fresh isolated ML smoke, Wave 2 opened at `base=1/20`; `BaseHealthChanged` sequence `199` read `1->15`, and `RewardSelected` sequence `200` read `rewardId=EmergencyRepairs;amount=0;baseRepair=14;currencyAfter=94`. The run entered authored Wave 3 at `15/20`, then naturally ended in `Defeat` at `RunFinished` sequence `481` (`wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=44`) followed by `Defeat` sequence `482`. `TileMapValid=true` and `HasGeneratedWave=false`. This validates the repair branch and telemetry, while final-wave Victory remains unproven. Play was stopped explicitly; no trainer, new task chat, or worktree was restarted.

### 9.29 Authored Wave 2 pressure correction

The authored `Wave_02` composition is now `8 Turtle + 4 Frog` instead of `9 Turtle + 5 Frog + 1 Boss No Damage`, with the existing progression contract asserting `12` enemies. Both mid-run roles remain present, the boss stays deferred to the authored final wave, and the final-wave asset is unchanged. R84 full EditMode passed `86/86`; fresh isolated ML telemetry completed Wave 2 at `7 kills / 5 leaks`, repaired base `4->15`, and entered authored Wave 3 with `41` enemies. This is a bounded authored-pressure improvement, not Victory evidence.

### 9.30 Combat-power reinforcement over upgrade

With full entrance coverage and both purchases affordable, the player agent compares a new tower's planning combat power with the selected upgrade's marginal gain. It prefers the new tower only above a bounded `2x` margin, and the accepted combat-power placement is not required to add route coverage. The existing placement owner remains unchanged. The ML decision log now includes `placementReason`, so the choice can be read as `combat-power-over-upgrade` rather than inferred from a tower count alone.

R85 direct validation returned `0` warnings and `0` errors for `TowerDefenceAgent.cs`; the contract file retained three existing analyzer warnings and `0` errors. The pure contract passed `1/1`; forced recompilation returned `0` compiler errors; and full EditMode passed `87/87`, `0` failed, `0` skipped. Final isolated ML readback logged `placementIntent=reinforcement;placementReason=combat-power-over-upgrade` at `coverage=4/4` and `currency=52->12`. The run reached authored Wave 3 with four towers and valid topology, but ended naturally in `Defeat` at base `0/20` (`22` kills, `24` leaks, `wavesCompleted=2`, no generated wave). Final-wave Victory remains unproven. Play was stopped explicitly; no trainer or task-chat restart was needed.

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

### 9.34 Delayed bounty guard and reward-decision telemetry

The existing `BountyContract` owner path was already implemented by `WaveManager` and exposed by `WaveUI`, but the player heuristic never selected it. `TowerDefenceAgent` now selects that existing reward only when a future authored wave remains, the base is above the 75% repair band, at least two towers and full entrance coverage are present, and the bank can still buy the cheapest tower. Emergency Repairs keeps priority when health is low; ResourceCache remains the catch-up path. The bounded reward log now includes `wave=current/total` and `coverage=covered/total`.

Direct validation returned `0/0` for `TowerDefenceAgent.cs`; the contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML inference ended naturally in `Defeat`: Wave 1 selected `ResourceCache` at `base=17/20;currency=28;towers=1;coverage=2/4` and completed `4 kills / 3 leaks`; Wave 2 selected `EmergencyRepairs` at `base=5/20;currency=96;towers=2;coverage=3/4`, completed `4 kills / 8 leaks`, and repaired base `5->15`. Wave 3 terminal telemetry recorded twelve Tank kills and eight Runner plus one Berserker leaks; the run ended at `20 kills / 20 leaks`, `base=0/20`, four towers, valid topology, and `HasGeneratedWave=false`. The bounty branch is contract-covered but not claimed as a natural runtime selection because its safety preconditions were correctly false in this seed. Natural Victory remains unproven; the next gap is Wave 2 Runner exposure and combat survival. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

### 9.35 Target archetype exposure telemetry

`GameplayTelemetry` now includes `archetype` and `priority` in `TowerTargetAcquired` and `TowerFired`, alongside range, distance, and target health. This extends the existing combat journal without changing `Tower` targeting ownership. A temporary Wave 2 Runner health probe (`1.0 -> 0.8`) was rejected: Runner max health became `36.00`, but all four Wave 2 Runners still leaked and the run ended at `14 kills / 22 leaks`; `Wave_02` and its contract were restored to `healthMultiplier=1.0`.

The fresh authored-baseline smoke recorded Wave 2 `17` target acquisitions, `19` fires, `26` damage applications, `5` kills, and `7` leaks. `Nearest` acquired five Runner targets and fired at four of them, proving the Runner path is reachable; the remaining gap is exposure/damage throughput and build timing, not another global target-priority change. The run naturally ended in `Defeat` with `RunFinished` `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=28`; no Victory claim is made. Direct validation returned `0/0` for the telemetry and progression paths, forced recompilation returned `0` compiler errors, the Console error filter returned `0`, and full EditMode remained `88/88`. Play was stopped explicitly; trainer was unavailable and Unity used inference fallback.

### 9.36 Wave 2 spawn-pacing probe rejected

The existing authored `Wave_02` Runner/Frog group was temporarily changed through Unity AssetDatabase from `spawnDelay=2` to `3` to test whether slower exposure would improve the existing combat path. The existing `WaveManager` spawn owner and targeting priority were unchanged. The probe was rejected: Wave 2 recorded `19` target acquisitions, `18` tower fires, `27` damage applications, `5` kills, and `7` leaks; five Runner acquisitions and four Runner fires still produced four Runner leaks. This matched the latest authored baseline outcome (`5/7`) and did not improve survival, so `Wave_02` was restored to `spawnDelay=2` and the temporary contract assertion was removed.

Direct validation returned `0/0` for `WaveProgressionContractTests.cs`; forced recompilation returned `0` compiler errors; the Console error filter returned no errors; and full EditMode passed `88/88`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable, entered authored Wave 3, and ended naturally in `Defeat` (`RunFinished`: `wavesCompleted=2;finalWave=3;baseHealth=0/20;currency=9`), with no Victory claim. Play was stopped explicitly. The persistent instrumentation and bounty guard remain; the next gap is damage throughput/build timing, not another spawn-delay probe.

### 9.37 Final-wave upgrade reserve

The existing player-agent preparation path now treats the phase before the final authored wave as a final-wave upgrade reserve when the latest completed wave is immediately before the final wave, entrance coverage is complete, and an upgrade is affordable. It suppresses reinforcement placement only in that bounded state; incomplete coverage still keeps the coverage obligation. The upgrade continues through the existing `Tower.UpgradeSpendingCost` and `ResourceManager` owners. The commit log now records `reason=final-wave-upgrade-reserve` and `wave=current/total`.

The new pure contract passed; direct validation returned `0/0` for `TowerDefenceAgent.cs`, while the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation returned `0` compiler errors and full EditMode passed `89/89`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback because the trainer was unavailable and captured two final-preparation upgrades (`Tesla 0->1`, `Tesla 1->2`) with full `coverage=1/1`; the terminal snapshot read `TowersUpgraded=2`, `wavesCompleted=2`, `BaseHealth=0/20`, `HasGeneratedWave=false`, followed by natural `Defeat`. The branch is runtime-confirmed, but final-wave Victory remains unproven. Play was stopped explicitly; no trainer, task-chat restart, or worktree was used.

### 9.38 Spawn role and pacing telemetry

The existing `WaveManager` spawn owner now exposes the last spawned group/index/archetype/scaled health/scaled speed/spawn delay to `GameplayTelemetry`. `EnemySpawned` keeps its causal wave payload and adds bounded details in the form `group`, `enemy`, `archetype`, `health`, `speed`, and `spawnDelay`; no second spawn owner or runtime balance change was introduced. A temporary Runner-first Wave 3 order probe was rejected: it produced `12` Runner target acquisitions, `8` fires, `0` kills, and `8` leaks, destroyed the base after `10/21` authored spawns, and ended at `8` kills / `19` leaks in that seed. The authored order was restored to `Tank -> Runner -> Berserker` through Unity AssetDatabase.

The telemetry formatter contract and all changed C# files validated with `0` warnings and `0` errors; forced recompilation finished without compiler errors; full EditMode passed `89/89`, `0` failed, `0` skipped. Two bounded Play attempts returned MCP `Entered play mode` but Unity immediately reported `EditorApplication.isPlaying=false`; isolation and `td_gameplay_telemetry` therefore could not start, and no runtime result is claimed for this slice. Existing prior ML runs used inference fallback because the trainer was unavailable; no chat or worktree was restarted. The next gap is final-wave Runner throughput using the new spawn-role telemetry, not another uninstrumented balance probe.


### 9.39 Runtime compile correction and Play transition blocker

R93 exposed a latent compile defect in the new spawn telemetry: `scaledHealth` was declared inside the health-initialization block but consumed by the following telemetry block. Unity Editor.log caught the real `CS0103` during compilation; the fix hoisted the local to `SpawnEnemy` scope and did not change serialized assets or balance. Direct validation then returned `0` warnings and `0` errors for the changed C# files, forced recompilation finished without compiler errors, and full EditMode passed `90/90`, `0` failed, `0` skipped. Unity still reports the existing non-blocking `CS8785` Odin source-generator warning.

After the compile fix, Play entered `is_playing=true` but remained `is_changing=true` for `40-47` seconds on repeated attempts. The temporary editor-only toggle of Enter Play Mode Options did not change the transition and was restored to `enabled=true;options=3`. Play was stopped through MCP; no gameplay telemetry, ML-agent result, or Victory claim is made. The next gap remains final-wave Runner throughput once Unity Play transition is healthy.


### 9.40 Runtime spawn-role readback

The repaired telemetry refactor was runtime-confirmed in an isolated ML Play smoke using inference fallback because no trainer was connected. `EnemySpawned` readback preserved authored groups and scaling: Wave 1 `Tank 7/7`, `health=20.00`, `speed=3.00`, `spawnDelay=1.00`; Wave 2 `Tank 8/8`, `health=24.75`, `speed=3.30`, `spawnDelay=1.00`, then `Runner 4/4`, `health=45.00`, `speed=3.00`, `spawnDelay=2.00`; Wave 3 reached `Tank 12/12`, `health=32.50`, `speed=3.00`, `spawnDelay=0.50`, then `Runner 5/8`, `health=97.50`, `speed=4.50`, `spawnDelay=1.00` before the base was destroyed. The run produced `19` target acquisitions, `23` tower fires, and `46` damage applications; terminal telemetry identified Runner and Tank leaks by archetype. The final snapshot was `Wave 3`, `17/21` spawned, `8` kills, `28` leaks, `2` towers, `2/4` coverage, `base=0/20`, natural `Defeat`, and no Victory. Play was stopped explicitly; the next gameplay gap is preparation/combat survival, not missing spawn observability.

### 9.41 Route-reinforcement priority under incomplete coverage

The player agent now allows the existing route-reinforcement path to outrank an affordable upgrade when entrance coverage is incomplete and a direct coverage placement is unavailable. The route branch still requires an affordable tower, an existing tower, incomplete coverage, and a route-contributing slot; the final-wave upgrade reserve remains gated by complete coverage. No second decision owner or fallback placement path was introduced.

The contract now covers route reinforcement before upgrade in this state. Direct validation returned `0` warnings and `0` errors for `TowerDefenceAgent.cs`; the ML contract retained three existing analyzer warnings and `0` errors. Forced recompilation finished without compiler errors and full EditMode passed `90/90`, `0` failed, `0` skipped. Fresh isolated ML Play used inference fallback and emitted `TowerPlaced` telemetry for the route branch (`Tesla`, `cost=40`, `coverage=3/3`, `routeCoverage=35/37`) before Wave 3. The terminal snapshot recorded `3` towers, `3/3` coverage, `14` kills, `23` leaks, `19/21` spawns, and `base=0/20`, then natural `Defeat`; Play was stopped explicitly and Victory remains unproven. The next gap is Wave 3 combat throughput, with Runner leaks still visible in terminal telemetry.

### 9.42 Tower-grid input ownership repair

The Gameplay scene now separates the manual input default from ML input: all authored ML agents start with `_trainingMode=0`, and the standalone root `Synthetic Mouse` is disabled. This prevents `InputProvider_NewInputSystem` from receiving a synthetic origin position while a player is selecting a tower. An ML smoke may set the existing player agent's `TrainingMode=true` at runtime; that state is not saved back into the scene.

The bounded ML check after this change committed `TowerPlaced` at `(-3.00, 0.50, 4.00)`, with integer grid coordinates and the existing `TowerPlacementSystem` owner. The smoke used the current synthetic transport only for verification, reached Wave 2 with valid map telemetry, and was stopped explicitly. This is input-routing evidence, not a Victory result.

### 9.43 ML agent owns synthetic mouse activation

The active player agent is authored with `_trainingMode=1`, while its nested `TD ML Input` object and `SyntheticMouse` component are authored inactive. `TowerDefenceAgent.Start` and the `TrainingMode` property are the only runtime path that activates that input for ML gameplay. The standalone root `Synthetic Mouse` stays inactive, and the disabled diagnostic agents keep `_trainingMode=0`; this prevents duplicate input providers and keeps manual play on the hardware mouse.

The same smoke also covers the combat owner fix: `Tower.UpdateTarget` retains a live target from the current overlap result instead of rejecting it by transform-center distance after collider-bound acquisition. Direct validation had `0` errors, full EditMode passed `90/90`, and runtime telemetry reported `training=true`, `inputActive=true`, `standaloneActive=false`, `input.device=TD Synthetic Mouse`, `TileMapValid=true`. The ML inference episode naturally ended in `Defeat` with `2` towers, `2/4` coverage, and `base=0/20`; no Victory claim is made.
