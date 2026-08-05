---
title: Низкоуровневая математика и механики tower defense
type: gameplay math contract
status: design-target
updated: 2026-08-04
scope: movement, targeting, attacks, damage, shields, armor, effects, auras, upgrades and economies
related: 01_GAME_LOOPS_AND_ECONOMIES.md, 03_DATA_CONTEXTS_AND_FORMATS.md, 04_SERVICES_AND_LIFECYCLES.md, 05_SCENE_OBJECTS_AND_COMPONENTS.md
---

# Низкоуровневая математика и механики tower defense

## 1. Назначение

Документ фиксирует минимальную математику, достаточную для симуляции одной 3D tower-defense волны, переноса состояния внутри забега и расчёта мета-экономики.

Это не готовый числовой баланс. Формулы задают порядок вычислений, единицы, владельцев и инварианты. Конкретные значения хранятся в Definition/Stats assets и проверяются playtest-ами.

Базовый принцип:

> Одно событие симуляции создаёт один неизменяемый вход, один owner-side результат и одно изменение authoritative state.

## 2. Граница и уровень модели

Минимальная модель включает:

- Ground и Flying движение к базе;
- поиск допустимой цели;
- cadence атаки и доставку попадания;
- HP, shield, armor/resistance и damage types;
- crit, AoE, pierce, beam, statuses и auras как условные расширения;
- upgrade и derived stats;
- road-contact Tower, которая не блокирует путь;
- optional auto-repair между волнами или по времени в волне;
- деньги одной волны, одного забега и meta profile;
- deterministic random для saveable gameplay choices.

Не входят физически точная баллистика, полноценный 3D air pathfinding, сетевой rollback, replay determinism и ECS. Они добавляются только отдельной задачей.

## 3. Единицы и обозначения

| Символ | Смысл | Единица/граница |
| --- | --- | --- |
| `t`, `Δt` | simulation time и шаг | секунды, `Δt >= 0` |
| `p` | world position | Unity world units |
| `L` | длина маршрута | world units |
| `v` | скорость | units/second, `v >= 0` |
| `R` | радиус/дальность | units, `R >= 0` |
| `H`, `Hmax` | HP | points, `0 <= H <= Hmax` |
| `S`, `Smax` | shield | points, `0 <= S <= Smax` |
| `D` | damage | points, `D >= 0` |
| `f` | multiplicative factor | безразмерный, обычно `f >= 0` |
| `Tattack` | интервал атак | seconds/attack, `> 0` |
| `q` | shots per second | attacks/second, `q = 1 / Tattack` |
| `B` | run currency balance | integer или fixed precision, `B >= 0` |
| `M` | meta currency balance | integer, `M >= 0` |
| `g` | grade/level Tower | integer, `0..MaxGrade` |

Проценты в Definition хранятся в одной выбранной форме: factor `1.25` либо normalized delta `0.25`. В одном поле нельзя смешивать `25`, `0.25` и `1.25`.

## 4. Минимальное состояние симуляции

### 4.1 Enemy

```text
EnemyState
├── EntityId, DefinitionId
├── MovementDomain: Ground | Flying
├── Position, RouteProgress, RemainingDistance
├── MoveSpeed
├── Current/MaxHealth
├── Current/MaxShield
├── Armor/Resistance snapshot
├── ActiveEffects[]
├── Reward, LeakDamage
└── Terminal: Active | Kill | Leak | Despawn
```

### 4.2 Tower

```text
TowerState
├── EntityId, DefinitionId
├── Position, Grade, Branches
├── Damage, AttackInterval, Range
├── ProjectileSpeed/DeliveryType
├── TargetFilter, TargetPolicy
├── Cooldown, CurrentTargetId
├── Modifiers/Effects/Auras
└── optional Health/Broken/RepairState
```

### 4.3 Wave и экономика

```text
WaveState: spawn cursor, spawned IDs, active terminal set, payout guards
EconomyState: current balance, modifiers, ledger sequence
BaseState: current HP/shield, destroyed-once guard
```

Definition задаёт исходные параметры. Runtime owner хранит текущие значения. Save хранит snapshot. UI показывает ReadModel. Эти четыре формы не являются взаимозаменяемыми. Конкретные названия классов не задаются существующим кодом и выбираются при greenfield-реализации по этому контракту.

## 5. Время и порядок обновления

