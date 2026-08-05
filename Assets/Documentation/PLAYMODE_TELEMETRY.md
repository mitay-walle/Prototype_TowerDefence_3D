# Play Mode telemetry

## Purpose

`GameplayTelemetry` is an observer of the existing gameplay owners. It does not own game state or replace gameplay callbacks. It records input actions from the authored `PlayerInput`, owner UnityEvents, and before/after state needed to diagnose accepted and rejected actions.

The observer is authored on `GameplayBootstrap` in `Assets/Scenes/Gameplay.unity` and references the existing `GameManager`, `WaveManager`, `ResourceManager`, `PlayerBase`, `TowerPlacementSystem`, and `TilePlacementSystem`.

## MCP surface

- `td_gameplay_telemetry` with `operation=status` returns the current snapshot, input transport, pointer/raycast evidence, and the event journal.
- `td_gameplay_telemetry` with `operation=clear` resets the journal and sequence cursor while preserving the current gameplay state.
- `td_gameplay_telemetry` with `operation=events` or `status` and `after_sequence=N` returns only events after the cursor.
- `td_virtual_mouse` and `td_virtual_gamepad` are the synthetic input transport. They require Play Mode and must use their synthetic source.

Each event contains the input or owner callback plus before/after game state, wave, currency, base health, enemy and tower counts, pause state, reward/challenge state, placement state, and tile selection state. A no-op is visible as an input event whose relevant before/after values do not change.

`TilePlacementChoiceSelected` also records `coverage=covered/before->covered/after`. The after value uses hypothetical spawn anchors produced by the existing `TileMapManager` without mutating the map, so the event can compare the current authored tower coverage with the selected tile consequence.

The snapshot also exposes `TileMapGenerationSeed` from the existing `LevelGenerator` owner chain. A non-zero value identifies the generated layout used by the current tile snapshot; `0` means the generator has not produced a level or the observer cannot resolve its parent owner.

## Bounded smoke protocol

1. Enter Play Mode and call telemetry `status`.
2. Call telemetry `clear`; retain the returned cursor.
3. Inspect the target UI control with the pointer evidence or a measured screen-space position.
4. Send one synthetic input action.
5. Poll telemetry after the cursor until the owner callback and resulting state transition appear.
6. Continue polling for delayed spawn, combat, economy, base, reward, or terminal effects.
7. Check Console errors after the run and report verified evidence separately from assumptions.

The first verified slice is `Preparation → WaveActive → WaveResolve`: clicking `Start Wave` records the synthetic mouse input, `WaveStarted`, and `GameStateChanged`; the same run can then observe all first-wave spawns, enemy deaths, base damage, rewards, `WaveCompleted`, and the resolve state without user participation.

## Диагностика первого активного врага

Snapshot дополнительно содержит `FirstActiveEnemy*`: имя и архетип, позицию, `onNavMesh`, `hasPath`, `pathPending`, `pathStatus`, `remainingDistance`, desired/actual velocity и расстояние до базы. Эти поля additive и предназначены для bounded runtime smoke; они не входят в ML observation/action contract.

Для path stall сравни два snapshot: `PathComplete` + конечное `remainingDistance`, совпадающие desired/actual velocity и уменьшающееся `FirstActiveEnemyDistanceToBase` означают рабочее движение. `remainingDistance=Infinity`, нулевая velocity или отсутствие уменьшения расстояния означают, что нужно проверять `MonsterMove` recovery/repath.
## Тесты MonsterMove

Для MCP Test Runner используй полные имена `TD.Tests.MonsterMoveContractTests.MovementUsesExplicitBaseTarget` и `TD.Tests.MonsterMoveContractTests.NonFinitePathDistanceCannotCountAsRouteProgress`. Фильтр только по имени класса может вернуть `total=0`, хотя тесты присутствуют в `mcpforunity://tests`.
## Combat и leak smoke

Обычный bounded smoke с authored башнями подтверждает `EnemySpawned`, `TowerTargetAcquired`, `TowerFired`, `EnemyKilled` и `MonsterDeath`. Это не является leak-тестом: башня может уничтожить врага раньше базы. Leak следует проверять отдельным non-combat harness, который отключает damage через существующий typed owner API и автоматически восстанавливается при выходе из Play; не отключай компоненты через generic MCP reflection calls.
## Leak smoke harness

Для отдельной проверки пути до базы запусти Play и меню `TD/ML-Agents/Play Mode/Enable Leak Smoke (Disable Tower Damage)`. Меню временно отключает только живые `Tower` components, не сохраняет сцену и автоматически восстанавливает исходное состояние при выходе из Play. После capture ожидай `EnemiesLeaked > 0` и уменьшение `BaseHealth`; затем останови Play. Для обычного gameplay smoke этот режим не включай.