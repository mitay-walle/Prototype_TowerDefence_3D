# Map generation runtime notes

## Compact initial layout

`LevelGenerator` owns the initial map size and passes its configured tile count to `MapGenerator`.

The default initial layout uses four generated road tiles plus the base tile. This keeps the number of open road entrances manageable for the first wave and avoids deleting tiles after the topology has already been generated.

`MapGenerator` remains seed-driven. The same non-zero seed produces the same layout; an automatic seed is used when the configured seed is zero.

Tile selection is weighted for larger maps: straight and corner pieces are four times more likely to be considered than three-way or cross junctions. For the four-tile compact layout, branch pieces are excluded so the initial map exposes at most four open road entrances. The weighting changes only the deterministic candidate order; it does not bypass connection validation.

Each queued grid position is marked processed after its placement attempt, while occupied positions are skipped immediately. This keeps a failed candidate from being silently consumed before the generator has recorded the failed connection, and prevents duplicate queue entries from producing extra tiles.

## Validation contract

`TilePlacementValidator` remains the owner of connection matching and root connectivity. Compact-size coverage is tested by `TD.Tests.MapGeneratorTests.TestMapGeneration_BoundsOpenRoadEndsAcrossSeeds`, which verifies that the four-tile generation stays within five total tiles including the base, exposes no more than four open road entrances, and remains connected across the fixed seed set.

`TileMapValid` proves connection/topology validity. Runtime telemetry should also report the generated seed and open-road entrance count so gameplay coverage can be evaluated separately from geometric validity.

The base gameplay anchor is the center of grid tile `(0, 0)`, exposed by `TileMapManager.BasePosition`. `GameplayBootstrap` places `PlayerBase` at that anchor, and `MonsterMove` uses the existing `PlayerBase.transform.position` as the path destination.

## Runtime acceptance gate

EditMode contracts do not prove navigation. A valid gameplay check must run the `Gameplay` scene in Play Mode with the baked NavMesh and capture, for one spawned enemy, the `PlayerBase` position, `NavMeshAgent.destination`, `pathStatus`, `hasPath`, movement over time, and either a `Leak` terminal event or a completed wave. A source-level test that only checks a `PlayerBase` reference is not sufficient evidence for this gate.