### 5.1 Basic real-time

Для таймера:

```text
remaining(t + Δt) = max(0, remaining(t) - Δt)
```

Для накопителя:

```text
accumulator(t + Δt) = accumulator(t) + Δt
while accumulator >= interval:
    resolve one tick
    accumulator -= interval
```

`while`, а не один `if`, сохраняет число ticks при редком кадре. Для защиты от зависания задаётся технический maximum ticks per frame, а потерянные ticks обрабатываются явной policy.

### 5.2 Scopes времени

- Pause останавливает gameplay simulation time.
- UI animation может использовать unscaled time, но не меняет gameplay timers.
- Wave timer прекращается при `WaveResolve`, `Defeat` или `Abandon`.
- Actor timer прекращается при terminal/return to pool.
- Saveable random не зависит от frame rate.

## 6. Движение Ground и Flying

### 6.1 Ground

Ground Enemy следует валидному маршруту/NavMesh.

```text
distanceStep = v_effective × Δt
remainingDistance' = max(0, remainingDistance - distanceStep)
normalizedProgress = 1 - remainingDistance / max(pathLength, ε)
timeToBase ≈ remainingDistance / max(v_effective, ε)
```

`ε` — малое техническое число, предотвращающее деление на ноль. Enemy с нулевой скоростью не получает бесконечный числовой результат в UI; показывается состояние immobilized.

### 6.2 Flying

Basic Flying Enemy не использует NavMeshAgent.

Для прямого полёта:

```text
direction = normalize(targetPosition - position)
position' = MoveTowards(position, targetPosition, v_effective × Δt)
remainingDistance = distance(position, targetPosition)
```

Для waypoint route:

```text
L_air = Σ distance(waypoint[i], waypoint[i + 1])
```

После достижения waypoint выбирается следующий authored aerial anchor. Missing required aerial route блокирует spawn/wave; Ground route не используется как fallback.

### 6.3 Slow и speed modifiers

Один понятный порядок:

```text
v_effective = clamp(
    (v_base + Σ flatSpeed) × Π speedFactors,
    v_min,
    v_max)
```

Если slow хранится как `slowFraction`:

```text
speedFactor = 1 - clamp(slowFraction, 0, slowCap)
```

Basic рекомендуется `slowCap < 1`, чтобы обычный slow не превращался в stun. Stun — отдельный status с собственным lifecycle.

## 7. Геометрия range и допустимость цели

Цель допустима, если одновременно:

```text
alive
AND targetable
AND movementDomain matches Ground/Flying/Both
AND squaredDistance(tower, target) <= R²
AND layer/faction/tag filters pass
AND lineOfSight passes, if enabled
```

Для сравнения range используется squared distance без квадратного корня.

### 7.1 Target policies

После фильтрации Tower сортирует candidates по одному стабильному ключу:

| Policy | Основной ключ | Tie-breaker |
| --- | --- | --- |
| First | максимальный route progress | EntityId |
| Last | минимальный route progress | EntityId |
| Nearest | минимальная squared distance | EntityId |
| Farthest | максимальная squared distance | EntityId |
| Strongest | максимальный effective HP | route progress, EntityId |
| Weakest | минимальный effective HP | route progress, EntityId |
| LowestShield | минимальный current shield | route progress, EntityId |

Tie-breaker нужен для предсказуемости. UI меняет policy, но не передаёт Tower готовую «правильную» цель.

### 7.2 Exposure time

Для участка маршрута длиной `Lrange`, находящегося в range Tower:

```text
Texposure ≈ Lrange / max(v_effective, ε)
```

Это низкоуровневая связь карты и боя:

```text
длиннее участок в range → больше Texposure → больше атак → больше damage
```

## 8. Cadence атаки и доставка

### 8.1 Интервал и DPS

Если Definition задаёт shots per second:

```text
Tattack = 1 / max(shotsPerSecond, ε)
```

Если задаёт attack interval, он используется напрямую. Нельзя одному UI называть `FireRate` интервалом, а другому — выстрелами в секунду.

При непрерывной стрельбе без crit/resistance:

```text
DPS_raw = DamagePerAttack / Tattack
```

Число атак за exposure при первой атаке через `firstShotDelay`:

```text
Nshots = max(0, 1 + floor((Texposure - firstShotDelay) / Tattack))
```

если `Texposure < firstShotDelay`, `Nshots = 0`.

### 8.2 Cooldown

```text
cooldown' = max(0, cooldown - Δt)
if cooldown == 0 AND validTarget:
    fire exactly once
    cooldown += Tattack
```

Policy пропущенных выстрелов при длинном кадре должна быть единой: либо catch-up с ограничением, либо не более одной атаки за frame. Она не должна зависеть от weapon variant случайно.

### 8.3 Projectile travel

Для неподвижной цели:

```text
Ttravel = distance(origin, target) / max(projectileSpeed, ε)
```

Basic projectile может быть homing либо лететь в captured point. LostTargetPolicy (`Dissipate`, `Continue`, `Retarget`) задаётся Definition. Это вариант поведения, не fallback.

### 8.4 Predictive intercept — Extended

Для постоянной скорости цели `vTarget`, относительной позиции `r` и скорости projectile `s` ищется минимальное `t > 0`:

```text
(dot(vTarget, vTarget) - s²) × t²
+ 2 × dot(r, vTarget) × t
+ dot(r, r) = 0
```

Если положительного решения нет, weapon применяет заранее выбранную policy: fire at current position, do not fire или homing. Нельзя молча менять delivery type.

## 9. Damage pipeline

### 9.1 Immutable вход

`DamagePacket` фиксирует source, attack ID, raw damage, damage type, crit result, bypass/penetration и status payload. Random crit определяется до receiver commit и не перебрасывается разными consumers.

### 9.2 Порядок

1. Проверить target active/alive и duplicate AttackId.
2. Проверить immunity.
3. Получить raw/crit damage.
4. Разделить shield bypass и shield lane.
5. Поглотить shield с type factor.
6. Применить health resistance.
7. Применить armor/penetration.
8. Изменить HP.
9. Применить status по result rules.
10. Сформировать один `DamageResult`.
11. При `HP <= 0` пройти одну terminal gate.

### 9.3 Raw и crit

```text
Dbase = max(0, BaseDamage + Σ flatDamage)
Dscaled = Dbase × Π damageFactors
Dcrit = Dscaled × (isCritical ? CritFactor : 1)
```

Expected damage для баланса, не для runtime hit:

```text
E[D] = Dscaled × (1 + CritChance × (CritFactor - 1))
```

`CritChance` ограничивается `0..1`, если Definition явно не поддерживает guaranteed multi-crit.

### 9.4 Shield

Пусть `b` — bypass fraction, `Fs` — effectiveness damage type против shield.

```text
Dbypass = Dcrit × clamp(b, 0, 1)
DshieldLane = Dcrit - Dbypass
nominalCapacity = S / max(Fs, ε)
DnominalAbsorbed = min(DshieldLane, nominalCapacity)
ShieldDamage = DnominalAbsorbed × Fs
S' = max(0, S - ShieldDamage)
DafterShieldNominal = Dbypass + (DshieldLane - DnominalAbsorbed)
```

Так `Fs > 1` быстрее расходует shield, но остаток не получает случайно тот же multiplier против HP.

Shield recharge:

```text
if timeSinceShieldDamage >= RechargeDelay:
    S' = min(Smax, S + RechargeRate × Δt)
```

Break event возникает только при переходе `S > 0 → S' = 0`.

### 9.5 Resistance и armor

Пусть `Fh` — damage-type factor против health, `Aflat` — flat armor, `Apct` — percentage armor, `Pflat/Ppct` — penetration.

```text
Dtyped = DafterShieldNominal × max(0, Fh)
AflatEffective = max(0, Aflat - Pflat)
ApctEffective = clamp(Apct - Ppct, 0, ArmorPercentCap)
DafterFlat = max(0, Dtyped - AflatEffective)
Dhealth = DafterFlat × (1 - ApctEffective)
```

Если minimum damage включён:

```text
Dhealth = Dtyped > 0 ? max(MinimumDamage, Dhealth) : 0
```

Порядок `flat → percent` фиксирован Definition/общими rules. UI и projectile не пересчитывают его.

### 9.6 HP и terminal

```text
H' = clamp(H - Dhealth, 0, Hmax)
overkill = max(0, Dhealth - H)
```

Terminal claim выполняется только при первом переходе `H > 0 → H' = 0`. Повторный hit не создаёт второй kill reward.

### 9.7 Damage types

Basic закрытый набор:

- `Physical` — обычный health/armor matchup;
- `Energy` — отдельная shield effectiveness;
- `Explosive` — delivery с area policy;
- `True` — только явно заданный bypass resistance/armor/shield.

Damage type отвечает за matchup. Delivery (`Projectile`, `Beam`, `AoE`) отвечает за то, как packet достигает targets. Это разные оси.

## 10. Multi-target damage

### 10.1 AoE

Для distance `d` от impact и radius `Raoe`:

```text
u = clamp(d / max(Raoe, ε), 0, 1)
falloff = FalloffCurve(u)
Dtarget = Dcenter × falloff
```

Ground surface AoE по умолчанию фильтрует Flying. Spherical/anti-air explosion использует `Both`, если так задано Definition.

Каждый target получает отдельный DamagePacket/AttackTargetSequence, но один area attack ID. Hit set не позволяет одному collider получить damage несколько раз.

### 10.2 Pierce

Для попадания с индексом `i`:

```text
Di = D0 × PierceFactor^i
```

или authored curve. Target IDs уникальны в hit set.

### 10.3 Chain

```text
Di = D0 × ChainFactor^i
```

Следующая цель выбирается из допустимых, ещё не поражённых targets в chain range. Stable tie-breaker обязателен.

### 10.4 Beam и damage-over-time

```text
Nticks = floor(BeamDuration / TickInterval) + optional final policy
TotalRaw = Σ DamagePerTick
```

Каждый tick имеет отдельную sequence identity либо receiver-side deduplication key.

## 11. Status effects

### 11.1 Effect instance

```text
EffectInstance = DefinitionId + SourceId + TargetId + Stacks + RemainingDuration + TickAccumulator
```

### 11.2 Stack policies

- `Replace`: новый instance заменяет старый.
- `Refresh`: magnitude/stacks сохраняются, duration обновляется.
- `AddStacks`: `stacks' = min(MaxStacks, stacks + added)`.
- `Strongest`: активен strongest magnitude, duration обновляется по policy.
- `Independent`: отдельные source instances, если это нужно design-у.

### 11.3 Tick

```text
while accumulator >= TickInterval:
    apply one typed tick packet
    accumulator -= TickInterval
```

DoT kill проходит тот же Enemy terminal и reward path с source attribution. Status component не начисляет деньги.

## 12. Auras и modifier stacking

Aura применяет target-owned modifier handle при enter и снимает тот же handle при exit/source death.

### 12.1 Stat aggregation

Рекомендуемый общий порядок:

```text
X0 = BaseDefinitionValue
X1 = X0 + Σ FlatModifiers
X2 = X1 × (1 + Σ AdditivePercentModifiers)
X3 = X2 × Π MultiplicativeFactorsByGroup
X4 = OverrideHighestPriorityIfAny(X3)
Xfinal = clamp(X4, Min, Max)
```

Для каждого stacking group определяется `Add`, `Multiply`, `Strongest`, `Unique` или `Refresh`. Порядок не зависит от порядка входа colliders.

### 12.2 Aura cadence

Trigger enter/exit — Basic, если topology стабильна. Periodic scan:

```text
scanAccumulator += Δt
if scanAccumulator >= ScanInterval:
    query eligible targets
    diff previous/new sets
    apply/remove handles
```

Spatial registry добавляется только после profiling. Receiver остаётся owner итоговых stats/effects.

## 13. Stats и upgrades

### 13.1 Grade curve

Для stat `X` на grade `g`:

```text
X(g) = RoundIfRequired(clamp(
    BaseX + GrowthCurveX(g) + Σ flatRunModifiers,
    MinX,
    MaxX) × Π factors)
```

Альтернатива simple geometric growth:

```text
X(g) = BaseX × GrowthFactor^g
```

Один stat использует одну authored model. UI показывает `X(current) → X(next)` из того же owner calculation.

### 13.2 Upgrade cost

```text
UpgradeCost(g → g + 1) = round(BaseUpgradeCost × CostGrowthFactor^g)
```

или authored cost curve/list. Команда валидирует grade/branch/conflicts/currency до commit.

```text
TotalInvestment = BuildCost + Σ AppliedUpgradeCosts
SellValue = floor(TotalInvestment × RefundFraction)
```

Monster-caused Tower break не является sell и по умолчанию даёт `0` refund.

### 13.3 Branches

Branch — stable ID + prerequisites + conflicts + modifiers. Save хранит branch IDs, а не final DPS/range. Respec отсутствует либо имеет явную цену.

## 14. Road-contact Tower без блокировки пути

Road-contact Tower имеет trigger/contact volume, но не закрывает graph и не включает NavMesh carving.

### 14.1 Contact gate

Контакт принимается, если:

```text
Tower active
AND Enemy active
AND MovementDomain passes filter
AND pair/correlation is not already resolved
AND per-enemy cooldown <= 0
```

### 14.2 Варианты resolution

- `InstantKillEnemy`: Enemy получает terminal Kill; Tower остаётся.
- `InstantBreakTower`: Tower становится Broken; Enemy продолжает путь.
- `ComparePower`: большее authored contact power переживает контакт, равенство — mutual result.
- `MutualDamage`: оба receivers получают typed packets.
- `DamageEnemyThenTower`: порядок фиксирован; второй шаг выполняется по authored policy.

Итог всегда один `TowerEnemyContactResult` с двумя независимыми receiver results.

### 14.3 Tower HP и Broken

```text
TowerH' = clamp(TowerH - ResolvedContactDamage, 0, TowerHmax)
Broken = TowerH' == 0
```

Broken Tower отключает weapon, contact и offensive aura. При `RemainBroken` instance остаётся в сцене и может восстановиться. Выживший Enemy продолжает обычный маршрут.

### 14.4 Auto-repair между волнами

```text
BetweenWavesFull:   TowerH' = TowerHmax
BetweenWavesAmount: TowerH' = min(TowerHmax, TowerH + RepairAmount)
```

Применение имеет guard по `WaveInstanceId` и выполняется один раз на переходе `WaveResolve → Preparation`.

### 14.5 Auto-repair внутри волны

```text
if timeSinceLastDamage >= RepairDelay AND repairAllowed:
    TowerH' = min(TowerHmax, TowerH + RepairRate × Δt)
```

Повторный damage может сбрасывать delay. Rebuild from Broken — отдельный authored flag/delay. Timer использует simulation time и owner cancellation token.

## 15. Wave spawn и completion

Для spawn group `j`:

```text
spawnTime(j, i) = GroupStartTime(j) + i × SpawnInterval(j)
```

Фактический count после scaling:

```text
CountResolved = max(0, RoundByPolicy(BaseCount × CountFactor))
HealthResolved = max(MinHealth, BaseHealth × HealthFactor)
SpeedResolved = clamp(BaseSpeed × SpeedFactor, MinSpeed, MaxSpeed)
```

Wave complete:

```text
spawnScheduleExhausted
AND activeEnemyTerminalSet is empty
AND NOT baseDestroyed
AND completionGuard not yet claimed
```

Flying и Ground Enemy находятся в одном terminal set. Отдельный wave owner для Flying не нужен.

## 16. Экономика одной волны

### 16.1 Открытие и commit

```text
Bcommit = Bopen
        + PreparationGrants
        - TileCosts
        - TowerBuildCosts
        - UpgradeCosts
        - RepairCosts
```

Каждый расход — атомарный `TrySpend`. При отказе balance и domain state не меняются.

### 16.2 Kill income

Для Enemy `i`:

```text
Rkill(i) = round(
    BaseKillReward(i)
    × DifficultyRewardFactor
    × RunRewardFactors
    × BountyFactor(i)
    × EarlyKillFactor(i))
```

Early-kill вариант:

```text
progress = clamp(RouteProgressAtDeath, 0, 1)
EarlyKillFactor = lerp(MaxEarlyFactor, 1, progress)
```

Он включается только authored policy. Kill reward выдаётся один раз; Leak даёт ноль kill reward.

### 16.3 Производство внутри волны — Extended

Для period `Tincome`:

```text
NincomeTicks = floor(ActiveEligibleTime / Tincome)
Rproduction = NincomeTicks × IncomePerTick
```

Eligibility, cap и остановка при Broken/disabled задаются Definition. Production tick имеет correlation ID и ledger entry.

### 16.4 Закрытие волны

```text
BendCombat = Bcommit + Σ Rkill + Rproduction
Bclose = BendCombat + Rcompletion + Rpassive
```

`Rcompletion` и `Rpassive` применяются один раз после успешного completion. При Defeat completion payout отсутствует.

## 17. Экономика одного забега

```text
Bopen(w + 1) = Bclose(w)
```

Полный банк после `n` волн:

```text
Bclose(n) = Bstart
          + Σ PreparationGrants(w)
          + Σ KillIncome(w)
          + Σ ProductionIncome(w)
          + Σ CompletionIncome(w)
          + Σ PassiveIncome(w)
          - Σ BuildCosts(w)
          - Σ UpgradeCosts(w)
          - Σ TileCosts(w)
          - Σ RepairCosts(w)
          + Σ SellRefunds(w)
```

Run-level decisions:

- build breadth против upgrade depth;
- текущая безопасность против reserve;
- repair против новой обороны;
- tile/map expense против прямого DPS;
- economy/production против риска потерять источник дохода.

### 17.1 Проверки tension

```text
AffordabilityRatio = Bopen / CostOfBestImmediateAnswer
ReserveRatio = Bclose / ExpectedNextWavePreparationCost
IncomeToSpendRatio = ExpectedWaveIncome / ExpectedRequiredSpend
```

Это telemetry/balance metrics, не gameplay rules. Если игрок всегда покупает всё, costs/rewards не создают tension. Если ни один показанный counter недоступен, возникает forced loss.

## 18. Meta-экономика

### 18.1 Settlement reward

```text
Mprogress   = ProgressReward(WavesCompleted)
Moutcome    = VictoryBonus | DefeatProgressBonus | 0 for Abandon
Mchallenge  = Σ CompletedChallengeBonuses
Mobjective  = Σ FirstCompletionBonuses

Mreward = max(0,
    round((Mprogress + Moutcome + Mchallenge) × DifficultyMetaFactor)
    + Mobjective)
```

Не конвертировать остаток run currency напрямую в meta currency без отдельного design contract: иначе оптимальная игра может стать накоплением денег вместо защиты базы.

### 18.2 Idempotency

```text
if SettlementReceipts contains RunId:
    Delta = 0
    return AlreadySettled
else:
    apply reward and receipt in one atomic ProfileSave transaction
```

### 18.3 Purchase

```text
canPurchase = unlockedPrerequisites
          AND notAlreadyPurchased
          AND MetaBalance >= UnlockCost

MetaBalance' = MetaBalance - UnlockCost
```

Balance и unlock применяются либо оба, либо ни один. UI не подтверждает покупку до успешного ProfileSave result.

### 18.4 Power policy

Basic meta приоритетно открывает горизонтальные варианты. Для vertical upgrade:

```text
PermanentBonus(level) = min(MaxBonus, Curve(level))
```

Cap обязателен. Базовый run должен оставаться проходимым без grind.

## 19. Effective HP, TTK и throughput

### 19.1 Effective HP

При постоянной percentage reduction `r` без shield:

```text
EHP ≈ H / max(1 - r, ε)
```

Со shield без bypass:

```text
EHPapprox ≈ H / (1 - rHealth) + S / ShieldDamageFactor
```

Это оценка для balance. Runtime использует полный pipeline.

### 19.2 Time to kill

Для постоянного resolved damage `Dhit`:

```text
HitsToKill = ceil(EffectiveHitPoints / max(Dhit, ε))
TTK ≈ firstShotDelay + (HitsToKill - 1) × Tattack + deliveryDelay
```

Tower успевает убить цель в своей зоне, если приблизительно:

```text
TTK <= Texposure
```

### 19.3 Wave throughput

```text
RequiredDPS ≈ TotalEffectiveEnemyHP / WaveCombatWindow
CoverageUptime = TimeWithValidTarget / WaveActiveTime
OverkillRatio = TotalOverkillDamage / TotalResolvedHealthDamage
LeakRate = LeakedEnemies / SpawnedEnemies
```

Эти метрики объясняют слабость билда: недостаточный DPS, короткая exposure, плохая policy, overkill или неверный damage matchup.

## 20. Deterministic random

Gameplay random использует run-scoped PRNG state.

```text
RandomState' = Next(RandomState)
value = Map(RandomOutput, requested range)
```

Отдельные named streams рекомендуется использовать для map, rewards и wave variation. Cosmetic VFX random не сдвигает gameplay stream.

Save хранит state либо уже сгенерированный exact offer. Load не reroll-ит reward, crit history, map choice или wave composition.

## 21. Basic и Extended

### Basic

- deterministic hit без accuracy/evasion;
- Ground path + простой Flying direct/waypoint path;
- one target per attack;
- HP, optional one shield, one resistance/armor order;
- Physical/Energy; Explosive только с AoE;
- линейные/простые grade curves;
- kill/completion/passive income;
- one run currency и one meta currency;
- road-contact Tower с одним contact policy;
- optional one repair mode.

### Extended

- predictive intercept;
- AoE falloff, pierce/chain/beam;
- multiple status stacks и auras;
- production income, bounty и early-kill modifiers;
- branch upgrades, respec/sell;
- typed shields/barriers;
- multiple aerial waypoint/spline variants;
- objectives/challenges и capped vertical meta upgrades.

Extended math не должна добавляться в runtime до появления соответствующей механики.

## 22. Владельцы вычислений

| Вычисление | Владелец |
| --- | --- |
| Route/path progress | Ground/Flying movement owner |
| Target filtering/policy | Tower |
| Attack cadence | Tower/weapon contract |
| Delivery geometry | Weapon/Projectile |
| Shield/armor/HP resolution | Receiver + pure damage resolver |
| Status instances | Target effect owner |
| Aura membership | AuraEmitter; target owns applied modifier |
| Grade/derived stats | TowerStats/Tower owner |
| Contact outcome | Tower contact/root receiver chain |
| Auto-repair | Tower/TowerHealth owned repair state |
| Wave scaling/completion | Один wave-flow owner |
| Run balance | Один run-economy owner |
| Meta balance/unlocks | Один profile/meta owner |
| Save serialization | SaveService; formulas остаются domain owners |
| Display | UI read models; без authoritative recalculation |

## 23. Числовые инварианты

- HP/shield/balance не отрицательны.
- Attack interval, tick interval и projectile speed не равны нулю, если действие активно.
- Probability находится в `0..1`.
- Radius/range/count/cost/reward не отрицательны.
- Один AttackId не commit-ится дважды одному target, если multi-hit не разрешён.
- Один Enemy получает `Kill XOR Leak`.
- Один contact pair/correlation создаёт один ContactResult.
- Completion payout, reward choice и settlement idempotent.
- Derived stat имеет один фиксированный modifier order.
- Ground/Flying filter применяется до damage/contact.
- Road-contact Tower не меняет route/NavMesh.
- Preview и UI calculations не мутируют gameplay state.
- Floating-point presentation округляется только в UI; gameplay использует единый внутренний precision contract.

## 24. Минимальные проверочные примеры

### 24.1 Damage

Дано: `D = 20`, shield `S = 15`, `Fs = 1`, bypass `0`, flat armor `2`, percent armor `25%`, HP `50`.

```text
shield absorbs 15
nominal remainder = 5
after flat = 3
health damage = 3 × 0.75 = 2.25
HP after = 47.75
```

### 24.2 Exposure

Дано: участок в range `12`, speed `3`, attack interval `1`, first shot delay `0`.

```text
Texposure = 12 / 3 = 4 seconds
Nshots = 1 + floor(4 / 1) = 5
```

Граница включения последнего shot должна совпадать с runtime cooldown policy и тестом.

### 24.3 Wave economy

```text
Bopen = 100
build = 40
upgrade = 20
kill income = 35
completion = 15
passive = 10

Bclose = 100 - 40 - 20 + 35 + 15 + 10 = 100
```

Каждый source/sink должен появиться отдельной ledger причиной.

### 24.4 Contact repair

Дано: Tower HP `30/100`, repair delay `5 s`, rate `10 HP/s`.

После `8 s` без damage:

```text
repairActiveTime = 8 - 5 = 3
repaired = 3 × 10 = 30
Tower HP = min(100, 30 + 30) = 60
```

## 25. Шаблон будущей balance/mechanics задачи

```text
Mechanic:
Authoritative owner:
Definition fields and units:
Runtime state:
Formula and exact order:
Clamp/rounding/precision:
Ground/Flying applicability:
Damage/shield/armor/status/aura interaction:
Wave/run/meta economy impact:
Save/determinism impact:
UI old→new/readability:
VFX/SFX result cues:
Failure/no-fallback behavior:
EditMode numeric cases:
Bounded Play Mode scenario:
```

Если порядок вычислений не зафиксирован, mechanic не готова к реализации и балансировке.
